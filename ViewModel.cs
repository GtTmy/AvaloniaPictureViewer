using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia2
{
    public class ViewModel: BindableBase
    {
        private DelegateCommand _NextPageCommand;
        public DelegateCommand NextPageCommand =>
            _NextPageCommand ?? (_NextPageCommand = new DelegateCommand(NextPage));

        void NextPage()
        {
            System.Diagnostics.Debug.WriteLine("NextPage");
        }

        private DelegateCommand _PrevPageCommand;
        public DelegateCommand PrevPageCommand =>
            _PrevPageCommand ?? (_PrevPageCommand = new DelegateCommand(PrevPage));

        void PrevPage()
        {
            System.Diagnostics.Debug.WriteLine("PrevPage");
        }

        public ReactiveProperty<string> Text1 { get; } = new ReactiveProperty<string>("Hello!");
        public ReactiveProperty<string> Text2 { get; } = new ReactiveProperty<string>("Avalonia!");

    }
}
