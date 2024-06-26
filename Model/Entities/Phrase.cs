namespace Model.Entities;

[Table("phrase")]
public class Phrase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("phrase_hash")]
    [MaxLength(250)]
    
    public string PhraseHash      { get; set; }
    
    public virtual ICollection<PhraseWord> PhraseWords { get; set; }
    
}