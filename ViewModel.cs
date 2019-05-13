using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

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

        public ViewModel()
        {
            PictureSelecter = new PictureSelecter(System.IO.Path.Combine(".", "image"));
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
                }))
                .Publish();

            PicturePath = buttonClickedSource
                .ToReadOnlyReactiveProperty(PictureSelecter.CurrentPicture);

            PageNum =
                buttonClickedSource
                .Select(_ => PictureSelecter.PageNumForUser)
                .ToReadOnlyReactiveProperty(PictureSelecter.PageNumForUser);
            buttonClickedSource.Connect();

            Title = "AvaloniaUIApps On " + System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        }

        PictureSelecter PictureSelecter { get; }

        public ReactiveCommand NextPageCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PrevPageCommand { get; } = new ReactiveCommand();

        public ReadOnlyReactiveProperty<string> PicturePath { get; }

        public ReadOnlyReactiveProperty<string> PageNum { get; }
    }
}
