using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using API;

namespace Fie.Pages {

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : ContentPage {
        public const string TITLE = "Home";

        public HomePage() {
            Title = TITLE;
            InitializeComponent();
        }
    }
}