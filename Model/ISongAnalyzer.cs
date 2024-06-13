namespace Model;

public interface ISongAnalyzer
{
    string? Path {
        get;
        set;
    }
    string? SongName {
        get;
        set;
    }
    bool Processed { get; set; }
    Task<string> LoadSong(string path);
    Task ProcessSong();
    Task AddSong(HashSet<Name> composers, HashSet<Name> performers, HashSet<Name> writers);
}