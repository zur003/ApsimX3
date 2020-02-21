//-----------------------------------------------------------------------
// <copyright file="PropertyPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using APSIM.Shared.Utilities;
    using EventArguments;
    using Interfaces;
    using Models;
    using Models.CLEM;
    using Models.Core;
    using Models.Surface;
    using Utility;
    using Views;
    using Commands;
    using System.Drawing;
    using Models.CLEM.Resources;
    using Models.Storage;
    using System.Globalization;

    /// <summary>
    /// <para>
    /// This presenter displays properties of a Model in an IGridView.
    /// The properties must be public, read/write and have a [Description]
    /// attribute. Array properties are supported if they are integer, double
    /// or string arrays.
    /// </para>
    /// <para>
    /// There is also a method (RemoveProperties) for excluding properties from 
    /// the PropertyGrid. This is important when a PropertyGrid is embedded on
    /// a ProfileGrid and the ProfileGrid is displaying some properties as well.
    /// We don't want properties to be on both the ProfileGrid and the PropertyGrid.
    /// </para>
    /// </summary>
    public class PropertyPresenter : GridPresenter
    {
        /// <summary>
        /// Linked storage reader
        /// </summary>
        [Link]
        private IDataStore storage = null;

        /// <summary>
        /// The model we're going to examine for properties.
        /// </summary>
        protected IModel model;

        /// <summary>
        /// A list of all properties found in the Model.
        /// </summary>
        protected List<IVariable> properties = new List<IVariable>();

        /// <summary>
        /// The category name to filter for on the Category Attribute for the properties
        /// </summary>
        public string CategoryFilter { get; set; }

        /// <summary>
        /// The subcategory name to filter for on the Category Attribute for the properties
        /// </summary>
        public string SubcategoryFilter { get; set; }

        /// <summary>
        /// The completion form.
        /// </summary>
        ///// private IntellisensePresenter intellisense;

        /// <summary>
        /// If set to true, will only show scalar properties.
        /// </summary>
        protected bool scalarsOnly;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to</param>
        /// <param name="view">The view to connect to</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public override void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            base.Attach(model, view, explorerPresenter);
            grid.ContextItemsNeeded += GetContextItems;
            grid.CanGrow = false;
            this.model = model as Model;
            ///// intellisense = new IntellisensePresenter(grid as ViewBase);

            // The grid does not have control-space intellisense (for now).
            ///// intellisense.ItemSelected += OnIntellisenseItemSelected;
            // if the model is Testable, run the test method.
            ITestable testModel = model as ITestable;
            if (testModel != null)
            {
                testModel.Test(false, true);
                grid.ReadOnly = true;
            }

            grid.NumericFormat = "G6";

            if (this.model != null)
            {
                FindAllProperties(this.model);
                if (grid.DataSource == null)
                    PopulateGrid(this.model);
                else
                    FormatTestGrid();
            }

            grid.CellsChanged += OnCellsChanged;
            grid.ButtonClick += OnFileBrowseClick;
            this.presenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Gets a value indicating whether the grid is empty (i.e. no rows).
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return grid.RowCount == 0;
            }
        }

        /// <summary>
        /// If set to true, will only show scalar (non-array) properties.
        /// </summary>
        public bool ScalarsOnly
        {
            get
            {
                return scalarsOnly;
            }
            set
            {
                scalarsOnly = value;
                if (properties != null)
                {
                    if (value)
                        properties = properties.Where(p => !p.DataType.IsArray).ToList();
                    else
                        FindAllProperties(model);
                    PopulateGrid(model);
                }
            }
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public override void Detach()
        {
            try
            {
                base.Detach();
                if (grid != null)
                {
                    grid.CellsChanged -= OnCellsChanged;
                    grid.ButtonClick -= OnFileBrowseClick;
                    grid.ContextItemsNeeded -= GetContextItems;
                }
                if (presenter != null)
                    presenter.CommandHistory.ModelChanged -= OnModelChanged;
                /////if (intellisense != null)
                /////{
                /////    intellisense.ItemSelected -= OnIntellisenseItemSelected;
                /////    intellisense.Cleanup();
                /////}
            }
            catch (NullReferenceException)
            {
                // to keep Neil happy
            }
        }

        /// <summary>
        /// Populate (refresh) the grid.
        /// </summary>
        /// <param name="model">The model to examine for properties.</param>
        public void PopulateGrid()
        {
            PopulateGrid(model);
        }

        /// <summary>
        /// Populate the grid.
        /// </summary>
        /// <param name="model">The model to examine for properties.</param>
        private void PopulateGrid(IModel model)
        {
            if (grid == null)
                return;

            IGridCell selectedCell = grid.GetCurrentCell;
            this.model = model;

            DataTable table = CreateGrid();
            FillTable(table);
            grid.DataSource = table;
            FormatGrid();

            if (selectedCell != null)
                grid.GetCurrentCell = selectedCell;
        }

        protected virtual DataTable CreateGrid()
        {
            bool hasData = properties.Count > 0;

            DataTable table = new DataTable();
            table.Columns.Add(hasData ? "Description" : "No values are currently available", typeof(string));
            table.Columns.Add(hasData ? "Value" : " ", typeof(object));

            return table;
        }

        /// <summary>
        /// Remove the specified properties from the grid.
        /// </summary>
        /// <param name="propertysToRemove">The names of all properties to remove</param>
        public void RemoveProperties(IEnumerable<string> propertysToRemove)
        {
            foreach (var property in propertysToRemove)
            {
                // Try and find the description in our list of properties.
                int i = properties.FindIndex(p => p.Name == property);

                // If found then remove the property.
                if (i != -1)
                {
                    properties.RemoveAt(i);
                }
            }
            PopulateGrid(model);
        }

        /// <summary>
        /// Gets all public instance members of a given type,
        /// sorted by the line number of the member's declaration.
        /// </summary>
        /// <param name="o">Object whose members will be retrieved.</param>
        private List<MemberInfo> GetMembers(object o)
        {
            var members = o.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public).ToList();
            members.RemoveAll(m => !Attribute.IsDefined(m, typeof(DescriptionAttribute)));
            var orderedMembers = members.OrderBy(m => ((DescriptionAttribute)m.GetCustomAttribute(typeof(DescriptionAttribute), true)).LineNumber).ToList();
            return orderedMembers;
        }

        public void Refresh()
        {
            if (model == null)
                return;
            properties.Clear();
            FindAllProperties(model);
            PopulateGrid(model);
        }

        /// <summary>
        /// Find all properties from the model and fill this.properties.
        /// </summary>
        /// <param name="model">The mode object</param>
        protected virtual void FindAllProperties(IModel model)
        {
            this.model = model;
            bool filterByCategory = !((this.CategoryFilter == "") || (this.CategoryFilter == null));
            bool filterBySubcategory = !((this.SubcategoryFilter == "") || (this.SubcategoryFilter == null));
            if (this.model != null)
            {
                var orderedMembers = GetMembers(model);

                foreach (MemberInfo member in orderedMembers)
                {
                    IVariable property = null;
                    if (member is PropertyInfo)
                        property = new VariableProperty(this.model, member as PropertyInfo);
                    else if (member is FieldInfo)
                        property = new VariableField(this.model, member as FieldInfo);

                    if (property != null && property.Description != null && property.Writable)
                    {
                        // Only allow lists that are double[], int[], string[] or DateTime[]
                        bool includeProperty = true;
                        if (property.DataType.GetInterface("IList") != null)
                            includeProperty = true;

                        if (Attribute.IsDefined(member, typeof(SeparatorAttribute)))
                        {
                            SeparatorAttribute[] separators = Attribute.GetCustomAttributes(member, typeof(SeparatorAttribute)) as SeparatorAttribute[];
                            foreach (SeparatorAttribute separator in separators)
                                properties.Add(new VariableObject(separator.ToString()));  // use a VariableObject for separators
                        }

                        //If the above conditions have been met and,
                        //If a CategoryFilter has been specified. 
                        //filter only those properties with a [Catagory] attribute that matches the filter.

                        if (includeProperty && filterByCategory)
                        {
                            bool hasCategory = Attribute.IsDefined(member,typeof(CategoryAttribute), false);
                            if (hasCategory)
                            {
                                CategoryAttribute catAtt = (CategoryAttribute)Attribute.GetCustomAttribute(member,typeof(CategoryAttribute));
                                if (catAtt.Category == this.CategoryFilter)
                                {
                                    if (filterBySubcategory)
                                    {
                                        //the catAtt.Subcategory is by default given a value of 
                                        //"Unspecified" if the Subcategory is not assigned in the Category Attribute.
                                        //so this line below will also handle "Unspecified" subcategories.
                                        includeProperty = (catAtt.Subcategory == this.SubcategoryFilter);
                                    }
                                    else
                                    {
                                        includeProperty = true;
                                    }
                                } 
                                else
                                {
                                    includeProperty = false;
                                }
                                
                            }
                            else
                            {
                                //if we are filtering on "Unspecified" category then there is no Category Attribute
                                // just a Description Attribute on the property in the model.
                                //So we still may need to include it in this case.
                                if (this.CategoryFilter == "Unspecified")
                                    includeProperty = true;
                                else
                                    includeProperty = false;
                            }
                        }
			
                        if (includeProperty)
                            properties.Add(property);

                        if (property.DataType == typeof(DataTable))
                            grid.DataSource = property.Value as DataTable;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the lists of Cultivar and Field names in the model.
        /// This is used when the model has been changed. For example, when a 
        /// new crop has been selecled.
        /// </summary>
        /// <param name="model">The new model</param>
        public void UpdateModel(Model model)
        {
            this.model = model;
            if (this.model != null)
            {
                IGridCell curCell = grid?.GetCurrentCell;
                for (int i = 0; i < properties.Count; i++)
                {
                    IGridCell cell = grid.GetCell(1, i);
                    if (curCell != null && cell.RowIndex == curCell.RowIndex && cell.ColumnIndex == curCell.ColumnIndex)
                    {
                        continue;
                    }

                    if (properties[i].Display != null &&
                        properties[i].Display.Type == DisplayType.CultivarName)
                    {
                        IPlant crop = GetCrop(properties);
                        if (crop != null)
                        {
                            cell.DropDownStrings = GetCultivarNames(crop);
                        }
                    }
                    else if (properties[i].Display != null &&
                             properties[i].Display.Type == DisplayType.FieldName)
                    {
                        List<string> fieldNames = GetFieldNames();
                        if (fieldNames != null)
                        {
                            fieldNames.Insert(0, string.Empty);
                            cell.DropDownStrings = fieldNames.ToArray();
                        }
                    }
                }
            }
        }
        
        private void GetContextItems(object o, NeedContextItemsArgs e)
        { /* /////
            try
            {
                if (e.ControlShiftSpace)
                    intellisense.ShowMethodCompletion(model, e.Code, e.Offset, new Point(e.Coordinates.X, e.Coordinates.Y));
                else if (intellisense.GenerateGridCompletions(e.Code, e.Offset, model, true, false, false, e.ControlSpace))
                    intellisense.Show(e.Coordinates.X, e.Coordinates.Y);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            } */ /////
        }

        /// <summary>
        /// Fill the specified table with columns and rows based on this.Properties
        /// </summary>
        /// <param name="table">The table that needs to be filled</param>
        protected virtual void FillTable(DataTable table)
        {
            foreach (IVariable property in properties)
                AddPropertyToTable(table, property);
        }

        /// <summary>
        /// Adds a property to the DataTable of properties.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="property"></param>
        protected virtual void AddPropertyToTable(DataTable table, IVariable property)
        {
            if (property is VariableObject)
                table.Rows.Add(new object[] { property.Value, null });
            else if (property.Value is IModel)
                table.Rows.Add(new object[] { property.Description, Apsim.FullPath(property.Value as IModel) });
            else
            {
                string description = property.Description;
                if (!string.IsNullOrEmpty(property.Units))
                    description += " (" + property.Units + ")";
                table.Rows.Add(new object[] { description, property.ValueWithArrayHandling });
            }
        }

        /// <summary>
        /// Format the grid when displaying Tests.
        /// </summary>
        private void FormatTestGrid()
        {
            int numCols = grid.DataSource.Columns.Count;

            for (int i = 0; i < numCols; i++)
            {
                grid.GetColumn(i).Format = "F4";
            }
        }

        /// <summary>
        /// Format the grid.
        /// </summary>
        protected virtual void FormatGrid()
        {
            for (int i = 0; i < properties.Count; i++)
            {
                IGridCell cell = grid.GetCell(1, i);
                    
                if (properties[i] is VariableObject)
                {
                    cell.EditorType = EditorTypeEnum.TextBox;

                    grid.SetRowAsSeparator(i, true);
                }
                else if (properties[i].Display != null && 
                         properties[i].Display.Type == DisplayType.TableName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    cell.DropDownStrings = storage.Reader.TableNames.ToArray();
                }
                else if (properties[i].Display != null && 
                         properties[i].Display.Type == DisplayType.CultivarName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    IPlant crop = GetCrop(properties);
                    if (crop != null)
                    {
                        cell.DropDownStrings = GetCultivarNames(crop);
                    }
                }
                else if (properties[i].Display != null && 
                         properties[i].Display.Type == DisplayType.FileName)
                {
                    cell.EditorType = EditorTypeEnum.Button;
                }
                else if (properties[i].Display != null && 
                         properties[i].Display.Type == DisplayType.FieldName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    List<string> fieldNames = GetFieldNames();
                    if (fieldNames != null)
                    {
                        fieldNames.Insert(0, string.Empty);
                        cell.DropDownStrings = fieldNames.ToArray();
                    }
                }
                else if (properties[i].Display != null && 
                         properties[i].Display.Type == DisplayType.ResidueName &&
                         model is SurfaceOrganicMatter)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    string[] fieldNames = GetResidueNames();
                    if (fieldNames != null)
                    {
                        cell.DropDownStrings = fieldNames;
                    }
                }
                else if (properties[i].Display != null &&  
					(properties[i].Display.Type == DisplayType.CLEMResourceName))
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    List<string> fieldNames = new List<string>();
                    fieldNames.AddRange(this.GetCLEMResourceNames(this.properties[i].Display.CLEMResourceNameResourceGroups));

                    // add any extras elements provided to the list.
                    if (this.properties[i].Display.CLEMExtraEntries != null)
                    {
                        fieldNames.AddRange(this.properties[i].Display.CLEMExtraEntries);
                    }

                    if (fieldNames.Count != 0)
                    {
                        cell.DropDownStrings = fieldNames.ToArray();
                    }
                }
                else if (properties[i].Display != null && 
					(properties[i].Display.Type == DisplayType.CLEMCropFileName))
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    List<string> fieldNames = new List<string>();
                    Simulation clemParent = Apsim.Parent(this.model, typeof(Simulation)) as Simulation;
                    // get crop file names
                    fieldNames.AddRange(Apsim.ChildrenRecursively(clemParent, typeof(FileCrop)).Select(a => a.Name).ToList());
                    fieldNames.AddRange(Apsim.ChildrenRecursively(clemParent, typeof(FileSQLiteCrop)).Select(a => a.Name).ToList());
                    if (fieldNames.Count != 0)
                    {
                        cell.DropDownStrings = fieldNames.ToArray();
                    }
                }
                else if (properties[i].Display != null &&  
					(properties[i].Display.Type == DisplayType.CLEMGraspFileName))
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    List<string> fieldNames = new List<string>();
                    Simulation clemParent = Apsim.Parent(this.model, typeof(Simulation)) as Simulation;
                    // get GRASP file names
                    fieldNames.AddRange(Apsim.ChildrenRecursively(clemParent, typeof(FileGRASP)).Select(a => a.Name).ToList());
                    fieldNames.AddRange(Apsim.ChildrenRecursively(clemParent, typeof(FileSQLiteGRASP)).Select(a => a.Name).ToList());
                    if (fieldNames.Count != 0)
                    {
                        cell.DropDownStrings = fieldNames.ToArray();
                    }
                }				
                else if (properties[i].Display != null && 
                         properties[i].Display.Type == DisplayType.Model)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;

                    string[] modelNames = GetModelNames(properties[i].Display.ModelType);
                    if (modelNames != null)
                        cell.DropDownStrings = modelNames;
                }
                else
                {
                    object cellValue = properties[i].ValueWithArrayHandling;
                    if (cellValue is DateTime)
                    {
                        cell.EditorType = EditorTypeEnum.DateTime;
                    }
                    else if (cellValue is bool)
                    {
                        cell.EditorType = EditorTypeEnum.Boolean;
                    }
                    else if (cellValue.GetType().IsEnum)
                    {
                        cell.EditorType = EditorTypeEnum.DropDown;
                        cell.DropDownStrings = VariableProperty.EnumToStrings(cellValue);
                        Enum cellValueAsEnum = cellValue as Enum;
                        if (cellValueAsEnum != null)
                            cell.Value = VariableProperty.GetEnumDescription(cellValueAsEnum);
                    }
                    else if (cellValue.GetType() == typeof(IPlant))
                    {
                        cell.EditorType = EditorTypeEnum.DropDown;
                        List<string> cropNames = new List<string>();
                        foreach (Model crop in Apsim.FindAll(model, typeof(IPlant)))
                        {
                            cropNames.Add(crop.Name);
                        }

                        cell.DropDownStrings = cropNames.ToArray();
                    }
                    else if (properties[i].DataType == typeof(IPlant))
                    {
                        List<string> plantNames = Apsim.FindAll(model, typeof(IPlant)).Select(m => m.Name).ToList();
                        cell.EditorType = EditorTypeEnum.DropDown;
                        cell.DropDownStrings = plantNames.ToArray();
                    }
                    else if (!string.IsNullOrWhiteSpace(properties[i].Display?.Values))
                    {
                        MethodInfo method = model.GetType().GetMethod(properties[i].Display.Values);
                        string[] values = ((IEnumerable<object>)method.Invoke(model, null))?.Select(v => v?.ToString())?.ToArray();
                        cell.EditorType = EditorTypeEnum.DropDown;
                        cell.DropDownStrings = values;
                    }
                    else
                    {
                        cell.EditorType = EditorTypeEnum.TextBox;
                    }
                }
            }

            IGridColumn descriptionColumn = grid.GetColumn(0);
            descriptionColumn.Width = -1;
            descriptionColumn.ReadOnly = true;

            IGridColumn valueColumn = grid.GetColumn(1);
            valueColumn.Width = -1;
        }

        /// <summary>Get a list of cultivars for crop.</summary>
        /// <param name="crop">The crop.</param>
        /// <returns>A list of cultivars.</returns>
        private string[] GetCultivarNames(IPlant crop)
        {
            if (crop.CultivarNames.Length == 0)
            {
                Simulations simulations = Apsim.Parent(crop as IModel, typeof(Simulations)) as Simulations;
                Replacements replacements = Apsim.Child(simulations, typeof(Replacements)) as Replacements;
                if (replacements != null)
                {
                    IPlant replacementCrop = Apsim.Child(replacements, (crop as IModel).Name) as IPlant;
                    if (replacementCrop != null)
                    {
                        return replacementCrop.CultivarNames;
                    }
                }
            }
            else
            {
                return crop.CultivarNames;
            }

            return new string[0];
        }

        /// <summary>Get a list of database fieldnames. 
        /// Returns the names associated with the first table name in the property list
        /// </summary>
        /// <returns>A list of fieldnames.</returns>
        private List<string> GetFieldNames()
        {
            List<string> fieldNames = null;
            for (int i = 0; i < properties.Count; i++)
            {
                if (properties[i].Display != null && properties[i].Display.Type == DisplayType.TableName)
                {
                    IGridCell cell = grid.GetCell(1, i);
                    if (cell.Value != null && cell.Value.ToString() != string.Empty)
                    {
                        string tableName = cell.Value.ToString();
                        if (storage.Reader.TableNames.Contains(tableName))
                        {
                            fieldNames = storage.Reader.ColumnNames(tableName).ToList();
                            if (fieldNames.Contains("SimulationID"))
                                fieldNames.Add("SimulationName");
                        }
                    }
                }
            }
            return fieldNames;
        }

        /// <summary>
        /// Go find a crop property in the specified list of properties or if not
        /// found, find the first crop in scope.
        /// </summary>
        /// <param name="properties">The list of properties to look through.</param>
        /// <returns>The found crop or null if none found.</returns>
        private IPlant GetCrop(List<IVariable> properties)
        {
            foreach (IVariable property in properties)
            {
                if (property.DataType == typeof(IPlant))
                {
                    IPlant plant = property.Value as IPlant;
                    if (plant != null)
                    {
                        return plant;
                    }
                }
            }

            // Not found so look for one in scope.
            return Apsim.Find(model, typeof(IPlant)) as IPlant;
        }

        private string[] GetResidueNames()
        {
            if (model is SurfaceOrganicMatter)
            {
                List<string> names = new List<string>();
                names = (this.model as SurfaceOrganicMatter).ResidueTypeNames();
                names.Sort();
                return names.ToArray();
            }
            return null;
        }

        /// <summary>
        /// Gets the names of all the items for each ResourceGroup whose items you want to put into a dropdown list.
        /// eg. "AnimalFoodStore,HumanFoodStore,ProductStore"
        /// Will create a dropdown list with all the items from the AnimalFoodStore, HumanFoodStore and ProductStore.
        /// 
        /// To help uniquely identify items in the dropdown list will need to add the ResourceGroup name to the item name.
        /// eg. The names in the drop down list will become AnimalFoodStore.Wheat, HumanFoodStore.Wheat, ProductStore.Wheat, etc. 
        /// </summary>
        /// <returns>Will create a string array with all the items from the AnimalFoodStore, HumanFoodStore and ProductStore.
        /// to help uniquely identify items in the dropdown list will need to add the ResourceGroup name to the item name.
        /// eg. The names in the drop down list will become AnimalFoodStore.Wheat, HumanFoodStore.Wheat, ProductStore.Wheat, etc. </returns>
        private string[] GetCLEMResourceNames(Type[] resourceNameResourceGroups)
        {
            List<string> result = new List<string>();
            ZoneCLEM zoneCLEM = Apsim.Parent(this.model, typeof(ZoneCLEM)) as ZoneCLEM;
            ResourcesHolder resHolder = Apsim.Child(zoneCLEM, typeof(ResourcesHolder)) as ResourcesHolder;
            if (resourceNameResourceGroups != null)
            {
                // resource groups specified (use them)
                foreach (Type resGroupType in resourceNameResourceGroups)
                {
                    IModel resGroup = Apsim.Child(resHolder, resGroupType);
                    if (resGroup != null)  //see if this group type is included in this particular simulation.
                    {
                        foreach (IModel item in resGroup.Children)
                        {
                            if (item.GetType() != typeof(Memo))
                            {
                                result.Add(resGroup.Name + "." + item.Name);
                            }
                        }
                    }
                }
            }
            else
            {
                // no resource groups specified so use all avaliable resources
                foreach (IModel resGroup in Apsim.Children(resHolder, typeof(IModel)))
                {
                    foreach (IModel item in resGroup.Children)
                    {
                        if (item.GetType() != typeof(Memo))
                        {
                            result.Add(resGroup.Name + "." + item.Name);
                        }
                    }
                }

            }
            return result.ToArray();
        }

        /// <summary>
        /// Gets a list of all models of a given type which are in
        /// scope of a given model.
        /// </summary>
        /// <param name="t">Type of models to search for.</param>
        private string[] GetModelNames(Type t)
        {
            List<IModel> models;
            if (t == null)
                models = Apsim.FindAll(model);
            else
                models = Apsim.FindAll(model, t);

            List<string> modelNames = new List<string>();
            foreach (IModel model in models)
                modelNames.Add(Apsim.FullPath(model));
            return modelNames.ToArray();
        }

        /// <summary>
        /// User has changed the value of a cell. Validate the change
        /// apply the change.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="args">Event parameters</param>
        private void OnCellsChanged(object sender, GridCellsChangedArgs args)
        {
            List<ChangeProperty.Property> changes = new List<ChangeProperty.Property>();
            foreach (GridCellChangedArgs cell in args.ChangedCells)
            {
                if (cell.NewValue == cell.OldValue)
                    continue; // silently fail

                // If there are multiple changed cells, each change will be
                // individually undoable.
                IVariable property = GetProperty(cell.RowIndex, cell.ColIndex);
                if (property == null)
                    continue;

                // Parse the input string to the appropriate type.
                object newValue = GetNewPropertyValue(property, cell);

                // Update the value of the model's property.
                SetPropertyValue(property, newValue);

                // Update the value shown in the grid.
                grid.DataSource.Rows[cell.RowIndex][cell.ColIndex] = GetCellValue(property, cell.RowIndex, cell.ColIndex);
            }

            UpdateReadOnlyProperties();
        }

        protected virtual void UpdateReadOnlyProperties()
        {
        }

        /// <summary>
        /// Fetches from the model the value which should be displayed in a given cell.
        /// </summary>
        /// <param name="row">Row index of the cell.</param>
        /// <param name="column">Column index of the cell.</param>
        protected virtual object GetCellValue(IVariable property, int row, int column)
        {
            return property.ValueWithArrayHandling;
        }

        /// <summary>
        /// Gets the property in a given displayed by a given cell.
        /// </summary>
        /// <remarks>
        /// <param name="row">Row inex.</param>
        /// <param name="column">Column index.</param>
        protected virtual IVariable GetProperty(int row, int column)
        {
            return properties[row];
        }

        /// <summary>
        /// Gets the new value of the cell from a string containing the
        /// cell's new contents.
        /// </summary>
        /// <param name="cell">Cell which has been changed.</param>
        protected virtual object GetNewPropertyValue(IVariable property, GridCellChangedArgs cell)
        {
            if (typeof(IPlant).IsAssignableFrom(property.DataType))
                return Apsim.Find(property.Object as IModel, cell.NewValue);

            if (property.Display != null && property.Display.Type == DisplayType.Model)
                return Apsim.Get(property.Object as IModel, cell.NewValue);

            try
            {
                return ReflectionUtilities.StringToObject(property.DataType, cell.NewValue, CultureInfo.CurrentCulture);
            }
            catch (FormatException err)
            {
                throw new Exception($"Value '{cell.NewValue}' is invalid for property '{property.Name}' - {err.Message}.");
            }
        }

        /// <summary>
        /// Set the value of the specified property
        /// </summary>
        /// <param name="property">The property to set the value of</param>
        /// <param name="value">The value to set the property to</param>
        private void SetPropertyValue(IVariable property, object value)
        {
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
            try
            {
                ChangeProperty cmd = new ChangeProperty(property.Object, property.Name, value);
                presenter.CommandHistory.Add(cmd);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
            presenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// The model has changed, update the grid.
        /// </summary>
        /// <param name="changedModel">The model that has changed</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == model)
                PopulateGrid(model);
        }

        /// <summary>
        /// Called when user clicks on a file name.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnFileBrowseClick(object sender, GridCellChangedArgs e)
        {
            // todo - add file extension paramter to display attribute?
            IFileDialog fileChooser = new FileDialog()
            {
                Action = FileDialog.FileActionType.Open,
                Prompt = "Select file path",
                InitialDirectory = e.OldValue
            };
            string fileName = fileChooser.GetFile();

            if (!string.IsNullOrWhiteSpace(fileName) && fileName != e.OldValue)
            {
                e.NewValue = fileName;
                OnCellsChanged(sender, new GridCellsChangedArgs(e));
                PopulateGrid(model);
            }
        }

        /// <summary>
        /// Invoked when the user selects an item in the intellisense.
        /// Inserts the selected item at the caret.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        {
            grid.InsertText(args.ItemSelected);
        }
    }
}
