namespace BehaviorTest.Application.RBAC.Tools;

public class DataEncryption
{
    public static string Sha1Encrypt(string input)
    {
        using (var sha1 = System.Security.Cryptography.SHA1.Create())
        {
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hashBytes = sha1.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}