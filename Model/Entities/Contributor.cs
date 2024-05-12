namespace Model.Entities;

[Table("contributor")]
public class Contributor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; }

    [Column("last_name")]
    public string LastName { get; set; }

    [NotMapped] // Exclude from mapping as it's computed by the database
    public string FullName { get; set; }
}