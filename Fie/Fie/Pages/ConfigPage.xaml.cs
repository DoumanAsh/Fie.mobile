﻿using System;
using System.ComponentModel;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Config;
using System.Windows.Input;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Tweetinvi.Models;

namespace Fie.Pages {

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfigPage : CarouselPage, INotifyPropertyChanged {
        public const string TITLE = "Configuration";
        private bool is_setting_changed;
        private ApiConfig config;

        public void unset_setting_change() {
            is_setting_changed = false;
            save_command.ChangeCanExecute();
        }

        public ConfigPage() {
            BindingContext = this;
            is_setting_changed = false;

            Title = TITLE;
            config = ((App)Application.Current).config();

            save_command = new Command(
                execute: () => {
                    ((App)Application.Current).set_config(config);
                    is_setting_changed = false;
                    this.unset_setting_change();
                },
                canExecute: () => {
                    return is_setting_changed;
                }
            );
            connect_twitter = new Command(
                execute: () => {
                    on_connect_twitter();
                },
                canExecute: () => {
                    return this.twitter_access_key == null || this.twitter_access_secret == null;
                }
            );
            reset_twitter = new Command(
                execute: () => {
                    this.twitter_access_key = null;
                    this.twitter_access_secret = null;
                    reset_twitter.ChangeCanExecute();
                    connect_twitter.ChangeCanExecute();
                },
                canExecute: () => {
                    return this.twitter_access_key != null || this.twitter_access_secret != null;
                }
            );

            InitializeComponent();
        }

        //Message subscription
        protected void msg_center_creds_subscribe() {
            MessagingCenter.Subscribe<ConfigPage, ITwitterCredentials>(this, "set_creds", (sender, creds) => {
                this.twitter_access_key = creds.AccessToken;
                this.twitter_access_secret = creds.AccessTokenSecret;
                msg_center_creds_unsubscribe();
                reset_twitter.ChangeCanExecute();
                connect_twitter.ChangeCanExecute();
            });
        }
        protected void msg_center_creds_unsubscribe() {
            MessagingCenter.Unsubscribe<ConfigPage, ITwitterCredentials>(this, "set_creds");
        }

        //Commands
        public Command save_command { private set; get; }
        public Command connect_twitter { private set; get; }
        public Command reset_twitter { private set; get; }


        //Settings change event
        public event PropertyChangedEventHandler PropertyChanged;

        protected void on_setting_change(string name) {
#if DEBUG
            Console.WriteLine("Fie: setting {0} is changed", name);
#endif
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            is_setting_changed = true;
            save_command.ChangeCanExecute();
        }

        //Generic setting updater
        protected void set_property<T>(ref T property, T value, [CallerMemberName] string name = null) {
            if (object.Equals(property, value)) return;

            property = value;
            on_setting_change(name);
        }

        //Twitter ON switch
        public bool twitter_on {
            set => set_property(ref config.twitter.enabled, value);
            get => config.twitter.enabled;
        }

        //Twitter Access's key
        public string twitter_access_key {
            set => set_property(ref config.twitter.access.key, value);
            get => config.twitter.access.key;
        }

        //Twitter Access's key
        public string twitter_access_secret {
            set => set_property(ref config.twitter.access.secret, value);
            get => config.twitter.access.secret;
        }

        private void on_connect_twitter() {
            const string GET_PIN = "document.getElementById(\"oauth_pin\").innerText";

            msg_center_creds_subscribe();

            var twatter = new API.Twitter();
            var url = twatter.get_auth();
            var page = new UtilsPages.WebPageView(url, GET_PIN, async (res) => {
                string pin = null;
                var split = res.Split(':');
                if (split.Length > 1) {
                    pin = split[1].Replace(@"\n", string.Empty);
                    if (!int.TryParse(pin, out int n)) {
                        pin = null;
                    }
                }
#if DEBUG
                Console.WriteLine("Fie: Pin={0}", pin);
#endif
                if (pin != null) {
                    var creds = twatter.set_pin(pin);
                    //Use message here to properly update value on UI thread.
                    MessagingCenter.Send<ConfigPage, ITwitterCredentials>(this, "set_creds", creds);
                    return true;
                } else {
                    await DisplayAlert("No PIN", "PIN is not found. Make sure to log in and authorize app.", "Ok");
                    return false;
                }
            }, () => msg_center_creds_unsubscribe());

            Navigation.PushModalAsync(page);
        }
    }
}