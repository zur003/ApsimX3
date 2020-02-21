﻿// -----------------------------------------------------------------------
// <copyright file="AxisView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Describes an interface for an axis view.
    /// </summary>
    interface IMapView
    {
        /// <summary>
        /// Invoked when the zoom level or map center is changed
        /// </summary>
        event EventHandler ViewChanged;

        /// <summary>Show the map</summary>
        void ShowMap(List<Models.Map.Coordinate> coordinates, List<string> locNames, double zoom, Models.Map.Coordinate center);

        /// <summary>Export the map to an image.</summary>
        Image Export();
        /// <summary>
        /// Get or set the zoom factor of the map
        /// </summary>
        double Zoom { get; set; }

        /// <summary>
        /// Get or set the center position of the map
        /// </summary>
        Models.Map.Coordinate Center { get; set; }

        /// <summary>
        /// Store current position and zoom settings
        /// </summary>
        void StoreSettings();
    }

    /// It would be good if we could retrieve the current center and zoom values for a map,
    /// and store them as part of the Map object, so that maps can be recreated and exported
    /// using those settings. 
    /// Google map readily allows the center and zoom values to be obtained in JavaScript, and
    /// provides event handlers for when those values change, but the problem is getting those
    /// values back to the hosting application. With IE, it should be possible to use
    /// the ObjectForScripting approach. For Webkit, it may be a bit harder. See
    /// http://stackoverflow.com/questions/9804360/how-to-call-javascript-from-monos-webkitsharp
    /// for a workaround using the document title as a mechanism for receiving information. Webkit
    /// does provide a listener for title changes.
    /// 
    /// <summary>
    /// A Windows forms implementation of an AxisView
    /// </summary>
    public class MapView : HTMLView, IMapView
    {
        /// <summary>
        /// Invoked when the zoom level or map center is changed
        /// </summary>
        public event EventHandler ViewChanged;

        /// <summary>Construtor</summary>
        public MapView(ViewBase owner) : base(owner)
        {
        }

        /// <summary>Show the map</summary>
        public void ShowMap(List<Models.Map.Coordinate> coordinates, List<string> locNames, double zoom, Models.Map.Coordinate center)
        {
            string html =
@"<!DOCTYPE html>
<html>
<meta charset=""UTF-8"">
<head>
  <link rel=""stylesheet"" href=""https://unpkg.com/leaflet@1.3.1/dist/leaflet.css""
            integrity=""sha512-Rksm5RenBEKSKFjgI3a41vrjkw4EVPlJ3+OiI65vTjIdo9brlAacEuKOiQ5OFh7cOI1bkDwLqdLw3Zg0cRJAAQ==""
            crossorigin=""""/>
  <script type=""text/javascript"" src=""https://unpkg.com/leaflet@1.3.1/dist/leaflet.js""
         integrity=""sha512-/Nsx9X4HebavoBvEBuyp3I7od5tA0UzAxs+j83KgC8PU0kgB4XiK4Lfe4y4cgBtaRJQEIFCW+oC506aPT2L1zw==""
         crossorigin=""""></script>
</head>
<body>
  <!--Make sure you put this AFTER Leaflet's CSS -->

  <div id='mapid' style='position:fixed; top:0; bottom:0; left:0; right:0;' ></div>";

            html += @"
  <script>
    var locations = [";

            for (int i = 0; i < coordinates.Count; i++)
            {
                html += "[" + coordinates[i].Latitude.ToString() + ", " + coordinates[i].Longitude.ToString() + ", '" + locNames[i] + "']";
                if (i < coordinates.Count - 1)
                    html += ',';
            }
            html += "];" + Environment.NewLine;

            html += "    var mymap = L.map('mapid', {";
            html += "center: new L.LatLng(" + center.Latitude.ToString() + ", " + center.Longitude.ToString() + ")";
            html += ", zoom: " + zoom.ToString();
            if (popupWindow != null) // Exporting to a report, so leave off the zoom control
                html += ", zoomControl: false";
            html += "});";


           html += @"

    mymap.zoomDelta = 0.1;
    L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token=pk.eyJ1IjoiZXJpY3p1cmNoZXIiLCJhIjoiY2pmYzFxcnJ5MXZidjN1bXprdXRtZjd2aCJ9.BSxI0r_GNzUAZnbvmuHpHA', {
        maxZoom: 18,
        attribution: 'Map data &copy; <a href=""http://openstreetmap.org"">OpenStreetMap</a> contributors, ' +
        '<a href=""http://creativecommons.org/licenses/by-sa/2.0/"">CC-BY-SA</a>, ' +
        'Imagery © <a href=""http://mapbox.com"">Mapbox</a>',
        id: 'mapbox.emerald'
    }).addTo(mymap);

    L.control.scale({metric: true, imperial: false, updateWhenIdle: true}).addTo(mymap);

    var marker, i;
    for (i = 0; i<locations.length; i++)
    {
        L.marker(locations[i]).addTo(mymap).bindPopup('<b>' + locations[i][2] + '</b><br>Latitude: ' + locations[i][0] + '<br>Longitude: ' + locations[i][1]);
    }

    function SetTitle()
    {
        window.document.title = mymap.getZoom().toString() + ', (' + mymap.getCenter().lat.toString() + ', ' + mymap.getCenter().lng.toString() + ')';
    }
    function SetZoom(newZoom)
    {
        mymap.setZoom(newZoom);
    }
    function SetCenter(lat, long)
    {
        var center = new L.LatLng(lat, long);
        mymap.panTo(center);
    }

    mymap.on('zoomend', SetTitle);
    mymap.on('moveend', SetTitle);
	
    var popup = L.popup();
  </script>
  <script>alert('HEY! Why isn't the map displaying?');</script>
</body>
</html>";

            html = "<html><body><script>alert('HEY! Why isn\\'t the map displaying?');</script>Hello, world!</body></html>";
            SetContents(html, false);
        }

        /// <summary>
        /// Returns the Popup window used for exporting
        /// </summary>
        /// <returns></returns>
        public Gtk.Window GetPopupWin() { return popupWindow; }

        /// <summary>
        /// Export the map to an image.
        /// </summary>
        public Image Export()
        {
            // Create a Bitmap and draw the DataGridView on it.
            int x, y;
            int width;
            int height;
            Gdk.Window gridWindow = MainWidget.Window;
            gridWindow.GetGeometry(out x, out y, out width, out height);

            Gdk.Pixbuf screenshot = Gdk.PixbufHelper.GetFromWindow(gridWindow, 0, 0, width, height);
            byte[] buffer = screenshot.SaveToBuffer("png");
            MemoryStream stream = new MemoryStream(buffer);
            System.Drawing.Bitmap bitmap = new Bitmap(stream);
            return bitmap;
        }

        private double zoom = 1.0;

        private Models.Map.Coordinate center = new Models.Map.Coordinate() { Latitude = 0.0, Longitude = 0.0 };

        /// <summary>
        /// Get or set the zoom factor of the map
        /// </summary>
        public double Zoom
        {
            get
            {
                return zoom;
            }
            set
            {
                zoom = Math.Truncate(value + 0.5);
                browser.ExecJavaScript("SetZoom", new object[] { (int)zoom });
                if (popupWindow != null)
                {
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    while (watch.ElapsedMilliseconds < 500)
                        Gtk.Application.RunIteration();
                }
            }
        }

        /// <summary>
        /// Get or set the center position of the map
        /// </summary>
        public Models.Map.Coordinate Center
        {
            get
            {
                return center;
            }
            set
            {
                center = value;
                browser.ExecJavaScript("SetCenter", new object[] { value.Latitude, value.Longitude });

                // With WebKit, it appears we need to give it time to actually update the display
                // Really only a problem with the temporary windows used for generating documentation
                if (popupWindow != null)
                {
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    while (watch.ElapsedMilliseconds < 500)
                        Gtk.Application.RunIteration();
                }
            }
        }

        public void StoreSettings()
        {
            NewTitle(browser.GetTitle());
        }

        protected override void NewTitle(string title)
        {
            if (!String.IsNullOrEmpty(title))
            {
                double newLat, newLong, newZoom;
                bool modified = false;
                // Incoming title should look like "6, (-27.15, 151.25)"
                // That is Zoom, then lat, long pair
                // We remove the brackets and split on the commas
                title = title.Replace("(", "");
                title = title.Replace(")", "");
                string[] parts = title.Split(new char[] { ',' });
                if (Double.TryParse(parts[0], out newZoom) && newZoom != zoom)
                {
                    zoom = newZoom;
                    modified = true;
                }
                if (Double.TryParse(parts[1], out newLat) &&
                    Double.TryParse(parts[2], out newLong) &&
                    (newLat != center.Latitude || newLong != center.Longitude))
                {
                    center.Latitude = newLat;
                    center.Longitude = newLong;
                    modified = true;
                }
                if (modified && ViewChanged != null)
                    ViewChanged.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
