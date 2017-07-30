using Cloudflare_Bypass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AppTest
{
    class Program
    {
        static void Main(string[] args)
        {
            CF_WebClient client = new CF_WebClient();
            //client.CookieContainer.Add(new Cookie("a", "1") { Domain = "sinfulforums.net" });

            string html = client.DownloadString("https://sinfulforums.net/thread-1159.html");
        }
    }
}
