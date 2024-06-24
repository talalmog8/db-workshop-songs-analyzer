using System.Text;
using System.Text.RegularExpressions;
using Group = Model.Entities.Group;

namespace Model;

public class SongAnalyzer(Func<SongsContext> ctxFactory) : ISongAnalyzer
{
    private readonly Regex _wordsRegex = new(@"\b\w+\b", RegexOptions.Compiled);
    private readonly SemaphoreSlim _searchSync = new(1, 1);

    private readonly string[] _stanzaSplit = [$"{Environment.NewLine}{Environment.NewLine}"];
    private readonly string[] _lineSplit = [$"{Environment.NewLine}"];
    
    private Song? _song;
    
    public string? Path { get; set; }
    public string? SongName => System.IO.Path.GetFileNameWithoutExtension(Path)?.ToLower();
    public required string SongContent { get; set; }
    public bool Processed { get; set; }
    
    #region Load

    public async Task<string> LoadSong(string path)
    {
        Path = path;
        SongContent = await File.ReadAllTextAsync(Path);
        SongContent = SongContent.ToLower();

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
                _song = song;
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

            _song = song;
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
        ArgumentNullException.ThrowIfNull(composers);
        ArgumentNullException.ThrowIfNull(writers);
        ArgumentNullException.ThrowIfNull(performers);
        ArgumentNullException.ThrowIfNull(_song);
        
        await InsertContributorsIfMissing(composers, ContributorType.MusicComposer, _song);
        await InsertContributorsIfMissing(writers, ContributorType.Writer, _song);
        await InsertContributorsIfMissing(performers, ContributorType.Performer, _song);
    }
    
    public async Task<ComposerView[]> GetComposers()
    {
        await using var ctx = ctxFactory();

        var composers = await ctx.Contributors
            .TagWithCallSite()
            .Include(x => x.ContributorContributorTypes)
            .ThenInclude(x => x.ContributorType)
            .ToListAsync();

        var composersView =
            composers.Select(x => new ComposerView
                {
                    Id = x.Id,
                    ComposerTypes = GetComposersList(x.ContributorContributorTypes),
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                })
                .OrderBy(x => x.Id).ToArray();

        return composersView;
    }

    public int GetTokenCount(string text)
    {
        return _wordsRegex.Matches(text).Count;
    }

    private string GetComposersList(ICollection<ContributorContributorType> contributorContributorTypes)
    {
        var builder = new StringBuilder();

        builder = contributorContributorTypes.Select(x => x.ContributorType)
            .Select(x => x.ContributorTypeDescription)
            .Aggregate(builder,
                (current, contributorTypeDescription) => current.Append($"{contributorTypeDescription}, "));

        if (builder.Length > 1)
            builder.Length -= 2;

        return builder.ToString();
    }

    #endregion

    #region Words

    public async Task<List<WordTable>> GetWords(string? songName = null, bool filterCurrentSong = false)
    {
        await using var ctx = ctxFactory();

        var query = ctx.WordView.AsQueryable();

        if (filterCurrentSong)
            query = query.Where(x => x.SongId == _song.Id);

        if (!string.IsNullOrEmpty(songName))
            query = query.Where(x => x.Song_Name.StartsWith(songName));

        var words = await query.Select(g => new WordTable
        {
            Id = g.WordId,
            WordText = g.WordText,
            MinOffset = g.MinOffset,
            Length = g.WordLength,
            NumberOfOccurrences = g.SumOfOccurrences,
        }).ToListAsync();

        return words;
    }

    public TextOccurence[] GetPhraseReference(string word)
    {
        var occurrences =
            FindPhraseOccurrences(SongContent, word)
                .Where(x => !string.IsNullOrEmpty(x.Text))
                .ToArray();

        return occurrences;
    }

    public async Task<string> FindWords(int stanzaOffset, int lineOffset, int wordOffset)
    {
        await using var ctx = ctxFactory();

        var stanza = await ctx.SongStanzas
            .Include(x => x.SongLines)
            .Where(x => x.Offset == stanzaOffset)
            .Where(x => x.SongId == _song.Id)
            .FirstOrDefaultAsync();

        if (stanza is null)
            return "Location Does Not Exist In This Song";

        var line = stanza.SongLines?.ElementAtOrDefault(lineOffset);

        if (line is null)
            return "Location Does Not Exist In This Song";

        var query = ctx.WordIndexView.AsQueryable()
            .Where(x => x.SongId == _song.Id)
            .Where(x => x.SongStanzaOffset == stanzaOffset)
            .Where(x => x.SongLineOffset == line.Offset);

        var result = await query
            .OrderBy(x => x.WordLocationOffset)
            .Select(x => x.WordText)
            .ToListAsync();

        return result.ElementAtOrDefault(wordOffset) ?? "Location Does Not Exist In This Song";
    }

    #endregion

    #region Words Index

