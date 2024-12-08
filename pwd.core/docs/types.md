# Types

## ICipher

A cipher. It encrypts and decrypts streams of data. Implementation can provide an initialisation data that can be used to create a new cipher with the same configuration. If the same initialisation data is used to create a cipher, the cipher produces the same output for the same input. 

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

A string encoder. It encodes and decodes stream of data to and from strings.

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

A repository with encrypted files.

Implementations: `FolderRepository`

```mermaid
classDiagram

class IRepository {
    <<interface>>
    string GetCurrentFolder()
    string SetCurrentFolder(string path)
    void Write(string path, string value)
    Task WriteAsync(string path, string value)
    string ReadText(string path)
    Task<string> ReadTextAsync(string path)
    void CreateFolder(string path)
    void Delete(string path)
    void Move(string path, string newLocation)
    IEnumerable<Location> List(string path, ListOptions? options = null)
    bool FileExist(string path)
    bool FolderExist(string path)
}
```

Repository paths are relative to the current folder and use forward slashes as separators. For example, the path `folder/file.txt` refers to a file named `file.txt` in a folder named `folder` in the repository's root folder.

Repository names can contain any character except for the forward slash. Repository paths are case-sensitive. The names `.` and `..` are considered folders (current folder and parent folder).
