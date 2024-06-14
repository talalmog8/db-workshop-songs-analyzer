using Model.Contract;

namespace Model.Entities;

[Table("contributor")]
public class Contributor
{
    public Contributor(Name name)
    {
        FirstName = name.FirstName;
        LastName = name.LastName;
        FullName = $"{FirstName} {LastName}";
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