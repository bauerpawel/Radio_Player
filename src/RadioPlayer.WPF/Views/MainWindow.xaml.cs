using System.Windows;

namespace RadioPlayer.WPF.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Wire up event handlers
        VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
        PlayButton.Click += PlayButton_Click;
        StopButton.Click += StopButton_Click;
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VolumeText != null)
        {
            VolumeText.Text = $"{(int)e.NewValue}%";
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement play functionality when RadioPlayer service is ready
        StatusText.Text = "Playing...";
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement stop functionality when RadioPlayer service is ready
        StatusText.Text = "Stopped";
    }
}
