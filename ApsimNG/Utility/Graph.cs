﻿namespace Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Data;
    using System.Xml;
    using Models.Core;
    using APSIM.Shared.Utilities;
    using System.Reflection;

    /// <summary>
    /// Graph utility functions.
    /// </summary>
    public class Graph
    {
        public static Models.Graph.Graph CreateGraphFromResource(string resourceName)
        {
            string graphXmL = ApsimNG.Properties.Resources.ResourceManager.GetString(resourceName);

            if (graphXmL != null)
            {
                List<Exception> errors = null;
                Models.Graph.Graph graph = Models.Core.ApsimFile.FileFormat.ReadFromString<Models.Graph.Graph>(graphXmL, out errors);
                if (errors != null && errors.Any())
                    throw errors.First();
                Apsim.ParentAllChildren(graph);
                return graph;
            }
            return null;
        }
    }
}
