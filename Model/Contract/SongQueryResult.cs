namespace Model.Contract;

public class SongQueryResult
{
    public long SongId { get; set; }
    public string Name { get; set; }
    public DateTime DocDate { get; set; }
    public int WordLength { get; set; }
}