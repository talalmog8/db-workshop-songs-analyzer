using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Model;

namespace SongsAnalyzer
{
    public partial class AddSongWindow : Window
    {
        private readonly ISongAnalyzer _songAnalyzer;
        
        public AddSongWindow()
        {
            InitializeComponent();
            _songAnalyzer = App.Provider.GetRequiredService<ISongAnalyzer>();
        }
        
        private async void BrowseSong_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Document", // Default file name
                DefaultExt = ".txt", // Default file extension
                Filter = "Text documents (.txt)|*.txt" // Filter files by extension
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                var filename = dialog.FileName;
                var content = await _songAnalyzer.LoadSong(filename);
                await FullSongTextBox.Dispatcher.InvokeAsync(() => FullSongTextBox.Text = content);
                var processingResult = await _songAnalyzer.ProcessSong();
                await SongNameTextBox.Dispatcher.InvokeAsync(() => SongNameTextBox.Text = Path.GetFileNameWithoutExtension(filename));

                if(processingResult.ProcessingResult == ProcessingResult.Failed)
                    MessageBox.Show("Failed To Process Song", "Error", MessageBoxButton.OK, MessageBoxImage.Error);                
            }
        }

        private void SongIsNotProcessed()
        {
            MessageBox.Show("Please load a song first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
