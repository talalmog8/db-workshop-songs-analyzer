using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Model.Contract;
using Model.Entities;
using Group = Model.Entities.Group;

namespace Model;

public class SongAnalyzer(Func<SongsContext> ctxFactory) : ISongAnalyzer
{
    private readonly Regex _wordsRegex = new Regex(@"\b\w+\b", RegexOptions.Compiled);

    private readonly string[] _wordSplit =
    [
        " ", "\t", "\u00A0", ".", ",", "!", "?", ":", ";", "-", "–", "—", "/", "\\", "\"", "'", "(", ")", "[", "]", "{",
        "}", "<", ">", "_", "@", "&", "*", "+", "=", "|", "~", "`", Environment.NewLine
    ];

    private readonly string[] _stanzaSplit = [$"{Environment.NewLine}{Environment.NewLine}"];
    private readonly string[] _lineSplit = [$"{Environment.NewLine}"];
    public string? Path { get; set; }
    public string? SongName => System.IO.Path.GetFileNameWithoutExtension(Path)?.ToLower();
    public string SongContent { get; set; }


    public Song? Song { get; set; }
    public bool Processed { get; set; }

    #region Load

    public async Task<string> LoadSong(string path)
    {
        Path = path;
        SongContent = await File.ReadAllTextAsync(Path);
        return SongContent;
    }

    public async Task<ProcessingResult> ProcessSong()
    {
        try
        {
            var text = SongContent;
            var createDate = File.GetCreationTime(Path);

            await using var ctx = ctxFactory();

            var song = await ctx.Songs.AsQueryable()
                .Where(x => x.Name == SongName)
                .FirstOrDefaultAsync();

            if (song is not null)
            {
                Song = song;
                Processed = true;
                return ProcessingResult.AlreadyExists;
            }

            await using var tran = await ctx.Database.BeginTransactionAsync();

            song = await InsertSong(Path, createDate, text, ctx);
            var songStanzas = await InsertSongStanzas(text, song, ctx);
            var songLines = await InsertSongLines(songStanzas, text, song, ctx);
            var (wordIndex, words) = await InsertWordsIfMissing(ctx, text);
            var songWords = await InsertSongWords(words, song, wordIndex, ctx);
            _ = await InsertWordLocations(songLines, text, songWords, ctx);

            await tran.CommitAsync();

            Song = song;
            Processed = true;

            return ProcessingResult.Succeeded;
        }
        catch (Exception e)
        {
            Processed = false;
            return ProcessingResult.Failed;
        }
    }

    #endregion

    #region Add Song

    public async Task AddSong(HashSet<Name> composers, HashSet<Name> performers, HashSet<Name> writers)
    {
        await using var ctx = ctxFactory();
        await using var tran = await ctx.Database.BeginTransactionAsync();

        await InsertContributorsIfMissing(composers, ctx, ContributorType.MusicComposer, Song);
        await InsertContributorsIfMissing(writers, ctx, ContributorType.Writer, Song);
        await InsertContributorsIfMissing(performers, ctx, ContributorType.Performer, Song);

        await tran.CommitAsync();
    }

    #endregion

    #region Words

    public async Task<List<WordTable>> GetWords(string? songName = null, bool filterCurrentSong = false)
    {
        await using var ctx = ctxFactory();

        var query = ctx.Songs.AsQueryable();

        if (filterCurrentSong)
            query = query.Where(x => x.Id == Song.Id);
        if (!string.IsNullOrEmpty(songName))
            query = query.Where(x => x.Name.Contains(songName));

        var words = await query.Include(x => x.SongWords)
            .ThenInclude(y => y.Word)
            .SelectMany(x => x.SongWords)
            .GroupBy(x => new
            {
                x.Word.Id,
                x.Word.WordText,
                x.Word.Length
            })
            .Select(g => new WordTable
            {
                Id = g.Key.Id,
                WordText = g.Key.WordText,
                Length = g.Key.Length,
                NumberOfOccurrences = g.Sum(x => x.NumberOfOccurrences)
            })
            .OrderBy(x => x.Id).ToListAsync();

        return words;
    }

    public TextOccurence[] GetWordReference(string word)
    {
        var occurrences =
            FindPhraseOccurrences(SongContent, word)
                .Where(x => !string.IsNullOrEmpty(x.Text))
                .ToArray();

        return occurrences;
    }

