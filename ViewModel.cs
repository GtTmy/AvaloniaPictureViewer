using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Controls;
using System.IO;
using System.Reactive.Subjects;

namespace AvaloniaPictureViewer
{
    public class ViewModel: BindableBase
    {
        public ReadOnlyReactiveProperty<string> Title { get; }
        public void SetFilename(string filename)
        {
            if (System.IO.File.Exists(filename) &&
                PictureSelecter.SupportedExtensions.Contains(System.IO.Path.GetExtension(filename).Substring(1)))
            {
                var fullpath = System.IO.Path.GetFullPath(filename);
                PictureSelecter = new PictureSelecter(filename);
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
    }
}
