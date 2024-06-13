using Microsoft.EntityFrameworkCore;
using Model.Entities;
namespace Model;

public class SongAnalyzer(Func<SongsContext> ctxFactory) : ISongAnalyzer
{
    public Contributor? Writer { get; set; }
    public Contributor? Performer { get; set; }
    public Contributor? MusicComposer { get; set; }
    public string? Path { get; set; }
    public string? SongName { get; set; }
    public string SongContent { get; set; }

    public async Task<string> LoadSong(string path)
    {
        Path = path;
        SongContent = await File.ReadAllTextAsync(Path);
        
        return SongContent;
    }
    
    public async Task ProcessSong()
    {
        var text = SongContent;
        var createDate = File.GetCreationTime(Path);
        await using var ctx = ctxFactory();
        await using var tran = await ctx.Database.BeginTransactionAsync();

        var song = await InsertSong(SongName, Path, createDate, text, ctx);
        var songStanzas = await InsertSongStanzas(text, song, ctx);
        var songLines = await InsertSongLines(text, song, ctx);
        var (wordIndex, words) = await InsertWordsIfMissing(ctx, text);
        var songWords = await InsertSongWords(words, song, wordIndex, ctx);
        var wordLocations = await InsertWordLocations(text, songWords, ctx);
        
        var writerContributorContributorType = await InsertContributorIfMissing(Writer, ctx, ContributorType.Writer, song);
        var musicComposerContributorContributorType = await InsertContributorIfMissing(Performer, ctx, ContributorType.MusicComposer, song);
        var performerContributorContributorType = await InsertContributorIfMissing(MusicComposer, ctx, ContributorType.Performer, song);

        await tran.CommitAsync();
    }

    private static async Task<SongWord[]> InsertSongWords(Word[] words, Song song, Dictionary<string, int> wordIndex, SongsContext ctx)
    {
        var songWords = words.Select(x => new SongWord
        {
            WordId = x.Id,
            SongId = song.Id,
            NumberOfOccurrences = wordIndex[x.WordText],
            Word = x,
            Song = song
        }).ToArray();

        ctx.SongWords.AddRange(songWords);
        await ctx.SaveChangesAsync();

        return songWords;
    }

