using System.Collections.ObjectModel;

namespace SongsAnalyzer;

public partial class WindowHandlers : Window
{
    private ObservableCollection<SongComposer> _songComposers;

    private async Task UpdateSongsResultsTable()
    {
        // populate  word data grid
            
        _songComposers.Clear();
            
        var words = await _songAnalyzer.GetWords();
           
        foreach (var word in words)
            _words.Add(word);
    }
}