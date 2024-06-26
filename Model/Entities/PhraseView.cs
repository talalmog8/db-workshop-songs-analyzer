namespace Model.Entities;

[Table("phrase_view")]
public class PhraseView
{
    [Key]
    [Column("phrase_id")]
    public long PhraseId { get; set; }

    [Column("phrase_values")]
    public string PhraseValues { get; set; }
}