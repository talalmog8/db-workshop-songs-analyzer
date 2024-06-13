using Microsoft.EntityFrameworkCore;
using Model.Entities;

namespace Model;

public class SongAnalyzer(Func<SongsContext> ctxFactory) : ISongAnalyzer
{
    public string? Path { get; set; }
    public string? SongName => System.IO.Path.GetFileNameWithoutExtension(Path);
    public string SongContent { get; set; }
    public Song Song { get; set; }
    public bool Processed { get; set; }

    public async Task<string> LoadSong(string path)
    {
        Path = path;
        SongContent = await File.ReadAllTextAsync(Path);
        return SongContent;
    }

    public async Task<SongInformation> ProcessSong()
    {
        try
        {
            var text = SongContent;
            var createDate = File.GetCreationTime(Path);

            await using var ctx = ctxFactory();

            if (await IsSongExists(ctx))
                return await LoadSongInformation(ctx);
            
            await using var tran = await ctx.Database.BeginTransactionAsync();

            var song = await InsertSong(Path, createDate, text, ctx);
            var songStanzas = await InsertSongStanzas(text, song, ctx);
            var songLines = await InsertSongLines(text, song, ctx);
            var (wordIndex, words) = await InsertWordsIfMissing(ctx, text);
            var songWords = await InsertSongWords(words, song, wordIndex, ctx);
            var wordLocations = await InsertWordLocations(text, songWords, ctx);

            await tran.CommitAsync();

            var songInfo = new SongInformation
            {
                Song = song,
                SongStanzas = songStanzas,
                SongLines = songLines,
                SongWords = songWords,
                WordLocations = wordLocations,
                WordIndex = wordIndex,
                Words = words,
                ProcessingResult = ProcessingResult.Succeeded,
            };

            Song = song;
            Processed = true;

            return songInfo;
        }
        catch (Exception e)
        {
            Processed = false;
            return new SongInformation { ProcessingResult = ProcessingResult.Failed };
        }
    }

    private async Task<SongInformation> LoadSongInformation(SongsContext ctx)
    {
        var si = new SongInformation();
        
        si.Song = await ctx.Songs.Where(x => x.Name == SongName).FirstAsync();
        si.SongWords = await ctx.SongWords.Where(x => x.SongId == si.Song.Id).ToListAsync();

        // TODO - finish this
        
        return si;
    }

    public async Task AddSong(HashSet<Name> composers, HashSet<Name> performers, HashSet<Name> writers)
    {
        await using var ctx = ctxFactory();
        await using var tran = await ctx.Database.BeginTransactionAsync();

        await InsertContributorsIfMissing(composers, ctx, ContributorType.MusicComposer, Song);
        await InsertContributorsIfMissing(writers, ctx, ContributorType.Writer, Song);
        await InsertContributorsIfMissing(performers, ctx, ContributorType.Performer, Song);

        await tran.CommitAsync();
    }

    private async Task InsertContributorsIfMissing(HashSet<Name> composers, SongsContext ctx,
        ContributorType contributorType, Song song)
    {
        try
        {
            foreach (var composer in composers)
                await InsertContributorIfMissing(new Contributor(composer), ctx, contributorType, song);
        }
        catch (Exception e)
        {
            // TODO fix this
        }
    }

    private static async Task<SongWord[]> InsertSongWords(Word[] words, Song song, Dictionary<string, int> wordIndex,
        SongsContext ctx)
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
        var words = text.Split([" ", Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);

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

    private async Task<Song> InsertSong(string? path, DateTime createDate, string text,
        SongsContext ctx)
    {
        var song = new Song
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(path),
            Path = path,
            DocDate = createDate.ToUniversalTime(),
            WordLength = text.Length
        };

        ctx.Add(song);
        await ctx.SaveChangesAsync();

        return song;
    }

    public async Task<bool> IsSongExists(SongsContext ctx)
    {
        var isSongExists = await ctx.Songs.AsQueryable()
            .Where(x => x.Name == SongName)
            .AnyAsync();

        return isSongExists;
    }

    private async Task<(ContributorContributorType, SongComposer)> InsertContributorIfMissing(Contributor contributor, SongsContext ctx, ContributorType contributorTypeId, Song song)
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
            SongId = song.Id
        };

        var existingSongComposer = await ctx.SongComposers.AsQueryable()
            .Where(x => x.ContributorId == contributor.Id && x.ContributorTypeId == (int)contributorTypeId &&
                        x.SongId == song.Id)
            .FirstOrDefaultAsync();

        if (existingSongComposer is null)
        {
            ctx.SongComposers.Add(songComposer);
            await ctx.SaveChangesAsync();
        }
        else
            songComposer = existingSongComposer;

        songComposer.Contributor = contributor;
        songComposer.Song = song;
        
        return (contributorContributorType, songComposer);
    }

    private async Task<SongStanza[]> InsertSongStanzas(string input, Song song, SongsContext ctx)
    {
        var stanzas = input.Split($"{Environment.NewLine}{Environment.NewLine}",
            StringSplitOptions.RemoveEmptyEntries);

        var songStanzas = new List<SongStanza>();

        var offset = 0;

        foreach (var stanza in stanzas)
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
        var lines = input.Split($"{Environment.NewLine}");

        var songLines = new List<SongLine>();

        var offset = 0;

        foreach (var line in lines)
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

        var words = input.Split(new[] { " ", "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        var wordLocations = new List<WordLocation>();

        var offset = 0;

        foreach (var word in words)
        {
            if (wordToSongWord.TryGetValue(word, out var songWord))
            {
                var wordLocation = new WordLocation
                {
                    Offset = offset,
                    SongWordId = songWord.Id,
                    SongWord = songWord
                };

                offset += word.Length;
                wordLocations.Add(wordLocation);
            }
            else
                throw new NullReferenceException("songWord could not be found in wordToSongWord dictionary");
        }

        ctx.WordLocations.AddRange(wordLocations);
        await ctx.SaveChangesAsync();
        return wordLocations.ToArray();
    }
}