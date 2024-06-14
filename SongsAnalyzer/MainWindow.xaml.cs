using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Model;
using Model.Contract;
using Model.Entities;

namespace SongsAnalyzer
{
    public partial class AddSongWindow : Window
    {
        private readonly ISongAnalyzer _songAnalyzer;
        private ObservableCollection<Word> _words;

        public AddSongWindow()
        {
            InitializeComponent();
            _songAnalyzer = App.Provider.GetRequiredService<ISongAnalyzer>();
            _words = new ObservableCollection<Word>();
            WordsDataGrid.ItemsSource = _words;
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
            if (result != true) 
                return;
            
            var filename = dialog.FileName;
            var content = await _songAnalyzer.LoadSong(filename);
                
            var processingResult = await _songAnalyzer.ProcessSong();
                
            if(processingResult.ProcessingResult == ProcessingResult.Failed)
                MessageBox.Show("Failed To Process Song", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else
            {
                await FullSongTextBox.Dispatcher.InvokeAsync(() => FullSongTextBox.Text = content);
                await SongNameTextBox.Dispatcher.InvokeAsync(() => SongNameTextBox.Content = Path.GetFileNameWithoutExtension(filename));
                await UpdateStats();
                await UpdateWordTable();
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Populate Words
            await UpdateWordTable();
            
            // Populate Stats
            await UpdateStats();
        }

        private async Task UpdateWordTable()
        {
            // populate  word data grid
            
            _words.Clear();
            
            var words = await _songAnalyzer.GetWords();
           
            foreach (var word in words)
                _words.Add(word);
        }
        
        private async Task UpdateStats()
        {
            var stats = await _songAnalyzer.GetStats();
            await Stats_PerWord.Dispatcher.InvokeAsync(() =>  Stats_PerWord.Text = stats.AverageWordLength.ToString(CultureInfo.InvariantCulture));
            await Stats_PerLine.Dispatcher.InvokeAsync(() =>  Stats_PerLine.Text = stats.AverageSongLineWordLength.ToString(CultureInfo.InvariantCulture));
            await Stats_PerStanza.Dispatcher.InvokeAsync(() =>  Stats_PerStanza.Text = stats.AverageSongStanzaWordLength.ToString(CultureInfo.InvariantCulture));
            await Stats_PerSong.Dispatcher.InvokeAsync(() =>  Stats_PerSong.Text = stats.AverageSongWordLength.ToString(CultureInfo.InvariantCulture));
        }

        private void SongIsNotProcessed()
        {
            MessageBox.Show("Please load a song first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
