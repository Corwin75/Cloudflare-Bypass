using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace Cloudflare_Bypass
{
    public class CF_Interpreter
    {
        public static bool IsCloudflare(string html)
        {
            return Regex.IsMatch(html, "cdn-cgi\\/l\\/chk_jschl");
        }

        public static bool IsCloudflare(HttpWebResponse response)
        {
            return response.StatusCode == HttpStatusCode.ServiceUnavailable && response.Server == "cloudflare-nginx";
        }

        public static string Interpret(string url, string html)
        {
            string r = Regex.Matches(url, "https?:\\/\\/")[0].Value;
            url = url.Substring(r.Length);
            url = url.Split('/')[0];

            string variableName = GetVariableName(html);
            int res = ParseAllNativeCode(html, variableName) + url.Length;

            MatchCollection mc = Regex.Matches(html, "<input type=\"hidden\" name=\"jschl_vc\" value=\"(.*?)\"/>|<input type=\"hidden\" name=\"pass\" value=\"(.*?)\"/>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            string jschl_vc = mc[0].Groups[1].Value;
            string pass = mc[1].Groups[2].Value;

            return r + url + "/cdn-cgi/l/chk_jschl?jschl_vc=" + HttpUtility.UrlEncode(jschl_vc) + "&pass=" + HttpUtility.UrlEncode(pass) + "&jschl_answer=" + res;
        }

        private static int ParseAllNativeCode(string data, string variableName)
        {
            int res = 0;
            string[] vn = variableName.Split('.');
            MatchCollection mc = Regex.Matches(data, vn[0] + "={\"" + vn[1] + "\":(.*?)};|" + variableName + "(.*?);", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            for (int i = 0; i < mc.Count; i++)
            {
                if (i == 0)
                {
                    res = ParseNative(mc[i].Groups[1].Value);
                }
                else
                {
                    string[] nativecode = mc[i].Groups[2].Value.Split('=');
                    switch (nativecode[0])
                    {
                        case "+":
                            res += ParseNative(nativecode[1]);
                            break;
                        case "-":
                            res -= ParseNative(nativecode[1]);
                            break;
                        case "*":
                            res *= ParseNative(nativecode[1]);
                            break;
                        case "/":
                            res /= ParseNative(nativecode[1]);
                            break;
                    }
                }
            }
            return res;
        }

        private static string GetVariableName(string data)
        {
            string[] ini = data.Split(new string[] { "var s,t,o,p,b,r,e,a,k,i,n,g,f, ", "={\"", "\":" }, StringSplitOptions.None);
            return ini[1] + "." + ini[2];
        }

        private static int ParseNative(string data)
        {
            data = data.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "");
            string ressubdata = "";
            List<string> subdata = new List<string>();
            string res = "";

            if (data.StartsWith("+("))
            {
                data = data.Substring(2);
                data = data.Substring(0, data.Length - 1);
            }

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == '(')
                    ressubdata = "";
                else if (data[i] == ')' || i == data.Length - 1)
                    subdata.Add(ressubdata);
                else
                    ressubdata += data[i];
            }

            foreach (string str in subdata)
            {
                res += Regex.Matches(str, "!+[]|+!![]").Count;
            }

            return int.Parse(res);
        }
    }
}
