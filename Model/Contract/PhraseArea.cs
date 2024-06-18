namespace Model.Contract;

public record PhraseArea(int Offset, int NextLine, int PrevLineIndex, string Text)
{
    public override string ToString()
    {
        return $"{{ Offset = {Offset}, NextLine = {NextLine}, PrevLineIndex = {PrevLineIndex} Text = {Text} }}";
    }
}