using System.Windows;
using WhisperPlayer.Views;

namespace WhisperPlayer.Extensions;

public partial class MyDialogWindow : Window, IDialogWindow
{
    public IDialogResult? Result { get; set; }

    public MyDialogWindow()
    {
        InitializeComponent();

        MainWindow.SetTitleBarDarkMode(this);
    }
}
