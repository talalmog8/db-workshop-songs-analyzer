using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Model.Entities;
using Npgsql;
namespace Model;

public enum ContributorType
{
    Writer = 1,
    MusicComposer,
    Performer
}

public class SongAnalyzer
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;

    public SongAnalyzer(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _loggerFactory = loggerFactory;
        _configuration = configuration;

    }

    public async Task ProcessSong(
        string songName,
        string path,
        Contributor writer,
        Contributor performer,
        Contributor musicComposer)
    {
        string text = await File.ReadAllTextAsync(path);
        string[] lines = text.Split(Environment.NewLine);

        var createDate = File.GetCreationTime(path);

        await using var ctx = new SongsContext(_loggerFactory, _configuration);
        await using var tran = await ctx.Database.BeginTransactionAsync();

        var song = await InsertSong(songName, path, createDate, text, ctx);
        var songStanzas = await InsertSongStanzas(text, song, ctx);
        var songLines = await InsertSongLines(text, song, ctx);
        var (wordIndex, words) = await InsertWordsIfMissing(ctx, text);
        var songWords = await InsertSongWords(words, song, wordIndex, ctx);



        var writerContributorContributorType = await InsertContributorIfMissing(writer, ctx, ContributorType.Writer, song);
        var musicComposerContributorContributorType = await InsertContributorIfMissing(performer, ctx, ContributorType.MusicComposer, song);
        var performerContributorContributorType = await InsertContributorIfMissing(musicComposer, ctx, ContributorType.Performer, song);

        await tran.CommitAsync();
    }
    private static async Task<IEnumerable<SongWord>> InsertSongWords(Word[] words, Song song, Dictionary<string, int> wordIndex, SongsContext ctx)
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
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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

    private async Task<Song> InsertSong(string songName, string path, DateTime createDate, string text, SongsContext ctx)
    {
        var song = new Song
        {
            Name = songName,
            Path = path,
            DocDate = createDate,
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

    private async Task<ContributorContributorType> InsertContributorIfMissing(Contributor contributor, SongsContext ctx, ContributorType contributorTypeId, Song song)
    {
        var existingContributor = await ctx.Contributors.AsQueryable()
            .Where(x => x.FullName == contributor.FullName)
            .FirstOrDefaultAsync();

        if (existingContributor is null)
        {
            ctx.Add(contributor);
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
            ctx.Add(contributorContributorType);
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
            ctx.Add(songComposer);
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

            offset += stanza.Length;

            songStanzas.Add(songStanza);

        }

        ctx.SongStanzas.AddRange(songStanzas);
        await ctx.SaveChangesAsync();

        return songStanzas.ToArray();
    }

    private async Task<SongLine[]> InsertSongLines(string input, Song song, SongsContext ctx)
    {
        string[] lines = input.Split($"{Environment.NewLine}", StringSplitOptions.RemoveEmptyEntries);

        var songLines = new List<SongLine>();

        var offset = 0;

        foreach (string line in lines)
        {
            var songLine = new SongLine
            {
                SongId = song.Id,
                Song = song,
                Offset = offset,
                WordLength = line.Length,
            };

            offset += line.Length;
            songLines.Add(songLine);
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