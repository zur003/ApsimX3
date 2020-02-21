﻿// -----------------------------------------------------------------------
// <copyright file="EditorView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using EventArguments;
    using Gtk;
#if SOURCEVIEW    
    using GtkSharp.SourceView;
#else
    using Mono.TextEditor;
#endif
    using Utility;
    using Presenters;
    using Cairo;
    using System.Globalization;

    /// <summary>
    /// This is IEditorView interface
    /// </summary>
    public interface IEditorView
    {
        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        event EventHandler<NeedContextItemsArgs> ContextItemsNeeded;

        /// <summary>
        /// Invoked when the user changes the text in the editor.
        /// </summary>
        event EventHandler TextHasChangedByUser;

        /// <summary>
        /// Invoked when the user leaves the text editor.
        /// </summary>
        event EventHandler LeaveEditor;

        /// <summary>
        /// Invoked when the user changes the style.
        /// </summary>
        event EventHandler StyleChanged;

        /// <summary>
        /// Gets or sets the text property to get and set the content of the editor.
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// Gets or sets the lines property to get and set the lines in the editor.
        /// </summary>
        string[] Lines { get; set; }

        /// <summary>
        /// Gets or sets the characters that bring up the intellisense context menu.
        /// </summary>
        string IntelliSenseChars { get; set; }

        /// <summary>
        /// Gets the current line number
        /// </summary>
        int CurrentLineNumber { get; }

        /// <summary>
        /// Gets or sets the current location of the caret (column and line)
        /// </summary>
        System.Drawing.Rectangle Location { get; set; }
        
        /// <summary>
        /// Indicates whether we are editing a script, rather than "ordinary" text.
        /// </summary>
        bool ScriptMode { get; set; }

        /// <summary>
        /// Add a separator line to the context menu
        /// </summary>
        MenuItem AddContextSeparator();

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        /// <param name="shortcut">Describes the key to use as the accelerator</param>
        MenuItem AddContextActionWithAccel(string menuItemText, System.EventHandler onClick, string shortcut);

        /// <summary>
        /// Offset of the caret from the beginning of the text editor.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Returns true iff this text editor has the focus
        /// (ie it can receive keyboard input).
        /// </summary>
        bool HasFocus { get; }

        /// <summary>
        /// Inserts text at a given offset in the editor.
        /// </summary>
        /// <param name="text">Text to be inserted.</param>
        void InsertAtCaret(string text);

        /// <summary>
        /// Inserts a new completion option at the caret, potentially overwriting a partially-completed word.
        /// </summary>
        /// <param name="triggerWord">
        /// Word to be overwritten. May be empty.
        /// This function will overwrite the last occurrence of this word before the caret.
        /// </param>
        /// <param name="completionOption">Completion option to be inserted.</param>
        void InsertCompletionOption(string completionOption, string triggerWord);

        /// <summary>
        /// Gets the location (in screen coordinates) of the cursor.
        /// </summary>
        /// <returns>Tuple, where item 1 is the x-coordinate and item 2 is the y-coordinate.</returns>
        System.Drawing.Point GetPositionOfCursor();

        /// <summary>
        /// Redraws the text editor.
        /// </summary>
        void Refresh();
    }

    /// <summary>
    /// This class provides an intellisense editor and has the option of syntax highlighting keywords.
    /// </summary>
    public class EditorView : ViewBase, IEditorView
    {
        /// <summary>
        /// The find-and-replace form
        /// </summary>
        private FindAndReplaceForm findForm = new FindAndReplaceForm();

        /// <summary>
        /// Scrolled window
        /// </summary>
        private ScrolledWindow scroller;

#if SOURCEVIEW
        /// <summary>
        /// The main text editor
        /// </summary>
        ///// private TextEditor textEditor;
        private GtkSourceView textEditor;

        /// <summary>
        /// Text buffer for the source view editor
        /// </summary>
        private GtkSourceBuffer textBuffer;
#else
        /// <summary>
        /// The main text editor
        /// </summary>
        private TextEditor textEditor;
#endif

        /// <summary>
        /// The popup menu options on the editor
        /// </summary>
        private Menu popupMenu = new Menu();

        /// <summary>
        /// Menu accelerator group
        /// </summary>
        private AccelGroup accel = new AccelGroup();

        /// <summary>
        /// Horizontal scroll position
        /// </summary>
        private int horizScrollPos = -1;

        /// <summary>
        /// Vertical scroll position
        /// </summary>
        private int vertScrollPos = -1;

        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        public event EventHandler<NeedContextItemsArgs> ContextItemsNeeded;

        /// <summary>
        /// Invoked when the user changes the text in the editor.
        /// </summary>
        public event EventHandler TextHasChangedByUser;

        /// <summary>
        /// Invoked when the user leaves the text editor.
        /// </summary>
        public event EventHandler LeaveEditor;

        /// <summary>
        /// Invoked when the user changes the style.
        /// </summary>
        public event EventHandler StyleChanged;

        /// <summary>
        /// Gets or sets the text property to get and set the content of the editor.
        /// </summary>
        public string Text
        {
            get
            {
#if SOURCEVIEW
                return textBuffer.Text;
#else
                return textEditor.Text;
#endif
            }

            set
            {
#if SOURCEVIEW
                textBuffer.Text = value;
                if (ScriptMode)
                {
                    GtkSourceLanguageManager lm = new GtkSourceLanguageManager();
                    GtkSourceLanguage language = lm.GetLanguage("c-sharp");
                    textBuffer.Language = language;
                    textBuffer.HighlightSyntax = true;
                    textBuffer.HighlightMatchingBrackets = true;
                }
            }
#else
                textEditor.Text = value;
                if (ScriptMode)
                    textEditor.Document.MimeType = "text/x-csharp";
            }
#endif
        }

        /// <summary>
        /// Gets or sets the lines in the editor.
        /// </summary>
        public string[] Lines
        {
            get
            {
#if SOURCEVIEW
                string text = textBuffer.Text.TrimEnd("\r\n".ToCharArray());
                return text.Split(new string[] { Environment.NewLine, "\r\n", "\n" }, StringSplitOptions.None);
#else
                string text = textEditor.Text.TrimEnd("\r\n".ToCharArray());
                return text.Split(new string[] { textEditor.EolMarker, "\r\n", "\n" }, StringSplitOptions.None);
#endif
            }

            set
            {
                string st = string.Empty;
                if (value != null)
                {
                    foreach (string avalue in value)
                    {
                        if (st != string.Empty)
#if SOURCEVIEW
                            st += Environment.NewLine;
#else
                            st += textEditor.EolMarker;
#endif
                        st += avalue;
                    }
                }
                Text = st;
            }
        }

        /// <summary>
        /// Gets or sets the characters that bring up the intellisense context menu.
        /// </summary>
        public string IntelliSenseChars { get; set; }

        /// <summary>
        /// Indicates whether we are editing a script, rather than "ordinary" text.
        /// </summary>
        public bool ScriptMode { get; set; }

        /// <summary>
        /// Gets the current line number
        /// </summary>
        public int CurrentLineNumber
        {
            get
            {
#if SOURCEVIEW
                return textBuffer.GetIterAtOffset(textBuffer.CursorPosition).Line;
#else
                return textEditor.Caret.Line;
#endif
            }
        }

        private MenuItem styleMenu;
        private MenuItem styleSeparator;

        /// <summary>
        /// Gets or sets the current location of the caret (column and line) and the current scrolling position
        /// This isn't really a Rectangle, but the Rectangle class gives us a convenient
        /// way to store these values.
        /// </summary>
        public System.Drawing.Rectangle Location
        {
            get
            {
#if SOURCEVIEW
                TextIter iter = textBuffer.GetIterAtOffset(textBuffer.CursorPosition);
                return new System.Drawing.Rectangle(iter.LineOffset, iter.Line, Convert.ToInt32(scroller.Hadjustment.Value, CultureInfo.InvariantCulture), Convert.ToInt32(scroller.Vadjustment.Value, CultureInfo.InvariantCulture));
#else
                DocumentLocation loc = textEditor.Caret.Location;
                return new System.Drawing.Rectangle(loc.Column, loc.Line, Convert.ToInt32(scroller.Hadjustment.Value, CultureInfo.InvariantCulture), Convert.ToInt32(scroller.Vadjustment.Value, CultureInfo.InvariantCulture));
#endif
            }

            set
            {
#if SOURCEVIEW
                TextIter iter = textBuffer.StartIter;
                iter.Line = value.Y;
                iter.LineOffset = value.X;
                textBuffer.PlaceCursor(iter);
#else
                textEditor.Caret.Location = new DocumentLocation(value.Y, value.X);
#endif
                horizScrollPos = value.Width;
                vertScrollPos = value.Height;

                // Unfortunately, we often can't set the scroller adjustments immediately, as they may not have been set up yet
                // We make these calls to set the position if we can, but otherwise we'll just hold on to the values until the scrollers are ready
                Hadjustment_Changed(this, null);
                Vadjustment_Changed(this, null);
            }
        }

        /// <summary>
        /// Offset of the caret from the beginning of the text editor.
        /// </summary>
        public int Offset
        {
            get
            {
#if SOURCEVIEW
                return textBuffer.CursorPosition;
#else
                return textEditor.Caret.Offset;
#endif
            }
        }

        /// <summary>
        /// Returns true iff this text editor has the focus
        /// (ie it can receive keyboard input).
        /// </summary>
        public bool HasFocus
        {
            get
            {
                return textEditor.HasFocus;
            }
        }

        /// <summary>
        /// Default constructor that configures the Completion form.
        /// </summary>
        /// <param name="owner">The owner view</param>
        public EditorView(ViewBase owner) : base(owner)
        {
            scroller = new ScrolledWindow();
#if SOURCEVIEW
            textBuffer = new GtkSourceBuffer();
            textEditor = new GtkSourceView(textBuffer);
#else
            textEditor = new TextEditor();
#endif
            scroller.Add(textEditor);
            mainWidget = scroller;
#if SOURCEVIEW
            textEditor.ShowLineMarks = true;
            textEditor.ShowLineNumbers = true;
            textEditor.HighlightCurrentLine = true;
            textEditor.AutoIndent = true;
            textEditor.InsertSpacesInsteadOfTabs = true;
            textEditor.FocusInEvent += OnTextBoxEnter;
            textEditor.FocusOutEvent += OnTextBoxLeave;
            textBuffer.Changed += OnTextHasChanged;
            textEditor.KeyPressEvent += OnKeyPress;
            // textEditor.Popup += DoPopup;

            CssProvider cssProvider = new CssProvider();
            cssProvider.LoadFromData("textview { font-family: Monospace; font-size: 10pt; }");
            textEditor.StyleContext.AddProvider(cssProvider, StyleProviderPriority.Theme);
#else
            Mono.TextEditor.CodeSegmentPreviewWindow.CodeSegmentPreviewInformString = "";
            Mono.TextEditor.TextEditorOptions options = new Mono.TextEditor.TextEditorOptions();
            options.EnableSyntaxHighlighting = true;
            options.ColorScheme = Configuration.Settings.EditorStyleName;
            options.Zoom = Configuration.Settings.EditorZoom;
            options.HighlightCaretLine = true;
            options.EnableSyntaxHighlighting = true;
            options.HighlightMatchingBracket = true;
            textEditor.Options = options;
            textEditor.Options.Changed += EditorOptionsChanged;
            textEditor.Options.ColorScheme = Configuration.Settings.EditorStyleName;
            textEditor.Options.Zoom = Configuration.Settings.EditorZoom;
            textEditor.TextArea.DoPopupMenu = DoPopup;
            textEditor.Document.LineChanged += OnTextHasChanged;
            textEditor.TextArea.FocusInEvent += OnTextBoxEnter;
            textEditor.TextArea.FocusOutEvent += OnTextBoxLeave;
            textEditor.TextArea.KeyPressEvent += OnKeyPress;
#endif
            scroller.Hadjustment.Changed += Hadjustment_Changed;
            scroller.Vadjustment.Changed += Vadjustment_Changed;
            mainWidget.Destroyed += _mainWidget_Destroyed;

            AddContextActionWithAccel("Cut", OnCut, "Ctrl+X");
            AddContextActionWithAccel("Copy", OnCopy, "Ctrl+C");
            AddContextActionWithAccel("Paste", OnPaste, "Ctrl+V");
            AddContextActionWithAccel("Delete", OnDelete, "Delete");
            AddContextSeparator();
            AddContextActionWithAccel("Undo", OnUndo, "Ctrl+Z");
            AddContextActionWithAccel("Redo", OnRedo, "Ctrl+Y");
            AddContextActionWithAccel("Find", OnFind, "Ctrl+F");
            AddContextActionWithAccel("Replace", OnReplace, "Ctrl+H");
            styleSeparator = AddContextSeparator();
            styleMenu = AddMenuItem("Use style", null);
            Menu styles = new Menu();

            // find all the editor styles and add sub menu items to the popup
            string[] styleNames = Mono.TextEditor.Highlighting.SyntaxModeService.Styles;
            Array.Sort(styleNames, StringComparer.InvariantCulture);
            foreach (string name in styleNames)
            {
                CheckMenuItem subItem = new CheckMenuItem(name);
#if !SOURCEVIEW
                if (string.Compare(name, options.ColorScheme, true) == 0)
                    subItem.Toggle();
#endif
                subItem.Activated += OnChangeEditorStyle;
                subItem.Visible = true;
                styles.Append(subItem);
            }
            styleMenu.Submenu = styles;

            IntelliSenseChars = ".";
        }

        /// <summary>
        /// Cleanup events
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
#if SOURCEVIEW
            textEditor.FocusInEvent -= OnTextBoxEnter;
            textEditor.FocusOutEvent -= OnTextBoxLeave;
            textBuffer.Changed -= OnTextHasChanged;
            textEditor.KeyPressEvent -= OnKeyPress;
            // textEditor.Popup -= DoPopup;
