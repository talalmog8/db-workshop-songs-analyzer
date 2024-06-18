namespace Model.Entities;

[Table("group_word_details_view")]
public class GroupWordDetailsView : WordDetailsView
{
    [Column("group_name"), ]
    public string GroupName { get; set; }
}