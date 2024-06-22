using System.ComponentModel;

namespace Model.Entities;

[Table("word_index_view")]
public class WordIndexView
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("word_location_offset"), ]
    public int WordLocationOffset { get; set; }
    
    [Column("word_num_of_occurrences")]
    public int WordNumOfOccurrences { get; set; }
    
    [Column("word_length"), ]
    public int WordLength { get; set; }

    [Column("word_text"), ]
    public string WordText { get; set; }

    [Column("song_line_word_length"), ]
    public int SongLineWordLength { get; set; }

    [Column("song_stanza_offset"), ]
    public int SongStanzaOffset { get; set; }
    
    [Column("song_line_offset"), ]
    public int SongLineOffset { get; set; }
    
    [Column("song_stanza_word_length"), ]
    public int SongStanzaWordLength { get; set; } 
    
    [Column("song_id"), ]
    public long SongId { get; set; }   
}