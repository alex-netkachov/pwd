using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace pwd.core;

public static class ConfigFile
{
   public static bool TryGetCipherInitialisationData(
      IFileSystem fs,
      string pwdJsonFilePath,
      out byte[]? initialisationData)
   {
      initialisationData = GetCipherInitialisationData(fs, pwdJsonFilePath);
      return initialisationData is not null;
   }
   
   public static byte[]? GetCipherInitialisationData(
      IFileSystem fs,
      string pwdJsonFilePath)
   {
      if (!fs.File.Exists(pwdJsonFilePath))
         return null;

      byte[] pwdJsonFileContent;
      try
      {
         pwdJsonFileContent = fs.File.ReadAllBytes(pwdJsonFilePath);
      }
      catch
      {
         return null;
      }

      if (JsonNode.Parse(pwdJsonFileContent) is not { } jsonNode)
         return null;

      var configObject = jsonNode.AsObject();
      if (!configObject.TryGetPropertyValue("Cipher", out var cipherConfigPropertyValue)
          || cipherConfigPropertyValue is not { } cipherConfig)
      {
         return null;
      }

      var cipherConfigObject = cipherConfig.AsObject();
      if (!cipherConfigObject.TryGetPropertyValue("InitialisationData", out var initialisationDataPropertyValue)
          || initialisationDataPropertyValue is not { } initialisationData)
      {
         return null;
      }

      var initialisationDataString = initialisationData.AsValue().GetValue<string>();
      var cipherInitialisationData = Convert.FromBase64String(initialisationDataString);

      return cipherInitialisationData;
   }

   public static void WriteCipherInitialisationData(
      IFileSystem fs,
      string pwdJsonFilePath,
      byte[] initialisationData)
   {
      var content = new MemoryStream();

      var jsonWriter = new Utf8JsonWriter(content);

      // { "Cipher": { "InitialisationData": "..." } }
      jsonWriter.WriteStartObject();
      jsonWriter.WritePropertyName("Cipher");
      jsonWriter.WriteStartObject();
      jsonWriter.WriteBase64String(
         "InitialisationData",
         initialisationData);
      jsonWriter.WriteEndObject();
      jsonWriter.WriteEndObject();
      jsonWriter.Flush();
      
      fs.File.WriteAllBytes(
         pwdJsonFilePath,
         content.ToArray());
   }
}