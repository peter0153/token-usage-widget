# 내 AI 한도 · Windows

Codex 5시간·주간 한도와 최근 일별 토큰을 보여주는 작은 Windows 위젯입니다. OpenAI 공식 앱이 아닌 개인 제작 무료 도구입니다.

## 설치와 사용

[최신 설치 파일](https://github.com/peter0153/token-usage-widget/releases/latest)에서 Windows ZIP을 받고 **압축을 모두 푼 다음 `설치.cmd`를 실행**하세요. 계정은 각자 공식 브라우저에서 연결합니다.

- 큰 위젯과 작업표시줄 미니바를 별도로 표시하고 숨길 수 있습니다.
- X는 큰 창만 숨깁니다. 완전 종료는 트레이 우클릭 → 앱 종료입니다.
- 미니바 왼쪽 가장자리를 드래그하거나 우클릭 → 미니바 크기로 너비를 바꿉니다.
- 작은 미니바는 %와 남은 시간, 넓은 미니바는 초기화 시각도 표시합니다.
- 첫 화면 아래에서 최근 **5일/10일** 막대그래프를 보고, 기록 탭에서 날짜별 정확한 값을 확인합니다.
- 서버 집계를 우선하며 없는 날짜는 이 PC 참고값으로 구분합니다. 다른 기기의 PC 기록은 포함하지 않습니다. 미집계를 0으로 표시하지 않습니다.
- 초기화권은 계정이 수량을 제공할 때만 표시합니다. 초기화권·사용량 링크로 공식 사용량 화면을 엽니다. 앱이 초기화권을 사용하지 않습니다. 브라우저 로그인은 별도입니다.
- 새 설치의 기본 테마는 다크입니다. 설정에서 테마와 자동 시작·업데이트 알림을 선택합니다.

현재 사용자 폴더에 설치하며 관리자 권한을 요구하지 않습니다. 공인 Windows 코드 서명은 적용하지 않아 PC 보안 정책에 따른 경고가 남을 수 있습니다. 내부 업데이트 검증 서명은 Windows 게시자 인증과 다른 기능입니다.

## 빌드

Windows x64의 .NET Framework C# 컴파일러와 PowerShell을 사용합니다.

```powershell
.\distribution-src\download-runtime.ps1
.\distribution-src\test-features.ps1
.\distribution-src\test-transactions.ps1
.\distribution-src\build.ps1
```

고정된 OpenAI Codex app-server 0.163.0의 해시를 확인하며 런타임 LICENSE/NOTICE를 함께 배포합니다. 대화나 모델 요청을 만들지 않습니다. Claude 연결은 아직 지원하지 않습니다.

## 유지보수자의 새 버전 배포

1. `distribution-src/version.txt`의 버전을 올리고 변경한 소스를 main에 올립니다.
2. GitHub Actions가 검사·빌드하여 공개 사전 배포 ZIP을 생성합니다. 이 단계에는 비밀 서명 키가 없습니다.
3. 제작자 PC로 그 ZIP을 내려받아 아래 명령으로 검증하고 서명합니다. 기존 비공개 키는 제작자 PC의 `.release-signing/update-key.xml`에 보관하며 저장소에 올리지 않습니다.

```powershell
.\distribution-src\sign-published.ps1 -Archive '다운로드한 TokenWidget-버전-win64.zip 경로'
```

4. 생성된 **공개용** `update.json`을 저장소의 `release/update.json`으로 올리고 커밋 메시지에 `[publish-manifest]`를 넣습니다.
5. Actions가 공개 키·해시·ZIP 구성·실행 파일 버전을 다시 확인한 뒤 서명 정보를 Release에 게시하고 정식 배포로 전환합니다.

앱은 최신 정식 Release를 확인해 알리고, 승인한 업데이트를 내려받아 검증·교체·재실행합니다. 새 버전 시작 실패는 이전 파일로 복원하고, 중간 종료는 바로가기로 다음 실행할 때 복구합니다. 설정과 계정은 실행 파일과 별도 보관합니다.

로그인 파일, 개인 설정·기록, 진단 로그, 비밀 키는 공개 저장소에 올리지 마세요. 소스는 MIT, 번들 Codex 런타임은 Apache-2.0입니다.
