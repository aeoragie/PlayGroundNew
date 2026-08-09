# 빌드 · 퍼블리시 · 배포 — 무엇이 서버로 가는가

> "빌드하면 나오는 Debug/Release 폴더를 통째로 올리는 건가?"에 대한 답이다.
> 파이프라인 실물은 `.github/workflows/deploy.yml` + `Deploy/deploy-app.sh`,
> 서버 구축 절차는 `Deploy/README.md`.

## 배포 대상은 build 출력이 아니라 publish 산출물이다

| | `dotnet build` 출력 (`Binary/…`) | `dotnet publish` 산출물 |
|---|---|---|
| 목적 | 개발 중간 결과물 — 로컬 실행·테스트용 | **실행에 필요한 것만 모은 완성 접시** |
| 내용 | 프로젝트별 폴더에 흩어짐. 참조 DLL·pdb·테스트 어셈블리까지 | Server DLL + 의존성 전부 + `wwwroot`(Blazor WASM 클라이언트 — br/gz 압축본 포함) + appsettings |
| 상태 | 폴더 여러 개, 그대로 옮기면 안 돌아감 | 폴더 하나. 서버에서 `dotnet PlayGround.Server.dll`이면 끝 |

`publish`는 build를 포함한다 — "빌드하고 나서 실행에 필요한 것만 골라 한 폴더에 담는" 명령이다.

## 왜 반드시 Release인가 — 이 프로젝트에선 보안 경계다

`#if DEBUG` 코드(DebugClock 시간 이동 경로)는 Debug 빌드에 **물리적으로 존재**한다.
"RELEASE에는 이 경로가 없다"(CLAUDE.md 시간 이동 절)는 보장은 `-c Release`로 publish할 때만
성립한다. `Debug.Assert`도 마찬가지로 Debug에만 산다. 성능 이전에 경계의 문제다.

## 배포 사이클 — 로컬 빌드는 배포와 무관하다

```
코드 수정 → git push (main) → GitHub Actions "Deploy" 실행
             └ 러너가 저장소를 새로 받아 publish → zip → scp → 서버의 playground-deploy가 교체+재시작
```

- 배포되는 것은 항상 **push된 코드**다. 내 PC의 빌드 산출물은 어떤 경로로도 서버에 가지 않는다 —
  "내 PC에서는 됐는데"가 원천 차단된다.
- 파이프라인에 시크릿 가드가 있다: publish 산출물에 `appsettings.Local.json`이 섞이면 배포가
  중단된다. 시크릿은 서버의 `/etc/playground/playground.env`로만 들어간다.
- 서버의 `playground-deploy`는 이전 버전을 백업해 두고 교체한다 — 롤백은 스크립트 한 번이다.

## 도구 지형 — 셋은 말이 통하는 상대가 다르다

| 도구 | 프로토콜/대상 | 하는 일 | 출처 |
|---|---|---|---|
| `aws` | AWS API (HTTPS) | 인스턴스 조회, S3 업로드, DNS 관리 | AWS CLI 설치 |
| `ssh` / `scp` | SSH — 서버라면 어디든 | 원격 셸, 파일 전송 | **Windows 10 1809+ 기본 탑재** (System32\OpenSSH). Git for Windows도 한 벌 더 깔아 둔다 |
| `sqlcmd` | TDS — SQL Server | 쿼리·스크립트 실행 | SQL Server 도구 설치 |

`scp`가 AWS CLI 덕에 도는 게 아니다 — AWS CLI는 API를 부를 뿐, 서버 안으로 들어가는 길(SSH)과는
별개다. 반대로 ssh·scp는 AWS를 전혀 모른다. 서버 IP와 키만 있으면 어느 클라우드든 같다.

## DB 스키마 배포는 앱 배포와 별도 트랙이다

앱은 Actions가 자동으로 나르지만, 스키마(테이블·프로시저)는 로컬에서 `sqlcmd`로 직접 적용한다
(`Source/Database/README.md`의 루프 — 원격은 `-S <EIP>,47821 -U ... -C`만 다르다).
적용 검증은 손이 아니라 **DB 계약 테스트**로 한다:
`PLAYGROUND_TEST_*_CONNSTR` 환경변수를 운영 DB로 잡고 `dotnet test Tests/Tests.Infrastructure`.

## 수동 배포 시 zip 함정 (실측 2026-08-09)

PowerShell `Compress-Archive`는 zip 안에 경로 구분자를 `\`로 쓴다(규격 위반). Linux `unzip`이
폴더 구조를 못 살리고 `wwwroot\index.html` 같은 납작한 파일로 풀어 앱이 깨진다.
**Windows 기본 탑재 `tar`로 만든다** — 규격대로 `/`를 쓴다:

```powershell
tar -a -cf C:\Temp\playground-app.zip -C C:\Workspace\Publish\PlayGround .
```

GitHub Actions 경로는 Linux `zip`으로 만들므로 이 함정이 없다 — 수동 배포에서만 밟는다.

## 호스티드 배포와 지문(fingerprint) 자리표시자 (실측 2026-08-09)

.NET 10 Blazor의 `#[.{fingerprint}]` 자리표시자(+ `OverrideHtmlAssetPlaceholders`)는
**Client를 직접 publish하는 standalone 배포**에서만 치환된다. 우리처럼 **Server를 publish하는
호스티드 배포**에서는 치환 경로가 없어(서버 정적 자산 매니페스트에 index.html 항목 자체가 없다)
자리표시자가 브라우저까지 날것으로 나가 부팅이 Loading에서 멈춘다 — 개발 모드(dotnet run)에서는
정상이라 배포 전엔 안 보이는 함정. `WasmFingerprintAssets=false` + 지문 없는 파일명 참조로 해결.
대가: 프레임워크 자산 캐시 버스팅 없음(배포 후 브라우저 강력 새로고침이 필요할 수 있다).
