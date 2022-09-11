using System.ComponentModel.DataAnnotations;

namespace LibrelancerAuthentication;

public class User
{
    [Key] public int Id { get; set; }

    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Guid { get; set; }
}