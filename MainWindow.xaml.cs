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
                var vm = DataContext as ViewModel;
                if (!vm.IsPictureSelected())
                {
                    var d = new OpenFileDialog()
                    {
                        Filters = new List<FileDialogFilter>
                        {
                            new FileDialogFilter()
                            {
                                Extensions = PictureSelecter.SupportedExtensions,
                            }
                        }
                    };
                    var files = await d.ShowAsync(this);
                    if (!files.Any())
                    {
                        this.Close();
                        return;
                    }
                    vm.SetFilename(files.Single());
                }

            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }
}
