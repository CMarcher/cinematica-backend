using System.ComponentModel.DataAnnotations;

public class users {
    [Key]
    public string user_id { get; set; }
    public string? profile_picture { get; set; }
    public string? cover_picture { get; set; }
}