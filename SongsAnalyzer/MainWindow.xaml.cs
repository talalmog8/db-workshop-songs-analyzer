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
        private readonly ObservableCollection<WordIndexView> _wordDetailsViews = [];
        private readonly ObservableCollection<GroupResult> _groups = [];
        private readonly ObservableCollection<string> _pharses = [];
        private readonly ObservableCollection<TextOccurence> _wordReferences = [];
        private readonly ObservableCollection<TextOccurence> _phrasesReferences = [];

        public WindowHandlers()
        {
            InitializeComponent();
            _songAnalyzer = App.Provider.GetRequiredService<ISongAnalyzer>();
            WordsDataGrid.ItemsSource = _words;
            QuerySongResultsDataGrid.ItemsSource = _songComposers;
            WordsIndexDataGrid.ItemsSource = _wordDetailsViews;
            GroupsDataGrid.ItemsSource = _groups;
            PhrasesListBox.ItemsSource = _pharses;
            WordReferencesDataGrid.ItemsSource = _wordReferences;
            PhrasesReferencesDataGrid.ItemsSource = _phrasesReferences;
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

            if (processingResult == ProcessingResult.Failed)
                MessageBox.Show("Failed To Process Song", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else
            {
                await FullSongTextBox.Dispatcher.InvokeAsync(() => FullSongTextBox.Text = content);
                await SongNameTextBox.Dispatcher.InvokeAsync(() => SongNameTextBox.Text = _songAnalyzer.SongName);
                await RefreshUI(true);
            }
        }

        #endregion

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshUI();
            await GetGroups();
            await GetPhrases();
        }

        private async Task RefreshUI(bool filterCurrentSong = false)
        {
            await UpdateWordTable(filterCurrentSong: filterCurrentSong);
            await UpdateStats();
            await UpdateSongsResultsTable();
            await UpdateWordIndex();
        }

        #region Tab 4 Word Index

        private async Task UpdateWordIndex(string groupName = null)
        {
            var wordIndex = await _songAnalyzer.GetWordIndex(groupName);

            _wordDetailsViews.Clear();

            foreach (var word in wordIndex)
                _wordDetailsViews.Add(word);
        }

        #endregion

        #region Stats Tab 5

        private async Task UpdateStats()
        {
            var stats = await _songAnalyzer.GetStats();
            await Stats_PerWord.Dispatcher.InvokeAsync(() =>
                Stats_PerWord.Text = stats.AverageWordLength.ToString(CultureInfo.InvariantCulture));
            await Stats_PerLine.Dispatcher.InvokeAsync(() =>
                Stats_PerLine.Text = stats.AverageSongLineWordLength.ToString(CultureInfo.InvariantCulture));
            await Stats_PerStanza.Dispatcher.InvokeAsync(() =>
                Stats_PerStanza.Text = stats.AverageSongStanzaWordLength.ToString(CultureInfo.InvariantCulture));
            await Stats_PerSong.Dispatcher.InvokeAsync(() =>
                Stats_PerSong.Text = stats.AverageSongWordLength.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

        #region Tab 2 - QuerySong

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

        private async void FilterSongWordButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_songAnalyzer.Processed)
            {
                SongIsNotProcessed();
                return;
            }

            await UpdateWordTable(songName: null, filterCurrentSong: true);
        }

        private async Task UpdateWordTable(string? songName = null, bool filterCurrentSong = false)
        {
            // populate  word data grid

            _words.Clear();
            _wordReferences.Clear();

            var words = await _songAnalyzer.GetWords(songName, filterCurrentSong);

            foreach (var word in words)
                _words.Add(word);
        }

        private async void UnFilterSongWordButton_Click(object sender, RoutedEventArgs e)
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
            await FindWord_Result_WordViewTextBox.Dispatcher.InvokeAsync(() => FindWord_Result_WordViewTextBox.Clear());
        }

        private void WordsDataGrid_Selected(object sender, RoutedEventArgs e)
        {
            if (WordsDataGrid.SelectedItem is null)
                return;

            if (WordsDataGrid.SelectedItem is not WordTable selectedItem)
                return;

            if (!_songAnalyzer.Processed)
                return;

            _wordReferences.Clear();

            foreach (var reference in _songAnalyzer.GetPhraseReference(selectedItem.WordText))
                _wordReferences.Add(reference);
        }

        private async void FindWord_WordView_Click(object sender, RoutedEventArgs e)
        {
            if (!_songAnalyzer.Processed)
            {
                SongIsNotProcessed();
                return;
            }

            int? stanzaOffset;
            int? lineOffset;
            int? wordOffset;

            try
            {
                stanzaOffset = string.IsNullOrEmpty(QuerySong_WordByStanza_WordViewTextBox.Text)
                    ? null
                    : int.Parse(QuerySong_WordByStanza_WordViewTextBox.Text);
                lineOffset = string.IsNullOrEmpty(QuerySong_WordByLine_WordViewTextBox.Text)
                    ? null
                    : int.Parse(QuerySong_WordByLine_WordViewTextBox.Text);
                wordOffset = string.IsNullOrEmpty(QuerySong_ByWord_WordViewTextBox.Text)
                    ? null
                    : int.Parse(QuerySong_ByWord_WordViewTextBox.Text);

                if (stanzaOffset is < 0)
                    throw new FormatException();
                if (lineOffset is < 0)
                    throw new FormatException();
                if (wordOffset is < 0)
                    throw new FormatException();
                
            }
            catch (OverflowException exception)
            {
                MessageBox.Show("Please enter a positive integer within 32 bit number range", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (FormatException exception)
            {
                MessageBox.Show("Please enter a positive integer", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (stanzaOffset is null)
            {
                MessageBox.Show("Please enter a stanza offset", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (lineOffset is null)
            {
                MessageBox.Show("Please enter a line offset", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (wordOffset is null)
            {
                MessageBox.Show("Please enter a word offset", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var words = await _songAnalyzer.FindWords(stanzaOffset.Value, lineOffset.Value, wordOffset.Value);

            await FindWord_Result_WordViewTextBox.Dispatcher.InvokeAsync(() =>
                FindWord_Result_WordViewTextBox.Text = words);
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
            if (!string.IsNullOrEmpty(composer) &&
                composer.Split(SPACE, StringSplitOptions.RemoveEmptyEntries).Length > 1)
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

            var performer = Interaction.InputBox("Enter performer full name. use a space as a separator:",
                "Add Performer", "");
            if (!string.IsNullOrEmpty(performer) &&
                performer.Split(SPACE, StringSplitOptions.RemoveEmptyEntries).Length > 1)
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
                MessageBox.Show($"Failed to add song. {exception}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PopulateSet(HashSet<Name> hashSet, ItemCollection collection)
        {
            foreach (var item in collection)
            {
                if (item is not null && !string.IsNullOrEmpty(item?.ToString()))
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
            MessageBox.Show("Please use a full name, use a space as a separator", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private static void StringEmptyError(string what)
        {
            MessageBox.Show($"{what} should not be null or empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region Messages

        private void SongIsNotProcessed()
        {
            MessageBox.Show("Please load a song first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region Groups

        private async void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            var groupName = Interaction.InputBox("Enter group name:", "Add Group", "");

            if (string.IsNullOrEmpty(groupName))
            {
                StringEmptyError("Group value");
                return;
            }

            await GroupValuesListBox.Dispatcher.InvokeAsync(() => GroupValuesListBox.Items.Add(groupName));
        }

        private async void SaveGroup_Click(object sender, RoutedEventArgs e)
        {
            var groupName = GroupNameTextBox.Text;

            if (string.IsNullOrEmpty(groupName))
            {
                StringEmptyError("Group Name");
                return;
            }

            var values = GroupValuesListBox.Items.Cast<string>().Select(x => x.ToLower()).ToArray();

            var added = await _songAnalyzer.AddGroup(groupName, values);

            if (added)
            {
                await GetGroups();
                await GroupValuesListBox.Dispatcher.InvokeAsync(() => GroupValuesListBox.Items.Clear());
                await GroupNameTextBox.Dispatcher.InvokeAsync(() => GroupNameTextBox.Clear());
            }
            else
                MessageBox.Show("Group Must Be Unique", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async Task GetGroups()
        {
            _groups.Clear();

            var groups = await _songAnalyzer.GetGroups();

            groups.ForEach(group => _groups.Add(group));
        }


        private async void FilterCurrentGroupButton_WordIndexView_Click(object sender, RoutedEventArgs e)
        {
            if (GroupsDataGrid.SelectedItem is null)
            {
                MessageBox.Show("No Group Is Selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (GroupsDataGrid.SelectedItem is not GroupResult selectedItem)
            {
                MessageBox.Show("Selected Group Is Not Valid", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var groupName = selectedItem.Name;

            if (string.IsNullOrEmpty(groupName))
            {
                MessageBox.Show("Selected Group Is Not Valid", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await UpdateWordIndex(groupName);
        }

        private async void ClearFilterButton_WordIndexView_Click(object sender, RoutedEventArgs e)
        {
            await UpdateWordIndex();
        }

        #endregion

        #region Phrase

        private async void SavePhraseButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(PhraseTextBox.Text))
            {
                StringEmptyError("Phrase");
                return;
            }

            var (phrase, added) = await _songAnalyzer.AddPhrase(PhraseTextBox.Text);

            if (added)
            {
                _pharses.Add(phrase);
                await PhraseTextBox.Dispatcher.InvokeAsync(() => PhraseTextBox.Clear());
            }
            else
                MessageBox.Show("Phrase already exists or invalid", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
        }

        private async Task GetPhrases()
        {
            _pharses.Clear();

            var phrases = await _songAnalyzer.GetPhrases();

            phrases.ForEach(phrase => _pharses.Add(phrase));
        }


        private void PhrasesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PhrasesListBox.SelectedItem is null)
                return;

            if (PhrasesListBox.SelectedItem is not string selectedItem)
                return;
            if (!_songAnalyzer.Processed)
                return;

            _phrasesReferences.Clear();

            foreach (var occurence in _songAnalyzer.GetPhraseReference(selectedItem))
                _phrasesReferences.Add(occurence);
        }

        #endregion
    }
}