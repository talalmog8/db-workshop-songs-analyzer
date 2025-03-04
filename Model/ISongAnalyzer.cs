﻿namespace Model;

public interface ISongAnalyzer
{
    Song? Song { get; }
    string? Path {
        get;
        set;
    }
    string? SongName { get; }
    bool Processed { get; set; }
    Task<string> LoadSong(string path);
    Task<ProcessingResult> ProcessSong();
    Task<bool> AddGroup(string name, string[] values);
    Task<List<GroupResult>> GetGroups();
    Task<(string phrase, bool)> AddPhrase(string phrase);
    Task<List<string>> GetPhrases();
    Task AddSong(HashSet<Name> composers, HashSet<Name> performers, HashSet<Name> writers);
    Task<List<WordTable>> GetWords(string? songName = null, bool filterCurrentSong = false);
    Task<List<WordIndexView>> GetWordIndex(string groupName);
    Task<Stats> GetStats();
    Task<List<SongQueryResult>> GetSongs(string songName, string composerFirstName, string composerLastName, string freeText);
    TextOccurence[] GetPhraseReference(string word);
    Task<string> FindWords(int stanzaOffset, int lineOffset, int wordOffset);
    Task<ComposerView[]> GetComposers();
    int GetTokenCount(string text);
    Task<List<SongName>> SearchSongs(string searchTerm);
    Task SetUpDatabase();
    void LoadSong(string path, string songContent);
    Task InsertContributorsIfMissing(HashSet<Name> composers, ContributorType contributorType, Song? song);
}