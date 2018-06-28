using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GmailBackupper
{
    class Program
    {
        private const string _target = "backup";
        private static readonly Regex _regex = new Regex(@"[-_]");

        static void Main(string[] args)
        {
            Run(args[0], args[1], args[2]).Wait();
        }

        private static async Task Run(string cid, string cs, string rt)
        {
            var jp = TimeSpan.FromHours(9);
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
                    var time = DateTimeOffset.FromUnixTimeMilliseconds(message.internalDate).ToOffset(jp);
                    Console.Write(time.ToString(" yyyy-MM-dd HH:mm:ss"));

                    var path = Path.Combine(_target, time.ToString("yyyy"), time.ToString("yyyy-MM"), time.ToString("yyyy-MM-dd"));
                    Directory.CreateDirectory(path);

                    var file = Path.Combine(path, message.id + ".eml");
                    if (!File.Exists(file))
                    {
                        message = await gmail.GetMessage(mid.Id, Gmail.MessageFormat.Raw);
                        var str = _regex.Replace(message.raw, m => m.Value == "-" ? "+" : "/");
                        var raw = Convert.FromBase64String(str);
                        await File.WriteAllBytesAsync(file, raw);
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
