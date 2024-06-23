namespace Model.Contract;

public class SongQueryResult
{
    public string Name { get; set; }
    public DateTime DocDate { get; set; }
    public int WordLength { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string ContributionType { get; set; }
}