/*
* Copyright(C) 2006, 2007 Apple Inc.
* Copyright(C) 2007 Alp Toker <alp@atoker.com>
* Copyright(C) 2011 Lukasz Slachciak
* Copyright(C) 2011 Bob Murphy
*
*Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions
* are met :
*1. Redistributions of source code must retain the above copyright
* notice, this list of conditionsand the following disclaimer.
* 2. Redistributions in binary form must reproduce the above copyright
* notice, this list of conditionsand the following disclaimer in the
* documentationand /or other materials provided with the distribution.
*
*THIS SOFTWARE IS PROVIDED BY APPLE COMPUTER, INC. ``AS IS'' AND ANY
* EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
* IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
* PURPOSE ARE DISCLAIMED.IN NO EVENT SHALL APPLE COMPUTER, INC.OR
* CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
* EXEMPLARY, OR CONSEQUENTIAL DAMAGES(INCLUDING, BUT NOT LIMITED TO,
	*PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
	* PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY
	* OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
	* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
	* OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
	*/

#include <gtk/gtk.h>
#include <webkit/webkit.h>


static void destroyWindowCb(GtkWidget * widget, GtkWidget * window);
static bool closeWebViewCb(WebKitWebView* webView, GtkWidget* window);

int main(int argc, char* argv[])
{
	// Initialize GTK+
	gtk_init(&argc, &argv);

	// Create an 800x600 window that will contain the browser instance
	GtkWidget* main_window = gtk_window_new(GTK_WINDOW_TOPLEVEL);
	gtk_window_set_default_size((GtkWindow*)main_window, 800, 600);

	// Create a browser instance
	WebKitWebView* webView = (WebKitWebView*)webkit_web_view_new();

	// Put the browser area into the main window
	gtk_container_add(GTK_CONTAINER(main_window), GTK_WIDGET(webView));

	// Set up callbacks so that if either the main window or the browser instance is
	// closed, the program will exit
	g_signal_connect(main_window, "destroy", G_CALLBACK(destroyWindowCb), NULL);
	g_signal_connect(webView, "close", G_CALLBACK(closeWebViewCb), main_window);

	// Load a web page into the browser instance
	webkit_web_view_load_uri(webView, "https://www.apsim.info/");

	char* html = "<!DOCTYPE html> \
		<html>\
		<meta charset=\"UTF-8\"> \
		<head>\
        <meta charset=\"utf-8\"/> \
		<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\
		<link rel=\"shortcut icon\" type=\"image/x-icon\" href=\"docs/images/favicon.ico\" />\
		<link rel=\"stylesheet\" href=\"https://unpkg.com/leaflet@1.5.1/dist/leaflet.css\" integrity=\"sha512-xwE/Az9zrjBIphAcBb3F6JVqxf46+CDLwfLMHloNu6KEQCAWi6HcDUbeOfBIptF7tcCzusKFjFw2yuvEpDL9wQ==\" crossorigin=\"\" />\
        <script src=\"https://unpkg.com/leaflet@1.5.1/dist/leaflet.js\" integrity=\"sha512-GffPMF3RvMeYyc1LWMHtK8EbPv0iNZ8/oTtHPx9/cc2ILxQ+u905qIwdpULaqDkyBKgOaB57QTMg7ztg8Jm2Og==\" crossorigin=\"\"></script> \
        </head> \
		<body> \
		<div id=\"mapid\" style=\"width: 600px; height: 400px;\"></div>\
		<script> \
        alert('What is going on?'); doInit(); \
        function doInit() { \
		alert('Are things happening in the wrong order?'); return 1; }</script><script>\
		var mymap = L.map('mapid').setView([51.505, -0.09], 13); \
        alert(mymap._pixelOrigin.x);\
		L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token=pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4NXVycTA2emYycXBndHRqcmZ3N3gifQ.rJcFIG214AriISLbB6B5aw', {\
		maxZoom: 18,\
		attribution: 'Map data &copy; <a href=\"https://www.openstreetmap.org/\">OpenStreetMap</a> contributors, ' +\
			'<a href=\"https://creativecommons.org/licenses/by-sa/2.0/\">CC-BY-SA</a>, ' +\
			'Imagery © <a href=\"https://www.mapbox.com/\">Mapbox</a>',\
		id : 'mapbox.streets'\
		}).addTo(mymap); \
	</script>\
    </body>\
	</html>";

	// Doing some testing. It appears that JavaScript is badly broken, and just attempting to define a new
	// function or accessing a user-defined function will cause a script section to fail.
	html = "<body> \
		<div id=\"mapid\" style=\"width: 600px; height: 400px;\"></div>\
        <script> alert('Can you see me?'); function test() {alert('Hi!');}</script><script>alert('What is going on?'); alert('HI');</script></body>";


	//webkit_web_view_load_uri(webView, "https://api.tiles.mapbox.com/v4/mapbox.streets/13/4095/2724.png?access_token=pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4NXVycTA2emYycXBndHRqcmZ3N3gifQ.rJcFIG214AriISLbB6B5aw");
	//webkit_web_view_load_html_string(webView, html, "about:blank");

	// Make sure that when the browser area becomes visible, it will get mouse
	// and keyboard events
	gtk_widget_grab_focus((GtkWidget*)webView);

	// Make sure the main window and all its contents are visible
	gtk_widget_show_all(main_window);

	// Run the main GTK+ event loop
	gtk_main();

	return 0;
}


static void destroyWindowCb(GtkWidget* widget, GtkWidget* window)
{
	gtk_main_quit();
}

static bool closeWebViewCb(WebKitWebView* webView, GtkWidget* window)
{
	gtk_widget_destroy(window);
	return true;
}