using System;

using MessagePack;

namespace Config {

    [MessagePackObject]
    public struct Pair {

        [Key("key")]
        public string key;

        [Key("secret")]
        public string secret;

        static public Pair constructor(string key, string secret) {
            return new Pair {
                key = key,
                secret = secret
            };
        }
    }

    [MessagePackObject]
    public struct Twitter {

        [Key("access")]
        public Pair access;

        [Key("enabled")]
        public bool enabled;

        static public Twitter with(Pair? access) {
            return new Twitter {
                access = access.GetValueOrDefault(),
                enabled = false,
            };
        }
    }

    [MessagePackObject]
    public struct ApiConfig {

        [Key("twitter")]
        public Twitter twitter;

        static public ApiConfig with(Twitter? twitter) {
            return new ApiConfig {
                twitter = twitter.GetValueOrDefault()
            };
        }

        public static ApiConfig deserialize(string text) {
            var bytes = MessagePackSerializer.FromJson(text);
            return MessagePackSerializer.Deserialize<ApiConfig>(bytes);
        }

        public string serialize() {
            var bytes = MessagePackSerializer.Serialize(this);
            var result = MessagePackSerializer.ToJson(bytes);
            return result;
        }

        //Returns whether there at least
        //single API enabled
        public bool is_any_enabled() {
            return twitter.enabled;
        }
    }
}