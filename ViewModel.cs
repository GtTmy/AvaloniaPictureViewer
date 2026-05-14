using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Threading;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.IO;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaPictureViewer
{
    public class ViewModel: ObservableObject, IDisposable
    {
        private readonly AestheticScoreWorker _scoreWorker = new AestheticScoreWorker();
        private readonly Dictionary<string, double> _scoreCache = new Dictionary<string, double>();
        private int _scoreRequestSerial;
        private string _currentPicturePath;
        private string _scoreText = "スコア: 未選択";

        public ReadOnlyReactiveProperty<string> Title { get; }
        public string ScoreText
        {
            get => _scoreText;
            private set => SetProperty(ref _scoreText, value);
        }

        public void SetFilename(string filename)
        {
            if (System.IO.File.Exists(filename) &&
                PictureSelecter.SupportedExtensions.Contains(System.IO.Path.GetExtension(filename).Substring(1)))
            {
                var fullpath = System.IO.Path.GetFullPath(filename);
                PictureSelecter = new PictureSelecter(fullpath);
                UpdatePic.OnNext(PictureSelecter.CurrentPicture);
            }
        }

        public Subject<string> UpdatePic { get; } = new Subject<string>();
        public ViewModel()
        {   
            //Title = "AvaloniaUIApps On " + System.Runtime.InteropServices.RuntimeInformation.OSDescription;

            var buttonClickedSource = Observable.Merge(
                NextPageCommand.Select(_ =>
                {
                    PictureSelecter.MoveNext();
                    return PictureSelecter.CurrentPicture;
                }),
                PrevPageCommand.Select(_ =>
                {
                    PictureSelecter.MovePrev();
                    return PictureSelecter.CurrentPicture;
                }),
                UpdatePic
            ).Publish();

            PicturePath = buttonClickedSource
                .ToReadOnlyReactiveProperty();

            buttonClickedSource.Subscribe(path => _ = UpdateScoreAsync(path));

            PageNum =
                buttonClickedSource
                .Select(_ => PictureSelecter.PageNumForUser)
                .ToReadOnlyReactiveProperty();
            buttonClickedSource.Connect();

            Title = buttonClickedSource
                .Select(path => $"{PictureSelecter.PageNumForUser} - {path}")
                .ToReadOnlyReactiveProperty();
        }

        PictureSelecter PictureSelecter { get; set; } = default;

        public bool IsPictureSelected() => PictureSelecter != null;

        public ReactiveCommand NextPageCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PrevPageCommand { get; } = new ReactiveCommand();

        public ReadOnlyReactiveProperty<string> PicturePath { get; }

        public ReadOnlyReactiveProperty<string> PageNum { get; }

        private async Task UpdateScoreAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                SetScoreText("スコア: 未選択");
                return;
            }

            var fullPath = Path.GetFullPath(path);
            _currentPicturePath = fullPath;
            var requestSerial = Interlocked.Increment(ref _scoreRequestSerial);

            if (_scoreCache.TryGetValue(fullPath, out var cachedScore))
            {
                SetScoreText(FormatScore(cachedScore));
                return;
            }

            SetScoreText("スコア: 計算中...");

            try
            {
                var score = await _scoreWorker.ScoreAsync(fullPath, requestSerial.ToString()).ConfigureAwait(false);
                _scoreCache[fullPath] = score;

                if (requestSerial == _scoreRequestSerial && _currentPicturePath == fullPath)
                {
                    SetScoreText(FormatScore(score));
                }
            }
            catch (Exception ex)
            {
                if (requestSerial == _scoreRequestSerial && _currentPicturePath == fullPath)
                {
                    SetScoreText($"スコア: エラー ({GetShortError(ex)})");
                }
            }
        }

        private static string FormatScore(double score) => $"スコア: {score:0.00}";

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
