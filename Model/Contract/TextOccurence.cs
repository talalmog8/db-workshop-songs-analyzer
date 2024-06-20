namespace Model.Contract;

public class TextOccurence(int offset, int nextLine, int prevLineIndex, string text, string word)
{
    public string Word { get; init; } = word;
    public string Text { get; init; } = text;
    
    public int Offset { get; init; } = offset;
    public int NextLine { get; init; } = nextLine;
    public int PrevLineIndex { get; init; } = prevLineIndex;
}