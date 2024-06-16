namespace Model.Entities;

[Table("song_stanza")]
public class SongStanza
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("song_id")]
    public long SongId { get; set; }

    [Column("offset")]
    public int Offset { get; set; }

    [Column("word_length")]
    public int WordLength { get; set; }

    [ForeignKey("SongId")]
    public virtual Song Song { get; set; }

    // Not saved to db
    [NotMapped]
    public string StanzaText { get; set; }
}