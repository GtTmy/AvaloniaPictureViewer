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
                    if ((e.Args == null) || (e.Args.Length < 1)) return;
                    vm.SetFilename(e.Args[0]);
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