    #endregion

    #region Words Index

    public async Task<List<WordDetailsView>> GetWordIndex(string groupName = null)
    {
        List<WordDetailsView> wordIndex;

        await using var ctx = ctxFactory();

        if (Song is null)
            return Enumerable.Empty<WordDetailsView>().ToList();

        if (!string.IsNullOrEmpty(groupName))
        {
            var groupWordDetails = await ctx.GroupWordDetailsView
                .Where(x => x.SongId == Song.Id)
                .Where(x => x.GroupName == groupName)
                .ToListAsync();

            wordIndex = groupWordDetails.Select(groupWordDetailsView => (WordDetailsView)groupWordDetailsView).ToList();

            return wordIndex;
        }


        wordIndex = await ctx.WordDetailsViews
            .Where(x => x.SongId == Song.Id)
            .ToListAsync();

        return wordIndex;
    }

    #endregion

    #region Stats

    public async Task<Stats> GetStats()
    {
        await using var ctx = ctxFactory();

        var averageWordLength = await ctx.Words.AnyAsync() ? await ctx.Words.AverageAsync(x => x.Length) : 0;
        var averageSongLineWordLength =
            await ctx.SongLines.AnyAsync() ? await ctx.SongLines.AverageAsync(x => x.WordLength) : 0;
        var averageSongStanzaWordLength =
            await ctx.SongStanzas.AnyAsync() ? await ctx.SongStanzas.AverageAsync(x => x.WordLength) : 0;
        var averageSongWordLength = await ctx.Songs.AnyAsync() ? await ctx.Songs.AverageAsync(x => x.WordLength) : 0;

        return new Stats(averageWordLength, averageSongLineWordLength, averageSongStanzaWordLength,
            averageSongWordLength);
    }

    #endregion

    #region Songs

    public async Task<List<SongQueryResult>> GetSongs(string songName, string composerFirstName,
        string composerLastName, string freeText)
    {
        List<SongQueryResult> result = null;

        await using var ctx = ctxFactory();

        if (!string.IsNullOrEmpty(songName))
        {
            var songs = await ctx.Songs.Where(x => x.Name.Contains(songName.ToLower())).ToListAsync();

            result = songs.Select(x => new SongQueryResult
            {
                SongId = x.Id,
                Name = x.Name,
                DocDate = x.DocDate,
                WordLength = x.WordLength
            }).ToList();
        }

        if (!(string.IsNullOrEmpty(composerFirstName) && string.IsNullOrEmpty(composerLastName)))
        {
            var pool = ctx.SongComposers
                .Include(x => x.Contributor)
                .Include(x => x.Song);

            IQueryable<SongComposer> query;

            if (!string.IsNullOrEmpty(composerFirstName))
                query = pool.Where(x => x.Contributor.FirstName == composerFirstName.ToLower());
            else
                query = pool.Where(x => x.Contributor.LastName == composerLastName.ToLower());

            var songComposers = await query.ToListAsync();

            result = songComposers.Select(x => new SongQueryResult
                {
                    SongId = x.Song.Id,
                    Name = x.Song.Name,
                    DocDate = x.Song.DocDate,
                    WordLength = x.Song.WordLength
                })
                .GroupBy(x => x.SongId)
                .Select(g => g.First())
                .ToList();
        }

        if (!string.IsNullOrEmpty(freeText)) // TODO now it is any we need all
        {
            var words = freeText.Split(_wordSplit, StringSplitOptions.RemoveEmptyEntries);

            var pool = ctx.SongWords
                .Include(x => x.Word)
                .Include(x => x.Song);

            var query = pool.Where(x => words.Contains(x.Word.WordText));
            var songWords = await query.ToListAsync();

            result = songWords.Select(x => new SongQueryResult
                {
                    SongId = x.Song.Id,
                    Name = x.Song.Name,
                    DocDate = x.Song.DocDate,
                    WordLength = x.Song.WordLength
                })
                .GroupBy(x => x.SongId)
                .Select(g => g.First())
                .ToList();
        }

        if (result is null)
        {
            var songs = await ctx.Songs.ToListAsync();
            result = songs.Select(x => new SongQueryResult
            {
                SongId = x.Id,
                Name = x.Name,
                DocDate = x.DocDate,
                WordLength = x.WordLength
            }).ToList();
        }

        return result;
    }