#else
            textEditor.Document.LineChanged -= OnTextHasChanged;
            textEditor.TextArea.FocusInEvent -= OnTextBoxEnter;
            textEditor.TextArea.FocusOutEvent -= OnTextBoxLeave;
            textEditor.TextArea.KeyPressEvent -= OnKeyPress;
            scroller.Hadjustment.Changed -= Hadjustment_Changed;
            scroller.Vadjustment.Changed -= Vadjustment_Changed;
            textEditor.Options.Changed -= EditorOptionsChanged;
#endif
            mainWidget.Destroyed -= _mainWidget_Destroyed;

            // It's good practice to disconnect all event handlers, as it makes memory leaks
            // less likely. However, we may not "own" the event handlers, so how do we 
            // know what to disconnect?
            // We can do this via reflection. Here's how it currently can be done in Gtk#.
            // Windows.Forms would do it differently.
            // This may break if Gtk# changes the way they implement event handlers.
            foreach (Widget w in popupMenu)
                w.FreeSignals();
            popupMenu.Dispose();
            accel.Dispose();
            textEditor.Dispose();
            textEditor = null;
            findForm.Destroy();
            owner = null;
        }

        /// <summary>
        /// The vertical position has changed
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void Vadjustment_Changed(object sender, EventArgs e)
        {
            if (vertScrollPos > 0 && vertScrollPos < scroller.Vadjustment.Upper)
            {
                scroller.Vadjustment.Value = vertScrollPos;
                scroller.Vadjustment.ChangeValue();
                vertScrollPos = -1;
            }
        }

        /// <summary>
        /// The horizontal position has changed
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void Hadjustment_Changed(object sender, EventArgs e)
        {
            if (horizScrollPos > 0 && horizScrollPos < scroller.Hadjustment.Upper)
            {
                scroller.Hadjustment.Value = horizScrollPos;
                scroller.Hadjustment.ChangeValue();
                horizScrollPos = -1;
            }
        }

        /// <summary>
        /// Preprocesses key strokes so that the ContextList can be displayed when needed. 
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Key arguments</param>
        [GLib.ConnectBefore] // Otherwise this is handled internally, and we won't see it
        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            e.RetVal = false;
            char keyChar = (char)Gdk.Keyval.ToUnicode(e.Event.KeyValue);
            Gdk.ModifierType ctlModifier = !APSIM.Shared.Utilities.ProcessUtilities.CurrentOS.IsMac ? Gdk.ModifierType.ControlMask
                //Mac window manager already uses control-scroll, so use command
                //Command might be either meta or mod1, depending on GTK version
                : (Gdk.ModifierType.MetaMask | Gdk.ModifierType.Mod1Mask);

            bool controlSpace = IsControlSpace(e.Event);
            bool controlShiftSpace = IsControlShiftSpace(e.Event);
