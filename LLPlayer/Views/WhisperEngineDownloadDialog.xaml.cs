using System.Windows;
using System.Windows.Controls;
using WhisperPlayer.ViewModels;

namespace WhisperPlayer.Views;

public partial class WhisperEngineDownloadDialog : UserControl
{
    public WhisperEngineDownloadDialog()
    {
        InitializeComponent();

        DataContext = ((App)Application.Current).Container.Resolve<WhisperEngineDownloadDialogVM>();
    }
}