    #endregion

    #region Group

    public async Task<List<GroupResult>> GetGroups()
    {
        await using var ctx = ctxFactory();

        var groups = await ctx.GroupsView
            .Select(group => new GroupResult
            {
                Name = group.GroupName,
                Values = group.GroupValues
            }).ToListAsync();

        return groups;
    }

    public async Task<bool> AddGroup(string name, string[] values)
    {
        name = name.ToLower();

        await using var ctx = ctxFactory();

        await ctx.Database.BeginTransactionAsync();

        await InsertWordsIfMissing(ctx, values);

        var existingGroup = await ctx.Group.Where(x => x.Name == name).FirstOrDefaultAsync();

        var words = await ctx.Words.Where(x => values.Contains(x.WordText))
            .ToListAsync();

        var wordIds = words.Select(x => x.Id).ToHashSet();

        if (existingGroup is not null)
        {
            var existingGroupWords = await ctx.WordGroup
                .Where(wg => wg.GroupId == existingGroup.Id && wordIds.Contains(wg.WordId))
                .Select(x => x.WordId)
                .ToListAsync();

            var existingGroupWordsIdsSet = existingGroupWords.ToHashSet();

            var wordGroups = words
                .Where(wg => !existingGroupWordsIdsSet.Contains(wg.Id))
                .Select(word => new WordGroup
                {
                    GroupId = existingGroup.Id,
                    WordId = word.Id,
                }).ToArray();

            ctx.WordGroup.AddRange(wordGroups);
        }
        else
        {
            var group = new Group
            {
                Name = name
            };

            ctx.Group.Add(group);
            await ctx.SaveChangesAsync();

            var wordGroups = words.Select(word => new WordGroup
            {
                GroupId = group.Id,
                WordId = word.Id,
            }).ToArray();

            ctx.WordGroup.AddRange(wordGroups);
        }

        await ctx.SaveChangesAsync();

        await ctx.Database.CommitTransactionAsync();

        return true;
    }

    #endregion

    #region Phrase

    public async Task<List<string>> GetPhrases()
    {
        await using var ctx = ctxFactory();
        var phrases = await ctx.Phrases.Select(x => x.PhraseText).ToListAsync();

        return phrases;
    }

    public async Task<(string phrase, bool)> AddPhrase(string phrase)
    {
        phrase = phrase.ToLower();

        await using var ctx = ctxFactory();

        bool phraseExists = await ctx.Phrases.Where(x => x.PhraseText == phrase).AnyAsync();

        if (phraseExists)
            return (phrase, false);

        await using var tran = await ctx.Database.BeginTransactionAsync();

        var phraseToInsert = new Phrase
        {
            PhraseText = phrase
        };

        ctx.Phrases.Add(phraseToInsert);
        await ctx.SaveChangesAsync();

        var offset = 0;

        var phraseWords = phrase.Split(_wordSplit, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => new PhraseWord
            {
                PhraseId = phraseToInsert.Id,
                Word = word,
                Offset = offset++
            }).ToArray();


        ctx.PhraseWords.AddRange(phraseWords);
        await ctx.SaveChangesAsync();

        await tran.CommitAsync();

        return (phrase, true);
    }

    #endregion

    #region Song Processor Inserts

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
        var words = text
            .Split(_wordSplit, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.ToLower())
            .ToArray();

