using System;
using System.Threading.Tasks;
using Xamarin.Forms;

using Logging;

using DoneCallbackType = System.Func<string, System.Threading.Tasks.Task<bool>>;
using BackCallbackType = System.Action;

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
        private DoneCallbackType done_cb;
        private BackCallbackType back_cb;
        private string to_eval;

        public WebPageView(string url, string to_eval, DoneCallbackType done_cb, BackCallbackType back_cb = null) {
            this.done_cb = done_cb;
            this.back_cb = back_cb;
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
                Debug.log("Fie: JS exception");
            }

            var is_exit = await done_cb(result ?? string.Empty);

            if (is_exit) {
                back_cb = null;
                await Navigation.PopModalAsync();
            }
        }

        protected override void OnDisappearing() {
            if (back_cb != null) {
                back_cb();
                back_cb = null;
            }
            web_view = null;
            done_cb = null;
            to_eval = null;
            base.OnDisappearing();
        }
    }
}