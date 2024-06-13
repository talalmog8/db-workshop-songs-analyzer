using Model.Entities;
namespace Model;

public interface ISongAnalyzer
{
    Contributor? Writer {
        get;
        set;
    }
    Contributor? Performer {
        get;
        set;
    }
    Contributor? MusicComposer {
        get;
        set;
    }
    string? Path {
        get;
        set;
    }
    string? SongName {
        get;
        set;
    }
    Task<string> LoadSong(string path);
    Task ProcessSong();
}