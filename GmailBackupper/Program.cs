using System;
using System.Threading.Tasks;

namespace GmailBackupper
{
    class Program
    {
        static void Main(string[] args)
        {
            Run(args[0], args[1], args[2]).Wait();
        }

        private static async Task Run(string cid, string cs, string rt)
        {
            var gmail = new Gmail(cid, cs, rt);
            var messages = gmail.GetMessageEnamerator();
            while (true)
            {
                var m = await messages.GetNextMessage();
                await gmail.GetMessage(m.Id, Gmail.MessageFormat.Raw);
                await gmail.MoveToTrash(m.Id);
            }
        }
    }
}
