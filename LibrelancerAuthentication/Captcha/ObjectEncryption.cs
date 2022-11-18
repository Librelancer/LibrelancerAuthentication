using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LibrelancerAuthentication.Captcha;

public class ObjectEncryption : IDisposable
{
    private RSACryptoServiceProvider csp;

    public ObjectEncryption()
    {
        csp = new RSACryptoServiceProvider(2048);
        csp.PersistKeyInCsp = false;
    }

    public string Encrypt<T>(T obj)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(obj);
        return Convert.ToBase64String(csp.Encrypt(bytes, RSAEncryptionPadding.Pkcs1));
    }

    public bool TryDecrypt<T>(string input, out T decrypted)
    {
        try
        {
            var bytes = csp.Decrypt(Convert.FromBase64String(input), RSAEncryptionPadding.Pkcs1);
            decrypted = JsonSerializer.Deserialize<T>(bytes);
            return true;
        }
        catch (Exception)
        {
            decrypted = default;
            return false;
        }
    }

    public void Dispose()
    {
        csp.Dispose();
    }
}