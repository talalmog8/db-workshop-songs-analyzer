namespace Model.Entities;

[Table("group")]
public class Group
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    [MaxLength(50)]
    
    public string? Name { get; set; }
}