using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using API;
using System;
using System.Threading.Tasks;

namespace Fie.Pages {

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : ContentPage {
        public const string TITLE = "Home";

        public string text { set; get; }
        public Command post_tweet { private set; get; }
        public Command open_file { private set; get; }

        public HomePage() {
            Title = TITLE;
            BindingContext = this;

            post_tweet = new Command(
                execute: async () => {
                    if (text == null) return;

                    try {
                        await API.Twitter.post_tweet(text, new API.Options());
                    } catch (Exception error) {
#if DEBUG
                        Console.WriteLine("Fie: error: {0}", error);
#endif
                        await DisplayAlert("Failed to post", "Error posting on twitter", "Ok");
                    }
                    //TODO: wtf!? it is not reflected on editor
                    text = null;
                },
                canExecute: () => {
                    return true;
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