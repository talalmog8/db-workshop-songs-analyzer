namespace Model.Entities;

[Table("contributor_contributor_type")]
public class ContributorContributorType
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("contributor_type")]
    public long ContributorTypeId { get; set; }

    [Column("contributor_id")]
    public long ContributorId { get; set; }

    [ForeignKey("ContributorId")]
    public virtual Contributor Contributor { get; set; }
}