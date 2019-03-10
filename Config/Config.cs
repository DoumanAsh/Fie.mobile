using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;


namespace Config {

    [Serializable]
    public struct Pair {
        public string key;
        public string secret;

        static public Pair constructor(string key, string secret) {
            return new Pair {
                key = key,
                secret = secret
            };
        }
    }

    [Serializable]
    public struct Twitter {
        public Pair access;
        public bool enabled;

        static public Twitter with(Pair? access) {
            return new Twitter {
                access = access.GetValueOrDefault(),
                enabled = false,
            };
        }

        public override string ToString() {
            return $"{{ access: {{ key: {access.key}, secret: {access.secret}, enable: {enabled} }}";
        }
    }

    [Serializable]
    public struct Minds {
        public string username;
        public string passowrd;
        public bool enabled;

        public override string ToString() {
            return $"{{ username: {username}, passowrd: {passowrd}, enable: {enabled} }}";
        }

    }

    [Serializable]
    public struct Gab {
        public string username;
        public string passowrd;
        public bool enabled;

        public override string ToString() {
            return $"{{ username: {username}, passowrd: {passowrd}, enable: {enabled} }}";
        }
    }

    [Serializable]
    public struct Mastodon {
        public string host;
        public string access_token;
        public bool enabled;

        public override string ToString() {
            return $"{{ host: {host}, access_token: {access_token}, enable: {enabled} }}";
        }
    }

    [Serializable]
    public struct ApiConfig {
        public Twitter twitter;
        public Gab gab;
        public Minds minds;
        public Mastodon mastodon;

        static public ApiConfig with(Twitter? twitter, Gab? gab, Minds? minds, Mastodon? mastodon) {
            return new ApiConfig {
                twitter = twitter.GetValueOrDefault(),
                gab = gab.GetValueOrDefault(),
                minds = minds.GetValueOrDefault(),
                mastodon = mastodon.GetValueOrDefault(),
            };
        }

        public static ApiConfig deserialize(string text) {
            var buffer = Convert.FromBase64String(text);
            BinaryFormatter formatter = new BinaryFormatter();

            using (var stream = new System.IO.MemoryStream(buffer)) {
                return (ApiConfig)formatter.Deserialize(stream);
            }
        }

        public string serialize() {
            BinaryFormatter formatter = new BinaryFormatter();

            using (var buffer = new System.IO.MemoryStream()) {
                formatter.Serialize(buffer, this);
                return Convert.ToBase64String(buffer.ToArray());
            }
        }

        public override string ToString() {
            return $"{{Gab: {gab}, Minds: {minds}, Twitter: {twitter}, Mastodon: {mastodon} }}";
        }
    }
}