namespace Model.Entities;

[Table("word")]
public class Word
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("word")]
    [MaxLength(45)]
    
    public string WordText { get; set; }

    [Column("length")]
    public int Length { get; set; }
}