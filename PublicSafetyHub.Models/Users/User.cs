namespace PublicSafetyHub.Models.Users;

public class User
{
    public int Id { get; set; }

    public required string Email { get; set; }

    public bool IsBlocked { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
