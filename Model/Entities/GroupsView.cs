namespace Model.Entities
{
    [Table("groups_view")]
    public class GroupsView
    {
        [Column("group_id")]
        public long GroupId { get; set; }

        [Column("group_name")]
        public string GroupName { get; set; }

        [Column("group_values")]
        public string GroupValues { get; set; }
    }
}