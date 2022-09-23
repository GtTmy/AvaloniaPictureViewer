using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.ApplicationLifetimes;

namespace AvaloniaPictureViewer
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var vm = new ViewModel();
                desktop.MainWindow = new MainWindow()
                {
                    DataContext = vm,
                };

                desktop.Startup += (o, e) =>
                {
                    vm.SetArgs(e.Args);
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
