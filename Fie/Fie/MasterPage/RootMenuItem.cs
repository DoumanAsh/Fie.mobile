using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fie.MasterPage {

    public class RootMenuItem {

        public RootMenuItem() {
            target = typeof(Pages.HomePage);
            title = Pages.HomePage.TITLE;
        }

        public string title { get; set; }
        public Type target { get; set; }
    }
}