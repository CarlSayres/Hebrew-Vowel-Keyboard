using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace HebrewVowelKeyboard.Tests;

internal static class PreviewRenderer
{
    internal static int Run()
    {
        var application = new Application();
        var window = new MainWindow();
        window.Loaded += async (_, _) =>
        {
            try
            {
                var folder = Path.GetFullPath("artifacts/previews");
                Directory.CreateDirectory(folder);
                foreach (var (tab, filename) in new[] { ("NiqqudTab", "niqqud.png"), ("TeamimTab", "teamim.png") })
                {
                    ((Button)window.FindName(tab)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(window);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = File.Create(Path.Combine(folder, filename));
                    encoder.Save(stream);
                    Console.WriteLine($"Rendered {filename}: {window.ActualWidth} x {window.ActualHeight}");
                }
            }
            finally { window.Close(); }
        };
        application.Run(window);
        return 0;
    }
}
