# 배포 — AWS EC2 단일 인스턴스

> 대상: **playgroundsport.com** · 서울 리전(ap-northeast-2)
> 구성: EC2 한 대에 **앱 + SQL Server Express + Redis**를 함께 올린다(초기 비용 최소화).
> 결정 근거와 단계는 `Docs/Development/ReleasePlan.md`.

## 확정된 선택

| 항목 | 값 | 이유 |
|---|---|---|
| 인스턴스 | **t3.medium** (2vCPU/4GB) | Express는 버퍼 풀이 ~1.4GB로 묶여 있어 4GB로 충분. 부족하면 중지→타입변경으로 5분 내 확장 |
| OS | **Ubuntu 22.04 LTS** | SQL Server 2022가 **24.04용 저장소를 제공하지 않는다**(2025만 있음). 22.04는 가이드·트러블슈팅 자료가 압도적으로 많다 |
| DB | **SQL Server 2022 Express** | 무료. DB당 10GB — 우리 스키마는 바이너리를 담지 않아(파일은 URL만) 한참 여유 |
| 캐시 | **Redis (배포판 패키지)** | Linux에선 정품. 토큰 무효화용 |
| TLS | **Nginx + Let's Encrypt** | ALB는 월 $20 안팎이 추가된다. 이중화가 필요해지면 그때 |
| 고정 IP | **Elastic IP 필수** | 없으면 재시작 때 IP가 바뀌어 DNS가 끊긴다 |

> **Ubuntu Pro 무료 등록**(개인 5대까지)을 해 두면 22.04 보안 업데이트가 2032년까지 연장된다.
> `sudo pro attach <토큰>` — 표준 지원 종료(2027-04) 전에 해 두면 재설치가 필요 없다.

## 보안 그룹

| 포트 | 개방 | 비고 |
|---|---|---|
| 22 | **내 IP만** | SSH |
| 80 · 443 | 전체 | 웹. 80은 certbot 갱신에도 쓰인다 |
| 1433 | **닫음** | SSMS는 SSH 터널로 붙는다(아래) |

## 순서

### 1. 인스턴스 생성

- AMI: Ubuntu Server 22.04 LTS (x86_64)
- 타입: t3.medium · 디스크: gp3 **50GB**
- **user-data**에 `Ec2Setup.sh` 내용을 붙여 넣는다 — **시크릿이 없으므로 안전하다**
  (user-data는 인스턴스 메타데이터로 조회되므로 비밀번호를 넣으면 안 된다)
- 생성 후 **Elastic IP 할당·연결**

### 2. SQL Server 초기 설정 (SSH 접속 후, 대화형)

sa 비밀번호가 들어가므로 **user-data가 아니라 여기서** 한다.

```bash
sudo MSSQL_PID=Express /opt/mssql/bin/mssql-conf setup
# → Express 선택, sa 비밀번호 입력(8자 이상·복잡도 필요)

sudo /opt/mssql/bin/mssql-conf set memory.memorylimitmb 2048   # 앱·Redis 몫을 남긴다
sudo systemctl restart mssql-server
```

DB 생성은 `Source/Database/README.md`의 콜레이션 규칙을 그대로 따른다
(`COLLATE Latin1_General_100_CI_AS_SC_UTF8`).

### 3. 스키마 배포

로컬에서 SSH 터널을 열고 평소처럼 `sqlcmd`로 적용한다.
순서는 **Tables → Indexes → Migrations → Procedures → Seeds**
(`Docs/Development/LocalVerification.md`와 동일).

반영 후 **DB 계약 테스트로 확인**한다 — 누락을 손으로 찾지 않는다:

```powershell
$env:PLAYGROUND_TEST_ACCOUNT_CONNSTR = "Server=127.0.0.1,14330;Database=PlayGround_Account;User Id=sa;Password=...;TrustServerCertificate=True"
$env:PLAYGROUND_TEST_SOCCER_CONNSTR  = "Server=127.0.0.1,14330;Database=PlayGround_Soccer;User Id=sa;Password=...;TrustServerCertificate=True"
dotnet test Tests/Tests.Infrastructure/Tests.Infrastructure.csproj
```

### 4. 앱 배포

`DeployApp.sh` 참조. CI가 만든 publish 산출물을 올리고 서비스를 재시작한다.

### 5. 도메인 연결 (Route 53)

- A 레코드 `playgroundsport.com` → Elastic IP
- A 레코드 `www.playgroundsport.com` → Elastic IP
- 전파 확인 후 TLS 발급:

```bash
sudo certbot --nginx -d playgroundsport.com -d www.playgroundsport.com
```

### 6. OAuth 리다이렉트 URI 등록

도메인이 붙은 뒤 **4곳 모두** 등록해야 소셜 로그인이 동작한다:

```
https://playgroundsport.com/api/auth/social/google/callback
https://playgroundsport.com/api/auth/social/kakao/callback
https://playgroundsport.com/api/auth/social/naver/callback
https://playgroundsport.com/api/auth/social/apple/callback
```

## SSMS로 붙기 (1433을 열지 않고)

로컬에서 SSH 터널을 연다:

```powershell
ssh -i <키.pem> -L 14330:localhost:1433 ubuntu@<Elastic IP> -N
```

SSMS 접속 정보:

- 서버: `127.0.0.1,14330`
- 인증: **SQL Server 인증** (Linux는 Windows 인증을 쓰지 않는다)
- 로그인: `sa` (운영용 계정을 따로 만들고 sa는 잠그는 것이 원칙)

## 환경은 1단 (Production)

투자 유치용 서비스 구축 단계라 **서버 한 대 · 파이프라인 1단**으로 간다.
정식 스펙 재구성 시 **Local(개발자) · Dev(공동 테스트) · Staging(정식 QA) · Production(라이브)**
4단으로 확장할 예정이고, 설정 레이어(`appsettings.{Environment}.json`)는 이미 그 구조다.

> **서버가 한 대여도 `ASPNETCORE_ENVIRONMENT`는 `Production`이다.**
> 환경 "개수"와 환경 "이름"은 별개 결정이다. `Development`로 두면
> OpenAPI 노출 · WASM 디버깅 프록시 · **상세 예외 페이지**가 공개 URL에 켜지고 HSTS가 빠진다.
> 투자자에게 스택 트레이스가 한 번 뜨면 그걸로 인상이 결정된다.

## 아직 남은 것

- **백업 S3 버킷** 생성 + 인스턴스에 IAM 역할 부여 (`BackupDatabase.sh`가 전제한다)
- **GitHub Environment `Production` 시크릿** — `DEPLOY_HOST`(Elastic IP) · `DEPLOY_USER` ·
  `DEPLOY_SSH_KEY` · `DEPLOY_KNOWN_HOSTS`. Required reviewers도 함께 거는 것을 권한다.
- **서버 환경변수 파일** `/etc/playground/playground.env` (chmod 600) —
  `Jwt__Key` · OAuth 시크릿 4종 · DB 커넥션 2종 · `RedisConfig__Connections__0__ConnectionString`
- **환경 이름 분기 정리(권장)** — `Program.cs`가 `IsDevelopment()`로 OpenAPI·디버깅을 켠다.
  4단으로 갈 때 `Dev`는 이 조건에 안 걸려 혼란을 부르므로, 설정 플래그로 바꾸는 편이 낫다.
