using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.Forms
{
    using Gtk;
    using Views;

    class HelpForm
    {
        private Window window = null;
        private HTMLView htmlView = null;
        private string url;
        private static HelpForm helpForm;

        private HelpForm()
        {
            this.window = new Window(WindowType.Toplevel);
            this.htmlView = new HTMLView(new ViewBase(null));
            window.Title = "APSIM Help";
            window.Add(htmlView.MainWidget);
            window.Resize(800, 600);
            window.MapEvent += OnShown;
            window.Destroyed += OnFormClosing;
        }

        public static HelpForm GetHelpForm()
        {
            if (helpForm == null)
                helpForm = new HelpForm();
            return helpForm;
        }

        public void Show(string url)
        {
            this.url = url;
            window.ShowAll();
            if (htmlView.MainWidget.IsRealized)
                htmlView.SetContents(this.url, false, true);
            window.Window.Focus(0);
        }

        /// <summary>
        /// Form has loaded. Populate the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnShown(object sender, EventArgs e)
        {
            htmlView.SetContents(this.url, false, true);
        }

        /// <summary>
        /// Form is closing - clean up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFormClosing(object sender, EventArgs e)
        {
            window.MapEvent -= OnShown;
            window.Destroyed -= OnFormClosing;
            if (this == helpForm)
                helpForm = null;
        }
    }
}
