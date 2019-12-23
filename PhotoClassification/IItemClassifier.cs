using System.Linq;
using ErichMusick.Tools.OneDrive.PhotoSorter.Models;

namespace ErichMusick.Tools.OneDrive.PhotoSorter.PhotoClassification
{
    enum ItemType
    {
        Unclassified,

        Folder,
        File,
        Photo,
        Video,
    }

    class Classification
    {
        public Classification(ItemType type, string reason = null)
        {
            Type = type;
            Reason = reason;
        }

        public ItemType Type { get; }
        public string Reason { get; }

        override public string ToString()
        {
            return $"{Type} because {Reason}";
        }
    }

    interface IItemClassifier
    {
        Classification Classify(ItemModel item);

        static IItemClassifier CreateChain()
        {
            return new FolderClassifier().Then(
                new VideoClassifier(),

                new AppleIPhoneClassifier(),
                new AppleIOSScreenshotClassifier(),
                new PhotoWithExif(),

                // Must be Last:
                new UnclassifiedClassifier()
            );
        }
    }

    static class ClassifierExtensions
    {
        public static IItemClassifier Then(this IItemClassifier classifier, params IItemClassifier[] others)
        {
            return new CompositeClassifier(new [] { classifier }.Union(others).ToArray());
        }

        private class CompositeClassifier : IItemClassifier
        {
            private readonly IItemClassifier[] _classifiers;

            public CompositeClassifier(IItemClassifier[] classifiers)
            {
                _classifiers = classifiers;
            }

            public Classification Classify(ItemModel item)
            {
                foreach (var classifier in _classifiers)
                {
                    var result = classifier.Classify(item);
                    if (result != null)
                    {
                        return result;
                    }
                }

                return null;
            }
        }
    }

    class FolderClassifier : IItemClassifier
    {
        public Classification Classify(ItemModel item)
        {
            if (item.Item.Folder != null)
            {
                return new Classification(ItemType.Folder, "HasFolderProperty");
            }

            return null;
        }
    }

    class VideoClassifier : IItemClassifier
    {
        public Classification Classify(ItemModel item)
        {
            if (item.Item.Video != null)
            {
                return new Classification(ItemType.Video, "HasVideoProperty");
            }

            return null;
        }
    }

    class AppleIPhoneClassifier : IItemClassifier
    {
        public Classification Classify(ItemModel item)
        {
            var photo = item.Item.Photo;
            if (photo == null)
            {
                return null;
            }

            if (photo.CameraMake != "Apple")
            {
                return null;
            }

            if (!photo.CameraModel.StartsWith("iPhone "))
            {
                return null;
            }

            if (!(photo.CameraModel.EndsWith("5") || photo.CameraModel.EndsWith("6") || photo.CameraModel.EndsWith("11 Pro")))
            {
                return null;
            }

            return new Classification(ItemType.Photo, "iPhone" + photo.CameraModel.Substring("iPhone ".Length));
        }
    }

    class AppleIOSScreenshotClassifier : IItemClassifier
    {
        public Classification Classify(ItemModel item)
        {
            var photo = item.Item.Photo;
            if (photo == null)
            {
                return null;
            }

            if (!item.Name.EndsWith("_iOS.png"))
            {
                return null;
            }

            return new Classification(ItemType.Photo, "iOSScreenshot");
        }
    }

    /// <summary>
    /// Classifies items as "Unclassified". We weren't able to confidently
    /// determine the item's type.
    /// </summary>
    class UnclassifiedClassifier : IItemClassifier
    {
        public Classification Classify(ItemModel item)
        {
            return new Classification(ItemType.Unclassified, null);
        }
    }

    class PhotoWithExif : IItemClassifier
    {
        public Classification Classify(ItemModel item)
        {
            var photo = item.Item.Photo;
            if (photo == null)
            {
                return null;
            }

            if (photo.TakenDateTime == null && photo.ExposureNumerator == null)
            {
                return null;
            }

            return new Classification(ItemType.Photo, "HasExif");
        }
    }
}