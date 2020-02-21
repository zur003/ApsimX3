using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
////using System.Windows.Forms;
using Gtk;
////using WebKit2;
using GtkSharp.WebKit;
////using MonoMac.AppKit;
////using WebWindows;
using MarkdownSharp;
////using EO.WebBrowser;
using APSIM.Shared.Utilities;
using UserInterface.EventArguments;
using HtmlAgilityPack;
using UserInterface.Classes;
using System.IO;
using System.Drawing;

namespace UserInterface.Views
{
    /// <summary>
    /// An interface for a HTML view.
    /// </summary>
    interface IHTMLView
    {
        /// <summary>
        /// Path to find images on.
        /// </summary>
        string ImagePath { get; set; }

        /// <summary>
        /// Set the contents of the control. Can be RTF, HTML or MarkDown. If 
        /// the contents are markdown and 'allowModification' = true then
        /// user will be able to edit markdown.
        /// </summary>
        void SetContents(string contents, bool allowModification, bool isURI);

        /// <summary>
        /// Return the edited markdown.
        /// </summary>
        /// <returns></returns>
        string GetMarkdown();

        /// <summary>
        /// Tells view to use a mono spaced font.
        /// </summary>
        void UseMonoSpacedFont();

    }

    public interface IBrowserWidget : IDisposable
    {
        void Navigate(string uri);
        void LoadHTML(string html);

        /// <summary>
        /// Returns the Title of the current document
        /// </summary>
        /// <returns></returns>
        string GetTitle();

        Widget HoldingWidget { get; set; }

        /// <summary>
        /// Sets the foreground colour of the document.
        /// </summary>
        /// <value></value>
        System.Drawing.Color ForegroundColour { get; set; }

        /// <summary>
        /// Sets the foreground colour of the document.
        /// </summary>
        /// <value></value>
        System.Drawing.Color BackgroundColour { get; set; }

        /// <summary>
        /// Sets the font of the document.
        /// </summary>
        Pango.FontDescription Font { get; set; }

        void ExecJavaScript(string command, object[] args);

        void ExecJavaScript(string command);

        bool Search(string forString, bool forward, bool caseSensitive, bool wrap);
    }

    public class WebKitBrowser : IBrowserWidget
    {
        public GtkSharp.WebKit.WebView Browser { get; set; } = null;
        public ScrolledWindow ScrollWindow { get; set; } = new ScrolledWindow();
        public Widget HoldingWidget { get; set; }

        /// <summary>
        /// The find form
        /// </summary>
        private Utility.FindInBrowserForm findForm = new Utility.FindInBrowserForm();

        public void InitWebKit(Gtk.Box w)
        {
            HoldingWidget = w;
            Browser = new GtkSharp.WebKit.WebView();
            ScrollWindow.Add(Browser);
            // Hack to work around webkit bug; webkit will crash the app if a size is not provided
            // See https://bugs.eclipse.org/bugs/show_bug.cgi?id=466360 for a related bug report
            Browser.SetSizeRequest(2, 2);
            Browser.KeyPressEvent += Wb_KeyPressEvent;
            Browser.Settings.EnableScripts = true;
            w.PackStart(ScrollWindow, true, true, 0);
            w.ShowAll();
        }

        /// <summary>
        /// Gets the text selected by the user.
        /// </summary>
        public string GetSelectedText()
        {
            return "";  // To be implemented
        }

        /// <summary>
        /// Selects all text in the document.
        /// </summary>
        public void SelectAll()
        {
            // To be implemented
        }

        public void Navigate(string uri)
        {
            Browser.LoadUri(uri);
        }

        public void LoadHTML(string html)
        {
            Browser.LoadString(html, "text/html", "UTF-8", "about:blank");
            // Probably should make this conditional.
            // We use a timeout so we don't sit here forever if a document fails to load.
            Stopwatch watch = new Stopwatch();
            watch.Start();
            while (Browser != null && Browser.LoadStatus != LoadStatus.Finished && watch.ElapsedMilliseconds < 10000)
                while (Gtk.Application.EventsPending())
                    Gtk.Application.RunIteration();
        }

