using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Config;
using Logging;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace Fie {

    public partial class App : Application {
        protected const string CONFIG = "config";
        private ApiConfig api_config = ApiConfig.with(null);

        public App() {
            InitializeComponent();
            MainPage = new MasterPage.Root();
        }

        //Loads config from storage
        protected void load_config() {
            try {
                var config = (string)this.Properties[CONFIG];

                Debug.log("Fie: Stored config: {0}", config);

                this.api_config = ApiConfig.deserialize(config);

                if (this.api_config.twitter.access.key != null && this.api_config.twitter.access.secret != null) {
                    API.Twitter.set_creds(this.api_config.twitter.access.key, this.api_config.twitter.access.secret);
                }
            } catch (System.Collections.Generic.KeyNotFoundException) {
                this.save_config();
            } catch (Exception unexpected) {
                Debug.log("Fie: Unexpected exception: {0}", unexpected);
                this.save_config();
            }
        }

        public ApiConfig config() {
            return this.api_config;
        }

        public void set_config(ApiConfig new_config) {
            this.api_config = new_config;
            this.save_config();
        }

        protected void save_config() {
            this.Properties[CONFIG] = this.api_config.serialize();
            SavePropertiesAsync();
        }

        protected override void OnStart() {
            Debug.log("Fie: Started");
            load_config();
        }

        protected override void OnSleep() {
            Debug.log("Fie: Sleep");
            this.save_config();
        }

        protected override void OnResume() {
        }
    }
}