#if SOURCEVIEW
            string textBeforePeriod = GetWordBeforePosition(textBuffer.CursorPosition);
#else
            string textBeforePeriod = GetWordBeforePosition(textEditor.Caret.Offset);
#endif
            double x; // unused, but needed as an out parameter.
            if (e.Event.Key == Gdk.Key.F3)
            {
                if (string.IsNullOrEmpty(findForm.LookFor))
#if SOURCEVIEW
                    ;
                /////                    findForm.ShowFor(textBuffer, false);
#else
                    findForm.ShowFor(textEditor, false);
#endif
                else
                    findForm.FindNext(true, (e.Event.State & Gdk.ModifierType.ShiftMask) == 0, string.Format("Search text «{0}» not found.", findForm.LookFor));
                e.RetVal = true;
            }
            // If the text before the period is not a number and the user pressed either one of the intellisense characters or control-space:
            else if (!double.TryParse(textBeforePeriod.Replace(".", ""), out x) && (IntelliSenseChars.Contains(keyChar.ToString()) || controlSpace || controlShiftSpace) )
            {
                // If the user entered a period, we need to take that into account when generating intellisense options.
                // To do this, we insert a period manually and stop the Gtk signal from propagating further.
                e.RetVal = true;
                if (keyChar == '.')
                {
#if SOURCEVIEW
                    textBuffer.InsertAtCursor(keyChar.ToString());
#else
                    textEditor.InsertAtCaret(keyChar.ToString());
#endif

                    // Process all events in the main loop, so that the period is inserted into the text editor.
                    while (GLib.MainContext.Iteration()) ;
                }
                NeedContextItemsArgs args = new NeedContextItemsArgs
                {
#if SOURCEVIEW
                    Coordinates = GetPositionOfCursor(),
                    Code = textBuffer.Text,
                    Offset = this.Offset,
                    ControlSpace = controlSpace,
                    ControlShiftSpace = controlShiftSpace,
                    LineNo = textBuffer.GetIterAtOffset(textBuffer.CursorPosition).Line,
                    ColNo = textBuffer.GetIterAtOffset(textBuffer.CursorPosition).LineOffset - 1
#else
                    Coordinates = GetPositionOfCursor(),
                    Code = textEditor.Text,
                    Offset = this.Offset,
                    ControlSpace = controlSpace,
                    ControlShiftSpace = controlShiftSpace,
                    LineNo = textEditor.Caret.Line,
                    ColNo = textEditor.Caret.Column - 1
#endif
                };

                ContextItemsNeeded?.Invoke(this, args);
            }
