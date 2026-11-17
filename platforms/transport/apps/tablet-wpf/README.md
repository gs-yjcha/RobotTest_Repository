# tablet-wpf — Windows 태블릿 앱 (WPF)

기획안: [`docs/기획안/RobotTest_기획안_261117_v3.md`](../../docs/기획안/RobotTest_기획안_261117_v3.md) · 화면 설계: `UI 시안/` v10 (2026-11-18 승인)

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

## v10 화면 구성

- 운행: 예시 지도, 목적지 선택, 주행 설정, 작업 지시·위치 보정·수동 주행 탭.
- 맵 편집: 목적지·영역·맵 이미지 탭. 알람·이력: 수신 대기 표시와 실행 중 앱 이벤트.
- 설정: MobED 직접 연결 정보·인증서 폴더/파일·API Key·연결 시험 6단계 배치.
- 목적지·주행 모드·속도 선택은 화면 상태만 바꾼다. 주행·보정·맵 저장·인증서 선택·설정 저장은 미연결로 비활성화했다.
- 위치 일치율·배터리·알람은 미수신으로 표시한다. Mock 연결·정지 요청은 실제 로봇 상태 확인을 뜻하지 않는다.
- 검증 실행은 `dotnet run --project Tablet.App -- --windowed --mock`을 사용한다.
