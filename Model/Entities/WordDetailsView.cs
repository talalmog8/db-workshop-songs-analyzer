using System.ComponentModel;

namespace Model.Entities;

[Table("word_details_view")]
public class WordDetailsView
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("word_location_offset"), Display(Name ="word offset")]
    public int WordLocationOffset { get; set; }

    [Column("song_word_id"), Browsable(false)] 
    public int SongWordId { get; set; }

    [Column("word_num_of_occurrences"), Display(Name ="word song occurrences")]
    public int WordNumOfOccurrences { get; set; }
    
    [Column("word_length"), Display(Name ="word length")]
    public int WordLength { get; set; }

    [Column("word_text"), Display(Name ="word")]
    public string WordText { get; set; }

    [Column("song_line_word_length"), Display(Name ="line length")]
    public int SongLineWordLength { get; set; }

    [Column("song_stanza_offset"), Display(Name ="stanza offset")]
    public int SongStanzaOffset { get; set; }
    
    [Column("song_line_offset"), Display(Name ="line offset")]
    public int SongLineOffset { get; set; }
    
    [Column("song_stanza_word_length"), Display(Name ="stanza length")]
    public int SongStanzaWordLength { get; set; }
}