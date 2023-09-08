using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("users")]
public class User {
    [Key]
    public string user_id { get; set; }
    public string? profile_picture { get; set; }
    public string? cover_picture { get; set; }
}