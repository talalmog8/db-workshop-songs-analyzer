using Model.Entities;

namespace Model.Contract;

public class SongInformation
{
    public ProcessingResult ProcessingResult { get; set; } 
    public Song Song { get; set; } 
    public SongStanza[] SongStanzas { get; set; }
    public SongLine[] SongLines { get; set; }
    public ICollection<SongWord> SongWords { get; set; }
    public WordLocation[] WordLocations { get; set; }
    public Dictionary<string, int> WordIndex { get; set; }
    public Word[] Words { get; set; }
}