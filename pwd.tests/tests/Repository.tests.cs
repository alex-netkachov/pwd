namespace pwd.tests;

public sealed class Repository_Tests
{
        [Test]
    public async Task List1()
    {
        var repository = new Repository(Shared.GetMockFs(), new ZeroCipher(), new ZeroCipher(), ".");
        await repository.Initialise();
        Assert.That(!repository.List(".").Any());
    }

    [Test]
    public async Task List2()
    {
        var (pwd, _, _) = Shared.ContentEncryptionTestData();
        var cipher = new ContentCipher(pwd);
        var fs = await Shared.FileLayout1(Shared.GetMockFs());
        var repository = new Repository(fs, new ZeroCipher(), cipher, ".");
        Assert.That(string.Join(";", repository.List(".").Select(item => item.Path)) == "encrypted;regular_dir");
        Assert.That(string.Join(";", repository.List("regular_dir").Select(item => item.Path)) == "regular_dir/encrypted");
        Assert.That(string.Join(";", repository.List(".hidden_dir").Select(item => item.Path)) == ".hidden_dir/encrypted");
    }

    [Test]
    public async Task GetEncryptedFilesRecursively2()
    {
        var (pwd, _, _) = Shared.ContentEncryptionTestData();
        var cipher = new ContentCipher(pwd);
        var fs = await Shared.FileLayout1(Shared.GetMockFs());

        var repository = new Repository(fs, new ZeroCipher(), cipher, ".");

        Assert.That(
            string.Join(";", repository.List(".").Select(item => item.Path)),
            Is.EqualTo("encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";", repository.List(".").Select(item => item.Path)),
            Is.EqualTo("encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";", repository.List(".").Select(item => item.Path)),
            Is.EqualTo("encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";",
                repository.List(".").Select(item => item.Path)),
            Is.EqualTo(
                ".hidden_dir/.hidden_encrypted;.hidden_dir/encrypted;.hidden_encrypted;encrypted;regular_dir/.hidden_encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";", repository.List("regular_dir").Select(item => item.Path)),
            Is.EqualTo("regular_dir/encrypted"));

        Assert.That(
            string.Join(";",
                repository.List("regular_dir").Select(item => item.Path)),
            Is.EqualTo("regular_dir/.hidden_encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";", repository.List(".hidden_dir").Select(item => item.Path)),
            Is.EqualTo(".hidden_dir/encrypted"));

        Assert.That(
            string.Join(";",
                repository.List(".hidden_dir").Select(item => item.Path)),
            Is.EqualTo(".hidden_dir/.hidden_encrypted;.hidden_dir/encrypted"));
    }
}