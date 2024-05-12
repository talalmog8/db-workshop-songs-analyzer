namespace Model.Entities;

[Table("contributor")]
public class Contributor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; }

    [Column("last_name")]
    public string LastName { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)][Column("full_name")]
    public string FullName { get; set; }
}