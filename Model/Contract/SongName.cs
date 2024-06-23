namespace Model.Contract;

public class SongName
{
    [Column("name", TypeName = "varchar(50)")]
    public string Name { get; set; }
}   