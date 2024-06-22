namespace Model.Entities;

[Table("group_word_index_view")]
public class GroupWordIndexView : WordIndexView
{
    [Column("group_name"), ]
    public string GroupName { get; set; }
}