        public System.Drawing.Color BackgroundColour
        {
            get
            {
                return Color.Empty; // TODO
            }
            set
            {
                string colour = Utility.Colour.ToHex(value);
                Browser.ExecuteScript($"document.body.style.backgroundColor = \"{colour}\";");
            }
        }

        public System.Drawing.Color ForegroundColour
        {
            get
            {
                return Color.Empty; // TODO
            }
            set
            {
                string colour = Utility.Colour.ToHex(value);
                Browser.ExecuteScript($"document.body.style.color = \"{colour}\";");
            }
        }

        public Pango.FontDescription Font
        {
            get => throw new NotImplementedException();
            set
            {
                Browser.ExecuteScript($"document.body.style.fontFamily = \"{value.Family}\";");
                Browser.ExecuteScript($"document.body.style.fontSize = \"{1.5 * value.Size / Pango.Scale.PangoScale}\";");
            }
        }

        public WebKitBrowser(Gtk.Box w)
        {
            InitWebKit(w);
        }

        public string GetTitle()
        {
            return Browser.Title;
        }

        public bool Search(string forString, bool forward, bool caseSensitive, bool wrap)
        {
            return Browser.SearchText(forString, caseSensitive, forward, wrap);
        }

        public void Highlight(string text, bool caseSenstive, bool doHighlight)
        {
            // Doesn't seem to work as well as expected....
            // Browser.SelectAll();
            Browser.UnmarkTextMatches();
            if (doHighlight)
            {
                Browser.MarkTextMatches(text, caseSenstive, 0);
                Browser.HighlightTextMatches = true;
            }
        }


        public void ExecJavaScript(string command, object[] args)
        {
            string argString = "";
            bool first = true;
            foreach (object obj in args)
            {
                if (!first)
                    argString += ", ";
                first = false;
                argString += obj.ToString();
            }
            Browser.ExecuteScript(command + "(" + argString + ");");
        }

        public void ExecJavaScript(string script)
        {
            Browser.ExecuteScript(script);
        }

        // Flag: Has Dispose already been called? 
        bool disposed = false;

        [GLib.ConnectBefore]
        void Wb_KeyPressEvent(object o, Gtk.KeyPressEventArgs args)
        {
            args.RetVal = false;
            if ((args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)
            {
                if (args.Event.Key == Gdk.Key.f || args.Event.Key == Gdk.Key.F)
                {
                    findForm.ShowFor(this);
                }
                else if (args.Event.Key == Gdk.Key.g || args.Event.Key == Gdk.Key.G)
                {
                    findForm.FindNext((args.Event.State & Gdk.ModifierType.ShiftMask) != Gdk.ModifierType.ShiftMask, null);
                }
            }
            else if (args.Event.Key == Gdk.Key.F3)
                findForm.FindNext((args.Event.State & Gdk.ModifierType.ShiftMask) != Gdk.ModifierType.ShiftMask, null);
        }

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
            Browser.KeyPressEvent -= Wb_KeyPressEvent;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Browser.Dispose();
                Browser = null;
                ScrollWindow.Dispose();
            }

