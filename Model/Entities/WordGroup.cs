namespace Model.Entities;

[Table("word_group")]
public class WordGroup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("word_id")]
    public long WordId { get; set; }
    
    public virtual Group Group { get; set; }
    public virtual Word Word { get; set; }
}