namespace Model.Entities;

[Table("song")]
public class Song
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    [MaxLength(50)]
    
    public string Name { get; set; }

    [Column("path")]
    [MaxLength(400)]
    public string Path { get; set; }

    [Column("doc_date")]
    public DateTime DocDate { get; set; }

    [Column("word_length")]
    public int WordLength { get; set; }
}