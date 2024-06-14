using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace SongsAnalyzer
{
    public partial class WindowHandlers : Window
    {
        private readonly ISongAnalyzer _songAnalyzer;
        private ObservableCollection<Word> _words;
        private readonly ObservableCollection<SongComposer> _songComposers = [];

        public WindowHandlers()
        {
            InitializeComponent();
            _songAnalyzer = App.Provider.GetRequiredService<ISongAnalyzer>();
            _words = new ObservableCollection<Word>();
            WordsDataGrid.ItemsSource = _words;
            QuerySongResultsDataGrid.ItemsSource = _songComposers;
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


        #region QuerySong

        private async Task UpdateSongsResultsTable()
        {
            // populate  word data grid
            
            _songComposers.Clear();
            
            var songs = await _songAnalyzer.GetSongs(
                songName: QuerySongNameTextBox.Text,
                composerFirstName: QueryComposerFirstNameTextBox.Text,
                composerLastName: QueryComposerLastNameTextBox.Text,
                freeText: QueryWordsTextBox.Text);
           
            foreach (var song in songs)
                _songComposers.Add(song);

            QuerySongNameTextBox.Dispatcher.InvokeAsync(() => QuerySongNameTextBox.Clear());
            QueryComposerFirstNameTextBox.Dispatcher.InvokeAsync(() => QueryComposerFirstNameTextBox.Clear());
            QueryComposerLastNameTextBox.Dispatcher.InvokeAsync(() => QueryComposerLastNameTextBox.Clear());
            QueryWordsTextBox.Dispatcher.InvokeAsync(() => QueryWordsTextBox.Clear());
        }

        private async void QuerySongButton_Click(object sender, RoutedEventArgs e)
        {
            await UpdateSongsResultsTable();
        }
        
        #endregion
        
        #region Add Song

        private const string SPACE = " ";

        private void AddComposerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_songAnalyzer.Processed)
            {
                SongIsNotProcessed();
                return;
            }
            
            var composer = Interaction.InputBox("Enter composer name:", "Add Composer", "");
            if (!string.IsNullOrEmpty(composer) && composer.Split(SPACE, StringSplitOptions.RemoveEmptyEntries).Length > 1) 
                ComposersListBox.Items.Add(composer);
            else 
                FullNameError();
        }

        private void AddWriterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_songAnalyzer.Processed)
            {
                SongIsNotProcessed();
                return;
            }
            
            var writer = Interaction.InputBox("Enter writer name:", "Add Writer", "");
            if (!string.IsNullOrEmpty(writer) && writer.Split(SPACE, StringSplitOptions.RemoveEmptyEntries).Length > 1)
                WritersListBox.Items.Add(writer);
            else 
                FullNameError();
        }
        
        private void AddPerformerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_songAnalyzer.Processed)
            {
                SongIsNotProcessed();
                return;
            }
            
            var performer = Interaction.InputBox("Enter performer full name. use a space as a separator:", "Add Performer", "");
            if (!string.IsNullOrEmpty(performer) && performer.Split(SPACE, StringSplitOptions.RemoveEmptyEntries).Length > 1)
                PerformersListBox.Items.Add(performer);
            else 
                FullNameError();
        }

        private async void AddSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_songAnalyzer.Processed)
            {
                SongIsNotProcessed();
                return;
            }
            
            var composers = new HashSet<Name>();
            var writers = new HashSet<Name>();
            var performers = new HashSet<Name>();

            PopulateSet(composers, ComposersListBox.Items);
            PopulateSet(writers, WritersListBox.Items);
            PopulateSet(performers, PerformersListBox.Items);

            try
            {
                await _songAnalyzer.AddSong(composers, performers, writers);
                ComposersListBox.Dispatcher.InvokeAsync(() => ComposersListBox.Items.Clear());
                WritersListBox.Dispatcher.InvokeAsync(() => WritersListBox.Items.Clear());
                PerformersListBox.Dispatcher.InvokeAsync(() => PerformersListBox.Items.Clear());
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Failed to add song. {exception}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateSet(HashSet<Name> hashSet, ItemCollection collection)
        {
            foreach (var item in collection)
            {
                if(item is not null && !string.IsNullOrEmpty(item?.ToString()))
                {
                    var fullName = item!.ToString();
                    var sep = fullName!.IndexOf(SPACE, StringComparison.Ordinal);
                    var firstName = fullName[..sep].Trim();
                    var lastName = fullName[(sep + 1)..].Trim();
                    hashSet.Add(new Name(firstName, lastName));
                }
            }
        }

        private static void FullNameError()
        {
            MessageBox.Show("Please use a full name, use a space as a separator", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        #endregion
    }
}