        return await InsertWordsIfMissing(ctx, words);
    }

    private async Task<(Dictionary<string, int> wordIndex, Word[])> InsertWordsIfMissing(SongsContext ctx,
        string[] words)
    {
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

    private async Task<Song> InsertSong(string? path, DateTime createDate, string text, SongsContext ctx)
    {
        var song = new Song
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(path)?.ToLower(),
            Path = path,
            DocDate = createDate.ToUniversalTime(),
            WordLength = text.Length
        };

        ctx.Add(song);
        await ctx.SaveChangesAsync();

        return song;
    }

    private async Task InsertContributorIfMissing(Contributor contributor, SongsContext ctx,
        ContributorType contributorTypeId, Song song)
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
    }

    private async Task<SongStanza[]> InsertSongStanzas(string input, Song song, SongsContext ctx)
    {
        var stanzas = input.Split(_stanzaSplit, StringSplitOptions.RemoveEmptyEntries);

        var offset = 0;

        var songStanzas = stanzas.Select(stanza =>
        {
            var songStanza = new SongStanza
            {
                SongId = song.Id,
                Song = song,
                Offset = offset++,
                WordLength = stanza.Length,
                StanzaText = stanza
            };

            return songStanza;
        }).ToArray();

        ctx.SongStanzas.AddRange(songStanzas);
        await ctx.SaveChangesAsync();

        return songStanzas.ToArray();
    }

    private async Task<SongLine[]> InsertSongLines(SongStanza[] songStanzas, string input, Song song, SongsContext ctx)
    {
        var offset = 0;

        var songLines = songStanzas
            .SelectMany(songStanza => songStanza.StanzaText
                .Split(_lineSplit, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => new
                {
                    Line = line,
                    StanzaId = songStanza.Id
                }))
            .Select((lineAndStanza) =>
            {
                var songLine = new SongLine
                {
                    SongId = song.Id,
                    Song = song,
                    Offset = offset++,
                    WordLength = lineAndStanza.Line.Length,
                    SongStanzaId = lineAndStanza.StanzaId,
                    SongLineText = lineAndStanza.Line
                };

                return songLine;
            }).ToArray();

        ctx.SongLines.AddRange(songLines);

        await ctx.SaveChangesAsync();
        return songLines.ToArray();
    }

    private async Task<WordLocation[]> InsertWordLocations(SongLine[] songLines, string input, SongWord[] songWords,
        SongsContext ctx)
    {
        var wordToSongWord = songWords.ToDictionary(x => x.Word.WordText, y => y);

        var offset = 0;

        var wordLocations = songLines
            .SelectMany(songLine => songLine.SongLineText.Split(_wordSplit, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => new
                {
                    Word = word.ToLower(),
                    LineId = songLine.Id
                }))
            .Select(wordAndLineId =>
            {
                if (wordToSongWord.TryGetValue(wordAndLineId.Word, out var songWord))
                {
                    var wordLocation = new WordLocation
                    {
                        Offset = offset,
                        SongWordId = songWord.Id,
                        SongWord = songWord,
                        SongLineId = wordAndLineId.LineId
                    };

                    offset += wordAndLineId.Word.Length;

                    return wordLocation;
                }

                throw new NullReferenceException(
                    "songWord could not be found in wordToSongWord dictionary");
            }).ToArray();

        ctx.WordLocations.AddRange(wordLocations);

        await ctx.SaveChangesAsync();
        return wordLocations.ToArray();
    }

    #endregion

    public List<TextOccurence> FindPhraseOccurrences(string songText, string phrase)
    {
        var index = 0;
        var occurrences = new List<TextOccurence>();
        
        var wordRegex = new Regex($@"\b{Regex.Escape(phrase)}\b", RegexOptions.Compiled);
        
        foreach (Match match in wordRegex.Matches(songText))
        {
            index = match.Index;

            var nextLineIndex = FindNextLine(songText, phrase, index);
            var prevLineIndex = FindPreviousLine(songText, phrase, index);
            occurrences.Add(new TextOccurence(index, nextLineIndex, prevLineIndex, 
                songText[prevLineIndex .. nextLineIndex].Trim(), phrase));
        }

        return occurrences;
    }

    private int FindNextLine(string songText, string phrase, int index)
    {
        int numOfNewLines = 1;
        index = phrase.Length + index;

        while (index < songText.Length && (songText[index] != '\n' || numOfNewLines > 0))
        {
            if (songText[index] == '\n')
                numOfNewLines--;

            index++;
        }

        if (index < songText.Length)
            return index;

        return songText.Length - 1;
    }

    private int FindPreviousLine(string songText, string phrase, int index)
    {
        int numOfNewLines = 1;

        index--;

        while (index >= 0 && (songText[index] != '\n' || numOfNewLines > 0))
        {
            if (songText[index] == '\n')
                numOfNewLines--;

            index--;
        }

        if (index >= 0)
            return index;

        return 0;
    }
}