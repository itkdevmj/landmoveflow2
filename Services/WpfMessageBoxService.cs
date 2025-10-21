using System.Windows;
using LMFS.Interfaces;

namespace LMFS.Services
{
    public class WpfMessageBoxService : IMessageBoxService
    {
        public bool ShowMessage(string text, string caption)
        {
            MessageBox.Show(text, caption, MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }
    }
}
