namespace ErichMusick.Tools.OneDrive.PhotoSorter.Model
{
    class FolderModel
    {
        public FolderModel(string id, string name, FolderModel parent) : this(id, name, parent.FullName)
        { }

        public FolderModel(string id, string name, string parentName)
        {
            Id = id;
            Name = name;
            ParentName = parentName;
        }

        public string Id { get; }

        public string Name { get; }

        public string ParentName { get; }

        public string FullName => ParentName + "/" + Name;
    }
}