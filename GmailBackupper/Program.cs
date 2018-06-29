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
                    await Run(gs.ClientId, gs.ClientSecret, acs.RefreshToken, acs.BackupPath);
                }
                catch (Exception exp) { Console.WriteLine(exp.ToString()); }
            }
        }

        private static async Task Run(string cid, string cs, string rt, string bupath)
        {
            var _jp = TimeSpan.FromHours(9);
            var gmail = new Gmail(cid, cs, rt);
            var messages = gmail.GetMessageEnamerator();

            var limit = DateTimeOffset.Now.AddDays(0);

            while (true)
            {
                var mid = await messages.GetNextMessage();
                var message = await gmail.GetMessage(mid.Id, Gmail.MessageFormat.Minimal);

                Console.Write(message.id);

                if (message.labelIds.All(t => t != "CHAT"))
                {
                    var time = DateTimeOffset.FromUnixTimeMilliseconds(message.internalDate).ToOffset(_jp);
                    Console.Write(time.ToString(" yyyy-MM-dd HH:mm:ss"));

                    var path = Path.Combine(bupath, time.ToString("yyyy"), time.ToString("yyyy-MM"), time.ToString("yyyy-MM-dd"));
                    Directory.CreateDirectory(path);

                    var file = Path.Combine(path, message.id + ".json");
                    if (!File.Exists(file))
                    {
                        var rf = Path.ChangeExtension(file, ".eml");
                        message = await gmail.GetMessage(mid.Id, Gmail.MessageFormat.Raw);
                        var str = _regex.Replace(message.raw, m => m.Value == "-" ? "+" : "/");
                        var raw = Convert.FromBase64String(str);
                        await File.WriteAllBytesAsync(rf, raw);
                        File.SetCreationTime(rf, time.DateTime);
                        File.SetLastWriteTime(rf, time.DateTime);

                        using (var stream = File.Create(file))
                        {
                            await gmail.GetMessageStr(mid.Id, async src =>
                            {
                                await src.CopyToAsync(stream);
                            });
                        }
                        File.SetCreationTime(file, time.DateTime);
                        File.SetLastWriteTime(file, time.DateTime);

                        Console.Write(" Store");
                    }

                    if (message.labelIds.Any(t => t == "INBOX"))
                    {
                        if (message.labelIds.All(t => t != "STARRED"))
                        {
                            if (time < limit)
                            {
                                await gmail.MoveToTrash(mid.Id);
                                Console.Write(" Trash");
                            }
                        }
                    }

                    //await gmail.MoveToTrash(m.Id);
                }

                Console.WriteLine();
            }
        }
    }
}
