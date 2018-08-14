using System.Collections.Generic;
using System.Threading.Tasks;

using Tweetinvi;
using Tweetinvi.Core.Public.Parameters;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace API {

    public class Options {
        public bool nsfw = false;
        public List<byte[]> images;

        public Options set_nsfw() {
            this.nsfw = true;
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

    public class Twitter {
        private const string CONSUMER_KEY = "2PRGI5t7xGtn9KGkxGuNB3jbY";
        private const string CONSUMER_SECRET = "r4cwV6ut7gbMjm4fHD0u3hzWRCulvj6Xxxx3bR0xbhbx1ti3Qy";

        private TwitterCredentials creds = new TwitterCredentials(CONSUMER_KEY, CONSUMER_SECRET);
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
            Auth.ApplicationCredentials = new TwitterCredentials(CONSUMER_KEY, CONSUMER_SECRET, access_key, access_secret);
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