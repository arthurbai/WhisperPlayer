using System.Windows;
using System.Windows.Controls;
using WhisperPlayer.ViewModels;

namespace WhisperPlayer.Views;

public partial class SubtitlesExportDialog : UserControl
{
    public SubtitlesExportDialog()
    {
        InitializeComponent();

        DataContext = ((App)Application.Current).Container.Resolve<SubtitlesExportDialogVM>();
    }
}
