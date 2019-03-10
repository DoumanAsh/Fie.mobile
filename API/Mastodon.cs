using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Logging;
using System.Net.Http.Headers;
using System.Net;

namespace API
{
    [JsonObject(MemberSerialization.Fields)]
    class EntityId {
        public string id = "";
    }

    [JsonObject(MemberSerialization.Fields)]
    class NewStatus {
        public string status;
        public List<string> media_ids = new List<string>();
        public bool sensitive;

        public NewStatus(string text, Options options) {
            status = text;
            sensitive = options.nsfw;
        }
    }

    public class Mastodon {
        public static string HOST = null;
        public static string ACCESS_TOKEN = null;
        public static bool configure(string host, string access_token) {
            if (host != null && host.Length != 0 && access_token != null && access_token.Length != 0 && Uri.CheckHostName(host) != UriHostNameType.Unknown) {
                HOST = host;
                ACCESS_TOKEN = access_token;
                return true;
            } else {
                return false;
            }
        }

        public static bool is_auth() {
            return HOST != null && ACCESS_TOKEN != null;
        }
        public static void reset() {
            HOST = null;
            ACCESS_TOKEN = null;
        }

        public static async Task<string> upload_image(Image image) {
            var client = Http.client();
            var body = new MultipartFormDataContent();

            var image_field = new ByteArrayContent(image.data, 0, image.data.Length);
            image_field.Headers.ContentType = MediaTypeHeaderValue.Parse(image.mime);
            body.Add(image_field, "file", image.file_name);

            var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://{HOST}/api/v1/media")) {
                Headers = {
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {ACCESS_TOKEN}" }
                },
                Content = body,
            };

            var rsp = await client.SendAsync(request);
            if (!rsp.IsSuccessStatusCode) {
                Debug.log("Fie: Failed to upload image onto Mastodon. Status: {0}", rsp.StatusCode);
                return null;
            };

            var rsp_content = await rsp.Content.ReadAsStringAsync();
            EntityId result = null;
            try {
                result = JsonConvert.DeserializeObject<EntityId>(rsp_content);
            } catch (Exception error) {
                Debug.log($"Fie: Error deserializing Mastodon image upload response: {error}");
            }

            if (result == null) {
                Debug.log("Fie: Mastodon returned invalid response to upload='{0}'", rsp_content);
                return null;
            }

            return result.id;
        }

        public static async Task<bool> post(string text, Options opts) {
            if (!is_auth()) {
                Debug.log("Fie: Mastodon Not logged in");
                return false;
            }

            var client = Http.client();

            var mast_post = new NewStatus(text, opts);

            if (opts.images != null) {
                foreach (var image in opts.images) {
                    var upload_result = await upload_image(image);

                    if (upload_result == null) {
                        return false;
                    }

                    mast_post.media_ids.Add(upload_result);
                }
            }

            var post_data = JsonConvert.SerializeObject(mast_post);

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{HOST}/api/v1/statuses") {
                Headers = {
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {ACCESS_TOKEN}" }
                },
                Content = new StringContent(post_data, Encoding.UTF8, "application/json"),
            };

            var rsp = await client.SendAsync(request);
            if (!rsp.IsSuccessStatusCode) {
                Debug.log($"Fie: Failed to post onto Mastodon. Status code={rsp.StatusCode}");
                Debug.log($"Fie: Access token={ACCESS_TOKEN}");
                return false;
            };

            return true;
        }
    }
}
