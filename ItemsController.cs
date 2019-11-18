namespace ErichMusick.Tools.OneDrive.PhotoSorter.Controllers
{

    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System;
    using ErichMusick.Tools.OneDrive.PhotoSorter.Model;
    using ErichMusick.Tools.OneDrive.PhotoSorter.PhotoClassification;
    using Microsoft.Graph;
    using Models;
    using Newtonsoft.Json;

    public class ItemsController
    {
        private static readonly HttpClient Client = new HttpClient();
        private readonly GraphServiceClient _graphClient;

        public ItemsController(GraphServiceClient graphClient)
        {
            this._graphClient = graphClient;
        }

        /// <summary>
        /// Gets the root folder.
        /// </summary>
        internal async Task<FolderModel> GetRootFolder()
        {
            var itemRequest = this._graphClient.Me.Drive.Root.Request();
            var item = await itemRequest.GetAsync();

            return new FolderModel(item.Id, item.Name, string.Empty);
        }

        /// <summary>
        /// Gets the child folders of the specified item ID.
        /// </summary>
        /// <param name="id">The ID of the parent item.</param>
        /// <returns>The child folders of the specified item ID.</returns>
        internal async Task<ICollection<ItemModel>> GetFolders(FolderModel folder, bool recursive = false)
        {
            string id = folder.Id;
            List<ItemModel> results = new List<ItemModel>();

            IEnumerable<DriveItem> items;

            var expandString = "thumbnails, children($expand=thumbnails)";

            // If id isn't set, get the OneDrive root's photos and folders. Otherwise, get those for the specified item ID.
            // Also retrieve the thumbnails for each item if using a consumer client.
            var itemRequest = this._graphClient.Me.Drive.Items[id].Request().Expand(expandString);

            var item = await itemRequest.GetAsync();
            items = item.Children == null ?
                new List<DriveItem>() :
                item.Children.CurrentPage.Where(child => child.Folder != null);

            foreach (var child in items)
            {
                results.Add(new ItemModel(child, folder));

                if (recursive)
                {
                    results.AddRange(await GetFolders(new FolderModel(child.Id, child.Name, folder.FullName)));
                }
            }

            return results;
        }

        /// <summary>
        /// Gets the child folders and photos of the specified item ID.
        /// </summary>
        /// <param name="id">The ID of the parent item.</param>
        /// <returns>The child folders and photos of the specified item ID.</returns>
        internal async Task<ICollection<ItemModel>> GetImagesAndFolders(FolderModel folder)
        {
            string id = folder.Id;
            List<ItemModel> results = new List<ItemModel>();

            // If id isn't set, get the OneDrive root's photos and folders. Otherwise, get those for the specified item ID.
            // Also retrieve the thumbnails for each item if using a consumer client.
            var itemRequest = this._graphClient.Me.Drive.Items[id].Children.Request()
                .Top(1000);

            var classificationChain = IItemClassifier.CreateChain();
            while (itemRequest != null)
            {
                Console.WriteLine($"GetImagesAndFolders for {folder.Id}: {folder.FullName}::Invoking {itemRequest.Method} {itemRequest.RequestUrl}");
                var i = await itemRequest.GetAsync();
                Console.WriteLine($"==>Found {i.Count}. NextPage={i.NextPageRequest}");

                // Scope to Folders + Images
                // Note: Library also contains videos; it's harder to tell original source
                // from those since there's no exif-type data in the video. We've got all
                // sorts of attributes of the video, but no make/model.
                foreach (var child in i) // .Where(child => child.Folder != null || child.Image != null))
                {
                    var itemModel = new ItemModel(child, folder);

                    var classification = classificationChain.Classify(itemModel);
                    itemModel.Classification = classification;

                    results.Add(itemModel);
                }

                itemRequest = i.NextPageRequest;
            }

            return results;
        }

        internal async Task<ICollection<ItemModel>> GetImagesAndFoldersRecursiveAsync(FolderModel root)
        {
            List<ItemModel> results = new List<ItemModel>();

            Queue<FolderModel> folders = new Queue<FolderModel>();
            folders.Enqueue(root);

            while (folders.TryDequeue(out FolderModel folder))
            {
                var stuff = await GetImagesAndFolders(folder);

                foreach (var item in stuff)
                {
                    // Traverse folders
                    if (item.Classification?.Type == ItemType.Folder)
                    {
                        folders.Enqueue(new FolderModel(item.Id, item.Name, folder.FullName));
                    }

                    results.Add(item);
                }
            }

            return results;
        }

        internal async Task MoveItem(ItemModel item, FolderModel destination)
        {
            // TODO: How does move work in the Graph SDK?
            var request = new HttpRequestMessage(HttpMethod.Patch, "https://graph.microsoft.com/v1.0/me/drive/items/" + item.Id);

            await _graphClient.AuthenticationProvider.AuthenticateRequestAsync(request);

            request.Content = new StringContent(
                JsonConvert.SerializeObject(new
                {
                    parentReference = new
                    {
                        id = destination.Id
                    },
                }),
                Encoding.UTF8, "application/json");

            var response = await Client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Copy failed: " + response.StatusCode);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseBody);
        }

        internal async Task CreateFolder(string name, FolderModel parent)
        {
            var driveItem = new DriveItem
            {
                Name = name,
                Folder = new Folder { },
                AdditionalData = new Dictionary<string, object>()
                { { "@microsoft.graph.conflictBehavior", "fail" }
                }
            };

            await _graphClient.Me.Drive.Items[parent.Id].Children
                .Request()
                .AddAsync(driveItem);
        }
    }
}