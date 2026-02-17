using Palisades.Helpers;
using Palisades.Model;
using Palisades.View;
using Palisades.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml.Serialization;

namespace Palisades
{
    internal static class PalisadesManager
    {
        public static readonly Dictionary<string, Window> palisades = new();

        public static void LoadPalisades()
        {
            string saveDirectory = PDirectory.GetPalisadesDirectory();
            PDirectory.EnsureExists(saveDirectory);

            List<PalisadeModel> loadedModels = new();

            foreach (string identifierDirname in Directory.GetDirectories(saveDirectory))
            {
                string stateFile = Path.Combine(identifierDirname, "state.xml");
                if (!File.Exists(stateFile))
                    continue;

                try
                {
                    XmlSerializer deserializer = new(typeof(PalisadeModel));
                    using StreamReader reader = new(stateFile);
                    if (deserializer.Deserialize(reader) is PalisadeModel model)
                    {
                        loadedModels.Add(model);
                    }
                    reader.Close();
                }
                catch
                {
                    // Skip corrupted state files
                }
            }

            foreach (PalisadeModel loadedModel in loadedModels)
            {
                if (loadedModel.Type == PalisadeType.FolderPortal)
                {
                    var viewModel = new FolderPortalViewModel(loadedModel);
                    palisades.Add(loadedModel.Identifier, new FolderPortal(viewModel));
                }
                else
                {
                    palisades.Add(loadedModel.Identifier, new Palisade(new PalisadeViewModel(loadedModel)));
                }
            }
        }

        public static void CreatePalisade()
        {
            PalisadeViewModel viewModel = new();
            palisades.Add(viewModel.Identifier, new Palisade(viewModel));
            viewModel.Save();
        }

        public static void CreateFolderPortal(string rootPath, string title)
        {
            PalisadeModel model = new()
            {
                Type = PalisadeType.FolderPortal,
                Name = title,
                RootPath = rootPath,
                CurrentPath = rootPath
            };

            FolderPortalViewModel viewModel = new(model);
            palisades.Add(viewModel.Identifier, new FolderPortal(viewModel));
            viewModel.Save();
        }

        public static void ShowCreateFolderPortalDialog()
        {
            CreateFolderPortalDialog dialog = new();
            if (dialog.ShowDialog() == true)
            {
                CreateFolderPortal(dialog.SelectedPath, dialog.PortalTitle);
            }
        }

        public static void DeletePalisade(string identifier)
        {
            palisades.TryGetValue(identifier, out Window? window);
            if (window == null)
            {
                return;
            }

            if (window.DataContext is PalisadeViewModel palisadeVm)
            {
                palisadeVm.Delete();
            }
            else if (window.DataContext is FolderPortalViewModel folderPortalVm)
            {
                folderPortalVm.Delete();
            }

            window.Close();
            palisades.Remove(identifier);
        }

        public static Window GetWindow(string identifier)
        {
            palisades.TryGetValue(identifier, out Window? window);
            if (window == null)
            {
                throw new KeyNotFoundException(identifier);
            }
            return window;
        }

        public static Palisade GetPalisade(string identifier)
        {
            var window = GetWindow(identifier);
            if (window is Palisade palisade)
            {
                return palisade;
            }
            throw new KeyNotFoundException(identifier);
        }
    }
}
