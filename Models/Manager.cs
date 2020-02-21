﻿namespace Models
{
    using APSIM.Shared.Utilities;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.VisualBasic;
    using Models.Core;
    using Models.Core.Interfaces;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using System.CodeDom.Compiler;

    /// <summary>
    /// The manager model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ManagerView")]
    [PresenterName("UserInterface.Presenters.ManagerPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Zones.RectangularZone))]
    [ValidParent(ParentType = typeof(Zones.CircularZone))]
    [ValidParent(ParentType = typeof(Agroforestry.AgroforestrySystem))]
    [ValidParent(ParentType = typeof(Factorial.CompositeFactor))]
    [ValidParent(ParentType = typeof(Factorial.Factor))]
    public class Manager : Model, IOptionallySerialiseChildren
    {
        private static bool haveTrappedAssemblyResolveEvent = false;

        private static object haveTrappedAssemblyResolveEventLock = new object();

        /// <summary>The compiled code</summary>
        private string CompiledCode;

        /// <summary>Has the manager model been fully created yet?</summary>
        [JsonIgnore]
        private bool isCreated = false;
        
        /// <summary>The code to compile.</summary>
        private string cSharpCode;

        /// <summary>Gets or sets the code to compile.</summary>
        public string Code
        {
            get
            {
                return cSharpCode;
            }
            set
            {
                cSharpCode = value;
                RebuildScriptModel();
            }
        }

        /// <summary>The script Model that has been compiled</summary>
        public List<KeyValuePair<string, string>> Parameters { get; set; }

        /// <summary>Allow children to be serialised?</summary>
        public bool DoSerialiseChildren { get { return false; } }

        /// <summary>
        /// Stores column and line of caret, and scrolling position when editing in GUI
        /// This isn't really a Rectangle, but the Rectangle class gives us a convenient
        /// way to store both the caret position and scrolling information.
        /// </summary>
        [XmlIgnore]
        public Rectangle Location { get; set; }  = new Rectangle(1, 1, 0, 0);

        /// <summary>
        /// Stores whether we are currently on the tab displaying the script.
        /// Meaningful only within the GUI
        /// </summary>
        [XmlIgnore]
        public int ActiveTabIndex { get; set; }

        /// <summary>
        /// Called when the model has been newly created in memory whether from 
        /// cloning or deserialisation.
        /// </summary>
        public override void OnCreated()
        {
            // This looks weird but I'm trying to avoid having to call lock
            // everytime we come through here.
            if (!haveTrappedAssemblyResolveEvent)
            {
                lock (haveTrappedAssemblyResolveEventLock)
                {
                    if (!haveTrappedAssemblyResolveEvent)
                    {
                        haveTrappedAssemblyResolveEvent = true;

                        // Trap the assembly resolve event.
                        AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(Manager.ResolveManagerAssembliesEventHandler);
                        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Manager.ResolveManagerAssembliesEventHandler);

                        // Clean up apsimx manager .dll files.
                        CleanupOldAssemblies();
                    }
                }
            }

            isCreated = true;
            RebuildScriptModel();
        }

        /// <summary>At simulation commencing time, rebuild the script assembly if required.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            RebuildScriptModel();
            SetParametersInObject(Apsim.Child(this, "Script") as Model);
        }

        /// <summary>Rebuild the script model and return error message if script cannot be compiled.</summary>
        public void RebuildScriptModel()
        {
            // This is called from manager presenter on detach so go refresh our parameter values.
            if (Children.Count == 1)
                GetParametersFromScriptModel(Children[0]);

            if (Enabled && isCreated && Code != null && (Code != CompiledCode || Children.Count == 0))
            {
                try
                {
                    Children?.RemoveAll(x => x.GetType().Name == "Script");
                    Assembly compiledAssembly = CompileTextToAssembly(Code, GetAssemblyFileName());
                    if (compiledAssembly.GetType("Models.Script") == null)
                        throw new ApsimXException(this, "Cannot find a public class called 'Script'");

                    CompiledCode = Code;

                    // Create a new script model.
                    Model script = compiledAssembly.CreateInstance("Models.Script") as Model;
                    script.Children = new List<Model>();
                    script.Name = "Script";
                    script.IsHidden = true;

                    // Add the new script model to our models collection.
                    Children.Add(script);
                    script.Parent = this;

                    // Attempt to give the new script's properties the same
                    // values used by the old script.
                    SetParametersInObject(script);
                    script.OnCreated();
                }
                catch (Exception err)
                {
                    CompiledCode = null;
                    throw new Exception("Unable to compile \"" + Name + "\"" + ". Full path: " + Apsim.FullPath(this), err);
                }
            }
        }

        /// <summary>Work out the assembly file name (with path).</summary>
        public string GetAssemblyFileName()
        {
            return Path.ChangeExtension(Path.Combine(Path.GetTempPath(), "ApsimXManager" + Guid.NewGuid().ToString()), ".dll");
        }

        /// <summary>A handler to resolve the loading of manager assemblies when binary deserialization happens.</summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <remarks>
        /// Seems like it will only look for DLL's in the bin folder. We can't put the manager DLLs in there
        /// because when ApsimX is installed, the bin folder will be under c:\program files and we won't have
        /// permission to save the manager dlls there. Instead we put them in %TEMP% and use this 
        /// event handler to resolve the assemblies to that location.
        /// </remarks>
        /// <returns></returns>
        public static Assembly ResolveManagerAssembliesEventHandler(object sender, ResolveEventArgs args)
        {
            foreach (string fileName in Directory.GetFiles(Path.GetTempPath(), "ApsimXManager*.dll"))
                if (args.Name.Split(',')[0] == Path.GetFileNameWithoutExtension(fileName))
                    return Assembly.LoadFrom(fileName);
            return null;
        }

        /// <summary>
        /// Cleanup old assemblies in the TEMP folder.
        /// </summary>
        private void CleanupOldAssemblies()
        {
            var filesToCleanup = new List<string>();
            filesToCleanup.AddRange(Directory.GetFiles(Path.GetTempPath(), "ApsimXManager*.dll"));
            filesToCleanup.AddRange(Directory.GetFiles(Path.GetTempPath(), "ApsimXManager*.cs"));

            foreach (string fileName in filesToCleanup)
            {
                try
                {
                    TimeSpan timeSinceLastAccess = DateTime.Now - File.GetLastAccessTime(fileName);
                    if (timeSinceLastAccess.Hours > 1)
                        File.Delete(fileName);
                }
                catch (Exception)
                {
                    // File locked?
                }                
            }
        }

        /// <summary>Set the scripts parameters from the 'xmlElement' passed in.</summary>
        /// <param name="script">The script.</param>
        private void SetParametersInObject(Model script)
        {
            if (Parameters != null)
            {
                List<Exception> errors = new List<Exception>();
                foreach (var parameter in Parameters)
                {
                    try
                    {
                        PropertyInfo property = script.GetType().GetProperty(parameter.Key);
                        if (property != null)
                        {
                            object value;
                            if (parameter.Value.StartsWith(".") || parameter.Value.StartsWith("["))
                                value = Apsim.Get(this, parameter.Value);
                            else if (property.PropertyType == typeof(IPlant))
                                value = Apsim.Find(this, parameter.Value);
                            else
                                value = ReflectionUtilities.StringToObject(property.PropertyType, parameter.Value);
                            property.SetValue(script, value, null);
                        }
                    }
                    catch (Exception err)
                    {
                        errors.Add(err);
                    }
                }
                if (errors.Count > 0)
                {
                    string message = "";
                    foreach (Exception error in errors)
                        message += error.Message;
                    throw new Exception(message);
                }
            }
        }

        /// <summary>Get all parameters from the script model and store in our parameters list.</summary>
        /// <param name="script">The script.</param>
        /// <returns></returns>
        private void GetParametersFromScriptModel(Model script)
        {
            if (Parameters == null)
                Parameters = new List<KeyValuePair<string, string>>();
            Parameters.Clear();
            foreach (PropertyInfo property in script.GetType().GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
            {
                if (property.CanRead && property.CanWrite && 
                    ReflectionUtilities.GetAttribute(property, typeof(XmlIgnoreAttribute), false) == null)
                {
                    object value = property.GetValue(script, null);
                    if (value == null)
                        value = "";
                    else if (value is IModel)
                        value = "[" + (value as IModel).Name + "]";
                    Parameters.Add(new KeyValuePair<string, string>
                                        (property.Name, 
                                         ReflectionUtilities.ObjectToString(value)));
                }
            }
        }


        /// <summary>
        /// An assembly cache.
        /// </summary>
        private static Dictionary<string, Assembly> AssemblyCache = new Dictionary<string, Assembly>();

        /// <summary>
        /// Compile the specified 'code' into an executable assembly. If 'assemblyFileName'
        /// is null then compile to an in-memory assembly.
        /// </summary>
        public static Assembly CompileTextToAssembly(string code, string assemblyFileName, params string[] referencedAssemblies)
        {
            // See if we've already compiled this code. If so then return the assembly.
            if (AssemblyCache.ContainsKey(code))
                return AssemblyCache[code];

            lock (AssemblyCache)
            {
                if (AssemblyCache.ContainsKey(code))
                    return AssemblyCache[code];

                string runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location);

                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

                bool VB = code.IndexOf("Imports System") != -1;
                Compilation compilation;
                if (VB)
                {
                    compilation = VisualBasicCompilation.Create(
                    Path.GetFileNameWithoutExtension(assemblyFileName),
                    new[] { syntaxTree },
                    new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "mscorlib.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Collections.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Runtime.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Core.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Xml.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Xml.ReaderWriter.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Private.Xml.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Data.dll")),
                            MetadataReference.CreateFromFile(typeof(MathUtilities).Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(IModel).Assembly.Location)
                     },
                    new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)); ;
                }
                else
                {
                    compilation = CSharpCompilation.Create(
                        Path.GetFileNameWithoutExtension(assemblyFileName),
                        new[] { syntaxTree },
                        new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "mscorlib.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Collections.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Runtime.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Core.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Xml.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Xml.ReaderWriter.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Private.Xml.dll")),
                            MetadataReference.CreateFromFile(Path.Join(runtimePath, "System.Data.dll")),
                            MetadataReference.CreateFromFile(typeof(MathUtilities).Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(IModel).Assembly.Location)
                         },
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)); ;
                }

                MemoryStream ms = new MemoryStream();
                MemoryStream pdbStream = new MemoryStream();
                {
                    var emitResult = compilation.Emit(ms, pdbStream);
                    if (!emitResult.Success)
                    {
                        string Errors = "Cannot compile manager script to an assembly";
                        foreach (Diagnostic diag in emitResult.Diagnostics)
                        {
                            if (diag.Severity == DiagnosticSeverity.Error)
                                Errors += "\r\n" + diag.ToString();
                        }
                        throw new Exception(Errors);
                    }
                    ms.Seek(0, SeekOrigin.Begin);
                    return System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(ms);
                }
            }
        }
    }
}
