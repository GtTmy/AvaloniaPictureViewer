using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
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

        private async void CopyCheckedFiles(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var vm = DataContext as ViewModel;
            var paths = vm.GetCheckedPaths();
            if (!paths.Any())
            {
                vm.SetClipboardResult(0, "画像が選択されていません");
                return;
            }

            try
            {
                var files = new List<IStorageItem>();
                foreach (var path in paths)
                {
                    var file = await StorageProvider.TryGetFileFromPathAsync(path);
                    if (file != null)
                    {
                        files.Add(file);
                    }
                }

                await Clipboard.SetFilesAsync(files);
                await Clipboard.FlushAsync();
                vm.SetClipboardResult(files.Count);
            }
            catch (Exception ex)
            {
                vm.SetClipboardResult(0, ex.Message);
            }
        }

    }
}
