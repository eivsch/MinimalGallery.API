using System.Security.Cryptography;
using System.Text;

namespace MinimalGallery.API;

static class Globals
{
    public const string FILE_EXT = "dat";

    public static string StoragePath {get;set;} = "not set";

    // TODO: add salt
    public static string GetHash(string input)
    {
        byte[] data = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        StringBuilder sBuilder = new();

        // Loop through each byte of the hashed data
        // and format each one as a hexadecimal string.
        for (int i = 0; i < data.Length; i++)
        {
            string hexStr = data[i].ToString("x2");
            sBuilder.Append(hexStr);
        }

        return sBuilder.ToString();
    }

    public static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
    {
        byte[] data = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        string hashOfInput = GetHash(input);

        // Create a StringComparer an compare the hashes.
        StringComparer comparer = StringComparer.OrdinalIgnoreCase;

        return comparer.Compare(hashOfInput, hash) == 0;
    }
}