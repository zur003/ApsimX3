using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    public class VersionsPresenter : IPresenter
    {
        /// <summary>
        /// The model
        /// </summary>
        private Model model;

        /// <summary>
        /// The view to use
        /// </summary>
        ///// private IHTMLView genericView;

        /// <summary>
        /// The explorer
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Model;
            ///// this.genericView = view as IHTMLView;
            this.explorerPresenter = explorerPresenter;
            ///// this.genericView.SetContents(CreateHTML(), false, false);
        }

        private string CreateHTML()
        {
            string htmlString = "<!DOCTYPE html>\n" +
                "<html>\n<head>\n<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />\n<style>\n" +
                "body {color: [FontColor]; max-width:1000px; font-size:10pt;}" + 
                ".messagebanner {background-color:CornflowerBlue !important; border-radius:5px 5px 0px 0px; color:white; padding:5px; }" +
                ".messagecontent {background-color:[Background] !important; margin-bottom:20px; border-radius:0px 0px 5px 5px; border-color:CornflowerBlue; border-width:1px; border-style:none solid solid solid; padding:10px;}" +
                ".holdermain {margin: 20px 0px 20px 0px}" +
                ".version {font-weight:bold;float:left;}" +
                ".author {float:right;}" +
                ".messageentry {padding:5px 0px 5px 0px; line-height: 1.7em; }" +
                ".clearfix::after {content:\"\"; clear:both; display:table;}"+
                "a {color:#BCB948;}" +
                "@media print { body { -webkit - print - color - adjust: exact; }}" +
                "\n</style>\n</head>\n<body>";

            // apply theme based settings
            if (!Utility.Configuration.Settings.DarkTheme)
            {
                // light theme
                htmlString = htmlString.Replace("[FontColor]", "#000000");
                htmlString = htmlString.Replace("[Background]", "#FAFAFF");
            }
            else
            {
                // dark theme
                htmlString = htmlString.Replace("[FontColor]", "#E5E5E5");
                htmlString = htmlString.Replace("[Background]", "#030028");
            }



            foreach (VersionAttribute item in ReflectionUtilities.GetAttributes(model.GetType(), typeof(VersionAttribute), false))
            {
                htmlString += "\n<div class=\"holdermain\">";
                htmlString += "\n<div class=\"messagebanner clearfix\">";
                htmlString += "\n<div class=\"version\">V"+ item.ToString() + "</div>";
                htmlString += "</div>";
                htmlString += "\n<div class=\"messagecontent\">";
                htmlString += "\n<div class=\"messageentry\">" + (item.Comments().Length == 0?((item.ToString() == "1.0.1")?"Initial release of this component":"No details provided"):item.Comments().Replace("\n", "<br />"));
                htmlString += "\n</div>";
                htmlString += "\n</div>";
                htmlString += "\n</div>";
            }
            htmlString += "\n</body>\n</html>";
            return htmlString;
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
        }

    }
}
