using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System.Linq;

namespace AvaloniaPictureViewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Opened += async (o, e) => 
            {
                var vm = DataContext as ViewModel;
                if (!vm.IsPictureSelected())
                {
                    var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        FileTypeFilter = new[]
                        {
                            new FilePickerFileType("Pictures")
                            {
                                Patterns = PictureSelecter.SupportedExtensions.Select(x => $"*.{x}").ToArray(),
                            }
                        },
                        AllowMultiple = false,
                    });
                    if (!files.Any())
                    {
                        this.Close();
                        return;
                    }
                    var path = files.Single().TryGetLocalPath();
                    if (path == null)
                    {
                        this.Close();
                        return;
                    }
                    vm.SetFilename(path);
                }

            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }
}
