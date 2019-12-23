using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ErichMusick.Tools.OneDrive.PhotoSorter.Controllers;
using ErichMusick.Tools.OneDrive.PhotoSorter.Model;
using ErichMusick.Tools.OneDrive.PhotoSorter.Models;
using ErichMusick.Tools.OneDrive.PhotoSorter.PhotoClassification;
using Polly;
using Polly.Bulkhead;

namespace ErichMusick.Tools.OneDrive.PhotoSorter
{
    /// <summary>
    /// Core business logic of sorting / moving photos.
    /// Wraps granular OneDrive operations exposed through <see cref="ItemsController"/>.
    /// </summary>
    class PhotoSorter
    {
        private readonly AsyncBulkheadPolicy bulkhead = Policy.BulkheadAsync(16, int.MaxValue);

        public PhotoSorter(ItemsController controller)
        {
            _controller = controller;
        }

        public async Task<FolderModel> GetRootFolder()
        {
            return await _controller.GetRootFolder();
        }

        /// <summary>
        /// Gets sub folders of the given folder.
        /// If recurse is true, traverses the whole hierarchy and returns all folders.
        /// </summary>
        public async Task<ICollection<FolderModel>> GetFoldersAsync(FolderModel folder, bool recurse = false)
        {
            var folders = await _controller.GetFolders(folder, recurse);

            return folders.Select(f => new FolderModel(f.Id, f.Name, f.Folder)).ToList();
        }

        public async Task ListFoldersAsync(FolderModel folder, bool recurse = false)
        {
            var folders = await GetFoldersAsync(folder, recurse);

            foreach (var f in folders)
            {
                Console.WriteLine($"{f.FullName}: {f.Id}");
            }
        }

        public async Task ListItemsWithoutClassification(FolderModel root, bool recurse = true)
        {
            var items = recurse ? await _controller.GetImagesAndFoldersRecursiveAsync(root) : await _controller.GetImagesAndFolders(root);
            foreach (var item in items)
            {
                if (item.Classification == null || item.Classification?.Type == ItemType.Unclassified)
                {
                    Console.WriteLine($"{item.Item.Id}: {item.Item.Name}. Is={item.Classification}, Taken={item.Item.Photo.TakenDateTime}, Camera={item.Item.Photo?.CameraMake} {item.Item.Photo?.CameraModel}");
                    //Console.WriteLine($"AllData={JsonConvert.SerializeObject(item.Item)}");
                }
            }
        }

        public async Task MovePhotos(FolderModel sourceRoot, FolderModel destinationRoot, Func<ItemModel, bool> matches)
        {
            var destinations = new FolderProvider(destinationRoot, _controller);

            var items = await _controller.GetImagesAndFoldersRecursiveAsync(sourceRoot);
            Console.WriteLine($"Found {items.Count} at source.");
            var tasks = new List<Task>();
            foreach (var item in items.Where(matches))
            {
                // Camera Roll/YYYY/MM
                var currentPath = item.Folder.FullName;

                // Replace sourceRoot in item's current full path with destinationRoot.
                var folderPath = destinationRoot.FullName + currentPath.Substring(sourceRoot.FullName.Length);
                var destination = await destinations.GetOrCreate(folderPath);

                tasks.Add(
                    bulkhead.ExecuteAsync(async() =>
                    {
                        Console.WriteLine($"Moving {item.Name} from {item.Folder.FullName} to {destination.FullName}");
                        await _controller.MoveItem(item, destination);
                    })
                );
            }
            await Task.WhenAll(tasks);
        }

        public static async Task ListFolders(FolderModel root, ItemsController c)
        {
            var folders = await c.GetFolders(root, recursive : true);
            foreach (var folder in folders)
            {
                Console.WriteLine($"{folder.Id}: {folder.FullName}");
            }
        }

        private static readonly string[] ExpectedMonths = { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12" };
        private ItemsController _controller;

        /// <remarks>
        /// This is overly optimistic (i.e. expects at least one photo per month),
        /// but is mildly more straightforward than lazily creating folders as we go.
        /// </remarks>
        public async Task CreateFolderForEachMonth(FolderModel root)
        {
            var years = await _controller.GetFolders(root);
            foreach (var year in years)
            {
                Console.WriteLine($"{year.Name}");

                var folder = new FolderModel(year.Id, year.Name, root);
                var months = await _controller.GetFolders(folder);
                var missingMonths = ExpectedMonths.Except(months.Select(m => m.Name));

                foreach (var month in missingMonths)
                {
                    Console.WriteLine($"Creating {folder.FullName}/{month}");
                    await _controller.CreateFolder(month, folder);
                }
            }
        }

        // Move items inside searchRoot to appropriate folder in root.
        public async Task MovePhotosToMonthSpecificFolder(FolderModel root, FolderModel searchRoot)
        {
            List<Task> tasks = new List<Task>();

            // All folders
            var folders = await _controller.GetFolders(root, recursive : true);
            var folderByYearAndMonth = folders
                .ToLookup(item => item.FullName, item => new FolderModel(item.Id, item.Name, item.Folder));

            var items = await _controller.GetImagesAndFoldersRecursiveAsync(searchRoot);

            // Move any images out of root
            var photos = items.Where(i => i.Classification?.Type != ItemType.Folder).ToList();

            foreach (var item in photos)
            {
                // TODO: Generalize
                // This assumes a very specific (iOS) format which starts the filename
                // with YYYYMMDD_
                // Example: 20140216_194427722_iOS.jpg
                var matches = Regex.Match(item.Name, @"^(\d{4})(\d{2})(\d{2})_.*");
                if (matches.Success)
                {
                    // Camera Roll/YYYY/MM
                    var folderPath = $"Root/Pictures/Camera Roll/{matches.Groups[1]}/{matches.Groups[2]}";
                    if (folderPath != item.Folder.FullName)
                    {
                        var destination = folderByYearAndMonth[folderPath].FirstOrDefault();
                        Console.Write($"Moving {item.Name} ");
                        if (destination != null)
                        {
                            Console.WriteLine($"to {destination.FullName}");
                            tasks.Add(
                                bulkhead.ExecuteAsync(async() =>
                                {
                                    await _controller.MoveItem(item, destination);
                                })
                            );
                        }
                        else
                        {
                            Console.WriteLine($"destination not found at {folderPath}");
                        }
                    }
                }
            }

            await Task.WhenAll(tasks);
        }
    }
}