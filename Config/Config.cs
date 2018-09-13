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
    public struct ApiConfig {
        public Twitter twitter;

        static public ApiConfig with(Twitter? twitter) {
            return new ApiConfig {
                twitter = twitter.GetValueOrDefault()
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

        //Returns whether there at least
        //single API enabled
        public bool is_any_enabled() {
            return twitter.enabled;
        }

        public override string ToString() {
            return $"{{Twitter: {twitter} }}";
        }
    }
}