using Avalonia;
using Avalonia.Markup.Xaml;

namespace AvaloniaPictureViewer
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
