using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.Linq;

namespace AvaloniaPictureViewer
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Opened += async (o, e) => 
            {
                var d = new OpenFileDialog()
                {
                    Filters = new List<FileDialogFilter>
                    {
                        new FileDialogFilter()
                        {
                            Extensions = new List<string>
                            {
                                "jpg", "JPG", "jpeg", "JPEG", "png", "PNG"
                            }
                        }
                    }
                };
                var files = await d.ShowAsync(this);
                var vm = DataContext as ViewModel;
                vm.SetFilename(files.Single());
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }
}
