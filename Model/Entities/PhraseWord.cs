namespace Model.Entities;

[Table("phrase_word")]
public class PhraseWord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("phrase_id")]
    public long PhraseId { get; set; }

    [Column("word")]
    [MaxLength(45)]
    public string Word { get; set; }

    [Column("offset")]
    public int Offset { get; set; }

    [ForeignKey("PhraseId")]
    public virtual Phrase Phrase { get; set; }
}