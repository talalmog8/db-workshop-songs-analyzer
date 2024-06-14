namespace Model.Contract;

public class Stats(
    double averageWordLength,
    double averageSongLineWordLength,
    double averageSongStanzaWordLength,
    double averageSongWordLength)
{
    public double AverageWordLength { get; } = Math.Round(averageWordLength, 3);
    public double AverageSongLineWordLength { get; } = Math.Round(averageSongLineWordLength, 3);
    public double AverageSongStanzaWordLength { get; } = Math.Round(averageSongStanzaWordLength, 3);
    public double AverageSongWordLength { get; } = Math.Round(averageSongWordLength, 3);
}