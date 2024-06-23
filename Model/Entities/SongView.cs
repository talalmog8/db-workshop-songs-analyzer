namespace Model.Entities;

[Table("songs_view")]
public class SongView
{
    [Column("song_name", TypeName = "varchar(50)")]
    public string SongName { get; set; }

    [Column("first_name", TypeName = "varchar(50)")]
    public string FirstName { get; set; }

    [Column("last_name", TypeName = "varchar(50)")]
    public string LastName { get; set; }

    [Column("contribution_type", TypeName = "varchar(50)")]
    public string ContributionType { get; set; }

    [Column("file_path", TypeName = "varchar(400)")]
    public string FilePath { get; set; }

    [Column("document_date", TypeName = "timestamp")]
    public DateTime DocumentDate { get; set; }

    [Column("song_word_length", TypeName = "int")]
    public int SongWordLength { get; set; }

    [Column("word", TypeName = "varchar(50)")]
    public string Word { get; set; }
    
    [Column("song_composer_id", TypeName = "bigint")]
    public long SongComposerId { get; set; }
}