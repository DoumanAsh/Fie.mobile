using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Fie.MasterPage {

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Root : MasterDetailPage {

        public Root() {
            InitializeComponent();
            MasterPage.list_view.ItemSelected += ListView_ItemSelected;
        }

        private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e) {
            if (!(e.SelectedItem is RootMenuItem item)) return;

            var page = (Page)Activator.CreateInstance(item.target);

            Detail = new NavigationPage(page) {
                BarBackgroundColor = Color.Teal
            };
            IsPresented = false;
        }
    }
}