namespace Model.Contract;

public class WordTable
{
    public long Id { get; set; }
    public string WordText { get; set; }
    public int Length { get; set; }
    public int NumberOfOccurrences { get; set; }
}