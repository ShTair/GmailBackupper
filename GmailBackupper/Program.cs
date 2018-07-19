using GmailBackupper.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GmailBackupper
{
    class Program
    {
        private static readonly Regex _base64UrlRegex = new Regex(@"[-_]");
        private static readonly Regex _mailHeaderFromRegex = new Regex(@"""?(.*?)""?\s*<(.+)>");
        private static readonly Regex _fileNameRegex = new Regex(@"[\\/:*?""<>|\r\n\t]");
        private static readonly TimeSpan _japanTimeSpan = TimeSpan.FromHours(9);

        static void Main(string[] args)
        {
            Run(args[0]).Wait();
        }

        private static async Task Run(string settingsPath)
        {
            var gSettingsJson = await File.ReadAllTextAsync(settingsPath);
            var gSettings = JsonConvert.DeserializeObject<GlobalSettings>(gSettingsJson);

            foreach (var aSettingsPath in Directory.EnumerateFiles(gSettings.SettingsPath, "*.json"))
            {
                try
                {
                    var aSettingsJson = await File.ReadAllTextAsync(aSettingsPath);
                    var aSettings = JsonConvert.DeserializeObject<AccountSettings>(aSettingsJson);
                    await Run(gSettings.ClientId, gSettings.ClientSecret, aSettings.RefreshToken, aSettings.BackupPath, aSettings.Limit, aSettings.DeleteRules);
                }
                catch (Exception exp) { Console.WriteLine(exp.ToString()); }
            }
        }

        private static async Task Run(string clientId, string clientSecret, string refreshToken, string dstPath, int limitDays, DeleteRule[] deleteRules)
        {
            var gmail = new Gmail(clientId, clientSecret, refreshToken);
            var me = gmail.GetMessageEnamerator();
            var limit = DateTimeOffset.Now.AddDays(-limitDays);

            var aliveThreads = new HashSet<string>();
            foreach (var deleteRule in deleteRules)
            {
                deleteRule.LimitDate = DateTimeOffset.Now.AddDays(-deleteRule.Limit);
            }

            while (await me.Next())
            {
                string id = null;
                try
                {
                    id = me.CurrentMessageId;
                    var threadId = me.CurrentMessageThreadId;

                    Console.Write(id);

                    var jsonPath = Path.Combine(dstPath, "json", id.Substring(14), id.Substring(12));
                    var jsonName = Path.Combine(jsonPath, id + ".json");

                    if (!File.Exists(jsonName))
                    {
                        Directory.CreateDirectory(jsonPath);
                        await me.StoreFullMessage(jsonName);
                    }

                    var json = await File.ReadAllTextAsync(jsonName);
                    var message = JsonConvert.DeserializeObject<MinMessage>(json);
                    if (message == null || message.InternalDate == 0)
                    {
                        File.Delete(jsonName);
                        Console.WriteLine();
                        continue;
                    }

                    var time = DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate).ToOffset(_japanTimeSpan);
                    Console.Write($" {time:yyyy-MM-dd HH:mm:ss}");

                    var name = GenerateName(message);
                    Console.Write(" " + name);
                    name = $"{time:yyyyMMddHHmmss}_{message.Id}" + name;

                    var pathE = Path.Combine(dstPath, time.ToString("yyyy"), time.ToString("yyyy-MM"), time.ToString("yyyy-MM-dd"));
                    var fileE = Path.Combine(pathE, name + ".eml");

                    if (!File.Exists(fileE))
                    {
                        Directory.CreateDirectory(pathE);
                        var raw = await me.GetRawMessage();
                        await File.WriteAllBytesAsync(fileE, raw);

                        File.SetCreationTime(fileE, time.DateTime);
                        File.SetLastWriteTime(fileE, time.DateTime);
                        File.SetCreationTime(jsonName, time.DateTime);
                        File.SetLastWriteTime(jsonName, time.DateTime);

                        Console.Write(" Store");
                    }

                    if (!aliveThreads.Contains(threadId))
                    {
                        if (time < limit)
                        {
                            message = await me.GetMinimalMessage();

                            var deleteRule = deleteRules.FirstOrDefault(t => message.LabelIds.Contains(t.Id));
                            if (deleteRule != null && (deleteRule.Limit < 0 || time > deleteRule.LimitDate))
                            {
                                aliveThreads.Add(threadId);
                            }
                            else
                            {
                                await me.MoveToTrash();
                                Console.Write(" Trash");
                            }
                        }
                        else
                        {
                            aliveThreads.Add(threadId);
                        }
                    }

                    Console.WriteLine();
                }
                catch (Exception exp)
                {
                    Console.WriteLine();
                    Console.WriteLine(exp);

                    var fn = DateTimeOffset.Now.ToOffset(_japanTimeSpan).ToString("yyyyMMddHHmmss") + ".txt";
                    fn = Path.Combine(dstPath, fn);

                    var str = exp.ToString();
                    if (id != null) str = id + "\r\n" + str;
                    await File.WriteAllTextAsync(fn, str);
                }
            }
        }

        private static string GenerateName(MinMessage message)
        {
            var sb = new StringBuilder();

            var from = message.Payload.Headers.FirstOrDefault(t => t.Name.Equals("From", StringComparison.CurrentCultureIgnoreCase));
            if (from != null)
            {
                var m = _mailHeaderFromRegex.Match(from.Value);
                if (!m.Success)
                {
                    sb.Append("_" + from.Value.Trim(20));
                }
                else
                {
                    sb.Append("_" + (string.IsNullOrWhiteSpace(m.Groups[1].Value) ? m.Groups[2].Value : m.Groups[1].Value).Trim(20));
                }
            }

            var subj = message.Payload.Headers.FirstOrDefault(t => t.Name.Equals("Subject", StringComparison.CurrentCultureIgnoreCase));
            if (subj != null)
            {
                sb.Append("_" + subj.Value.Trim(20));
            }

            return _fileNameRegex.Replace(sb.ToString(), "");
        }
    }
}
