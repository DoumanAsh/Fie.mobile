using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Logging;
using System.Net.Http.Headers;
using System.Net;

namespace API {
    [JsonObject(MemberSerialization.Fields)]
    class UploadResponse {
        public string id = "";
    }

    [JsonObject(MemberSerialization.Fields)]
    class GabPost {
        public string body;
        public string reply_to = "";
        public int is_quote = 0;
        public int is_html = 0;
        public int nsfw;
        public int is_premium = 0;
        public string _method = "post";
        public string gif = "";
        public string category = null;
        public string topic = null;
        public bool share_twitter = false;
        public bool share_facebook = false;
        public List<string> media_attachments = new List<string>();

        public GabPost(string text, Options options) {
            body = text;
            nsfw = options.nsfw == true ? 1 : 0;
        }
    }

    [JsonObject(MemberSerialization.Fields)]
    class GabAuth {
        public string username;
        public string password;
        public string remember = "on";
        public string _token;

        public GabAuth(string username, string password, string token) {
            this.username = username;
            this.password = password;
            _token = token;
        }
    }

    public class Gab {
        const string LOGIN_URL = "https://gab.ai/auth/login";
        const string IMAGES_URL = "https://gab.ai/api/media-attachments/images";
        const string POST_URL = "https://gab.ai/posts";

        private static string TOKEN = null;

        public static bool is_auth() {
            return TOKEN != null;
        }

        public static string token() {
            return TOKEN;
        }

        public static void reset_token() {
            TOKEN = null;
            //Invalidate previous cookies
            foreach (var cookie in Http.cookies().GetCookies(new Uri(LOGIN_URL))) {
                ((Cookie)cookie).Expired = true;
            }
        }
        
        public static void set_token(string new_token) {
            Debug.log("Fie: Set new Gab token={0}", new_token);
            TOKEN = new_token;
        }

        public static async Task login(string username, string password) {
            if (is_auth()) {
                Debug.log("Fie: Already logged in Gab.ai");
                return;
            }

            string extract_token(string login_page) {
                //Look for _token
                var token_start = login_page.IndexOf("_token");
                if (token_start == -1) return null;
                token_start += 7;

                //Look for value="{}"
                var value_start = login_page.IndexOf('"', token_start);
                if (value_start == -1) return null;
                var value_end = login_page.IndexOf('"', value_start + 1);
                if (value_end == -1) return null;

                return login_page.Substring(value_start + 1, value_end - value_start - 1);
            }

            var client = Http.client();

            var uri = new Uri(LOGIN_URL);
            var rsp = await client.GetStringAsync(uri);
            var new_token = extract_token(rsp);
            if (new_token == null) {
                Debug.log("Fie: Gab login page is missing _token");
                return;
            }

            var auth = JsonConvert.SerializeObject(new GabAuth(username, password, new_token));
            var auth_page = await client.PostAsync(uri, new StringContent(auth, Encoding.UTF8, "application/json"));
            if (!auth_page.IsSuccessStatusCode) {
                Debug.log("Fie: Failed to login into Gab.ai");
                return;
            };

            var auth_page_html = await auth_page.Content.ReadAsStringAsync();
            if (auth_page_html == null) {
                Debug.log("Fie: Unable to get Gab home page after login");
                return;
            };

            var jwt_token = extract_token(auth_page_html);
            //Note that on failure we redirected to login page
            if (jwt_token == null || jwt_token == new_token) {
                Debug.log("Fie: Gab authorization failed");
                return;
            }
            set_token(jwt_token);

            return;
        }

        public static async Task<string> upload_image(Image image) {
            var client = Http.client();
            var body = new MultipartFormDataContent();

            var image_field = new ByteArrayContent(image.data, 0, image.data.Length);
            image_field.Headers.ContentType = MediaTypeHeaderValue.Parse(image.mime);
            body.Add(image_field, "file", image.file_name);

            var uri = new Uri($"{IMAGES_URL}?token={token()}");
            var rsp = await client.PostAsync(uri, body);
            if (!rsp.IsSuccessStatusCode) {
                Debug.log("Fie: Failed to upload image onto Gab.ai. Status: {0}", rsp.StatusCode);
                return null;
            };

            var rsp_content = await rsp.Content.ReadAsStringAsync();
            UploadResponse result = null;
            try {
                result = JsonConvert.DeserializeObject<UploadResponse>(rsp_content);
            } catch (Exception error) {
                Debug.log($"Fie: Error deserializing Gab.ai image upload response: {error}");
            }

            if (result == null) {
                Debug.log("Fie: Gab.ai returned invalid response to upload='{0}'", rsp_content);
                return null;
            }

            return result.id;
        }

        public static async Task<bool> post(string text, Options opts) {
            if (!is_auth()) {
                Debug.log("Fie: Gab Not logged in");
                return false;
            }

            var client = Http.client();

            var gab_post = new GabPost(text, opts);

            if (opts.images != null) {
                foreach (var image in opts.images) {
                    var upload_result = await upload_image(image);

                    if (upload_result == null) {
                        return false;
                    }

                    gab_post.media_attachments.Add(upload_result);
                }
            }

            var post_data = JsonConvert.SerializeObject(gab_post);

            var request = new HttpRequestMessage(HttpMethod.Post, POST_URL) {
                Headers = {
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {token()}" }
                },
                Content = new StringContent(post_data, Encoding.UTF8, "application/json"),
            };

            var rsp = await client.SendAsync(request);
            if (!rsp.IsSuccessStatusCode) {
                Debug.log("Fie: Failed to post onto Gab.ai");
                return false;
            };

            return true;
        }
    }
}
