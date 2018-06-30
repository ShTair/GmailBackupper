using GmailBackupper.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GmailBackupper
{
    class Program
    {
        private static readonly Regex _regex = new Regex(@"[-_]");
        private static readonly Regex _fromRegex = new Regex(@"""?(.*?)""?\s*<(.+)>");
        private static readonly Regex _fileRegex = new Regex(@"[\\/:*?""<>|\r\n\t]");
        private static readonly TimeSpan _jp = TimeSpan.FromHours(9);

        static void Main(string[] args)
        {
            Run(args[0]).Wait();
        }

        private static async Task Run(string settingsPath)
        {
            var settingsJson = await File.ReadAllTextAsync(settingsPath);
            var gs = JsonConvert.DeserializeObject<GlobalSettings>(settingsJson);

            foreach (var acsp in Directory.EnumerateFiles(gs.SettingsPath, "*.json"))
            {
                try
                {
                    var acsj = await File.ReadAllTextAsync(acsp);
                    var acs = JsonConvert.DeserializeObject<AccountSettings>(acsj);
                    await Run(gs.ClientId, gs.ClientSecret, acs.RefreshToken, acs.BackupPath, acs.Limit);
                }
                catch (Exception exp) { Console.WriteLine(exp.ToString()); }
            }
        }

        private static async Task Run(string cid, string cs, string rt, string bupath, int limitday)
        {
            var _jp = TimeSpan.FromHours(9);
            var gmail = new Gmail(cid, cs, rt);
            var messages = gmail.GetMessageEnamerator();
            var tempFile = Path.Combine(bupath, "temp.json");

            var limit = DateTimeOffset.Now.AddDays(-limitday);

            while (true)
            {
                var mid = await messages.GetNextMessage();
                if (mid == null) break;

                Console.Write(mid.Id);

                var pathJ = Path.Combine(bupath, "json", mid.Id.Substring(14), mid.Id.Substring(12));
                var fileJ = Path.Combine(pathJ, mid.Id + ".json");

                if (!File.Exists(fileJ))
                {
                    Directory.CreateDirectory(pathJ);
                    using (var stream = File.Create(fileJ))
                    {
                        await gmail.GetMessageStr(mid.Id, async src =>
                        {
                            await src.CopyToAsync(stream);
                        });
                    }
                }

                var json = await File.ReadAllTextAsync(fileJ);
                var message = JsonConvert.DeserializeObject<MessageResultModel>(json);
                if (message == null || message.internalDate == 0)
                {
                    File.Delete(fileJ);
                    Console.WriteLine();
                    continue;
                }

                var time = DateTimeOffset.FromUnixTimeMilliseconds(message.internalDate).ToOffset(_jp);
                Console.Write($" {time:yyyy-MM-dd HH:mm:ss}");

                var fn = $"{time:yyyyMMddHHmmss}_{message.id}";
                var from = message.payload.headers.FirstOrDefault(t => t.name.Equals("From", StringComparison.CurrentCultureIgnoreCase));
                if (from != null)
                {
                    var m = _fromRegex.Match(from.value);
                    if (!m.Success)
                    {
                        fn += "_" + from.value.Trim(20);
                    }
                    else
                    {
                        fn += "_" + (string.IsNullOrWhiteSpace(m.Groups[1].Value) ? m.Groups[2].Value : m.Groups[1].Value).Trim(20);
                    }
                }

                var subj = message.payload.headers.FirstOrDefault(t => t.name.Equals("Subject", StringComparison.CurrentCultureIgnoreCase));
                if (subj != null)
                {
                    fn += "_" + subj.value.Trim(20);
                }

                fn = _fileRegex.Replace(fn, "");
                var pathE = Path.Combine(bupath, time.ToString("yyyy"), time.ToString("yyyy-MM"), time.ToString("yyyy-MM-dd"));
                var fileE = Path.Combine(pathE, fn + ".eml");

                if (!File.Exists(fileE))
                {
                    Directory.CreateDirectory(pathE);
                    var mm = await gmail.GetMessage(mid.Id, Gmail.MessageFormat.Raw);
                    var str = _regex.Replace(mm.raw, m => m.Value == "-" ? "+" : "/");
                    var raw = Convert.FromBase64String(str);
                    await File.WriteAllBytesAsync(fileE, raw);

                    File.SetCreationTime(fileE, time.DateTime);
                    File.SetLastWriteTime(fileE, time.DateTime);
                    File.SetCreationTime(fileJ, time.DateTime);
                    File.SetLastWriteTime(fileJ, time.DateTime);

                    Console.Write(" Store");
                }

                if (time < limit)
                {
                    message = await gmail.GetMessage(mid.Id, Gmail.MessageFormat.Minimal);
                    if (message.labelIds.Any(t => t == "INBOX"))
                    {
                        if (message.labelIds.All(t => t != "STARRED"))
                        {
                            await gmail.MoveToTrash(mid.Id);
                            Console.Write(" Trash");
                        }
                    }
                }

                Console.WriteLine();
            }
        }
    }
}
