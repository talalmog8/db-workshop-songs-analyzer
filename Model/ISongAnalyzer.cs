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
    Task<SongInformation> ProcessSong();
    Task AddSong(HashSet<Name> composers, HashSet<Name> performers, HashSet<Name> writers);
    Task<List<Word>> GetWords();
    Task<Stats> GetStats();
}