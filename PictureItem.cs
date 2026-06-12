using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace AvaloniaPictureViewer
{
    public class PictureItem : ObservableObject
    {
        private bool _isChecked;

        public PictureItem(string path, Action checkStateChanged)
        {
            Path = path;
            FileName = System.IO.Path.GetFileName(path);
            CheckStateChanged = checkStateChanged;
        }

        public string Path { get; }
        public string FileName { get; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (SetProperty(ref _isChecked, value))
                {
                    CheckStateChanged();
                }
            }
        }

        private Action CheckStateChanged { get; }
    }
}
