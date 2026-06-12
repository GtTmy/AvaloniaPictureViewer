using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaPictureViewer
{
    public class ViewModel : ObservableObject, IDisposable
    {
        private readonly AestheticScoreWorker _scoreWorker = new AestheticScoreWorker();
        private readonly Dictionary<string, double> _scoreCache = new Dictionary<string, double>();
        private int _scoreRequestSerial;
        private string _currentPicturePath;
        private string _picturePath;
        private PictureItem _selectedPicture;
        private string _title = "Picture Workspace";
        private string _scoreText = "スコア: 未選択";
        private string _scoreValue = "--";
        private string _fileName = "画像を選択してください";
        private string _folderName = "--";
        private string _fileSizeText = "--";
        private string _pageNum = "0 / 0";
        private string _clipboardStatus = "チェックした画像をFinderへコピーできます";
        private IReadOnlyList<PictureItem> _pictures = Array.Empty<PictureItem>();
        private int _checkedCount;

        public ViewModel()
        {
            NextPageCommand.Subscribe(_ => Navigate(next: true));
            PrevPageCommand.Subscribe(_ => Navigate(next: false));
        }

        public string Title
        {
            get => _title;
            private set => SetProperty(ref _title, value);
        }

        public string PicturePath
        {
            get => _picturePath;
            private set => SetProperty(ref _picturePath, value);
        }

        public PictureItem SelectedPicture
        {
            get => _selectedPicture;
            set
            {
                if (SetProperty(ref _selectedPicture, value) &&
                    PictureSelecter != null &&
                    value != null)
                {
                    PictureSelecter.Select(value.Path);
                    ShowCurrentPicture();
                }
            }
        }

        public IReadOnlyList<PictureItem> Pictures
        {
            get => _pictures;
            private set => SetProperty(ref _pictures, value);
        }

        public int CheckedCount
        {
            get => _checkedCount;
            private set => SetProperty(ref _checkedCount, value);
        }

        public string CheckedCountText => $"{CheckedCount} selected";

        public string ClipboardStatus
        {
            get => _clipboardStatus;
            private set => SetProperty(ref _clipboardStatus, value);
        }

        public string ScoreText
        {
            get => _scoreText;
            private set => SetProperty(ref _scoreText, value);
        }

        public string ScoreValue
        {
            get => _scoreValue;
            private set => SetProperty(ref _scoreValue, value);
        }

        public string FileName
        {
            get => _fileName;
            private set => SetProperty(ref _fileName, value);
        }

        public string FolderName
        {
            get => _folderName;
            private set => SetProperty(ref _folderName, value);
        }

        public string FileSizeText
        {
            get => _fileSizeText;
            private set => SetProperty(ref _fileSizeText, value);
        }

        public string PageNum
        {
            get => _pageNum;
            private set => SetProperty(ref _pageNum, value);
        }

        public ReactiveCommand NextPageCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PrevPageCommand { get; } = new ReactiveCommand();

        private PictureSelecter PictureSelecter { get; set; }

        public bool IsPictureSelected() => PictureSelecter != null;

        public void SetFilename(string filename)
        {
            if (File.Exists(filename) &&
                PictureSelecter.SupportedExtensions.Contains(Path.GetExtension(filename).Substring(1)))
            {
                var fullPath = Path.GetFullPath(filename);
                PictureSelecter = new PictureSelecter(fullPath);
                Pictures = PictureSelecter.Pictures
                    .Select(path => new PictureItem(path, UpdateCheckedCount))
                    .ToList();
                SelectedPicture = FindPictureItem(PictureSelecter.CurrentPicture);
            }
        }

        public IReadOnlyList<string> GetCheckedPaths()
        {
            return Pictures.Where(picture => picture.IsChecked).Select(picture => picture.Path).ToList();
        }

        public void SetClipboardResult(int copiedCount, string error = null)
        {
            ClipboardStatus = error == null
                ? $"{copiedCount}枚の画像をFinderへコピーしました"
                : $"コピーできませんでした: {error}";
        }

        private void Navigate(bool next)
        {
            if (PictureSelecter == null)
            {
                return;
            }

            if (next)
            {
                PictureSelecter.MoveNext();
            }
            else
            {
                PictureSelecter.MovePrev();
            }

            SelectedPicture = FindPictureItem(PictureSelecter.CurrentPicture);
        }

        private void ShowCurrentPicture()
        {
            var path = PictureSelecter.CurrentPicture;
            PicturePath = path;
            PageNum = PictureSelecter.PageNumForUser;
            FileName = Path.GetFileName(path);
            FolderName = Path.GetFileName(PictureSelecter.DirName);
            FileSizeText = FormatFileSize(new FileInfo(path).Length);
            Title = $"{FileName} - Picture Workspace";
            _ = UpdateScoreAsync(path);
        }

        private PictureItem FindPictureItem(string path)
        {
            return Pictures.First(picture => picture.Path == path);
        }

        private void UpdateCheckedCount()
        {
            CheckedCount = Pictures.Count(picture => picture.IsChecked);
            OnPropertyChanged(nameof(CheckedCountText));
            ClipboardStatus = CheckedCount == 0
                ? "チェックした画像をFinderへコピーできます"
                : $"{CheckedCount}枚の画像を選択中";
        }

        private async Task UpdateScoreAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                SetScoreText("スコア: 未選択");
                SetScoreValue("--");
                return;
            }

            var fullPath = Path.GetFullPath(path);
            _currentPicturePath = fullPath;
            var requestSerial = Interlocked.Increment(ref _scoreRequestSerial);

            if (_scoreCache.TryGetValue(fullPath, out var cachedScore))
            {
                SetScoreText(FormatScore(cachedScore));
                SetScoreValue(cachedScore.ToString("0.00"));
                return;
            }

            SetScoreText("スコア: 計算中...");
            SetScoreValue("...");

            try
            {
                var score = await _scoreWorker.ScoreAsync(fullPath, requestSerial.ToString()).ConfigureAwait(false);
                _scoreCache[fullPath] = score;

                if (requestSerial == _scoreRequestSerial && _currentPicturePath == fullPath)
                {
                    SetScoreText(FormatScore(score));
                    SetScoreValue(score.ToString("0.00"));
                }
            }
            catch (Exception ex)
            {
                if (requestSerial == _scoreRequestSerial && _currentPicturePath == fullPath)
                {
                    SetScoreText($"スコア: エラー ({GetShortError(ex)})");
                    SetScoreValue("ERR");
                }
            }
        }

        private static string FormatScore(double score) => $"スコア: {score:0.00}";

        private static string FormatFileSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return $"{bytes / (1024d * 1024d):0.0} MB";
            }

            return $"{bytes / 1024d:0} KB";
        }

        private void SetScoreText(string value)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                ScoreText = value;
            }
            else
            {
                Dispatcher.UIThread.Post(() => ScoreText = value);
            }
        }

        private void SetScoreValue(string value)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                ScoreValue = value;
            }
            else
            {
                Dispatcher.UIThread.Post(() => ScoreValue = value);
            }
        }

        private static string GetShortError(Exception ex)
        {
            var message = ex.Message;
            if (string.IsNullOrWhiteSpace(message))
            {
                return "計算失敗";
            }

            return message.Length <= 48 ? message : message.Substring(0, 48) + "...";
        }

        public void Dispose()
        {
            _scoreWorker.Dispose();
        }
    }
}
