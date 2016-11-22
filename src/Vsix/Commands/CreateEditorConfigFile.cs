﻿using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Windows;

namespace EditorConfig
{
    internal sealed class CreateEditorConfigFile
    {
        private readonly Package _package;

        private CreateEditorConfigFile(Package package)
        {
            _package = package ?? throw new ArgumentNullException("package");

            var commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var cmdId = new CommandID(new Guid("a21ee3c5-45b9-4bd8-abd7-30ceb3d990c5"), 0x0100);
                var menuItem = new MenuCommand(CreateFile, cmdId);
                commandService.AddCommand(menuItem);
            }
        }
        public static CreateEditorConfigFile Instance
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new CreateEditorConfigFile(package);
        }

        private void CreateFile(object sender, EventArgs e)
        {
            var dte = ServiceProvider.GetService(typeof(DTE)) as DTE2;
            var folder = ProjectHelpers.GetSelectedItemPath(out object item);

            if (string.IsNullOrEmpty(folder))
                return;

            string fileName = Path.Combine(folder, ".editorconfig");

            if (File.Exists(fileName))
            {
                MessageBox.Show("An .editorconfig file already exist in this location", "EditorConfig Language Service", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ProjectItem newItem = null;
                File.WriteAllText(fileName, "[*]\r\nend_of_line = crlf\r\n\r\n[*.xml]\r\nindent_style = space");

                if (item is Project proj)
                {                    
                    newItem = proj.AddFileToProject(fileName, "None");
                }
                else if (item is ProjectItem projItem && projItem.ContainingProject != null)
                {
                    newItem = projItem.ContainingProject.AddFileToProject(fileName, "None");
                }
                else if (item is Solution2 solution)
                {
                    newItem = AddFileToSolution(fileName, solution);
                }

                if (newItem != null)
                {
                    VsShellUtilities.OpenDocument(ServiceProvider, fileName);
                    dte.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument");
                    dte.ActiveDocument.Activate();
                }
            }
        }

        private static ProjectItem AddFileToSolution(string fileName, Solution2 solution)
        {
            Project currentProject = null;

            foreach (Project project in solution.Projects)
            {
                if (project.Kind == Constants.vsProjectKindSolutionItems && project.Name == "Solution Items")
                {
                    currentProject = project;
                    break;
                }
            }

            if (currentProject == null)
                currentProject = solution.AddSolutionFolder("Solution Items");

            return currentProject.AddFileToProject(fileName, "None");
        }

        private static string FindFolder(object item)
        {
            if (item == null)
                return null;

            string folder = null;
            
            if (item is ProjectItem projectItem)
            {
                string fileName = projectItem.FileNames[1];

                if (File.Exists(fileName))
                {
                    folder = Path.GetDirectoryName(fileName);
                }
                else
                {
                    folder = fileName;
                }
            }
            else if (item is Project project )
            {
                folder = project.GetRootFolder();
            }
            else if (item is Solution solution)
            {
                folder = Path.GetDirectoryName(solution.FileName);
            }

            return folder;
        }
    }
}
