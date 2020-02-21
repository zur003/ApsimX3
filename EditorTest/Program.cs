using System;
using Gtk;
using Mono.TextEditor;

namespace EditorTest
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]

        public static int Main(string[] args)
        {
            Gtk.Application.Init();
            Gtk.Window mainWindow = new MainWindow();

            mainWindow.ShowAll();
            mainWindow.DeleteEvent += MainWindow_DeleteEvent;
            Application.Run();
            return 0;
        }

        private static void MainWindow_DeleteEvent(object o, DeleteEventArgs args)
        {
            Application.Quit();
        }

        public class MainWindow: Gtk.Window
        {
            public MainWindow(): base (Gtk.WindowType.Toplevel)
            {

                TextEditor textEditor = new TextEditor();
                this.Add(textEditor);
                CodeSegmentPreviewWindow.CodeSegmentPreviewInformString = "";
                TextEditorOptions options = new Mono.TextEditor.TextEditorOptions();
                options.HighlightCaretLine = true;
                options.EnableSyntaxHighlighting = true;
                options.HighlightMatchingBracket = true;
                textEditor.Options = options;
                textEditor.Document.MimeType = "text/x-csharp";
                textEditor.Text = "Hello, world!";
                textEditor.ShowAll();
            }
        }
    }
}
