using System.Windows;
using System.Windows.Controls;
using WhisperPlayer.ViewModels;

namespace WhisperPlayer.Views;

public partial class SubtitlesDownloaderDialog : UserControl
{
    public SubtitlesDownloaderDialog()
    {
        InitializeComponent();

        DataContext = ((App)Application.Current).Container.Resolve<SubtitlesDownloaderDialogVM>();
    }
}
