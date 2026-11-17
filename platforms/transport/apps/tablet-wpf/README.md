# tablet-wpf — Windows 태블릿 앱 (WPF)

기획안: [`docs/기획안/RobotTest_기획안_261117_v3.md`](../../docs/기획안/RobotTest_기획안_261117_v3.md) · 화면 설계: `UI 시안/` v9

| 프로젝트 | 역할 |
|---|---|
| `Tablet.App` | WPF 화면 (MVVM, CommunityToolkit.Mvvm) |
| `Tablet.Core` | 모델, `IRobotClient`, (T2~) Job 상태 머신 |
| `Tablet.Mock` | Mock Robot — 실기 없이 화면 시험 |
| `Tablet.Tests` | xUnit 단위 시험 |
| `../../local/Mobed.Client` | MobED 직접 연결 구현. **git 제외** (있을 때만 빌드에 포함) |

## 빌드·실행

```
dotnet build RobotTest.Tablet.sln
dotnet test RobotTest.Tablet.sln
dotnet run --project Tablet.App -- --windowed   # 개발용 창 모드
dotnet run --project Tablet.App                 # 전체화면 (F11로 전환)
```

- 요구: .NET 10 SDK, Windows.
- `local/Mobed.Client/Mobed.Client.csproj`가 있으면 `MOBED_DIRECT`로 빌드되어 실기에 연결한다. 없으면 Mock으로 실행된다. 실기 빌드에서도 `--mock`을 주면 Mock으로 실행한다.
- `Mobed.Client`는 `Tablet.Core`를 참조하고 `Mobed.Client.MobedClientFactory.Create()`로 `IRobotClient`를 돌려준다.

## 터치

- 창 전체에 `Stylus.IsPressAndHoldEnabled="False"` — 길게 누르기가 우클릭으로 바뀌지 않게 한다.
- 누르는 동안 이동 버튼은 `TouchUp`·`LostTouchCapture`·창 `Deactivated`에서 반드시 정지한다 (T7).
