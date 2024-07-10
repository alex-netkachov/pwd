# Types

## ICipher

Interface. Represents a deterministic cipher that can encrypt and decrypt data.

Implementations: `AesCipher`

```mermaid
classDiagram

class ICipher {
    <<interface>>
    byte[] GetInitialisationData()
    void Encrypt(Stream input, Stream output)
    Task EncryptAsync(Stream input, Stream output, CancellationToken token = default)
    void Decrypt(Stream input, Stream output)
    Task DecryptAsync(Stream input, Stream output, CancellationToken cancellationToken = default)
}
```

## IStringEncoder

Interface. Represents a string encoder that can encode and decode strings.

Implementations: `Base64Url`

```mermaid
classDiagram
    
class IStringEncoder {
    <<interface>>
    string Encode(Stream input)
    Task<string> EncodeAsync(Stream input, CancellationToken token = default)
    void Decode(string input, Stream output)
    Task DecodeAsync(string input, Stream output, CancellationToken token = default)
}
```

## IRepository

Interface. Represents a repository that can read and write encrypted files.

Implementations: `FolderRepository`

```mermaid
classDiagram

class IRepository {
    <<interface>>
    string GetCurrentFolder()
    string SetCurrentFolder(string path)
    void Write(string path, string value)
    Task WriteAsync(string path, string value)
    string Read(string path)
    Task<string> ReadAsync(string path)
    void CreateFolder(string path)
    void Delete(string path)
    void Move(string path, string newLocation)
    IEnumerable<Location> List(string path, ListOptions? options = null)
    bool FileExist(string path)
    bool FolderExist(string path)
}
```

Repository paths are relative to the repository root and use forward slashes as separators. The root is the repository's root folder. For example, the path `folder/file.txt` refers to a file named `file.txt` in a folder named `folder` in the repository's root folder.

Repository names can contain any character except for the forward slash. Repository paths are case-sensitive. The names `.` and `..` are considered folders (current folder and parent folder, respectively).
