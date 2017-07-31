using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace Cloudflare_Bypass
{
    public class CF_Interpreter
    {
        /// <summary>
        /// Detecte si la page html contient les mots clé cloudflare.
        /// </summary>
        /// <param name="html">Code source de la page</param>
        /// <returns>Retourne true si la page appartient à cloudflare</returns>
        public static bool IsCloudflare(string html)
        {
            //Parti de l'url dans le formulaire
            return Regex.IsMatch(html, "cdn-cgi\\/l\\/chk_jschl");
        }

        /// <summary>
        /// Detecte si la réponse d'une requête contient les informations relative à cloudflare.
        /// </summary>
        /// <param name="response">Réponse d'une requête sur un site</param>
        /// <returns>Retourne true si la réponse appartient à cloudflare</returns>
        public static bool IsCloudflare(HttpWebResponse response)
        {
            //Erreur 503 Service Unavailable provenant du serveur cloudflare-nginx
            return response.StatusCode == HttpStatusCode.ServiceUnavailable && response.Server == "cloudflare-nginx";
        }

        /// <summary>
        /// Interprète le javascript dans une page cloudflare.
        /// </summary>
        /// <param name="url">Url du site</param>
        /// <param name="html">Code source de la page</param>
        /// <returns>Retourne l'url permettant d'obtenir le cookie de clearance</returns>
        public static string Interpret(string url, string html)
        {
            //Récupère le nom de domaine
            string r = Regex.Matches(url, "https?:\\/\\/")[0].Value;
            url = url.Substring(r.Length);
            url = url.Split('/')[0];

            //Récupère le nom de la variable Javascript utilisé pour les calcules
            string variableName = GetVariableName(html);

            //Récupère le résultat du code native et lui ajoute la taille du nom de domaine
            int res = ParseAllNativeCode(html, variableName) + url.Length;

            //Récupère les tokens contenu dans le formulaire
            MatchCollection mc = Regex.Matches(html, "<input type=\"hidden\" name=\"jschl_vc\" value=\"(.*?)\"/>|<input type=\"hidden\" name=\"pass\" value=\"(.*?)\"/>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            string jschl_vc = mc[0].Groups[1].Value;
            string pass = mc[1].Groups[2].Value;

            //Génère l'url contenant le cookie de clearance
            return r + url + "/cdn-cgi/l/chk_jschl?jschl_vc=" + HttpUtility.UrlEncode(jschl_vc) + "&pass=" + HttpUtility.UrlEncode(pass) + "&jschl_answer=" + res;
        }

        /// <summary>
        /// Parse tout le code native contenu dans la page cloudflare
        /// </summary>
        /// <param name="data">Code source de la page</param>
        /// <param name="variableName">Nom de la variable Javascript utiliser pour les calcules</param>
        /// <returns>Retourne le résultat du calcule</returns>
        private static int ParseAllNativeCode(string data, string variableName)
        {
            int res = 0;
            string[] vn = variableName.Split('.');

            //Récupère tout les blocs de code native avec le nom de la variable de calcule
            MatchCollection mc = Regex.Matches(data, vn[0] + "={\"" + vn[1] + "\":(.*?)};|" + variableName + "(.*?);", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            for (int i = 0; i < mc.Count; i++)
            {
                if (i == 0)
                {
                    //Pour la première occurence on ajoute juste le résulat au total
                    res = ParseNative(mc[i].Groups[1].Value);
                }
                else
                {
                    //Sépare l'opérateur et le code native
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

        /// <summary>
        /// Récupère le nom de la variable utilisé pour les calcules.
        /// </summary>
        /// <param name="data">Code source de la page cloudflare</param>
        /// <returns>Retourne le nom de la variable Javascript</returns>
        private static string GetVariableName(string data)
        {
            //La variable de calcule est toujours après l'initialisation des variable s,t,o,p,b,r,e,a,k,i,n,g,f
            string[] ini = data.Split(new string[] { "var s,t,o,p,b,r,e,a,k,i,n,g,f, ", "={\"", "\":" }, StringSplitOptions.None);
            return ini[1] + "." + ini[2];
        }

        /// <summary>
        /// Parse un bloc de code native
        /// </summary>
        /// <param name="data">Bloc de code native</param>
        /// <returns>Retourne l'interprétation du bloc</returns>
        private static int ParseNative(string data)
        {
            //Supprime tout les caractères inutile
            data = data.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "");

            string ressubdata = "";
            List<string> subdata = new List<string>();
            string res = "";

            if (data.StartsWith("+("))
            {
                //Suprimme le signe et les parenthèses autour du bloc native
                data = data.Substring(2);
                data = data.Substring(0, data.Length - 1);
            }

            //Extrait chaque sous bloc native
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == '(')
                    ressubdata = "";
                else if (data[i] == ')' || i == data.Length - 1)
                    subdata.Add(ressubdata);
                else
                    ressubdata += data[i];
            }

            //Additionne chaque sous bloc native
            foreach (string str in subdata)
            {
                res += Regex.Matches(str, "!+[]|+!![]").Count;
            }

            return int.Parse(res);
        }
    }
}
