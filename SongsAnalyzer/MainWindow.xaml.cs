using System.Collections.Generic;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using Model;
using SongsAnalyzer;

namespace SongTextAnalyzer
{
    public partial class MainWindow : Window
    {
        private readonly ISongAnalyzer _songAnalyzer;
        
        public MainWindow()
        {
            InitializeComponent();
            _songAnalyzer = App.Provider.GetRequiredService<ISongAnalyzer>();
        }
        
        private void AddComposerButton_Click(object sender, RoutedEventArgs e)
        {
            var composer = Interaction.InputBox("Enter composer name:", "Add Composer", "");
            if (!string.IsNullOrEmpty(composer))
            {
                ComposersListBox.Items.Add(composer);
            }
        }

        private void AddWriterButton_Click(object sender, RoutedEventArgs e)
        {
            var writer = Interaction.InputBox("Enter writer name:", "Add Writer", "");
            if (!string.IsNullOrEmpty(writer))
            {
                WritersListBox.Items.Add(writer);
            }
        }

        private void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            var performer = Interaction.InputBox("Enter performer name:", "Add Performer", "");
            if (!string.IsNullOrEmpty(performer))
            {
                PerformersListBox.Items.Add(performer);
            }
        }
        private void AddPerformerButton_Click(object sender, RoutedEventArgs e)
        {
            var performer = Interaction.InputBox("Enter performer name:", "Add Performer", "");
            if (!string.IsNullOrEmpty(performer))
            {
                PerformersListBox.Items.Add(performer);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string songName = SongNameTextBox.Text;
            List<string> composers = new List<string>();
            List<string> writers = new List<string>();
            List<string> performers = new List<string>();

            foreach (var item in ComposersListBox.Items)
                composers.Add(item.ToString());

            foreach (var item in WritersListBox.Items)
                writers.Add(item.ToString());

            foreach (var item in PerformersListBox.Items)
                performers.Add(item.ToString());

            // Implement your saving logic here
            MessageBox.Show($"Song Name: {songName}\nComposers: {string.Join(", ", composers)}\nWriters: {string.Join(", ", writers)}\nPerformers: {string.Join(", ", performers)}", "Song Details Saved");
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
            }
        }
    }
}