    public async Task<List<WordIndexView>> GetWordIndex(string groupName = null)
    {
        List<WordIndexView> wordIndex;

        await using var ctx = ctxFactory();

        if (_song is null)
            return Enumerable.Empty<WordIndexView>().ToList();

        if (!string.IsNullOrEmpty(groupName))
        {
            var groupWordDetails = await ctx.GroupWordIndexView
                .Where(x => x.SongId == _song.Id)
                .Where(x => x.GroupName == groupName)
                .ToListAsync();

            wordIndex = groupWordDetails.Select(groupWordDetailsView => (WordIndexView)groupWordDetailsView).ToList();

            return wordIndex;
        }


        wordIndex = await ctx.WordIndexView
            .Where(x => x.SongId == _song.Id)
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

    public async Task<List<SongName>> SearchSongs(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return [];

        await _searchSync.WaitAsync();
        try
        {

            await using var ctx = ctxFactory();
            var result = await ctx.SearchSongsNames(searchTerm);
            return result;

        }
        finally
        {
            _searchSync.Release();
        }
    }

    public async Task SetUpDatabase()
    {
        await using var ctx = ctxFactory();
        await ctx.SetUpDatabase();
    }

    public async Task<List<SongQueryResult>> GetSongs(
        string songName,
        string composerFirstName,
        string composerLastName,
        string freeText)
    {
        var words = _wordsRegex.Matches(freeText).Select(x => x.Value?.Trim()?.ToLower()).ToHashSet();
        
        await using var ctx = ctxFactory();

        var query = ctx.SongView.AsQueryable();

        if (!string.IsNullOrEmpty(songName))
            query = query.Where(x => x.SongName.StartsWith(songName.Trim().ToLower()));
        if (!string.IsNullOrEmpty(composerFirstName))
            query = query.Where(x => x.FirstName.StartsWith(composerFirstName.Trim().ToLower()));
        if (!string.IsNullOrEmpty(composerLastName))
            query = query.Where(x => x.LastName.StartsWith(composerLastName.Trim().ToLower()));
        if (words.Any())
            query = query.Where(x => words.Contains(x.Word));

        var result = await query
            .GroupBy(view => new { view.SongName, view.FirstName, view.LastName, view.ContributionType, view.SongComposerId, view.DocumentDate, view.SongWordLength })
            .Select(x=> new SongQueryResult
            {
                Name = x.Key.SongName,
                DocDate = x.Key.DocumentDate,
                WordLength = x.Key.SongWordLength,
                FirstName= x.Key.FirstName,
                LastName= x.Key.LastName,
                ContributionType= x.Key.ContributionType,
            })
            .OrderBy(x=> x.Name)
            .ThenBy(x=> x.FirstName)
            .ThenBy(x=> x.LastName)
            .ToListAsync();

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

        var phraseWords = _wordsRegex.Matches(phrase)
            .Select(x => x.Value)
            .Select(word => new PhraseWord
            {
                PhraseId = phraseToInsert.Id,
                Word = word.TrimToMaxLength(45),
                Offset = offset++
            }).ToArray();


        ctx.PhraseWords.AddRange(phraseWords);
        await ctx.SaveChangesAsync();

        await tran.CommitAsync();

        return (phrase, true);
    }

    #endregion

    #region Song Processor Inserts

    private async Task InsertContributorsIfMissing(HashSet<Name> composers, ContributorType contributorType, Song song)
    {
        foreach (var composer in composers)
        {
            await using var ctx = ctxFactory();
            await using var tran = await ctx.Database.BeginTransactionAsync();
            try
            {
                await InsertContributorIfMissing(new Contributor(composer), ctx, contributorType, song);
                await tran.CommitAsync();
            }
            catch (Exception e)
            {
                await tran.RollbackAsync();
                throw;
            }
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
        var words = _wordsRegex
            .Matches(text)
            .Select(x => x.Value.ToLower())
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
            .AsNoTracking()
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
            .AsNoTracking()
            .Where(x => x.ContributorId == contributor.Id && x.ContributorTypeId == (int)contributorTypeId)
            .FirstOrDefaultAsync();

        var contributorContributorType = new ContributorContributorType
        {
            ContributorTypeId = (int)contributorTypeId,
            ContributorId = contributor.Id
        };

        if (existingContributorContributorType is null)
        {
            ctx.ContributorContributorTypes.Add(contributorContributorType);
            await ctx.SaveChangesAsync();
        }
        else
            contributorContributorType = existingContributorContributorType;

        contributorContributorType.Contributor = contributor;

        var songComposer = new SongComposer
        {
            ContributorId = contributor.Id,
            ContributorTypeId = contributorContributorType.ContributorTypeId,
            SongId = song.Id
        };

        var existingSongComposer = await ctx.SongComposers.AsQueryable()
            .AsNoTracking()
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
            .SelectMany(songLine => _wordsRegex.Matches(songLine.SongLineText)
                .Select(x=> x.Value)
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
                        Offset = offset++,
                        SongWordId = songWord.Id,
                        SongWord = songWord,
                        SongLineId = wordAndLineId.LineId
                    };

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

        var wordRegex = new Regex($@"\b{phrase}\b", RegexOptions.Compiled);

        foreach (Match match in wordRegex.Matches(songText))
        {
            index = match.Index;


            var nextLineIndex = FindNextLine(songText, phrase, index);
            var prevLineIndex = FindPreviousLine(songText, index);
            occurrences.Add(new TextOccurence(index, nextLineIndex, prevLineIndex,
                songText[prevLineIndex .. nextLineIndex].Trim(), phrase));
        }

        return occurrences;
    }

    private int FindNextLine(string songText, string phrase, int index)
    {
        int numOfNewLines = 2;
        index = phrase.Length + index;

        while (index < songText.Length && (songText[index] != '\n' || numOfNewLines > 0))
        {
            if (songText[index] == '\n')
                numOfNewLines--;

            index++;
        }

        if (index <= songText.Length)
            return index;

        return songText.Length - 1;
    }

    private int FindPreviousLine(string songText, int index)
    {
        int numOfNewLines = 2;

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