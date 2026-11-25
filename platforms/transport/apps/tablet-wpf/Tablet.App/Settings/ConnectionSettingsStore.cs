using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tablet.App.Settings;

internal sealed record SavedConnectionSettings(
    string? Endpoint,
    string? CertificateFolder,
    string? CertificateFile,
    byte[]? ProtectedApiKey);

internal sealed class ConnectionSettingsStore(string path)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static ConnectionSettingsStore ForCurrentUser() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RobotTest",
        "settings.json"));

    public SavedConnectionSettings? Load()
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            return JsonSerializer.Deserialize<SavedConnectionSettings>(stream, JsonOptions)
                ?? throw new JsonException("Connection settings must be a JSON object.");
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    public void Save(SavedConnectionSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, settings, JsonOptions);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    public static byte[] ProtectKey(string apiKey)
    {
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        try
        {
            return ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    public static string GetKeyPreview(byte[] protectedKey) => MaskKey(ReadKey(protectedKey));

    public static string ReadKey(byte[] protectedKey)
    {
        var bytes = ProtectedData.Unprotect(protectedKey, optionalEntropy: null, DataProtectionScope.CurrentUser);
        try
        {
            return Encoding.UTF8.GetString(bytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    public static string MaskKey(string apiKey) =>
        apiKey.Length > 4 ? apiKey[..4] + " ••••••••" : "••••••••";
}
