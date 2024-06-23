using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Model.Contract;
using Model.Entities;
using Npgsql;

namespace Model;

public class SongsContext : DbContext
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;

    public DbSet<Contributor> Contributors { get; set; }
    public DbSet<ContributorContributorType> ContributorContributorTypes { get; set; }
    public DbSet<Song> Songs { get; set; }
    public DbSet<SongComposer> SongComposers { get; set; }
    public DbSet<SongStanza> SongStanzas { get; set; }
    public DbSet<SongLine> SongLines { get; set; }
    public DbSet<Word> Words { get; set; }
    public DbSet<SongWord> SongWords { get; set; }
    public DbSet<WordLocation> WordLocations { get; set; }

    public DbSet<Phrase> Phrases { get; set; }

    public DbSet<PhraseWord> PhraseWords { get; set; }

    public DbSet<Group> Group { get; set; }
    public DbSet<WordGroup> WordGroup { get; set; }
    public DbSet<GroupsView> GroupsView { get; set; }
    public DbSet<WordIndexView> WordIndexView { get; set; }
    public DbSet<GroupWordIndexView> GroupWordIndexView { get; set; }
    public DbSet<WordView> WordView { get; set; }
    public DbSet<SongView> SongView { get; set; }
    public DbSet<SongName> SongsSearch { get; set; } 
    
    public SongsContext(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _loggerFactory = loggerFactory;
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.UseNpgsql(_configuration.GetConnectionString("songs"));
        optionsBuilder.UseLoggerFactory(_loggerFactory);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure unique indexes
        modelBuilder.Entity<Group>().HasIndex(g => g.Name).IsUnique();
        modelBuilder.Entity<Phrase>().HasIndex(p => p.PhraseText).IsUnique();
        modelBuilder.Entity<Word>().HasIndex(w => w.WordText).IsUnique();
        modelBuilder.Entity<Contributor>().HasIndex(w => w.FullName).IsUnique();
        modelBuilder.Entity<Song>().HasIndex(w => w.Name).IsUnique();
        modelBuilder.Entity<SongComposer>().HasIndex(w => new
        {
            w.ContributorId,
            w.ContributorTypeId,
            w.SongId
        }).IsUnique();
        modelBuilder.Entity<ContributorContributorType>().HasIndex(w => new
        {
            w.ContributorId,
            w.ContributorTypeId
        }).IsUnique();
        modelBuilder.Entity<SongLine>().HasIndex(w => new
        {
            w.SongId,
            w.Offset
        }).IsUnique();
        modelBuilder.Entity<SongWord>().HasIndex(x => new
        {
            x.SongId,
            x.WordId
        }).IsUnique();
        modelBuilder.Entity<WordLocation>().HasIndex(wl => new
        {
            wl.SongWordId,
            wl.Offset
        }).IsUnique();
        modelBuilder.Entity<WordGroup>().HasIndex(x => new
        {
            x.GroupId,
            x.WordId
        }).IsUnique();
        modelBuilder.Entity<PhraseWord>().HasIndex(x => new
        {
            x.PhraseId,
            x.Word,
            x.Offset
        }).IsUnique();
        modelBuilder.Entity<SongStanza>().HasIndex(x => new
        {
            x.SongId,
            x.Offset
        }).IsUnique();

        // One-to-Many Relationships
        modelBuilder.Entity<SongLine>()
            .HasOne(sl => sl.Song)
            .WithMany(s => s.SongLines)
            .HasForeignKey(sl => sl.SongId);

        modelBuilder.Entity<SongLine>()
            .HasOne(sl => sl.Stanza)
            .WithMany(s => s.SongLines)
            .HasForeignKey(sl => sl.SongStanzaId);

        modelBuilder.Entity<SongStanza>()
            .HasOne(ss => ss.Song)
            .WithMany(s => s.SongStanzas)
            .HasForeignKey(ss => ss.SongId);

        modelBuilder.Entity<SongComposer>()
            .HasOne(sc => sc.Song)
            .WithMany(s => s.SongComposers)
            .HasForeignKey(sc => sc.SongId);

        modelBuilder.Entity<SongComposer>()
            .HasOne(sc => sc.Contributor)
            .WithMany()
            .HasForeignKey(sc => sc.ContributorId);

        // Many-to-Many Relationships
        modelBuilder.Entity<ContributorContributorType>()
            .HasKey(cct => new { cct.ContributorId, cct.ContributorTypeId });

        modelBuilder.Entity<ContributorContributorType>()
            .HasOne(cct => cct.Contributor)
            .WithMany(c => c.ContributorContributorTypes)
            .HasForeignKey(cct => cct.ContributorId);

        modelBuilder.Entity<ContributorContributorType>()
            .HasOne(cct => cct.ContributorType)
            .WithMany()
            .HasForeignKey(cct => cct.ContributorTypeId);

        // Additional relationships

        modelBuilder.Entity<WordGroup>().HasKey(wg => new { wg.GroupId, wg.WordId });


        modelBuilder.Entity<WordGroup>()
            .HasOne(wg => wg.Group)
            .WithMany(g => g.WordGroups)
            .HasForeignKey(wg => wg.WordId);

        modelBuilder.Entity<WordGroup>()
            .HasOne(wg => wg.Group)
            .WithMany(w => w.WordGroups)
            .HasForeignKey(wg => wg.GroupId);

        modelBuilder.Entity<PhraseWord>()
            .HasOne(pw => pw.Phrase)
            .WithMany(p => p.PhraseWords)
            .HasForeignKey(pw => pw.PhraseId);

        modelBuilder.Entity<SongWord>()
            .HasOne(sw => sw.Word)
            .WithMany()
            .HasForeignKey(sw => sw.WordId);

        modelBuilder.Entity<SongWord>()
            .HasOne(sw => sw.Song)
            .WithMany(s => s.SongWords)
            .HasForeignKey(sw => sw.SongId);

        modelBuilder.Entity<WordLocation>()
            .HasOne(wl => wl.SongWord)
            .WithMany(sw => sw.WordLocations)
            .HasForeignKey(wl => wl.SongWordId);

        modelBuilder.Entity<WordIndexView>().ToView("word_index_view");
        modelBuilder.Entity<GroupWordIndexView>().ToView("group_word_index_view");
        modelBuilder.Entity<WordView>().ToView("words_view");
        modelBuilder.Entity<GroupsView>().ToView("groups_view").HasKey(view => new { view.GroupId, view.GroupName });
        modelBuilder.Entity<SongView>().ToView("songs_view").HasKey(view => new
        {
            view.SongName, view.FirstName, view.LastName, view.ContributionType, view.SongComposerId
        });
        
        modelBuilder.Entity<SongName>().HasNoKey();
    }
    
    public async Task<List<SongName>> SearchSongsNames(string searchTerm)
    {
        var termParam = new NpgsqlParameter("term", searchTerm);

        var results = await SongsSearch
            .FromSqlRaw("SELECT * FROM search_song_by_similarity({0})", termParam)
            .ToListAsync();

        return results;
    }
}