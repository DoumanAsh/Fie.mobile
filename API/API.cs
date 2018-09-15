using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace API {
    public class Image {
        public string mime;
        public string file_name;
        public byte[] data;
    }

    public class Options {
        public bool nsfw = false;
        public List<Image> images;

        public Options set_nsfw() {
            nsfw = true;
            return this;
        }

        public Options add_image(string mime, string file_name, byte[] image) {
            if (images is null) {
                images = new List<Image>(1);
            }
            images.Add(new Image {
                mime = mime,
                file_name = file_name,
                data = image,
            });

            return this;
        }
    }

    public class TwitterConsumerKeys {
        //Note embedded resource should be specified as if it would be module

        private const string CONSUMER_KEY_R_NAME = "API.consumer_key.txt";
        private const string CONSUMER_SECRET_R_NAME = "API.consumer_secret.txt";

        public string key { get; private set; }
        public string secret { get; private set; }

        public TwitterConsumerKeys() {
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(CONSUMER_KEY_R_NAME))
            using (var reader = new StreamReader(stream)) {
                key = reader.ReadToEnd();
            }

            using (var stream = assembly.GetManifestResourceStream(CONSUMER_SECRET_R_NAME))
            using (var reader = new StreamReader(stream)) {
                secret = reader.ReadToEnd();
            }
        }
    }

    public class Twitter {
        private static readonly TwitterConsumerKeys KEYS = new TwitterConsumerKeys();

        private TwitterCredentials creds = new TwitterCredentials(KEYS.key, KEYS.secret);
        private IAuthenticationContext ctx;

        public string get_auth() {
            ctx = AuthFlow.InitAuthentication(creds);

            return ctx.AuthorizationURL;
        }

        public ITwitterCredentials set_pin(string pin) {
            var user_creds = AuthFlow.CreateCredentialsFromVerifierCode(pin, ctx);
            Auth.SetCredentials(user_creds);
            return user_creds;
        }

        public static void set_creds(string access_key, string access_secret) {
            Auth.ApplicationCredentials = new TwitterCredentials(KEYS.key, KEYS.secret, access_key, access_secret);
        }

        public static bool can_post(string text) {
            //Currently the check is commented out.
            //return Tweet.CanBePublished(text);
            return TweetinviConsts.MAX_TWEET_SIZE >= Tweetinvi.Controllers.Tweet.TweetController.EstimateTweetLength(text);
        }

        public static Task<ITweet> post_tweet(string text, Options opts) {
            var task = Sync.ExecuteTaskAsync(() => {
                var options = new PublishTweetOptionalParameters {
                    PossiblySensitive = opts.nsfw,
                };

                if (opts.images != null) {
                    options.MediaBinaries = opts.images.Select(img => img.data).ToList();
                }
                return Tweet.PublishTweet(text, options);
            });

            return task;
        }
    }

    public class API {
        /// <summary>
        /// Determines whether provided file name belongs to supported image or not
        /// </summary>
        /// <param name="file_name"> File name to check.</param>
        /// <returns>Mime type of supported image, if file is unsupported then null</returns>
        public static string guess_image_mime(string file_name) {
            var dot_idx = file_name.LastIndexOf('.');

            if (dot_idx != -1 && file_name.Length > (dot_idx + 1)) {
                var extension = file_name.Substring(dot_idx + 1);

                switch (extension.ToLower()) {
                    case "png":
                        return "image/png";
                    case "jpg":
                    case "jpe":
                    case "jpeg":
                        return "image/jpeg";
                    default:
                        break;
                }
            }

            return null;
        }
    }
}