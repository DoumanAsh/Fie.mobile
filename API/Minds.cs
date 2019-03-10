using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

using Logging;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net;

namespace API {

    [JsonObject(MemberSerialization.Fields)]
    class MindsUploadResponse {
        public string guid = "";
    }

    [JsonObject(MemberSerialization.Fields)]
    class MindsPost {
        public string wire_threshold = null;
        public string message;
        public int is_rich = 0;
        public string title = null;
        public string description = null;
        public string thumbnail = null;
        public string url = null;
        public string attachment_guid = null;
        public int mature;
        public int access_id = 2;

        public MindsPost(string text, Options options) {
            message = text;
            mature = options.nsfw == true ? 1 : 0;
        }
    }

    [JsonObject(MemberSerialization.Fields)]
    class MindsAuth {
        public string grant_type = "password";
        public string client_id = "mobile";
        public string username;
        public string password;

        public MindsAuth(string username, string password) {
            this.username = username;
            this.password = password;
        }
    }

    [JsonObject(MemberSerialization.Fields)]
    class AuthResponse {
        public string access_token = null;
        public string user_id = null;
        public string refresh_token = null;
    }
    
    public class Minds {
        const string LOGIN_URL = "https://www.minds.com/api/v2/oauth/token";
        const string IMAGES_URL = "https://www.minds.com/api/v1/media";
        const string POST_URL = "https://www.minds.com/api/v1/newsfeed";

        private static string TOKEN = null;

        public static bool is_auth() {
            return TOKEN != null;
        }

        public static string token() {
            return TOKEN;
        }

        public static void reset_token() {
            TOKEN = null;
        }
        
        public static void set_token(string new_token) {
            Debug.log("Fie: Set new Minds token={0}", new_token);
            TOKEN = new_token;
        }
        
        public static async Task login(string username, string password) {
            if (is_auth()) {
                Debug.log("Fie: Already logged in Minds");
                return;
            }

            var client = Http.client();
            var uri = new Uri(LOGIN_URL);
            var auth = JsonConvert.SerializeObject(new MindsAuth(username, password));
            var auth_rsp = await client.PostAsync(uri, new StringContent(auth, Encoding.UTF8, "application/json"));
            if (!auth_rsp.IsSuccessStatusCode) {
                Debug.log("Fie: Failed to login into Minds: {0}", auth_rsp.StatusCode);
                return;
            };

            var auth_rsp_content = await auth_rsp.Content.ReadAsStringAsync();
            if (auth_rsp_content == null) {
                Debug.log("Fie: Unexpected error, Minds sent empty response to auth");
                return;
            };
            
            try {
                var auth_json = JsonConvert.DeserializeObject<AuthResponse>(auth_rsp_content);
                set_token(auth_json.access_token);
            } catch (Exception error) {
                Debug.log("Fie: Error parsing Minds auth response: {0}", error);
            }
        }

        public static async Task<string> upload_image(Image image) {
            var client = Http.client();
            var body = new MultipartFormDataContent();

            var image_field = new ByteArrayContent(image.data, 0, image.data.Length);
            image_field.Headers.ContentType = MediaTypeHeaderValue.Parse(image.mime);
            body.Add(image_field, "file", image.file_name);

            var request = new HttpRequestMessage(HttpMethod.Post, IMAGES_URL) {
                Headers = {
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {token()}" }
                },
                Content = body,
            };
            var rsp = await client.SendAsync(request);
            if (!rsp.IsSuccessStatusCode) {
                Debug.log("Fie: Failed to upload image onto Minds. Status: {0}", rsp.StatusCode);
                return null;
            };

            var rsp_content = await rsp.Content.ReadAsStringAsync();
            MindsUploadResponse result = null;
            try {
                result = JsonConvert.DeserializeObject<MindsUploadResponse>(rsp_content);
            } catch (Exception error) {
                Debug.log($"Fie: Error deserializing Minds image upload response: {error}");
            }

            if (result == null) {
                Debug.log("Fie: Minds returned invalid response to upload='{0}'", rsp_content);
                return null;
            }

            return result.guid;
        }

        public static async Task<bool> post(string text, Options opts) {
            if (!is_auth()) {
                Debug.log("Fie: Minds Not logged in");
                return false;
            }

            var client = Http.client();

            var minds_post = new MindsPost(text, opts);

            if (opts.images != null) {
                foreach (var image in opts.images) {
                    var upload_result = await upload_image(image);

                    if (upload_result == null) {
                        return false;
                    }

                    minds_post.attachment_guid = upload_result;
                    break;
                }
            }

            var post_data = JsonConvert.SerializeObject(minds_post);

            var request = new HttpRequestMessage(HttpMethod.Post, POST_URL) {
                Headers = {
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {token()}" }
                },
                Content = new StringContent(post_data, Encoding.UTF8, "application/json"),
            };

            var rsp = await client.SendAsync(request);
            if (!rsp.IsSuccessStatusCode) {
                Debug.log("Fie: Failed to post onto Minds");
                return false;
            };

            return true;
        }

    }
}
