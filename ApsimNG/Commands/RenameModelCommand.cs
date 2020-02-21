﻿namespace UserInterface.Commands
{
    using Models.Core;
    using Models.Core.ApsimFile;

    /// <summary>
    /// A command for renaming a model.
    /// </summary>
    class RenameModelCommand : ICommand
    {
        private Model modelToRename;
        private string newName;
        Interfaces.IExplorerView explorerView;
        private string originalName;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenameModelCommand"/> class.
        /// </summary>
        /// <param name="modelToRename">The model to rename.</param>
        /// <param name="newName">The new name.</param>
        /// <param name="explorerView">The explorer view.</param>
        public RenameModelCommand(Model modelToRename, string newName, Interfaces.IExplorerView explorerView)
        {
            if (modelToRename.ReadOnly)
                throw new ApsimXException(modelToRename, string.Format("Unable to rename {0} - it is read-only.", modelToRename.Name));
            this.modelToRename = modelToRename;
            this.newName = newName;
            this.explorerView = explorerView;
        }

        /// <summary>Performs the command.</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Do(CommandHistory commandHistory)
        {
            string originalPath = Apsim.FullPath(this.modelToRename);

            // Get original value of property so that we can restore it in Undo if needed.
            originalName = this.modelToRename.Name;

            // Set the new name.
            Structure.Rename(modelToRename, newName);
            explorerView.Tree.Rename(originalPath, this.modelToRename.Name);
        }

        /// <summary>Undoes the command.</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Undo(CommandHistory commandHistory)
        {
            explorerView.Tree.Rename(Apsim.FullPath(modelToRename), originalName);
            modelToRename.Name = originalName;
        }
    }
}
