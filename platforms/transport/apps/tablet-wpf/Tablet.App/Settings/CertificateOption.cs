using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Tablet.App.Settings;

public sealed record CertificateOption(
    string FileName,
    DateTime? ValidFrom,
    DateTime? ValidUntil)
{
    public bool IsExpired => ValidUntil is DateTime date && date < DateTime.Now;

    public string DisplayName => ValidUntil is DateTime date
        ? $"{FileName} · {(IsExpired ? "만료됨 " : "만료일 ")}{date:yyyy-MM-dd}"
        : $"{FileName} · 인증서 읽기 실패";

    internal string? CheckValidity()
    {
        if (ValidFrom is null || ValidUntil is null)
            return "인증서 파일을 읽을 수 없습니다.";
        if (IsExpired)
            return "만료된 인증서입니다. 새 인증서를 선택해 주세요.";
        if (ValidFrom > DateTime.Now)
            return "인증서의 유효 기간이 아직 시작되지 않았습니다.";
        return null;
    }

    internal static CertificateOption Read(string path)
    {
        var fileName = Path.GetFileName(path);
        try
        {
            using var certificate = X509CertificateLoader.LoadCertificateFromFile(path);
            return new(fileName, certificate.NotBefore, certificate.NotAfter);
        }
        catch (Exception error) when (error is CryptographicException or IOException or UnauthorizedAccessException)
        {
            return new(fileName, null, null);
        }
    }
}
