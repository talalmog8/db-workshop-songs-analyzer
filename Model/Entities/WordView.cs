namespace Model.Entities;

[Table("words_view")]
public class WordView
{
    [Key]
    [Column("word_id", TypeName = "bigint")]
    public long WordId { get; set; } 
    
    [Column("song_id", TypeName = "bigint")]
    public long SongId { get; set; }

    [Column("song_name", TypeName = "varchar(50)")]
    public string Song_Name { get; set; }

    [Column("word_text", TypeName = "varchar(45)")]
    [MaxLength(255)]
    public string WordText { get; set; }

    [Column("word_length", TypeName = "integer")]
    public int WordLength { get; set; }

    [Column("sum_of_occurrences", TypeName = "integer")]
    public int SumOfOccurrences { get; set; }

    [Column("min_offset", TypeName = "integer")]
    public int MinOffset { get; set; }
}