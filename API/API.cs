using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace API {

    public class Options {
        public bool nsfw = false;
        public List<byte[]> images;

        public Options set_nsfw() {
            nsfw = true;
            return this;
        }

        public Options add_image(byte[] image) {
            if (images is null) {
                images = new List<byte[]>(1);
            }
            images.Add(image);

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

        public bool can_post(string text) {
            return Tweet.CanBePublished(text);
        }

        public static Task<ITweet> post_tweet(string text, Options opts) {
            var task = Sync.ExecuteTaskAsync(() => {
                var options = new PublishTweetOptionalParameters {
                    PossiblySensitive = opts.nsfw,
                };

                if (opts.images != null) {
                    options.MediaBinaries = opts.images;
                }
                return Tweet.PublishTweet(text, options);
            });

            return task;
        }
    }

    public class API {
    }
}