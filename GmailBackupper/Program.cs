using System;
using System.Threading.Tasks;

namespace GmailBackupper
{
    class Program
    {
        static void Main(string[] args)
        {
            Run(args[0],args[1],args[2]).Wait();  
        }

        private static async Task Run(string cid, string cs, string rt)
        {
            var gmail = new Gmail(cid, cs, rt);
            await gmail.RefreshAccessToken();
        }
    }
}
