using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Fie.UtilsPages {

    public class WebPageView : ContentPage {
        private WebView web_view;
        private Func<string, Task> cb;
        private string to_eval;

        public WebPageView(string url, Func<string, Task> cb, string to_eval) {
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

        protected override bool OnBackButtonPressed() {
            return false;
        }

        protected async Task<Page> on_finish() {
            //NOTE: WebView methods must be called on the same thread....
            string result = null;
            try {
                result = await web_view.EvaluateJavaScriptAsync(to_eval);
            } catch {
#if DEBUG
                Console.WriteLine("Fie: JS exception");
#endif
            }

            if (result != null) await cb(result);
            return await Navigation.PopModalAsync();
        }

        protected override void OnDisappearing() {
            web_view = null;
            cb = null;
            to_eval = null;
            base.OnDisappearing();
        }
    }
}