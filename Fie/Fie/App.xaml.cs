using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Config;

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
#if DEBUG
                Console.WriteLine("Fie: Stored config: {0}", config);
#endif
                this.api_config = ApiConfig.deserialize(config);
            } catch (System.Collections.Generic.KeyNotFoundException) {
                this.save_config();
            } catch (Exception unexpected) {
#if DEBUG
                Console.WriteLine("Fie: Unexpected exception: {0}", unexpected);
#endif
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
#if DEBUG
            Console.WriteLine("Fie: Started");
#endif
            load_config();
        }

        protected override void OnSleep() {
#if DEBUG
            Console.WriteLine("Fie: Sleep");
#endif
            this.save_config();
        }

        protected override void OnResume() {
        }
    }
}