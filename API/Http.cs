using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Http;

namespace API {
    class Http {
        static private HttpClientHandler HANDLER;
        static private HttpClient CLIENT;

        static Http() {
            HANDLER = new HttpClientHandler {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };
            CLIENT = new HttpClient(HANDLER) {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        static public HttpClient client() {
            return CLIENT;
        }

        static public CookieContainer cookies() {
            return HANDLER.CookieContainer;
        }
    }
}
