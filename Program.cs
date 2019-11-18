using System;
using System.Linq;
using System.Threading.Tasks;
using ErichMusick.Tools.OneDrive.PhotoSorter;
using Newtonsoft.Json;

namespace OneDrivePhotoSorter
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                DoIt().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex);
                Console.ResetColor();
            }

            Console.WriteLine("exiting");
        }

        public async static Task DoIt()
        {
            var photoSorter = await PhotoSorterFactory.Create(runSampleRequest: true);

            // Query Folders
            var root = await photoSorter.GetRootFolder();
            Console.WriteLine(JsonConvert.SerializeObject(root));

            // Not necessarily efficient if your drive is large.
            // Better to query in the UI and grab folder ids from the query string.
            // OR: Run this once, print it out, and hard code it!
            var folders = await photoSorter.GetFoldersAsync(root, recurse : true);

            // This assumes a single "Camera Roll" folder in the drive.
            var cameraRoll = folders.Where(f => f.FullName.EndsWith("/Camera Roll")).FirstOrDefault();
            Console.WriteLine($"Found Camera Roll at: Id={cameraRoll?.Id}, Name={cameraRoll?.Name}, ParentName={cameraRoll.ParentName}");

            // This assumes a single "WhatsApp" folder in the drive.
            var whatsApp = folders.Where(f => f.FullName.EndsWith("/WhatsApp")).FirstOrDefault();
            Console.WriteLine($"Found WhatsApp at: Id={whatsApp?.Id}, Name={whatsApp?.Name}, ParentName={whatsApp.ParentName}");

            // Copy/Paste that output into this, and uncomment:
            // var cameraRoll = new FolderItem("DRIVEID!123", "Camera Roll", "/root/Pictures");
            // var whatsApp = new FolderItem("DRIVEID!456", "WhatsApp", "/root/Pictures");

            // Prepare library:
            // This is very brute-force; could instead create folders on demand.
            //await photoSorter.CreateFolderForEachMonth(whatsApp, i);

            //
            // Check heuristics ... are we matching WhatsApp photos properly?
            //
            await photoSorter.ListItemsWithoutClassification(cameraRoll);

            //
            // Move items not in month-specific folders to month-specific folders.
            //
            //await photoSorter.MovePhotosToMonthSpecificFolder(cameraRoll, cameraRoll);

            //
            // Move WhatsApp photos from Camera Roll => WhatsApp
            //
            // await photoSorter.MovePhotos(cameraRoll, whatsApp, (i) => i.Classification?.Type == ItemType.WhatsAppPhoto, _controller);
        }
    }
}