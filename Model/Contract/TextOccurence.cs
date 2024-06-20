namespace Model.Contract;

public class TextOccurence(int offset, int nextLine, int prevLineIndex, string text, string word)
{
    public int Offset { get; init; } = offset;
    public int NextLine { get; init; } = nextLine;
    public int PrevLineIndex { get; init; } = prevLineIndex;
    public string Text { get; init; } = text;
    public string Word { get; init; } = word;
}