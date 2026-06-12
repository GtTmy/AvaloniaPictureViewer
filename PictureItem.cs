using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace AvaloniaPictureViewer
{
    public class PictureItem : ObservableObject
    {
        private bool _isChecked;
        private double? _score;

        public PictureItem(string path, Action checkStateChanged)
        {
            Path = path;
            FileName = System.IO.Path.GetFileName(path);
            LastModified = System.IO.File.GetLastWriteTime(path);
            CheckStateChanged = checkStateChanged;
        }

        public string Path { get; }
        public string FileName { get; }
        public DateTime LastModified { get; }
        public string LastModifiedText => LastModified.ToString("yyyy-MM-dd HH:mm");
        public string ScoreText => Score.HasValue ? Score.Value.ToString("0.00") : "--";

        public double? Score
        {
            get => _score;
            set
            {
                if (SetProperty(ref _score, value))
                {
                    OnPropertyChanged(nameof(ScoreText));
                }
            }
        }

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
