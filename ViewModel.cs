using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace Avalonia2
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
            Observable.Merge(
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
                .Subscribe(x => 
                {
                    PicturePath = x;
                    Title = x;
                });
            PicturePath = PictureSelecter.CurrentPicture;
            Title = "AvaloniaUIApps";
        }

        PictureSelecter PictureSelecter { get; }

        public ReactiveCommand NextPageCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PrevPageCommand { get; } = new ReactiveCommand();

        // public ReadOnlyReactiveProperty<string> PicturePath { get; }

        private string _PicturePath;
        public string PicturePath
        {
            get { return _PicturePath; }
            set { SetProperty(ref _PicturePath, value); }
        }

    }
}
