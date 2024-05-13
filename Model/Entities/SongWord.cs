namespace Model.Entities;

[Table("song_word")]
public class SongWord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("word_id")]
    public long WordId { get; set; }

    [Column("song_id")]
    public long SongId { get; set; }
    
    [Column("num_of_occurrences")]
    public int NumberOfOccurrences { get; set; }

    [ForeignKey("WordId")]
    public virtual Word Word { get; set; }

    [ForeignKey("SongId")]
    public virtual Song Song { get; set; }
}