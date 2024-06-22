using Model.Contract;

namespace Model.Entities;

[Table("contributor")]
public class Contributor
{
    public Contributor(Name name)
    {
        FirstName = name.FirstName.TrimToMaxLength(50);
        LastName = name.LastName.TrimToMaxLength(50);
        FullName = $"{FirstName} {LastName}".TrimToMaxLength(100);
    }

    public Contributor()
    {
        
    }
    
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; }

    [Column("last_name")]
    public string LastName { get; set; }

    [Column("full_name")]
    public string FullName { get; set; }

    public virtual ICollection<ContributorContributorType> ContributorContributorTypes { get; set; }
}