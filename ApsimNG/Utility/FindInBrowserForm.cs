﻿namespace Utility
{
	using System;
    using Gtk;
    using UserInterface.Views;

    public class FindInBrowserForm
    {

		// Gtk Widgets
        private Window window1 = null;
        private CheckButton chkMatchCase = null;
        private CheckButton chkHighlightAll = null;
        private Entry txtLookFor = null;
        private Button btnCancel = null;
        private Button btnFindPrevious = null;
        private Button btnFindNext = null;
        private IBrowserWidget browser = null;


        public FindInBrowserForm()
        {
            Builder builder = ViewBase.BuilderFromResource("ApsimNG.Resources.Glade.BrowserFind.glade");
            window1 = (Window)builder.GetObject("window1");
            chkMatchCase = (CheckButton)builder.GetObject("chkMatchCase");
            chkHighlightAll = (CheckButton)builder.GetObject("chkHighlightAll");
            txtLookFor = (Entry)builder.GetObject("txtLookFor");
            btnCancel = (Button)builder.GetObject("btnCancel");
            btnFindPrevious = (Button)builder.GetObject("btnFindPrevious");
            btnFindNext = (Button)builder.GetObject("btnFindNext");

			txtLookFor.Changed += TxtLookFor_Changed;
            btnFindNext.Clicked += BtnFindNext_Click;
            btnFindPrevious.Clicked += BtnFindPrevious_Click;
            btnCancel.Clicked += BtnCancel_Click;
            chkHighlightAll.Clicked += ChkHighlightAll_Click;
			chkHighlightAll.Visible = false; // Hide this for now...
			chkHighlightAll.NoShowAll = true;
            window1.DeleteEvent += Window1_DeleteEvent;
            window1.Destroyed += Window1_Destroyed;
            AccelGroup agr = new AccelGroup();
            btnCancel.AddAccelerator("activate", agr, new AccelKey(Gdk.Key.Escape, Gdk.ModifierType.None, AccelFlags.Visible));
            window1.AddAccelGroup(agr);
        }


        private void Window1_Destroyed(object sender, EventArgs e)
        {
			txtLookFor.Changed -= TxtLookFor_Changed;
            btnFindNext.Clicked -= BtnFindNext_Click;
            btnFindPrevious.Clicked -= BtnFindPrevious_Click;
            btnCancel.Clicked -= BtnCancel_Click;
            chkHighlightAll.Clicked -= ChkHighlightAll_Click;
            window1.DeleteEvent -= Window1_DeleteEvent;
            window1.Destroyed -= Window1_Destroyed;
        }

        public void Destroy()
        {
            window1.Destroy();
        }

        private void Window1_DeleteEvent(object o, DeleteEventArgs args)
        {
            window1.Hide();
            args.RetVal = true;
        }

        /// <summary>
        /// Show an error message to caller.
        /// </summary>
        public void ShowMsg(string message)
        {
			if (browser != null)
			{
                MessageDialog md = new MessageDialog(browser.HoldingWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, message);
                md.Run();
                md.Destroy();
			}
        }

        public void ShowFor(IBrowserWidget browser)
        {
            this.browser = browser;
            window1.TransientFor = this.browser.HoldingWidget.Toplevel as Window;
            window1.Parent = this.browser.HoldingWidget.Toplevel;
            window1.WindowPosition = WindowPosition.CenterOnParent;
            window1.Show();
            txtLookFor.GrabFocus();
        }

		void TxtLookFor_Changed(object sender, EventArgs e)
        {
            // No, this isn't quite right. It keeps searching forward, rather than resting on the current selection
            // when the current selection matches. Disabling until a better way is found
			// Find();
        }

        private void BtnFindPrevious_Click(object sender, EventArgs e)
        {
            FindNext(false, "Text not found");
        }

        private void BtnFindNext_Click(object sender, EventArgs e)
        {
            FindNext(true, "Text not found");
        }

        public void FindNext(bool searchForward, string messageIfNotFound)
        {
			ChkHighlightAll_Click(this, new EventArgs());
            if (string.IsNullOrEmpty(txtLookFor.Text))
            {
                ShowMsg("No string specified to for search!");
                return;
            }
            if (!browser.Search(txtLookFor.Text, searchForward, chkMatchCase.Active, true))
			{
				if (!string.IsNullOrEmpty(messageIfNotFound))
				    ShowMsg(messageIfNotFound);
			}
                    
        }

		public void Find()
		{
			if (!string.IsNullOrEmpty(txtLookFor.Text))
				FindNext(true,"");
		}

		private bool isHighlighted = false;

        private void ChkHighlightAll_Click(object sender, EventArgs e)
        {
			if (browser is WebKitBrowser)
			{
				bool highlight = chkHighlightAll.Active;
				if (highlight != isHighlighted)
				{
					(browser as WebKitBrowser).Highlight(txtLookFor.Text, chkMatchCase.Active, highlight);
					isHighlighted = highlight;
				}
			}
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
			// if (browser is TWWebBrowserWK)
			//	(browser as TWWebBrowserWK).Highlight("", false, false);
            window1.Hide();
        }

        public string LookFor { get { return txtLookFor.Text; } }
    }

}

    
