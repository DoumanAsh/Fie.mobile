using System;
using System.Threading.Tasks;
using Xamarin.Forms;

using CallbackType = System.Func<string, System.Threading.Tasks.Task<bool>>;

namespace Fie.UtilsPages {

    /// <summary>
    /// Provides utility WebView that allows to acces web page
    /// and perform task when user done.
    /// 
    /// The callback can return false to indicate that
    /// task is not done yet, in which case view shall not
    /// exit
    /// </summary>
    public class WebPageView : ContentPage {
        private WebView web_view;
        private CallbackType cb;
        private string to_eval;

        public WebPageView(string url, CallbackType cb, string to_eval) {
            this.cb = cb;
            this.to_eval = to_eval;

            var button = new Button {
                Text = "Done"
            };
            button.Clicked += async (args, args2) => {
                await on_finish();
            };
            var header = new StackLayout {
                Children = {
                    button
                }
            };

            web_view = new WebView {
                Source = new UrlWebViewSource {
                    Url = url
                },
                VerticalOptions = LayoutOptions.FillAndExpand
            };

#if __IOS__
            // Accomodate iPhone status bar.
            this.Padding = new Thickness(0, 20, 0, 0);
#endif

            // Build the page.
            this.Content = new StackLayout {
                Children = {
                    header,
                    web_view
                }
            };
        }

        protected async Task on_finish() {
            //NOTE: WebView methods must be called on the same thread....
            string result = null;
            try {
                result = await web_view.EvaluateJavaScriptAsync(to_eval);
            } catch {
#if DEBUG
                Console.WriteLine("Fie: JS exception");
#endif
            }

            var is_exit = await cb(result ?? string.Empty);

            if (is_exit) {
                await Navigation.PopModalAsync();
            }
        }

        protected override void OnDisappearing() {
            web_view = null;
            cb = null;
            to_eval = null;
            base.OnDisappearing();
        }
    }
}