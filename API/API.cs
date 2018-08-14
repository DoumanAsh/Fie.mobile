using Tweetinvi;
using Tweetinvi.Models;

namespace API {

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
    }

    public class API {
    }
}