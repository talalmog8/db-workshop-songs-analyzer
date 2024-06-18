using Model.Contract;
using Model.Entities;

namespace Model;

public interface ISongAnalyzer
{
    string? Path {
        get;
        set;
    }
    string? SongName { get; }
    bool Processed { get; set; }
    Task<string> LoadSong(string path);
    Task<ProcessingResult> ProcessSong();
    Task<bool> AddGroup(string name, string[] array);
    Task<List<string>> GetGroups();
    Task<(string phrase, bool)> AddPhrase(string phrase);
    Task<List<string>> GetPhrases();
    Task AddSong(HashSet<Name> composers, HashSet<Name> performers, HashSet<Name> writers);
    Task<List<WordTable>> GetWords(string? songName = null, bool filterCurrentSong = false);
    Task<List<WordDetailsView>> GetWordIndex();
    Task<Stats> GetStats();
    Task<List<SongQueryResult>> GetSongs(string songName, string composerFirstName, string composerLastName, string freeText);
}