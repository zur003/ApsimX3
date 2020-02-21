using Gtk;
using GtkSharp.SourceView;
using System;

namespace GtkSourceViewTest
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

            Window window = new Window("GtkSourceViewSharp Test");
            window.SetSizeRequest(800, 600);
            window.DeleteEvent += Window_DeleteEvent;
            ScrolledWindow ScrollWindow = new ScrolledWindow();

            GtkSourceBuffer buffer = new GtkSourceBuffer();
            GtkSourceView view = new GtkSourceView(buffer);
            view.ShowLineMarks = true;
            view.ShowLineNumbers = true;
            view.HighlightCurrentLine = true;
            view.AutoIndent = true;
            view.InsertSpacesInsteadOfTabs = true;

            CssProvider cssProvider = new CssProvider();
            cssProvider.LoadFromData("textview { font-family: Monospace; font-size: 10pt; }");
            view.StyleContext.AddProvider(cssProvider, StyleProviderPriority.Theme);

            ScrollWindow.Add(view);
            window.Add(ScrollWindow);
            OpenFile(buffer, @"D:\Projects\ApsimX3\GtkSourceViewTest\Program.cs");

            window.ShowAll();

            Application.Run();

        }

        static bool OpenFile(GtkSourceBuffer buffer, string filename)
        {
            GtkSourceLanguageManager lm = new GtkSourceLanguageManager();
            GtkSourceLanguage language = lm.GetLanguage("c-sharp");
            language = lm.GuessLanguage(filename, "text/x-csrc");

            if (language == null)
            {
                Console.WriteLine("Could not determine language");
                buffer.HighlightSyntax = false;
            }
            else
            {
                Console.WriteLine("Language: " + language.Name);
                buffer.Language = language;
                buffer.HighlightSyntax = true;
                buffer.HighlightMatchingBrackets = true;
            }

            buffer.BeginNotUndoableAction();
            buffer.Text = System.IO.File.ReadAllText(filename);
            buffer.EndNotUndoableAction();
            buffer.Modified = false;
            buffer.PlaceCursor(buffer.StartIter);
            return true;
        }

        private static void Window_DeleteEvent(object o, DeleteEventArgs args)
        {
            Application.Quit();
        }
    }
}