#if !SOURCEVIEW
            else if ((e.Event.State & ctlModifier) != 0)
            {
                switch (e.Event.Key)
                {
                    case Gdk.Key.Key_0: textEditor.Options.ZoomReset(); e.RetVal = true; break;
                    case Gdk.Key.KP_Add:
                    case Gdk.Key.plus: textEditor.Options.ZoomIn(); e.RetVal = true; break;
                    case Gdk.Key.KP_Subtract:
                    case Gdk.Key.minus: textEditor.Options.ZoomOut(); e.RetVal = true; break;
                }
            }
#endif
        }

        /// <summary>
        /// Checks whether a keypress is a control+space event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        /// <returns>True iff the event represents a control+space click.</returns>
        private bool IsControlSpace(Gdk.EventKey e)
        {
            return Gdk.Keyval.ToUnicode(e.KeyValue) == ' ' && (e.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask;
        }

        /// <summary>
        /// Checks whether a keypress is a control-shift-space event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        /// <returns>True iff the event represents a control + shift + space click.</returns>
        private bool IsControlShiftSpace(Gdk.EventKey e)
        {
            return IsControlSpace(e) && (e.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;
        }

        /// <summary>
        /// Retrieve the word before the specified character position. 
        /// </summary>
        /// <param name="pos">Position in the editor</param>
        /// <returns>The position of the word</returns>
        private string GetWordBeforePosition(int pos)
        {
            if (pos == 0)
                return string.Empty;

#if SOURCEVIEW
            int posDelimiter = textBuffer.Text.LastIndexOfAny(" \r\n(+-/*".ToCharArray(), pos - 1);
            return textBuffer.Text.Substring(posDelimiter + 1, pos - posDelimiter - 1).TrimEnd(".".ToCharArray());
#else
            int posDelimiter = textEditor.Text.LastIndexOfAny(" \r\n(+-/*".ToCharArray(), pos - 1);
            return textEditor.Text.Substring(posDelimiter + 1, pos - posDelimiter - 1).TrimEnd(".".ToCharArray());
#endif
        }

        /// <summary>
        /// Gets the location (in screen coordinates) of the cursor.
        /// </summary>
        /// <returns>Tuple, where item 1 is the x-coordinate and item 2 is the y-coordinate.</returns>
        public System.Drawing.Point GetPositionOfCursor()
        {
#if SOURCEVIEW
            TextIter iter = textBuffer.GetIterAtOffset(textBuffer.CursorPosition);
            Gdk.Rectangle rect = textEditor.GetIterLocation(iter);
            int x, y;
            textEditor.BufferToWindowCoords(TextWindowType.Text, rect.X, rect.Y, out x, out y);
            return new System.Drawing.Point(x, y);
#else
            Point p = textEditor.TextArea.LocationToPoint(textEditor.Caret.Location);
            p.Y += (int)textEditor.LineHeight;
            // Need to convert to screen coordinates....
            int x, y, frameX, frameY;
            MasterView.MainWindow.GetOrigin(out frameX, out frameY);
            textEditor.TextArea.TranslateCoordinates(mainWidget.Toplevel, p.X, p.Y, out x, out y);

            return new System.Drawing.Point(x + frameX, y + frameY);
#endif
        }

        /// <summary>
        /// Redraws the text editor.
        /// </summary>
        public void Refresh()
        {
#if !SOURCEVIEW
            textEditor.Options.ColorScheme = Configuration.Settings.EditorStyleName;
#endif
            textEditor.QueueDraw();
        }

        /// <summary>
        /// Hide the completion window.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void HideCompletionWindow(object sender, EventArgs e)
        {
#if SOURCEVIEW
            textEditor.Editable = false;
#else
            textEditor.Document.ReadOnly = false;
#endif
            textEditor.GrabFocus();
        }

        /// <summary>
        /// Inserts a new completion option at the caret, potentially overwriting a partially-completed word.
        /// </summary>
        /// <param name="triggerWord">
        /// Word to be overwritten. May be empty.
        /// This function will overwrite the last occurrence of this word before the caret.
        /// </param>
        /// <param name="completionOption">Completion option to be inserted.</param>
        public void InsertCompletionOption(string completionOption, string triggerWord)
        {
            if (string.IsNullOrEmpty(completionOption))
                return;
#if !SOURCEVIEW
            // If no trigger word provided, insert at caret.
            if (string.IsNullOrEmpty(triggerWord))
            {
                int offset = Offset + completionOption.Length;
                textEditor.InsertAtCaret(completionOption);
                textEditor.Caret.Offset = offset;
                return;
            }

            // If trigger word is entire text, replace the entire text.
            if (textEditor.Text == triggerWord)
            {
                textEditor.Text = completionOption;
                textEditor.Caret.Offset = completionOption.Length;
                return;
            }

            // Overwrite the last occurrence of this word before the caret.
            int index = textEditor.GetTextBetween(0, Offset).LastIndexOf(triggerWord);
            if (index < 0)
                // If text does not contain trigger word, isnert at caret.
                textEditor.InsertAtCaret(completionOption);

            string textBeforeTriggerWord = textEditor.Text.Substring(0, index);

            string textAfterTriggerWord = "";
            if (textEditor.Text.Length > index + triggerWord.Length)
                textAfterTriggerWord = textEditor.Text.Substring(index + triggerWord.Length);

            textEditor.Text = textBeforeTriggerWord + completionOption + textAfterTriggerWord;
            textEditor.Caret.Offset = index + completionOption.Length;
#endif
        }

        /// <summary>
        /// Insert the currently selected completion item into the text box.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        public void InsertAtCaret(string text)
        {
#if !SOURCEVIEW
            textEditor.Document.ReadOnly = false;
            string textToCaret = textEditor.Text.Substring(0, Offset);
            if (textToCaret.LastIndexOf('.') != Offset - 1)
            {
                string textBeforeCaret = textEditor.Text.Substring(0, Offset);
                // TODO : insert text at the correct location
                // Currently, if we half-type a word, then hit control-space, the word will be inserted at the caret.
                textEditor.Text = textEditor.Text.Substring(0, textEditor.Text.LastIndexOf('.')) + textEditor.Text.Substring(Offset);
            }
            textEditor.InsertAtCaret(text);
            textEditor.GrabFocus();
#endif
        }

        /// <summary>
        /// User has changed text. Invoke our OnTextChanged event.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnTextHasChanged(object sender, EventArgs e)
        {
            if (TextHasChangedByUser != null)
                TextHasChangedByUser(sender, e);
        }

        /// <summary>
        /// Entering the textbox event
        /// </summary>
        /// <param name="o">The calling object</param>
        /// <param name="args">The arguments</param>
        private void OnTextBoxEnter(object o, FocusInEventArgs args)
        {
            ((o as Widget).Toplevel as Gtk.Window).AddAccelGroup(accel);
        }

        /// <summary>
        /// Leaving the textbox event
        /// </summary>
        /// <param name="o">The calling object</param>
        /// <param name="e">The event arguments</param>
        private void OnTextBoxLeave(object o, EventArgs e)
        {
            ((o as Widget).Toplevel as Gtk.Window).RemoveAccelGroup(accel);
            if (LeaveEditor != null)
                LeaveEditor.Invoke(this, e);
        }

#region Code related to Edit menu

        /// <summary>
        /// Show the popup menu
        /// </summary>
        /// <param name="b">The button</param>
        private void DoPopup(Gdk.EventButton b)
        {
            popupMenu.Popup();
        }

        /// <summary>
        /// Add a menu item to the menu
        /// </summary>
        /// <param name="menuItemText">Menu item caption</param>
        /// <param name="onClick">Event handler</param>
        /// <returns>The menu item that was created</returns>
        public MenuItem AddMenuItem(string menuItemText, System.EventHandler onClick)
        {
            MenuItem item = new MenuItem(menuItemText);
            if (onClick != null)
                item.Activated += onClick;
            popupMenu.Append(item);
            popupMenu.ShowAll();

            return item;
        }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        public MenuItem AddContextSeparator()
        {
            MenuItem result = new SeparatorMenuItem();
            popupMenu.Append(result);
            return result;
        }

        /// <summary>
        /// Add an action (on context menu) on the text area.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        /// <param name="shortcut">The shortcut string</param>
        public MenuItem AddContextActionWithAccel(string menuItemText, System.EventHandler onClick, string shortcut)
        {
            ImageMenuItem item = new ImageMenuItem(menuItemText);
            if (!string.IsNullOrEmpty(shortcut))
            {
                string keyName = string.Empty;
                Gdk.ModifierType modifier = Gdk.ModifierType.None;
                string[] keyNames = shortcut.Split(new char[] { '+' });
                foreach (string name in keyNames)
                {
                    if (name == "Ctrl")
                        modifier |= Gdk.ModifierType.ControlMask;
                    else if (name == "Shift")
                        modifier |= Gdk.ModifierType.ShiftMask;
                    else if (name == "Alt")
                        modifier |= Gdk.ModifierType.Mod1Mask;
                    else if (name == "Del")
                        keyName = "Delete";
                    else
                        keyName = name;
                }
                try
                {
                    Gdk.Key accelKey = (Gdk.Key)Enum.Parse(typeof(Gdk.Key), keyName, false);
                    item.AddAccelerator("activate", accel, (uint)accelKey, modifier, AccelFlags.Visible);
                }
                catch
                {
                }
            }
            if (onClick != null)
                item.Activated += onClick;
            popupMenu.Append(item);
            popupMenu.ShowAll();
            return item;
        }

        /// <summary>
        /// The cut menu handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnCut(object sender, EventArgs e)
        {
#if !SOURCEVIEW
            ClipboardActions.Cut(textEditor.TextArea.GetTextEditorData());
#endif
        }

        /// <summary>
        /// The Copy menu handler 
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnCopy(object sender, EventArgs e)
        {
#if !SOURCEVIEW
            ClipboardActions.Copy(textEditor.TextArea.GetTextEditorData());
#endif
        }

        /// <summary>
        /// The Past menu item handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnPaste(object sender, EventArgs e)
        {
#if !SOURCEVIEW
            ClipboardActions.Paste(textEditor.TextArea.GetTextEditorData());
#endif
        }

        /// <summary>
        /// The Delete menu handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnDelete(object sender, EventArgs e)
        {
#if !SOURCEVIEW
            DeleteActions.Delete(textEditor.TextArea.GetTextEditorData());
#endif
        }

        /// <summary>
        /// The Undo menu item handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnUndo(object sender, EventArgs e)
        {
#if !SOURCEVIEW
            MiscActions.Undo(textEditor.TextArea.GetTextEditorData());
#endif
        }

        /// <summary>
        /// The Redo menu item handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnRedo(object sender, EventArgs e)
        {
#if !SOURCEVIEW
            MiscActions.Redo(textEditor.TextArea.GetTextEditorData());
#endif
        }

        /// <summary>
        /// The Find menu item handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnFind(object sender, EventArgs e)
        {
#if !SOURCEVIEW
            findForm.ShowFor(textEditor, false);
#endif
        }

        /// <summary>
        /// The Replace menu item handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnReplace(object sender, EventArgs e)
        {
#if !SOURCEVIEW
            findForm.ShowFor(textEditor, true);
#endif
        }

        /// <summary>
        /// Changing the editor style menu item handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnChangeEditorStyle(object sender, EventArgs e)
        {
            MenuItem subItem = (MenuItem)sender;
            string caption = ((Gtk.Label)(subItem.Children[0])).LabelProp;

            foreach (CheckMenuItem item in ((Menu)subItem.Parent).Children)
            {
                item.Activated -= OnChangeEditorStyle;  // stop recursion
                item.Active = (string.Compare(caption, ((Gtk.Label)item.Children[0]).LabelProp, true) == 0);
                item.Activated += OnChangeEditorStyle;
            }

            Utility.Configuration.Settings.EditorStyleName = caption;
#if !SOURCEVIEW
            textEditor.Options.ColorScheme = caption;
#endif
            textEditor.QueueDraw();

            StyleChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handle other changes to editor options. All we're really interested in 
        /// here at present is keeping track of the editor zoom level.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void EditorOptionsChanged(object sender, EventArgs e)
        {
#if !SOURCEVIEW
            Utility.Configuration.Settings.EditorZoom = textEditor.Options.Zoom;
#endif
        }

        // The following block comes from the example code provided at 
        // http://www.codeproject.com/Articles/30936/Using-ICSharpCode-TextEditor
        // I leave it here because it provides the handlers needed for a popup menu
        // Currently find and replace functions are accessed via keystrokes (e.g, ctrl-F, F3)
        /*
        private void menuToggleBookmark_Click(object sender, EventArgs e)
        {
            DoEditAction(new ICSharpCode.TextEditor.Actions.ToggleBookmark());
            TextBox.IsIconBarVisible = TextBox.Document.BookmarkManager.Marks.Count > 0;
        }

        private void menuGoToNextBookmark_Click(object sender, EventArgs e)
        {
            DoEditAction(new ICSharpCode.TextEditor.Actions.GotoNextBookmark
                (bookmark => true));
        }

        private void menuGoToPrevBookmark_Click(object sender, EventArgs e)
        {
            DoEditAction(new ICSharpCode.TextEditor.Actions.GotoPrevBookmark
                (bookmark => true));
        }
        */

#endregion
    }
}
