namespace Model.Entities;

[Table("contributor")]
public class Contributor
{
    public Contributor(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
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
}