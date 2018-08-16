using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Fie.Pages {

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : CarouselPage {
        public const string TITLE = "Home";

        protected override void OnAppearing() {
            MessagingCenter.Subscribe<Data.HomePage.ViewModel, Data.Misc.DisplayAlert>(this, Data.Misc.DisplayAlert.NAME, async (sender, alert) => {
                await DisplayAlert(alert.title, alert.message, alert.accept);
            });
            base.OnAppearing();
        }

        protected override void OnDisappearing() {
            MessagingCenter.Unsubscribe<Data.HomePage.ViewModel, Data.Misc.DisplayAlert>(this, Data.Misc.DisplayAlert.NAME);
            base.OnDisappearing();
        }

        public HomePage() {
            Title = TITLE;
            InitializeComponent();
        }
    }
}