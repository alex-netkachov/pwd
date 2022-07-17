using System.Threading.Tasks;

namespace pwd;

// ReSharper disable UnusedMember.Local because the tests are called through reflection

public static partial class Program
{
    private static async Task Test_Cipher_Roundup()
    {
        var (password, text) = EncryptionTestData();
        var cipher = new Cipher(password);
        var encrypted = await cipher.Encrypt(text);
        var decrypted = await cipher.Decrypt(encrypted);
        Assert(text == decrypted);
    }
}