    private async Task<(Dictionary<string, int> wordIndex, Word[])> InsertWordsIfMissing(SongsContext ctx, string text)
    {
        string[] words = text.Split([" ", Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);

        string[] wordsDistinct = words.Distinct().ToArray();

        var wordIndex = words.GroupBy(x => x)
            .Select(group => new WordIndex(group.Key, group.Count()))
            .ToDictionary(x => x.Word, y => y.Count);

        var existingWords = await ctx.Words.Where(x => wordsDistinct.Contains(x.WordText)).ToListAsync();
        var existingWordsHashSet = existingWords.Select(x => x.WordText).ToHashSet();
        var missingWords = wordsDistinct.Where(x => !existingWordsHashSet.Contains(x)).Select(y => new Word
        {
            WordText = y,
            Length = y.Length
        }).ToArray();

        ctx.Words.AddRange(missingWords);
        await ctx.SaveChangesAsync();
        return (wordIndex, existingWords.Union(missingWords).ToArray());
    }

    private async Task<Song> InsertSong(string? songName, string? path, DateTime createDate, string text, SongsContext ctx)
    {
        var song = new Song
        {
            Name = songName,
            Path = path,
            DocDate = createDate.ToUniversalTime(),
            WordLength = text.Length
        };

        var existingSong = await ctx.Songs.AsQueryable()
            .Where(x => x.Name == songName)
            .FirstOrDefaultAsync();

        if (existingSong is null)
        {
            ctx.Add(song);
            await ctx.SaveChangesAsync();
        }
        else
            throw new InvalidOperationException("Songs already exists");

        return song;
    }

    private async Task<ContributorContributorType> InsertContributorIfMissing(Contributor? contributor, SongsContext ctx, ContributorType contributorTypeId, Song song)
    {
        var existingContributor = await ctx.Contributors.AsQueryable()
            .Where(x => x.FullName == contributor.FullName)
            .FirstOrDefaultAsync();

        if (existingContributor is null)
        {
            ctx.Contributors.Add(contributor);
            await ctx.SaveChangesAsync();
        }
        else
            contributor = existingContributor;

        var existingContributorContributorType = await ctx.ContributorContributorTypes.AsQueryable()
            .Where(x => x.ContributorId == contributor.Id && x.ContributorTypeId == (int)contributorTypeId)
            .FirstOrDefaultAsync();

        var contributorContributorType = new ContributorContributorType
        {
            ContributorTypeId = (int)contributorTypeId,
            ContributorId = contributor.Id,
            Contributor = contributor
        };

        if (existingContributorContributorType is null)
        {
            ctx.ContributorContributorTypes.Add(contributorContributorType);
            await ctx.SaveChangesAsync();
        }
        else
            contributorContributorType = existingContributorContributorType;

        var songComposer = new SongComposer
        {
            ContributorId = contributor.Id,
            ContributorTypeId = contributorContributorType.ContributorTypeId,
            SongId = song.Id,
            Contributor = contributor,
            Song = song
        };

        var existingSongComposer = await ctx.SongComposers.AsQueryable()
            .Where(x => x.ContributorId == contributor.Id && x.ContributorTypeId == (int)contributorTypeId && x.SongId == song.Id)
            .FirstOrDefaultAsync();

        if (existingSongComposer is null)
        {
            ctx.SongComposers.Add(songComposer);
            await ctx.SaveChangesAsync();
        }
        else
            songComposer = existingSongComposer;

        return contributorContributorType;
    }

    private async Task<SongStanza[]> InsertSongStanzas(string input, Song song, SongsContext ctx)
    {
        string[] stanzas = input.Split($"{Environment.NewLine}{Environment.NewLine}", StringSplitOptions.RemoveEmptyEntries);

        var songStanzas = new List<SongStanza>();

        int offset = 0;

        foreach (string stanza in stanzas)
        {
            var songStanza = new SongStanza
            {
                SongId = song.Id,
                Song = song,
                Offset = offset,
                WordLength = stanza.Length,
            };

            offset += stanza.Length + 4; // ad cr and newline twice 

            songStanzas.Add(songStanza);
        }

        ctx.SongStanzas.AddRange(songStanzas);
        await ctx.SaveChangesAsync();

        return songStanzas.ToArray();
    }

    private async Task<SongLine[]> InsertSongLines(string input, Song song, SongsContext ctx)
    {
        string[] lines = input.Split($"{Environment.NewLine}");

        var songLines = new List<SongLine>();

        var offset = 0;

        foreach (string line in lines)
        {
            if (line == string.Empty)
                offset += 2;
            else
            {


                var songLine = new SongLine
                {
                    SongId = song.Id,
                    Song = song,
                    Offset = offset,
                    WordLength = line.Length,
                };

                offset += line.Length + 2; // ad cr and newline 
                songLines.Add(songLine);
            }
        }

        ctx.SongLines.AddRange(songLines);
        await ctx.SaveChangesAsync();

        return songLines.ToArray();
    }

    private async Task<WordLocation[]> InsertWordLocations(string input, SongWord[] songWords, SongsContext ctx)
    {
        var wordToSongWord = songWords.ToDictionary(x => x.Word.WordText, y => y);

        string[] words = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);

        var wordLocations = new List<WordLocation>();

        var offset = 0;

        foreach (string word in words)
        {
            var wordLocation = new WordLocation
            {
                Offset = offset,
                SongWordId = wordToSongWord[word].Id,
                SongWord = wordToSongWord[word]
            };

            offset += word.Length;
            wordLocations.Add(wordLocation);
        }

        ctx.WordLocations.AddRange(wordLocations);
        await ctx.SaveChangesAsync();

        return wordLocations.ToArray();
    }
}