using System.Windows;
using System.Windows.Controls;
using WhisperPlayer.ViewModels;

namespace WhisperPlayer.Views;

public partial class WhisperModelDownloadDialog : UserControl
{
    public WhisperModelDownloadDialog()
    {
        InitializeComponent();

        DataContext = ((App)Application.Current).Container.Resolve<WhisperModelDownloadDialogVM>();
    }
}
