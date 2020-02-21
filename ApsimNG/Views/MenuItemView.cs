﻿namespace UserInterface.Views
{
    using Gtk;
    using System;

    /// <summary>Interface for a menu item.</summary>
    public interface IMenuItemView
    {
        /// <summary>Menu clicked event.</summary>
        event EventHandler Clicked;

        /// <summary>Gets or set the checked status of the menu item.</summary>
        bool Checked { get; set; }
    }



    /// <summary>Encapsulates a menu item.</summary>
    public class MenuItemView : IMenuItemView
    {
        private Gtk.MenuItem menuItem;

        /// <summary>Constructor</summary>
        public MenuItemView(Gtk.MenuItem item)
        {
            menuItem = item;
            menuItem.Activated += OnMenuClicked;
        }

        /// <summary>Menu clicked event.</summary>
        public event EventHandler Clicked;

        /// <summary>Gets or set the checked status of the menu item.</summary>
        public bool Checked
        {
            get
            {
                if (menuItem is CheckMenuItem)
                    return (menuItem as CheckMenuItem).Active;
                else
                    return false;
            }
            set
            {
                if (menuItem is CheckMenuItem)
                    (menuItem as CheckMenuItem).Active = value;
            }
        }

        /// <summary>Destroy the menu</summary>
        public void Destroy()
        {
            menuItem.Activated -= OnMenuClicked;
            menuItem.Dispose();
        }

        

        private void OnMenuClicked(object sender, EventArgs e)
        {
            Clicked?.Invoke(this, e);
        }

    }
}
