namespace Model.Entities;

[Table("phrase")]
public class Phrase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("phrase")]
    [MaxLength(250)]
    
    public string PhraseText { get; set; }
}