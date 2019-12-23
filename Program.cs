using System;
using System.Linq;
using System.Threading.Tasks;
using ErichMusick.Tools.OneDrive.PhotoSorter;
using ErichMusick.Tools.OneDrive.PhotoSorter.Model;
using ErichMusick.Tools.OneDrive.PhotoSorter.PhotoClassification;
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
            //var folders = await photoSorter.GetFoldersAsync(root, recurse : true);
            // OR: Run this once to print it out and hard code it!
            await photoSorter.ListFoldersAsync(root, recurse : true);

            // This assumes a single "Camera Roll" folder in the drive.
            // var cameraRoll = folders.Where(f => f.FullName.EndsWith("/Camera Roll")).FirstOrDefault();
            // Console.WriteLine($"Found Camera Roll at: Id={cameraRoll?.Id}, Name={cameraRoll?.Name}, ParentName={cameraRoll.ParentName}");

            // // This assumes a single "iPhone" folder in the drive.
            // var iPhone = folders.Where(f => f.FullName.EndsWith("/iPhone")).FirstOrDefault();
            // Console.WriteLine($"Found iPhone at: Id={iPhone?.Id}, Name={iPhone?.Name}, ParentName={iPhone.ParentName}");

            // Copy/Paste that output into this, and uncomment:
            // var cameraRoll = new FolderModel("DRIVEID!123", "Camera Roll", "/root/Pictures");
            // var myPhone = new FolderModel("DRIVEID!456", "iPhone", "/root/Pictures");

            // Prepare library:
            // This is very brute-force; could instead create folders on demand.
            //await photoSorter.CreateFolderForEachMonth(myPhone);

            //
            // Check heuristics ... are we matching iPhone photos properly?
            //
            //await photoSorter.ListItemsWithoutClassification(cameraRoll);

            //
            // Move items not in month-specific folders to month-specific folders.
            //
            //await photoSorter.MovePhotosToMonthSpecificFolder(cameraRoll, cameraRoll);

            //
            // Move iPhone photos from Camera Roll => iPhone
            //
            //await photoSorter.MovePhotos(cameraRoll, myPhone, (i) => i.Classification?.Type == ItemType.Photo);
        }
    }
}