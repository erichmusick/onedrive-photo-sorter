using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErichMusick.Tools.OneDrive.PhotoSorter.Controllers;
using ErichMusick.Tools.OneDrive.PhotoSorter.Model;

namespace ErichMusick.Tools.OneDrive.PhotoSorter
{
    internal class FolderProvider
    {
        public const String PathSeparator = "/";
        private readonly FolderModel _root;
        private readonly ItemsController _controller;
        private bool _initialized = false;
        private Dictionary<string, FolderModel> _foldersByPath;

        public FolderProvider(FolderModel root, ItemsController controller)
        {
            _root = root;
            _controller = controller;
        }

        private async Task EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            var folders = await _controller.GetFolders(_root, recursive : true);
            _foldersByPath = folders.ToDictionary(item => item.FullName, item => new FolderModel(item.Id, item.Name, item.Folder));
        }

        public async Task<FolderModel> GetOrCreate(string fullPath)
        {
            await EnsureInitialized();

            if (_foldersByPath.TryGetValue(fullPath, out var folder))
            {
                return folder;
            }

            // "/root/Pictures/iPhone/2014/02" => ["2014", "02"]
            // +1 for the trailing slash
            var parts = fullPath.Substring(_root.FullName.Length + 1).Split(PathSeparator);
            if (parts.Length == 0)
            {
                throw new ArgumentException("path has no parts");
            }

            FolderModel current = _root;
            string path = _root.FullName;
            for (int i = 0; i < parts.Length; i++)
            {
                path = path + PathSeparator + parts[i];
                if (!_foldersByPath.ContainsKey(path))
                {
                    Console.WriteLine($"Creating folder {parts[i]} at {current.FullName}");
                    var child = await _controller.CreateFolder(parts[i], current);
                    _foldersByPath[path] = child;
                    current = child;
                }
                else
                {
                    current = _foldersByPath[path];
                }
            }

            return _foldersByPath[fullPath];
        }
    }
}