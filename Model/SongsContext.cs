using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Model.Entities;

public class SongsContext : DbContext
{
    private readonly IConfiguration _configuration;

    public DbSet<Contributor> Contributors { get; set; }
    public DbSet<ContributorType> ContributorTypes { get; set; }
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
    
    public SongsContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_configuration.GetConnectionString("songs"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure unique indexes

        modelBuilder.Entity<Group>().HasIndex(g => g.Name).IsUnique();
        modelBuilder.Entity<Phrase>().HasIndex(p => p.PhraseText).IsUnique();
        modelBuilder.Entity<Word>().HasIndex(w => w.WordText).IsUnique();
        modelBuilder.Entity<Contributor>().HasIndex(w => w.FullName).IsUnique();
        modelBuilder.Entity<ContributorType>().HasIndex(w => w.ContributorTypeDescription).IsUnique();
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

        // Configure foreign key relationships

         
        modelBuilder.Entity<SongLine>().HasOne(x => x.Song).WithMany().HasForeignKey(x => x.SongId);
        modelBuilder.Entity<WordGroup>().HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId);
        modelBuilder.Entity<WordGroup>().HasOne(x => x.Word).WithMany().HasForeignKey(x => x.WordId);
        modelBuilder.Entity<PhraseWord>().HasOne(x => x.Phrase).WithMany().HasForeignKey(x => x.PhraseId);
        modelBuilder.Entity<SongWord>().HasOne(x => x.Word).WithMany().HasForeignKey(x => x.WordId);
        modelBuilder.Entity<SongWord>().HasOne(x => x.Song).WithMany().HasForeignKey(x => x.SongId);
        modelBuilder.Entity<WordLocation>().HasOne(wl => wl.SongWord).WithMany().HasForeignKey(wl => wl.SongWordId);
        modelBuilder.Entity<ContributorContributorType>().HasOne(cct => cct.Contributor).WithMany().HasForeignKey(cct => cct.ContributorId);
        modelBuilder.Entity<SongStanza>().HasOne(ss => ss.Song).WithMany().HasForeignKey(ss => ss.SongId);
        modelBuilder.Entity<SongComposer>().HasOne(ss => ss.Song).WithMany().HasForeignKey(ss => ss.SongId);
        modelBuilder.Entity<SongComposer>()
            .HasOne(ss => ss.Contributor)
            .WithMany().HasForeignKey(ss => ss.ContributorId);

    }
}