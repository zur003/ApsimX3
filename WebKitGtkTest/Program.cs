using Gtk;
using GtkSharp.WebKit;
using System;
 
namespace WebKitGtkTest
{
    class Hello
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.Init();

            Window window = new Window("helloworld");
            window.SetSizeRequest(800, 600);
            window.DeleteEvent += Window_DeleteEvent;
            ScrolledWindow ScrollWindow = new ScrolledWindow();
            WebView webView = new WebView();
            ScrollWindow.Add(webView);
            window.Add(ScrollWindow);
            webView.GrabFocus();
            ///webView.LoadUri("about:blank");
            webView.LoadUri("https://www.csiro.au");
            ///webView.LoadUri("https://www.apsim.info");
            webView.CloseWebView += WebView_CloseWebView;
            ///webView.LoadHtmlString("<html><body><script>alert(\"HEY!\");</script>hello, world!</body></html>", "about:blank");
            ///webView.LoadString("<body>Hello, world!</body>", null, null, "file://");
            window.ShowAll();

            Application.Run();

        }

        private static void WebView_CloseWebView(object o, CloseWebViewArgs args)
        {
            Application.Quit();
        }

        private static void Window_DeleteEvent(object o, DeleteEventArgs args)
        {
            Application.Quit();
        }
    }
}

