namespace Model;

public record WordIndex(string Word, int Count)
{
    public override string ToString()
    {
        return $"{{ Word = {Word}, Count = {Count} }}";
    }
}