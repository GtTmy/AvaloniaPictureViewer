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
        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
        }

        public void SetFilename(string filename)
        {
            PictureSelecter = new PictureSelecter(filename);
            UpdatePic.OnNext(PictureSelecter.CurrentPicture);
        }

        public Subject<string> UpdatePic { get; } = new Subject<string>();
        public ViewModel()
        {   
            Title = "AvaloniaUIApps On " + System.Runtime.InteropServices.RuntimeInformation.OSDescription;

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
        }

        PictureSelecter PictureSelecter { get; set;}

        public ReactiveCommand NextPageCommand { get; private set; } = new ReactiveCommand();
        public ReactiveCommand PrevPageCommand { get; private set; } = new ReactiveCommand();

        public ReadOnlyReactiveProperty<string> PicturePath { get; private set; }

        public ReadOnlyReactiveProperty<string> PageNum { get; private set; }
    }
}
