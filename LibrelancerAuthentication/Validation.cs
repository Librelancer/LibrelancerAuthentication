using System.Security.Cryptography;
using System.Text;

namespace LibrelancerAuthentication;

public interface IValidateRecord
{
    bool Validate(string difficulty, out IResult error);
}

public static class Validation
{
    static string ComputeSHA256(string rawData)  
    {  
        // Create a SHA256   
        using (SHA256 sha256Hash = SHA256.Create())  
        {  
            // ComputeHash - returns byte array  
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));  
  
            // Convert byte array to a string   
            StringBuilder builder = new StringBuilder();  
            for (int i = 0; i < bytes.Length; i++)  
            {  
                builder.Append(bytes[i].ToString("x2"));  
            }  
            return builder.ToString();  
        }  
    }

    public static bool Nonce(string nonce, ref IResult error)
    {
        if (nonce.Length > 16)
        {
            error = Results.BadRequest("NonceTooLong");
            return false;
        }
        return true;
    }

    public static bool Time(long timestamp, ref IResult error)
    {
        var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        if (dateTimeOffset < DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5) ||
            dateTimeOffset > DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5))
        {
            error = Results.BadRequest("InvalidTimestamp");
            return false;
        }
        return true;
    }
    
    public static bool Hash(string hash, string difficulty, ref IResult error, params string[] parameters)
    {
        if (string.IsNullOrWhiteSpace(difficulty)) return true;
        
        if (hash.Length != 64)
        {
            error = Results.BadRequest("InvalidHash");
            return false;
        }

        if (!hash.StartsWith(difficulty))
        {
            error = Results.BadRequest("InvalidProof");
            return false;
        }

        if (hash != ComputeSHA256(string.Concat(parameters)))
        {
            error = Results.BadRequest("HashMismatch");
            return false;
        }
        return true;
    }
    
    public static bool PasswordLength(string password, ref IResult error)
    {
        if (password.Length < 6)
        {
            error = Results.BadRequest("PasswordTooShort");
            return false;
        }
        if (password.Length > 128)
        {
            error = Results.BadRequest("PasswordTooLong");
            return false;
        }
        return true;
    }

    public static bool TokenLength(string token, ref IResult error)
    {
        if (token.Length > 40)
        {
            error = Results.BadRequest("TokenTooLong");
            return false;
        }
        return true;
    }
    
    public static bool Username(string username, ref IResult error)
    {
        if (string.IsNullOrWhiteSpace(username)) 
        {
            error = Results.BadRequest("UsernameEmpty");
            return false;
        }
        if (username.Length > 64)
        {
            error = Results.BadRequest("UsernameTooLong");
            return false;
        }
        foreach (var c in username) {
            //ASCII letters, numbers and punctuation (not space)
            if (c < 33 || c > 126)
            {
                error = Results.BadRequest("UsernameInvalidCharacter");
                return false;
            }
        }
        return true;
    }
}