namespace Model.Entities;

[Table("contributor_type")]
public class ContributorType
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("contributor_type_description")]
    [MaxLength(50)]
    
    [Required]
    public string ContributorTypeDescription { get; set; }
}