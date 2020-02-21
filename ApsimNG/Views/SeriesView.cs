﻿// -----------------------------------------------------------------------
// <copyright file="SeriesView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using Interfaces;
    using Gtk;

    /// <summary>This view allows a single series to be edited.</summary>
    public class SeriesView : ViewBase, ISeriesView
    {
        private Table table1 = null;
        private VBox vbox1 = null;
        private Label label4 = null;
        private Label label5 = null;

        private DropDownView checkpointDropDown;
        private DropDownView dataSourceDropDown;
        private DropDownView xDropDown;
        private DropDownView yDropDown;
        private DropDownView x2DropDown;
        private DropDownView y2DropDown;
        private DropDownView seriesDropDown;
        private DropDownView lineTypeDropDown;
        private DropDownView markerTypeDropDown;
        private ColourDropDownView colourDropDown;
        private DropDownView lineThicknessDropDown;
        private DropDownView markerSizeDropDown;
        private CheckBoxView checkBoxView1;
        private CheckBoxView checkBoxView2;
        private CheckBoxView checkBoxView3;
        private CheckBoxView checkBoxView4;
        private CheckBoxView checkBoxView5;
        private CheckBoxView checkBoxView6;
        private GraphView graphView1;
        private EditView editView1;
        private EventBox helpBox;

        /// <summary>Initializes a new instance of the <see cref="SeriesView" /> class</summary>
        public SeriesView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.SeriesView.glade");
            vbox1 = (VBox)builder.GetObject("vbox1");
            table1 = (Table)builder.GetObject("table1");
            label4 = (Label)builder.GetObject("label4");
            label5 = (Label)builder.GetObject("label5");
            mainWidget = vbox1;

            graphView1 = new GraphView(this);
            vbox1.PackStart(graphView1.MainWidget, true, true, 0);

            checkpointDropDown = new DropDownView(this);
            dataSourceDropDown = new DropDownView(this);
            xDropDown = new DropDownView(this);
            yDropDown = new DropDownView(this);
            x2DropDown = new DropDownView(this);
            y2DropDown = new DropDownView(this);
            seriesDropDown = new DropDownView(this);
            lineTypeDropDown = new DropDownView(this);
            markerTypeDropDown = new DropDownView(this);
            colourDropDown = new ColourDropDownView(this);
            lineThicknessDropDown = new DropDownView(this);
            markerSizeDropDown = new DropDownView(this);

            checkBoxView1 = new CheckBoxView(this);
            checkBoxView1.TextOfLabel = "on top?";
            checkBoxView2 = new CheckBoxView(this);
            checkBoxView2.TextOfLabel = "on right?";
            checkBoxView3 = new CheckBoxView(this);
            checkBoxView3.TextOfLabel = "cumulative?";
            checkBoxView4 = new CheckBoxView(this);
            checkBoxView4.TextOfLabel = "cumulative?";
            checkBoxView5 = new CheckBoxView(this);
            checkBoxView5.TextOfLabel = "Show in legend?";
            checkBoxView6 = new CheckBoxView(this);
            checkBoxView6.TextOfLabel = "Include series name in legend?";

            editView1 = new EditView(this);

            table1.Attach(checkpointDropDown.MainWidget, 1, 2, 0, 1, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(dataSourceDropDown.MainWidget, 1, 2, 1, 2, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(xDropDown.MainWidget, 1, 2, 2, 3, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(yDropDown.MainWidget, 1, 2, 3, 4, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(y2DropDown.MainWidget, 1, 2, 4, 5, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(x2DropDown.MainWidget, 1, 2, 5, 6, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(seriesDropDown.MainWidget, 1, 2, 6, 7, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(lineTypeDropDown.MainWidget, 1, 2, 7, 8, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(markerTypeDropDown.MainWidget, 1, 2, 8, 9, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(colourDropDown.MainWidget, 1, 2, 9, 10, AttachOptions.Fill, 0, 10, 2);

            Image helpImage = new Image(null, "ApsimNG.Resources.help.png");
            helpBox = new EventBox();
            helpBox.Add(helpImage);
            helpBox.ButtonPressEvent += Help_ButtonPressEvent;
            HBox filterBox = new HBox();
            filterBox.PackStart(editView1.MainWidget, true, true, 0);
            filterBox.PackEnd(helpBox, false, true, 0);

            //table1.Attach(editView1.MainWidget, 1, 2, 9, 10, AttachOptions.Fill, 0, 10, 2);
            table1.Attach(filterBox, 1, 2, 10, 11, AttachOptions.Fill, 0, 10, 2);

            table1.Attach(checkBoxView1.MainWidget, 2, 3, 2, 3, AttachOptions.Fill, 0, 0, 0);
            table1.Attach(checkBoxView2.MainWidget, 2, 3, 3, 4, AttachOptions.Fill, 0, 0, 0);
            table1.Attach(checkBoxView3.MainWidget, 3, 4, 2, 3, AttachOptions.Fill, 0, 0, 0);
            table1.Attach(checkBoxView4.MainWidget, 3, 4, 3, 4, AttachOptions.Fill, 0, 0, 0);

            table1.Attach(checkBoxView5.MainWidget, 2, 4, 9, 10, AttachOptions.Fill, 0, 0, 0);
            table1.Attach(checkBoxView6.MainWidget, 2, 4, 10, 11, AttachOptions.Fill, 0, 0, 0);

            table1.Attach(lineThicknessDropDown.MainWidget, 3, 4, 7, 8, AttachOptions.Fill, 0, 0, 5);
            table1.Attach(markerSizeDropDown.MainWidget, 3, 4, 8, 9, AttachOptions.Fill, 0, 0, 5);
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            mainWidget.Destroyed -= _mainWidget_Destroyed;
            helpBox.ButtonPressEvent -= Help_ButtonPressEvent;
            checkpointDropDown.MainWidget.Dispose();
            dataSourceDropDown.MainWidget.Dispose();
            xDropDown.MainWidget.Dispose();
            yDropDown.MainWidget.Dispose();
            x2DropDown.MainWidget.Dispose();
            y2DropDown.MainWidget.Dispose();
            seriesDropDown.MainWidget.Dispose();
            lineTypeDropDown.MainWidget.Dispose();
            markerTypeDropDown.MainWidget.Dispose();
            colourDropDown.MainWidget.Dispose();
            lineThicknessDropDown.MainWidget.Dispose();
            markerSizeDropDown.MainWidget.Dispose();
            checkBoxView1.MainWidget.Dispose();
            checkBoxView2.MainWidget.Dispose();
            checkBoxView3.MainWidget.Dispose();
            checkBoxView4.MainWidget.Dispose();
            checkBoxView5.MainWidget.Dispose();
            checkBoxView6.MainWidget.Dispose();
            graphView1.MainWidget.Dispose();
            editView1.MainWidget.Dispose();
            owner = null;
        }

        /// <summary>Checkpoint control</summary>
        public IDropDownView Checkpoint { get { return checkpointDropDown; } }

        /// <summary>Data source</summary>
        public IDropDownView DataSource { get { return dataSourceDropDown; } }

        /// <summary>X field</summary>
        public IDropDownView X { get { return xDropDown; } }

        /// <summary>Y field</summary>
        public IDropDownView Y { get { return yDropDown; } }

        /// <summary>X2 field</summary>
        public IDropDownView X2 { get { return x2DropDown; } }

        /// <summary>Y2 field</summary>
        public IDropDownView Y2 { get { return y2DropDown; } }

        /// <summary>Series type</summary>
        public IDropDownView SeriesType { get { return seriesDropDown; } }

        /// <summary>Line type</summary>
        public IDropDownView LineType { get { return lineTypeDropDown; } }

        /// <summary>MarkerType</summary>
        public IDropDownView MarkerType { get { return markerTypeDropDown; } }

        /// <summary>Line thickness</summary>
        public IDropDownView LineThickness { get { return lineThicknessDropDown; } }

        /// <summary>Marker size</summary>
        public IDropDownView MarkerSize { get { return markerSizeDropDown; } }

        /// <summary>Colour</summary>
        public IColourDropDownView Colour { get { return colourDropDown; } }

        /// <summary>X on top checkbox.</summary>
        public ICheckBoxView XOnTop { get { return checkBoxView1; } }

        /// <summary>Y on right checkbox.</summary>
        public ICheckBoxView YOnRight { get { return checkBoxView2; } }

        /// <summary>X cumulative checkbox.</summary>
        public ICheckBoxView XCumulative { get { return checkBoxView3; } }

        /// <summary>Y cumulative checkbox.</summary>
        public ICheckBoxView YCumulative { get { return checkBoxView4; } }

        /// <summary>Show in lengend checkbox.</summary>
        public ICheckBoxView ShowInLegend { get { return checkBoxView5; } }

        /// <summary>Include series name in legend.</summary>
        public ICheckBoxView IncludeSeriesNameInLegend { get { return checkBoxView6; } }

        /// <summary>Graph.</summary>
        public IGraphView GraphView { get { return graphView1; } }

        /// <summary>Filter box.</summary>
        public IEditView Filter { get { return editView1; } }

        /// <summary>Show or hide the x2 and y2 drop downs.</summary>
        /// <param name="show"></param>
        public void ShowX2Y2(bool show)
        {
            label4.Visible = show;
            label5.Visible = show;
            X2.IsVisible = show;
            Y2.IsVisible = show;
        }

        /// <summary>Show the filter help.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Help_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (args.Event.Button == 1)
              System.Diagnostics.Process.Start("https://apsimnextgeneration.netlify.com/usage/graphfilters/");
        }

        public void EndEdit()
        {
            editView1.EndEdit();
        }
    }
}
