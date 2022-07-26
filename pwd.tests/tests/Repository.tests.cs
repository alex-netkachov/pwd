namespace pwd.tests;

public sealed class Repository_Tests
{
        [Test]
    public async Task GetItems1()
    {
        var repository = new Repository(Shared.GetMockFs(), new ZeroCipher(), new ZeroCipher(), ".");
        Assert.That(!(await repository.GetItems()).Any());
        Assert.That(!(await repository.GetItems()).Any());
        Assert.That(!(await repository.GetItems(".")).Any());
    }

    [Test]
    public async Task GetItems2()
    {
        var (pwd, _, _) = Shared.ContentEncryptionTestData();
        var cipher = new ContentCipher(pwd);
        var fs = await Shared.FileLayout1(Shared.GetMockFs());
        var repository = new Repository(fs, new ZeroCipher(), cipher, ".");
        var items1 = (await repository.GetItems()).Select(item => item.Name);
        Assert.That(string.Join(";", items1) == "encrypted;regular_dir");
        Assert.That(string.Join(";", (await repository.GetItems(".")).Select(item => item.Name)) == "encrypted;regular_dir");
        Assert.That(string.Join(";", (await repository.GetItems()).Select(item => item.Name)) == "encrypted;regular_dir");
        Assert.That(string.Join(";", (await repository.GetItems("regular_dir")).Select(item => item.Name)) == "regular_dir/encrypted");
        Assert.That(string.Join(";", (await repository.GetItems(".hidden_dir")).Select(item => item.Name)) == ".hidden_dir/encrypted");
    }

    [Test]
    public async Task GetEncryptedFilesRecursively1()
    {
        var repository = new Repository(Shared.GetMockFs(), new ZeroCipher(), new ZeroCipher(), ".");

        Assert.That(!(await repository.GetEncryptedFilesRecursively()).ToList().Any());
        Assert.That(!(await repository.GetEncryptedFilesRecursively(".")).Any());
    }

    [Test]
    public async Task GetEncryptedFilesRecursively2()
    {
        var (pwd, _, _) = Shared.ContentEncryptionTestData();
        var cipher = new ContentCipher(pwd);
        var fs = await Shared.FileLayout1(Shared.GetMockFs());

        var repository = new Repository(fs, new ZeroCipher(), cipher, ".");

        Assert.That(
            string.Join(";", (await repository.GetEncryptedFilesRecursively()).Select(item => item.Path)),
            Is.EqualTo("encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";", (await repository.GetEncryptedFilesRecursively(".")).Select(item => item.Path)),
            Is.EqualTo("encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";", (await repository.GetEncryptedFilesRecursively()).Select(item => item.Path)),
            Is.EqualTo("encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";",
                (await repository.GetEncryptedFilesRecursively(includeHidden: true)).Select(item => item.Path)),
            Is.EqualTo(
                ".hidden_dir/.hidden_encrypted;.hidden_dir/encrypted;.hidden_encrypted;encrypted;regular_dir/.hidden_encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";", (await repository.GetEncryptedFilesRecursively("regular_dir")).Select(item => item.Path)),
            Is.EqualTo("regular_dir/encrypted"));

        Assert.That(
            string.Join(";",
                (await repository.GetEncryptedFilesRecursively("regular_dir", true)).Select(item => item.Path)),
            Is.EqualTo("regular_dir/.hidden_encrypted;regular_dir/encrypted"));

        Assert.That(
            string.Join(";", (await repository.GetEncryptedFilesRecursively(".hidden_dir")).Select(item => item.Path)),
            Is.EqualTo(".hidden_dir/encrypted"));

        Assert.That(
            string.Join(";",
                (await repository.GetEncryptedFilesRecursively(".hidden_dir", true)).Select(item => item.Path)),
            Is.EqualTo(".hidden_dir/.hidden_encrypted;.hidden_dir/encrypted"));
    }
}