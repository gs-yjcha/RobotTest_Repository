namespace Tablet.Core;

public enum RobotConnectionStage
{
    Address,
    Tls,
    Authentication,
    ServerVersion,
    DrivingMode,
    MapAndPose,
}

public sealed record RobotConnectionCheck(RobotConnectionStage Stage, bool Succeeded, string Detail);

public sealed class RobotConnectionSettings(Uri? endpoint, string? certificatePath, string? apiKey)
{
    public Uri? Endpoint { get; } = endpoint;
    public string? CertificatePath { get; } = certificatePath;
    public string? ApiKey { get; } = apiKey;
}

public interface IRobotConnectionDiagnostics
{
    Task<bool> CheckAsync(
        RobotConnectionSettings settings,
        IProgress<RobotConnectionCheck> progress,
        CancellationToken cancellationToken = default);
}
