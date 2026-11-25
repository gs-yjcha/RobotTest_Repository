using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Tablet.App.Settings;
using Tablet.Mock;

namespace Tablet.App.ViewModels;

public enum ConnectionCheckState
{
    Waiting,
    Checking,
    Passed,
    Failed,
    Skipped,
}

public sealed partial class ConnectionCheckStep(string title) : ObservableObject
{
    public string Title { get; } = title;

    [ObservableProperty] private ConnectionCheckState _state;
    [ObservableProperty] private string _detail = "대기";

    internal void SetResult(ConnectionCheckState state, string detail)
    {
        State = state;
        Detail = detail;
    }
}

public sealed partial class SettingsViewModel : ObservableObject
{
    private static readonly HashSet<string> CertificateExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".crt", ".pem", ".cer",
    };

    private readonly ConnectionSettingsStore _store;
    private byte[]? _protectedApiKey;
    private bool _keyAvailable;

    internal SettingsViewModel(ConnectionSettingsStore store, bool isMock)
    {
        _store = store;
        IsMock = isMock;
        Restore();
    }

    public bool IsMock { get; }
    public string SaveButtonText => IsMock ? "저장 후 연결 시험" : "설정 저장";
    public string TestScope => IsMock
        ? "Mock 시험입니다. 실제 MobED에는 연결하지 않습니다."
        : "실기 연결 시험은 T4에서 지원합니다.";

    public IReadOnlyList<ConnectionCheckStep> Steps { get; } =
    [
        new("①  접속 주소 응답"),
        new("②  TLS 인증서 확인"),
        new("③  인증 확인"),
        new("④  서버 API 판본"),
        new("⑤  주행 가능 상태"),
        new("⑥  맵·위치 상태"),
    ];

    [ObservableProperty] private string _endpoint = "";
    [ObservableProperty] private string _certificateFolder = "";
    [ObservableProperty] private IReadOnlyList<CertificateOption> _certificates = [];
    [ObservableProperty] private CertificateOption? _selectedCertificate;
    [ObservableProperty] private string _certificateMessage = "폴더를 지정하면 .crt, .pem, .cer 파일을 표시합니다.";
    [ObservableProperty] private string _keyPreview = "미등록";
    [ObservableProperty] private string _resultMessage = "시험 대기";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    private bool _isBusy;

    public bool CanEdit => !IsBusy;

    internal void SetCertificateFolder(string folder)
    {
        if (CertificateFolder == folder)
            RefreshCertificates(SelectedCertificate?.FileName);
        else
            CertificateFolder = folder;
    }

    partial void OnCertificateFolderChanged(string value) => RefreshCertificates();

    private void Restore()
    {
        try
        {
            var saved = _store.Load();
            if (saved is null)
                return;

            Endpoint = saved.Endpoint ?? "";
            CertificateFolder = saved.CertificateFolder ?? "";
            RestoreCertificateSelection(saved.CertificateFile);
            _protectedApiKey = saved.ProtectedApiKey;
            if (_protectedApiKey is not null)
            {
                KeyPreview = ConnectionSettingsStore.GetKeyPreview(_protectedApiKey);
                _keyAvailable = true;
            }

            ResultMessage = "저장된 설정을 불러왔습니다.";
        }
        catch (CryptographicException)
        {
            KeyPreview = "다시 등록 필요";
            ResultMessage = "이 Windows 계정에서 저장된 API Key를 열 수 없습니다. 키를 다시 입력해 주세요.";
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
        {
            ResultMessage = "저장된 설정을 읽을 수 없습니다. 설정 파일과 폴더 권한을 확인해 주세요.";
        }
    }

    private void RefreshCertificates(string? selectedFile = null)
    {
        Certificates = [];
        SelectedCertificate = null;
        var folder = CertificateFolder.Trim();
        CertificateMessage = "폴더를 지정하면 .crt, .pem, .cer 파일을 표시합니다.";
        if (folder.Length == 0)
            return;

        try
        {
            Certificates = Directory.EnumerateFiles(folder)
                .Where(file => CertificateExtensions.Contains(Path.GetExtension(file)))
                .Select(CertificateOption.Read)
                .OrderBy(file => file.FileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            CertificateMessage = Certificates.Count == 0
                ? "폴더에서 인증서 파일을 찾지 못했습니다."
                : $"인증서 {Certificates.Count}개 · 사용할 파일을 선택해 주세요.";
            RestoreCertificateSelection(selectedFile);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or ArgumentException or NotSupportedException)
        {
            CertificateMessage = "폴더를 열 수 없습니다. 경로와 접근 권한을 확인해 주세요.";
        }
    }

    private void RestoreCertificateSelection(string? fileName)
    {
        if (fileName is null)
            return;

        SelectedCertificate = Certificates.FirstOrDefault(file =>
            StringComparer.OrdinalIgnoreCase.Equals(file.FileName, fileName));
        if (SelectedCertificate is null)
            CertificateMessage = "저장된 인증서 파일을 찾을 수 없습니다. 다시 선택해 주세요.";
    }

    internal async Task<bool> SaveAndTestAsync(string enteredKey)
    {
        if (IsBusy)
            return false;

        IsBusy = true;
        try
        {
            foreach (var step in Steps)
                step.SetResult(ConnectionCheckState.Waiting, "대기");

            var endpoint = Endpoint.Trim();
            var folder = CertificateFolder.Trim();
            var file = SelectedCertificate?.FileName;
            var key = string.IsNullOrWhiteSpace(enteredKey) ? null : enteredKey.Trim();
            try
            {
                var protectedKey = key is null ? _protectedApiKey : ConnectionSettingsStore.ProtectKey(key);
                _store.Save(new SavedConnectionSettings(endpoint, folder, file, protectedKey));
                _protectedApiKey = protectedKey;
                if (key is not null)
                {
                    _keyAvailable = true;
                    KeyPreview = ConnectionSettingsStore.MaskKey(key);
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or CryptographicException)
            {
                ResultMessage = "설정을 저장하지 못했습니다. Windows 계정과 설정 폴더 권한을 확인해 주세요.";
                return false;
            }

            ResultMessage = "설정을 저장했습니다.";
            if (IsMock)
                await RunMockTestAsync(endpoint, folder, file);
            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunMockTestAsync(string endpoint, string folder, string? file)
    {
        ResultMessage = "설정 저장 완료 · Mock 시험 중";
        if (!IsValidEndpoint(endpoint))
        {
            Fail(0, "접속 주소와 포트를 확인해 주세요. 예: robot.example:443");
            return;
        }

        Steps[0].SetResult(ConnectionCheckState.Checking, "Mock 응답 확인 중");
        await using var mock = new MockRobotClient();
        await mock.ConnectAsync();
        Steps[0].SetResult(ConnectionCheckState.Passed, "주소 형식 및 Mock 응답 확인");

        var certificateError = file is null
            ? "인증서 파일을 선택해 주세요."
            : CertificateOption.Read(Path.Combine(folder, file)).CheckValidity();
        if (certificateError is not null)
        {
            Fail(1, certificateError);
            return;
        }

        Steps[1].SetResult(ConnectionCheckState.Passed, "로컬 인증서 유효 기간 확인 · Mock TLS 통과");
        if (!_keyAvailable)
        {
            Fail(2, "API Key를 입력해 주세요.");
            return;
        }

        Steps[2].SetResult(ConnectionCheckState.Passed, "등록된 API Key 확인 · Mock 인증 통과");
        Steps[3].SetResult(ConnectionCheckState.Passed, "Mock 판본 응답");
        Steps[4].SetResult(ConnectionCheckState.Passed, "Mock 주행 가능 응답");
        Steps[5].SetResult(ConnectionCheckState.Passed, "Mock 맵·위치 응답");
        ResultMessage = "설정 저장 완료 · Mock 시험 6단계 통과";
    }

    private void Fail(int index, string detail)
    {
        Steps[index].SetResult(ConnectionCheckState.Failed, detail);
        foreach (var step in Steps.Skip(index + 1))
            step.SetResult(ConnectionCheckState.Skipped, "건너뜀");
        ResultMessage = $"설정 저장 완료 · Mock 시험 {index + 1}단계 실패";
    }

    private static bool IsValidEndpoint(string endpoint)
    {
        var address = endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? endpoint
            : "https://" + endpoint;

        var portStart = address.LastIndexOf(':') + 1;
        return int.TryParse(address.AsSpan(portStart), NumberStyles.None, CultureInfo.InvariantCulture, out var port)
            && port is >= 1 and <= 65535
            && Uri.TryCreate(address, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.HostNameType is UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6
            && uri.UserInfo.Length == 0
            && uri.PathAndQuery == "/"
            && uri.Fragment.Length == 0;
    }
}
