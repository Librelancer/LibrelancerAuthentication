namespace LibrelancerAuthentication;

public interface IUserDbService
{
    bool CanRegister { get; }
    bool CanChangePassword { get;  }
    
    Task<User> Login(string username, string password);
    Task<bool> ChangePassword(string username, string oldpassword, string newpassword);
    Task<bool> Register(string username, string password);
}