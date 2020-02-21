﻿// -----------------------------------------------------------------------
// <copyright file="ToolStripView.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using Gtk;
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Encapsulates a toolstrip (button bar)
    /// </summary>
    public class ToolStripView : IToolStripView
    {
        private Toolbar toolStrip = null;

        /// <summary>Constructor</summary>
        public ToolStripView(Toolbar toolbar)
        {
            toolStrip = toolbar;
        }

        /// <summary>Destroy the toolstrip</summary>
        public void Destroy()
        {
            foreach (Widget child in toolStrip.Children)
            {
                child.FreeSignals();
                toolStrip.Remove(child);
                child.Dispose();
            }
        }

        /// <summary>Populate the main menu tool strip.</summary>
        /// <param name="menuDescriptions">Descriptions for each item.</param>
        public void Populate(List<MenuDescriptionArgs> menuDescriptions)
        {
            foreach (Widget child in toolStrip.Children)
            {
                toolStrip.Remove(child);
                child.Dispose();
            }
            foreach (MenuDescriptionArgs description in menuDescriptions)
            {
                Gtk.Image image = null;
                Gdk.Pixbuf pixbuf = null;
                ManifestResourceInfo info = Assembly.GetExecutingAssembly().GetManifestResourceInfo(description.ResourceNameForImage);

                if (info != null)
                {
                    pixbuf = new Gdk.Pixbuf(null, description.ResourceNameForImage, 20, 20);
                    image = new Gtk.Image(pixbuf);
                }
                ToolItem item = new ToolItem();
                item.Expand = true;

                if (description.OnClick == null)
                {
                    Label toolbarlabel = new Label();
                    if (description.RightAligned)
                        toolbarlabel.Xalign = 1.0F;
                    toolbarlabel.Xpad = 10;
                    toolbarlabel.ModifyFg(StateType.Normal, new Gdk.Color(0x99, 0x99, 0x99));
                    toolbarlabel.Text = description.Name;
                    toolbarlabel.TooltipText = description.ToolTip;
                    toolbarlabel.Visible = !String.IsNullOrEmpty(toolbarlabel.Text);
                    item.Add(toolbarlabel);
                    toolStrip.Add(item);
                    toolStrip.ShowAll();
                }
                else
                {
                    ToolButton button = new ToolButton(image, description.Name);
                    button.Homogeneous = false;
                    button.LabelWidget = new Label(description.Name);
                    button.Clicked += description.OnClick;
                    item = button;
                }
                toolStrip.Add(item);
            }
            toolStrip.ShowAll();
        }
    }
}
