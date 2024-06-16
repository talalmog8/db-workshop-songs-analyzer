using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace SongsAnalyzer
{
    public partial class WindowHandlers : Window
    {
        private readonly ISongAnalyzer _songAnalyzer;
        private readonly ObservableCollection<WordTable> _words = [];
        private readonly ObservableCollection<SongQueryResult> _songComposers = [];
        private readonly ObservableCollection<WordDetailsView> _wordDetailsViews = [];

        public WindowHandlers()
        {
            InitializeComponent();
            _songAnalyzer = App.Provider.GetRequiredService<ISongAnalyzer>();
            WordsDataGrid.ItemsSource = _words;
            QuerySongResultsDataGrid.ItemsSource = _songComposers;
            WordsIndexDataGrid.ItemsSource = _wordDetailsViews;
        }

        #region Load Song Tab 1

        
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
                
            if(processingResult == ProcessingResult.Failed)
                MessageBox.Show("Failed To Process Song", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else
            {
                await FullSongTextBox.Dispatcher.InvokeAsync(() => FullSongTextBox.Text = content);
                await SongNameTextBox.Dispatcher.InvokeAsync(() => SongNameTextBox.Text = _songAnalyzer.SongName);
                await RefreshUI();
            }
        }
        
        #endregion
        
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshUI();
        }
        
        private async Task RefreshUI()
        {
            await UpdateWordTable();
            await UpdateStats();
            await UpdateSongsResultsTable();
            await UpdateWordIndex();
        }

        #region Tab 4 Word Index
        
        private async Task UpdateWordIndex()
        {
            var wordIndex = await _songAnalyzer.GetWordIndex();
            
            _wordDetailsViews.Clear();
            
            foreach (var word in wordIndex)
                _wordDetailsViews.Add(word);
        }

        #endregion
        
        #region Stats Tab 5

        private async Task UpdateStats()
        {
            var stats = await _songAnalyzer.GetStats();
            await Stats_PerWord.Dispatcher.InvokeAsync(() =>  Stats_PerWord.Text = stats.AverageWordLength.ToString(CultureInfo.InvariantCulture));
            await Stats_PerLine.Dispatcher.InvokeAsync(() =>  Stats_PerLine.Text = stats.AverageSongLineWordLength.ToString(CultureInfo.InvariantCulture));
            await Stats_PerStanza.Dispatcher.InvokeAsync(() =>  Stats_PerStanza.Text = stats.AverageSongStanzaWordLength.ToString(CultureInfo.InvariantCulture));
            await Stats_PerSong.Dispatcher.InvokeAsync(() =>  Stats_PerSong.Text = stats.AverageSongWordLength.ToString(CultureInfo.InvariantCulture));
        }
        
        #endregion
        
        #region Tab 2 - Words + QuerySong

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
        }

        private async void QuerySongButton_Click(object sender, RoutedEventArgs e)
        {
            await UpdateSongsResultsTable();
        }
        
        private async void ClearQuerySongButton_Click(object sender, RoutedEventArgs e)
        {
            await UpdateSongsResultsTable();
            
            await QuerySongNameTextBox.Dispatcher.InvokeAsync(() => QuerySongNameTextBox.Clear());
            await QueryComposerFirstNameTextBox.Dispatcher.InvokeAsync(() => QueryComposerFirstNameTextBox.Clear());
            await QueryComposerLastNameTextBox.Dispatcher.InvokeAsync(() => QueryComposerLastNameTextBox.Clear());
            await QueryWordsTextBox.Dispatcher.InvokeAsync(() => QueryWordsTextBox.Clear());
        }
        
        #endregion

        #region Words Tab 3

        private async void FilterSongWordIndexButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_songAnalyzer.Processed)
            {
                SongIsNotProcessed();
                return;
            }
            
            await UpdateWordTable(filterCurrentSong: true);
        }
        
        private async Task UpdateWordTable(string? songName = null, bool filterCurrentSong = false)
        {
            // populate  word data grid
            
            _words.Clear();
            
            var words = await _songAnalyzer.GetWords(songName, filterCurrentSong);
           
            foreach (var word in words)
                _words.Add(word);
        }
        
        private async void UnFilterSongWordIndexButton_Click(object sender, RoutedEventArgs e)
        {
            await UpdateWordTable();
        }

        private async void FilterCurrentSongButton_WordView_Click(object sender, RoutedEventArgs e)
        {
            await UpdateWordTable(QuerySong_SongName_WordViewTextBox.Text);
        }

        private async void ClearFilterButton_WordView_Click(object sender, RoutedEventArgs e)
        {
            await UpdateWordTable();
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
        
        #region Messages

        private void SongIsNotProcessed()
        {
            MessageBox.Show("Please load a song first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion        
    }
}
