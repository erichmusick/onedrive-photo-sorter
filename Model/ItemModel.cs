namespace ErichMusick.Tools.OneDrive.PhotoSorter.Models
{
    using ErichMusick.Tools.OneDrive.PhotoSorter.Model;
    using ErichMusick.Tools.OneDrive.PhotoSorter.PhotoClassification;
    using Microsoft.Graph;

    public class ItemModel
    {
        internal ItemModel(DriveItem item, FolderModel folder)
        {
            this.Item = item;
            this.Folder = folder;
        }

        public string Id
        {
            get
            {
                return this.Item == null ? null : this.Item.Id;
            }
        }

        public DriveItem Item { get; private set; }
        internal FolderModel Folder { get; }

        public string Name
        {
            get
            {
                return this.Item.Name;
            }
        }

        public string FullName
        {
            get
            {
                return this.Folder?.FullName + "/" + this.Item.Name;
            }
        }

        internal Classification Classification { get; set; }
    }
}