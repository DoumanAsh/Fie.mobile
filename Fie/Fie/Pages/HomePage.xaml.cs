using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using API;
using System;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Fie.Pages {

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : CarouselPage, INotifyPropertyChanged {
        public const string TITLE = "Home";

        public Command post_tweet { private set; get; }
        public Command open_file { private set; get; }

        //TODO: We override's page PropertyChanged here
        //Consider should I supress or just make dummy model-controller class?
        //Settings change event
        public event PropertyChangedEventHandler PropertyChanged;
        protected void on_property_change([CallerMemberName] string name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _text;
        public string text {
            set {
                _text = value;
                on_property_change();
                post_tweet.ChangeCanExecute();
            }
            get => _text;
        }

        public HomePage() {
            Title = TITLE;
            BindingContext = this;

            post_tweet = new Command(
                execute: async () => {
                    try {
                        var task =  API.Twitter.post_tweet(text, new API.Options());
                        text = null;
                        await task;
                    } catch (Exception error) {
#if DEBUG
                        Console.WriteLine("Fie: error: {0}", error);
#endif
                        await DisplayAlert("Failed to post", "Error posting on twitter", "Ok");
                    }
                },
                canExecute: () => {
                    return text != null && text.Length > 0;
                }
            );
            open_file = new Command(
                execute: () => {
                    Console.WriteLine("Fie: pick file");
                },
                canExecute: () => {
                    return true;
                }
            );

            InitializeComponent();
        }
    }
}