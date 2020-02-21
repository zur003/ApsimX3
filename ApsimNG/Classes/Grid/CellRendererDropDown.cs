using System;
using Gtk;

namespace UserInterface.Classes
{
    /// <summary>
    /// This is an attempt to extend the default CellRendererComob widget to allow
    /// a drop-down arrow to be visible at all times, rather than just when editing.
    /// </summary>
    public class CellRendererDropDown : CellRendererCombo
    {
        /// <summary>
        /// Render the cell in the window.
        /// </summary>
        /// <param name="window">The owning window.</param>
        /// <param name="widget">The widget.</param>
        /// <param name="background_area">Background area.</param>
        /// <param name="cell_area">The cell area.</param>
        /// <param name="expose_area">Expose the area.</param>
        /// <param name="flags">Render flags.</param>
        protected override void OnRender(Cairo.Context cr, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, CellRendererState flags)
        {
            base.OnRender(cr, widget, background_area, cell_area, flags);
            widget.StyleContext.RenderArrow(cr, Math.PI, Math.Max(cell_area.X, cell_area.X + cell_area.Width - 20), cell_area.Y, 20.0);
        }

        protected override void OnEditingStarted(ICellEditable editable, string path)
        {
            base.OnEditingStarted(editable, path);
            editable.EditingDone += EditableEditingDone;
        }

        private void EditableEditingDone(object sender, EventArgs e)
        {
            if (sender is ICellEditable)
            {
                (sender as ICellEditable).EditingDone -= EditableEditingDone;
                if (sender is Widget && (sender as Widget).Parent is Gtk.TreeView)
                {
                    Gtk.TreeView view = (sender as Widget).Parent as Gtk.TreeView;
                    view.GrabFocus();
                }
            }
        }
    }
}