            // Free any unmanaged objects here. 
            //
            disposed = true;
        }
    }

    /// <summary>
    /// The Presenter for a HTML component.
    /// </summary>
    public class HTMLView : ViewBase, IHTMLView
    {
        /// <summary>
        /// The VPaned object which holds the containers for the memo view and web browser.
        /// </summary>
        private VPaned vpaned1 = null;

        /// <summary>
        /// VBox obejct which holds the web browser.
        /// </summary>
        private VBox vbox2 = null;

        /// <summary>
        /// Frame object which holds and is used to position <see cref="vbox2"/>.
        /// </summary>
        private Gtk.Frame frame1 = null;

        /// <summary>
        /// HBox which holds the memo view.
        /// </summary>
        private HBox hbox1 = null;

        /// <summary>
        /// Only used on Windows. Holds the HTML element which responds to key
        /// press events.
        /// </summary>
        private object keyPressObject = null;

        /// <summary>
        /// Web browser used to display HTML content.
        /// </summary>
        protected IBrowserWidget browser = null;

        /// <summary>
        /// Memo view used to display markdown content.
        /// </summary>
        private MemoView memo;

        /// <summary>
        /// Used when exporting a map (e.g. autodocs).
        /// </summary>
        protected Gtk.Window popupWindow = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public HTMLView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.HTMLView.glade");
            vpaned1 = (VPaned)builder.GetObject("vpaned1");
            vbox2 = (VBox)builder.GetObject("vbox2");
            frame1 = (Gtk.Frame)builder.GetObject("frame1");
            hbox1 = (HBox)builder.GetObject("hbox1");
            mainWidget = vpaned1;
            // Handle a temporary browser created when we want to export a map.
            if (owner.Owner == null)
            {
                popupWindow = new Gtk.Window(Gtk.WindowType.Popup);
                popupWindow.SetSizeRequest(500, 500);
                // Move the window offscreen; the user doesn't need to see it.
                // This works with IE, but not with WebKit
                // Not yet tested on OSX
                if (ProcessUtilities.CurrentOS.IsWindows)
                    popupWindow.Move(-10000, -10000);
                popupWindow.Add(MainWidget);
                popupWindow.ShowAll();
                while (Gtk.Application.EventsPending())
                    Gtk.Application.RunIteration();
            }
            memo = new MemoView(this);
            hbox1.PackStart(memo.MainWidget, true, true, 0);
            vpaned1.PositionSet = true;
            vpaned1.Position = 200;
            hbox1.Visible = false;
            hbox1.NoShowAll = true;
            memo.ReadOnly = false;
            memo.WordWrap = true;
            memo.MemoChange += this.TextUpdate;
            vpaned1.ShowAll();
            frame1.Drawn += OnWidgetExpose;
            hbox1.Realized += Hbox1_Realized;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        /// <summary>
        /// Path to find images on.
        /// </summary>
        public string ImagePath { get; set; }

        /// <summary>
        /// Invoked when the user wishes to copy data out of the HTMLView.
        /// This is currently only used on Windows, as the other web 
        /// browsers are capable of handling the copy event themselves.
        /// </summary>
        public event EventHandler<CopyEventArgs> Copy;

        /// <summary>
        /// Set the contents of the control. Can be RTF, HTML or MarkDown. If 
        /// the contents are markdown and 'allowModification' = true then
        /// user will be able to edit markdown.
        /// </summary>
        public void SetContents(string contents, bool allowModification, bool isURI = false)
        {
            TurnEditorOn(allowModification);
            if (contents != null)
            {
                if (allowModification)
                    memo.MemoText = contents;
                else
                    PopulateView(contents, isURI);
            }
        }

        // Although this isn't the obvious way to handle window resizing,
        // I couldn't find any better technique. 
        public void OnWidgetExpose(object o, DrawnArgs args)
        {
            int x, y, height, width;
            frame1.Window.GetGeometry(out x, out y, out width, out height);
            frame1.SetSizeRequest(width, height);
        }

        /// <summary>
        /// Return the edited markdown.
        /// </summary>
        /// <returns></returns>
        public string GetMarkdown()
        {
            return memo.MemoText;
        }

        /// <summary>
        /// Tells view to use a mono spaced font.
        /// </summary>
        public void UseMonoSpacedFont()
        {
        }

        /// <summary>
        /// Enables or disables the Windows web browser.
        /// </summary>
        /// <param name="state">True to enable the browser, false to disable it.</param>
        public void EnableBrowser(bool state)
        {
        }

        protected void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            memo.MemoChange -= this.TextUpdate;
            frame1.Drawn -= OnWidgetExpose;
            hbox1.Realized -= Hbox1_Realized;
            if (browser != null)
                browser.Dispose();
            if (popupWindow != null)
            {
                popupWindow.Dispose();
            }
            memo.MainWidget.Dispose();
            memo = null;
            mainWidget.Destroyed -= _mainWidget_Destroyed;
            owner = null;
        }

        protected virtual void NewTitle(string title)
        {
        }

        private void Hbox1_Realized(object sender, EventArgs e)
        {
            vpaned1.Position = vpaned1.Parent.Allocation.Height / 2;
        }

        private void Frame1_Unrealized(object sender, EventArgs e)
        {
        }

        private void MainWindow_SetFocus(object o, SetFocusArgs args)
        {
            if (MasterView.MainWindow != null)
                MasterView.MainWindow.Focus(0);
        }

        /// <summary>
        /// Populate the view given the specified text.
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="editingEnabled"></param>
        /// <returns></returns>
        private void PopulateView(string contents, bool isURI= false)
        {
            if (browser == null)
                browser = new WebKitBrowser(vbox2);
            if (isURI)
                browser.Navigate(contents);
            else
               browser.LoadHTML(contents);

            /////STYLE
            /////browser.Font = (MasterView as ViewBase).MainWidget.Style.FontDescription;
            /////browser.BackgroundColour = Utility.Colour.FromGtk(MainWidget.Style.Background(StateType.Normal));
            /////browser.ForegroundColour = Utility.Colour.FromGtk(MainWidget.Style.Foreground(StateType.Normal));

            //browser.Navigate("http://blend-bp.nexus.csiro.au/wiki/index.php");
        }

        /// <summary>
        /// Turn the editor on or off.
        /// </summary>
        /// <param name="turnOn">Whether or not the editor should be turned on.</param>
        private void TurnEditorOn(bool turnOn)
        {
            hbox1.Visible = turnOn;
        }

        /// <summary>
        /// Toggle edit / preview mode.
        /// </summary>
        private void ToggleEditMode()
        {
            bool editorIsOn = hbox1.Visible;
            TurnEditorOn(!editorIsOn);   // toggle preview / edit mode.
        }

        /// <summary>
        /// User has clicked 'edit'
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event argument.</param>
        private void OnEditClick(object sender, EventArgs e)
        {
            TurnEditorOn(true);
        }

        /// <summary>
        /// Text has been changed.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event argument.</param>
        private void TextUpdate(object sender, EventArgs e)
        {
            Markdown markDown = new Markdown();
            string html = markDown.Transform(memo.MemoText);
            html = ParseHtmlImages(html);
            PopulateView(html);
        }

        /// <summary>
        /// Checks the src attribute for all images in the HTML, and attempts to
        /// find a resource of the same name. If the resource exists, it is
        /// written to a temporary file and the image's src is changed to point
        /// to the temp file.
        /// </summary>
        /// <param name="html">String containing valid HTML.</param>
        /// <returns>The modified HTML.</returns>
        private static string ParseHtmlImages(string html)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            // Find images via xpath.
            var images = doc.DocumentNode.SelectNodes(@"//img");
            if (images != null)
            {
                foreach (HtmlNode image in images)
                {
                    string src = image.GetAttributeValue("src", null);
                    if (!File.Exists(src) && !string.IsNullOrEmpty(src))
                    {
                        string tempFileName = HtmlToMigraDoc.GetImagePath(src, Path.GetTempPath());
                        if (!string.IsNullOrEmpty(tempFileName))
                            image.SetAttributeValue("src", tempFileName);
                    }
                }
            }
            return doc.DocumentNode.OuterHtml;
        }

        /// <summary>
        /// User has clicked the help button. 
        /// Opens a web browser (outside of APSIM) and navigates to a help page on the Next Gen site.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event argument.</param>
        private void OnHelpClick(object sender, EventArgs e)
        {
            Process.Start("https://www.apsim.info/Documentation/APSIM(nextgeneration)/Memo.aspx");
        }
    }
}
