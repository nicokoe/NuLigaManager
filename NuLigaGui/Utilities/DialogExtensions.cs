using Microsoft.Win32;
using System.Windows;

namespace NuLigaGui.Utilities
{
    public static class DialogExtensions
    {
        public static string? GetPathWithSaveFileDialog(string defaultFileName, string filter, string defaultExt)
        {
            string? path = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dlg = new SaveFileDialog
                {
                    FileName = defaultFileName,
                    Filter = filter,
                    DefaultExt = defaultExt,
                    AddExtension = true
                };
                if (dlg.ShowDialog() == true)
                {
                    path = dlg.FileName;
                }
            });
            return path;
        }
    }
}
