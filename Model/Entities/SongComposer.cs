namespace Model.Entities;

[Table("song_composers")]
public class SongComposer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("contributor_id")]
    public long ContributorId { get; set; }

    [Column("contributor_type")]
    public long ContributorTypeId { get; set; }

    [Column("song_id")]
    public long SongId { get; set; }

    [ForeignKey("ContributorId")]
    public virtual Contributor Contributor { get; set; }

    [ForeignKey("ContributorTypeId")]
    public virtual ContributorType ContributorType { get; set; }

    [ForeignKey("SongId")]
    public virtual Song Song { get; set; }
}