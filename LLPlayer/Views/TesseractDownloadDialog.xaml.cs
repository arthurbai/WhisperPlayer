using System.Windows;
using System.Windows.Controls;
using WhisperPlayer.ViewModels;

namespace WhisperPlayer.Views;
public partial class TesseractDownloadDialog : UserControl
{
    public TesseractDownloadDialog()
    {
        InitializeComponent();

        DataContext = ((App)Application.Current).Container.Resolve<TesseractDownloadDialogVM>();
    }
}
