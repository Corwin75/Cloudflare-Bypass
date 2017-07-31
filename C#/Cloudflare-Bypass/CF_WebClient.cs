using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Threading;

namespace Cloudflare_Bypass
{
    public class CF_WebClient : WebClient
    {
        private WebRequest _request = null;
        private WebResponse _response = null;

        public CookieContainer CookieContainer { get; set; }
        public CookieCollection ResponseCookies { get { return _response != null ? (_response as HttpWebResponse).Cookies : null; } }
        public bool AutoRedirect { get; set; }

        public string Cookies { get { return GetHeaderValue("Set-Cookie"); } }
        public string Location { get { return GetHeaderValue("Location"); } }

        public HttpStatusCode StatusCode
        {
            get
            {
                var result = HttpStatusCode.Gone;

                if (_response != null)
                {
                    var response = _response as HttpWebResponse;

                    if (response != null)
                        result = response.StatusCode;
                }

                return result;
            }
        }

        public Action<HttpWebRequest> Setup { get; set; }

        public CF_WebClient(params Cookie[] cookies)
        {
            CookieContainer = new CookieContainer();
            foreach (var cookie in cookies)
                CookieContainer.Add(cookie);

            AutoRedirect = true;
        }

        public CF_WebClient(CookieContainer cookies = null, bool autoRedirect = true)
        {
            CookieContainer = cookies ?? new CookieContainer();
            AutoRedirect = autoRedirect;
        }

        public string GetHeaderValue(string headerName)
        {
            if (_response != null)
            {
                return _response?.Headers?[headerName];
            }

            return null;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            _request = base.GetWebRequest(address);

            var httpRequest = _request as HttpWebRequest;

            if (_request != null)
            {
                httpRequest.AllowAutoRedirect = AutoRedirect;
                httpRequest.CookieContainer = CookieContainer; // Force la création du CookieContainer pour obtenir les cookies dans la réponse
                httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                Setup?.Invoke(httpRequest);
            }

            return _request;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            //Force l'UserAgent dans la requête si il n'est pas défini pour que Cloudflare accepte la requête
            if (request.Headers[HttpRequestHeader.UserAgent] == null)
                (request as HttpWebRequest).UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:54.0) Gecko/20100101 Firefox/54.0";

            try
            {
                _response = base.GetWebResponse(request, result);
            }
            catch (WebException ex)
            {
                _response = ex.Response;
                HttpWebResponse error = (HttpWebResponse)_response;

                //Si l'exeption est du à l'erreur 503 de cloudflare
                if (CF_Interpreter.IsCloudflare(error))
                    _response = BypassCloudflare((HttpWebRequest)request, error);
                else
                    throw ex;
            }

            return _response;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            //Force l'UserAgent dans la requête si il n'est pas défini pour que Cloudflare accepte la requête
            if (request.Headers[HttpRequestHeader.UserAgent] == null)
                (request as HttpWebRequest).UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:54.0) Gecko/20100101 Firefox/54.0";

            try
            {
                _response = base.GetWebResponse(request);
            }
            catch (WebException ex)
            {
                _response = ex.Response;
                HttpWebResponse error = (HttpWebResponse)_response;

                //Si l'exeption est du à l'erreur 503 de cloudflare
                if (CF_Interpreter.IsCloudflare(error))
                    _response = BypassCloudflare((HttpWebRequest)request, error);
                else
                    throw ex;
            }

            return _response;
        }

        private WebResponse BypassCloudflare(HttpWebRequest request, HttpWebResponse response)
        {
            //Récupère le contenu de la page Clouflare
            string html = "";
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                html = reader.ReadToEnd();
            }

            //Interprète le code native de cloudflare et nous retourne l'url de clearance
            string newurl = CF_Interpreter.Interpret(response.ResponseUri.AbsoluteUri, html);

            //Attendre 3 secondes minimum pour que cloudflare accepte l'url de clearrance
            Thread.Sleep(3000);

            //Récupère le cookies de clearance et redirige sur la page souhaité initialement avec l'autoredirection
            HttpWebRequest newWebRequest = (HttpWebRequest)WebRequest.Create(newurl);
            newWebRequest.AllowAutoRedirect = true;
            newWebRequest.CookieContainer = CookieContainer;
            newWebRequest.Referer = response.ResponseUri.AbsoluteUri;
            newWebRequest.UserAgent = request.UserAgent;

            response = (HttpWebResponse)newWebRequest.GetResponse();

            return response;
        }
    }
}
