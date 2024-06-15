namespace Model.Entities;

[Table("word_location")]
public class WordLocation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("offset")]
    public int Offset { get; set; }

    [Column("song_word_id")]
    public long SongWordId { get; set; }
    public virtual SongWord SongWord { get; set; }
}