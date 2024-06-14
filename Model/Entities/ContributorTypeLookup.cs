namespace Model.Entities;

[Table("contributor_type")]
public class ContributorTypeLookup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("contributor_type_description")]
    public string ContributorTypeDescription { get; set; }
}