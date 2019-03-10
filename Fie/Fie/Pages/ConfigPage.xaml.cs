using System;
using System.ComponentModel;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using System.Windows.Input;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Tweetinvi.Models;

using Config;
using Logging;
using API;
using System.Net.Http;

namespace Fie.Pages {

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfigPage : CarouselPage, INotifyPropertyChanged {
        public const string TITLE = "Configuration";
        private bool is_setting_changed;
        private bool is_auth_in_progress;
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
            login_gab = new Command(
                execute: async () => {
                    if (gab_password == null || gab_password.Length == 0) {
                        await DisplayAlert("Missing password", "Please enter password", "Ok");
                        return;
                    }
                    if (gab_username == null || gab_username.Length == 0) {
                        await DisplayAlert("Missing username", "Please enter username", "Ok");
                        return;
                    }

                    if (is_auth_in_progress) {
                        await DisplayAlert("Gab authorization", "Authroization process is ongoing. Try after it is done", "Ok");
                        return;
                    }
                    is_auth_in_progress = true;

                    try {
                        await API.Gab.login(gab_username, gab_password);

                        if (API.Gab.is_auth()) {
                            reset_gab.ChangeCanExecute();
                            login_gab.ChangeCanExecute();
                            await DisplayAlert("Gab authorization Ok", "Successuflly authorized. Please save configuration", "Ok");
                        } else {
                            await DisplayAlert("Gab authorization Error", "Failed to login. Check your username and password", "Ok");
                        }
                    } catch (HttpRequestException error) {
                        Debug.log("Fie: Http error: {0}", error);
                        await DisplayAlert("Network Error", "Unable to connect. Please turn on network", "Ok");
                    } catch (Exception error) {
                        Debug.log("Fie: Unknown error: {0}", error);
                        await DisplayAlert("Internal Error", "Unable to connect. Please turn on network", "Ok");
                    }

                    is_auth_in_progress = false;
                },
                canExecute: () => {
                    return !API.Gab.is_auth();
                }
            );
            reset_gab = new Command(
                execute: () => {
                    API.Gab.reset_token();
                    gab_username = "";
                    gab_password = "";
                    login_gab.ChangeCanExecute();
                    reset_gab.ChangeCanExecute();
                },
                canExecute: () => {
                    return API.Gab.is_auth();
                }
            );
            login_minds = new Command(
                execute: async () => {
                    if (minds_password == null || minds_password.Length == 0) {
                        await DisplayAlert("Missing password", "Please enter password", "Ok");
                        return;
                    }
                    if (minds_username == null || minds_username.Length == 0) {
                        await DisplayAlert("Missing username", "Please enter username", "Ok");
                        return;
                    }

                    if (is_auth_in_progress) {
                        await DisplayAlert("Minds authorization", "Authroization process is ongoing. Try after it is done", "Ok");
                        return;
                    }
                    is_auth_in_progress = true;

                    try {
                        await API.Minds.login(minds_username, minds_password);
                        if (API.Minds.is_auth()) {
                            reset_minds.ChangeCanExecute();
                            login_minds.ChangeCanExecute();
                            await DisplayAlert("Minds authorization Ok", "Successuflly authorized. Please save configuration", "Ok");
                        } else {
                            await DisplayAlert("Minds authorization Error", "Failed to login. Check your username and password", "Ok");
                        }
                    } catch (HttpRequestException error) {
                        Debug.log("Fie: Http error: {0}", error);
                        await DisplayAlert("Network Error", "Unable to connect. Please turn on network", "Ok");
                    } catch (Exception error) {
                        Debug.log("Fie: Unknown error: {0}", error);
                        await DisplayAlert("Internal Error", "Unable to connect. Please turn on network", "Ok");
                    }

                    is_auth_in_progress = false;
                },
                canExecute: () => {
                    return !API.Minds.is_auth();
                }
            );
            reset_minds = new Command(
                execute: () => {
                    API.Minds.reset_token();
                    minds_username = "";
                    minds_password = "";
                    login_minds.ChangeCanExecute();
                    reset_minds.ChangeCanExecute();
                },
                canExecute: () => {
                    return API.Minds.is_auth();
                }
            );
            login_mastodon = new Command(
                execute: async () => {
                    if (mastodon_host == null || mastodon_host.Length == 0) {
                        await DisplayAlert("Missing host", "Please enter host", "Ok");
                        return;
                    }
                    if (mastodon_access_token == null || mastodon_access_token.Length == 0) {
                        await DisplayAlert("Missing access token", "Please enter access token", "Ok");
                        return;
                    }

                    if (API.Mastodon.configure(mastodon_host, mastodon_access_token)) {
                        reset_mastodon.ChangeCanExecute();
                        login_mastodon.ChangeCanExecute();
                        await DisplayAlert("Mastodon config OK", "Configuration is set", "Ok");
                    } else {
                        await DisplayAlert("Mastodon config Error", "Invalid host name, Please make sure to input right one", "Ok");
                    }
                },
                canExecute: () => {
                    return !API.Mastodon.is_auth();
                }
            );
            reset_mastodon = new Command(
                execute: () => {
                    API.Mastodon.reset();
                    mastodon_host = "";
                    mastodon_access_token = "";
                    login_mastodon.ChangeCanExecute();
                    reset_mastodon.ChangeCanExecute();
                },
                canExecute: () => {
                    return API.Mastodon.is_auth();
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
        public Command login_gab { private set; get; }
        public Command reset_gab { private set; get; }
        public Command login_minds { private set; get; }
        public Command reset_minds { private set; get; }
        public Command login_mastodon { private set; get; }
        public Command reset_mastodon { private set; get; }

        //Settings change event
        public event PropertyChangedEventHandler PropertyChanged;

        protected void on_setting_change(string name) {
            Debug.log("Fie: setting {0} is changed", name);
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

        //Gab ON switch
        public bool gab_on {
            set => set_property(ref config.gab.enabled, value);
            get => config.gab.enabled;
        }

        //Minds ON switch
        public bool minds_on {
            set => set_property(ref config.minds.enabled, value);
            get => config.minds.enabled;
        }
        //Mastodon ON switch
        public bool mastodon_on {
            set => set_property(ref config.mastodon.enabled, value);
            get => config.mastodon.enabled;
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

        //Gab creds
        public string gab_username {
            set => set_property(ref config.gab.username, value);
            get => config.gab.username;
        }
        public string gab_password {
            set => set_property(ref config.gab.passowrd, value);
            get => config.gab.passowrd;
        }

        //Minds creds
        public string minds_username {
            set => set_property(ref config.minds.username, value);
            get => config.minds.username;
        }
        public string minds_password {
            set => set_property(ref config.minds.passowrd, value);
            get => config.minds.passowrd;
        }
        //Mastodon creds
        public string mastodon_host {
            set => set_property(ref config.mastodon.host, value);
            get => config.mastodon.host;
        }
        public string mastodon_access_token {
            set => set_property(ref config.mastodon.access_token, value);
            get => config.mastodon.access_token;
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

                Debug.log("Fie: Pin={0}", pin);

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