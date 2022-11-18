using System.Collections.Concurrent;
using System.Text;

namespace LibrelancerAuthentication;

public class TokenProvider
{
    private readonly TimeSpan expireTime;

    private readonly ExpiringDictionary<string, Token> tokens = new();

    public TokenProvider(TimeSpan expireTime)
    {
        this.expireTime = expireTime;
    }


    private static string ToBase36(int value)
    {
        const string base36 = "0123456789abcdefghijklmnopqrstuvwxyz";
        var sb = new StringBuilder(13);
        do
        {
            sb.Insert(0, base36[(byte) (value % 36)]);
            value /= 36;
        } while (value != 0);

        return sb.ToString();
    }


    //This is guaranteed to produce a string < 40 characters
    public string GenerateToken(int id, string value)
    {
        var shortGuid = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .TrimEnd('=')
            .Replace("/", "_")
            .Replace("+", "-");
        var idStr = ToBase36(id);
        tokens.Set(idStr, new Token(DateTime.UtcNow + expireTime, shortGuid, value));
        return $"{idStr}Z{shortGuid}";
    }

    public bool VerifyToken(string token, out string value)
    {
        value = null;
        var zIdx = token.IndexOf('Z');
        if (zIdx == -1) return false;
        var idStr = token.Substring(0, zIdx);
        var shortGuid = token.Substring(zIdx + 1);

        if (!tokens.TryGetValue(idStr, out var tk)) return false;
        if (tk.TokenString != shortGuid) return false;
        tokens.Remove(idStr);

        value = tk.Value;
        return true;
    }

    private record Token(DateTime Expiry, string TokenString, string Value) : IExpiringItem;
}