using NaturalSort.Extension;
using System;
using System.Collections.Generic;
using System.Linq;


namespace AvaloniaPictureViewer
{
    public class PictureSelecter
    {
        public string DirName { get; }
        public IList<string> Pictures { get; }

        public static List<string> SupportedExtensions { get; } = new List<string>
        {
            "jpg", "JPG", "jpeg", "JPEG", "png", "PNG"
        };

        public PictureSelecter(string filename)
        {

            DirName = System.IO.Path.GetDirectoryName(filename);
            Pictures = System.IO.Directory.EnumerateFiles(DirName)
                .Where(x => SupportedExtensions.Select(x => $".{x}").Contains(System.IO.Path.GetExtension(x)))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase.WithNaturalSort())
                .ToList();
            var filenameBody = System.IO.Path.GetFileName(filename);
            var index = Pictures.Select((path, index) => new { Filename = System.IO.Path.GetFileName(path), Index = index }).First(x => x.Filename == filenameBody).Index;
            CurrentIndex = index;
        }

        public int CurrentIndex { get; private set; } = 0;

        public string PageNumForUser => $"{CurrentIndex + 1} / {Pictures.Count()}";

        public string CurrentPicture => Pictures[CurrentIndex];

        public void MoveNext()
        {
            CurrentIndex = (CurrentIndex < Pictures.Count() - 1) ? CurrentIndex + 1 : 0;
        }

        public void MovePrev()
        {
            CurrentIndex = (CurrentIndex > 0) ? CurrentIndex - 1 : Pictures.Count() - 1;
        }

    }
}
