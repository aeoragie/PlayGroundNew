# 배포 — AWS EC2 단일 인스턴스

> 대상: **playgroundsport.com** · 서울 리전(ap-northeast-2)
> 구성: EC2 한 대에 **앱 + SQL Server Express + Redis**를 함께 올린다(초기 비용 최소화).

## 이 폴더에서 무엇을 볼 것인가

| 무엇을 하려는가 | 어디를 볼 것인가 |
|---|---|
| **처음 서버를 세운다** (1회) | `AwsSetup.md` — 콘솔에서 무엇을 누르는지 |
| 우리 배포 구성·규칙을 알고 싶다 | **이 문서** |
| **뭔가 잘못됐다** | `Runbook.md` — 증상별 대응 |
| 설정값·시크릿이 어떻게 주입되나 | `Docs/Architecture/DeploymentAndConfiguration.md` |
| 배포까지 뭐가 남았나 | `Docs/Development/ReleasePlan.md` |

서버에 올라가는 실행물:

| 파일 | 어디로 |
|---|---|
| `ec2-setup.sh` | EC2 **user-data**에 붙여 넣는다 (시크릿 없음) |
| `deploy-app.sh` | `/usr/local/bin/playground-deploy` |
| `backup-database.sh` | `/usr/local/bin/playground-backup` |
| `playground.service` | `/etc/systemd/system/playground.service` |
| `playground.conf` | `/etc/nginx/sites-available/playground` |

> 실행물이 소문자·하이픈인 이유: **리눅스에서 실행·설치되는 것**이라 그 생태계 관례를 따르고,
> 설치 이름과도 같아진다. 문서(`.md`)는 우리 소유라 PascalCase.
> 규칙 전체는 CLAUDE.md "파일 네이밍".

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
| **47821** | **내 IP만** | SQL Server. 1433은 쓰지 않는다 |

> **SQL은 포트가 아니라 "내 IP"가 지켜 준다.** 47821을 쓰는 건 1433만 노리는 자동 봇을
> 피하려는 것이지 은폐가 아니다 — 전 포트 스캔은 몇 분이면 끝난다.
> **소스를 `0.0.0.0/0`으로 바꾸면 안 된다**: SQL Server on Linux에는 계정 잠금이 없어
> 비밀번호 대입이 무제한이다. 같은 이유로 `sa`는 잠그고 `pgadmin`을 쓴다(`AwsSetup.md` 7절).

## 순서

### 1~2. AWS 콘솔 작업 — **`Deploy/AwsSetup.md`**

키 페어 · 보안 그룹 · 인스턴스 · Elastic IP · SQL Server 초기 설정 · S3/IAM · Route 53.
콘솔에서 무엇을 누르는지와 **자주 막히는 지점**을 그 문서에 정리했다.

### 3. 스키마 배포

로컬에서 평소처럼 `sqlcmd`로 적용한다 — 서버 지정만 `<Elastic IP>,47821`로 바뀐다.
순서는 **Tables → Indexes → Migrations → Procedures → Seeds**
(`Docs/Development/LocalVerification.md`와 동일).

```powershell
sqlcmd -S <Elastic IP>,47821 -U pgadmin -P '<비번>' -C -d PlayGround_Soccer -b -f 65001 `
       -i Source\Database\Soccer\Tables\SoccerTeams.sql
```

반영 후 **DB 계약 테스트로 확인**한다 — 누락을 손으로 찾지 않는다:

```powershell
$env:PLAYGROUND_TEST_ACCOUNT_CONNSTR = "Server=<Elastic IP>,47821;Database=PlayGround_Account;User Id=pgadmin;Password=...;Encrypt=True;TrustServerCertificate=True"
$env:PLAYGROUND_TEST_SOCCER_CONNSTR  = "Server=<Elastic IP>,47821;Database=PlayGround_Soccer;User Id=pgadmin;Password=...;Encrypt=True;TrustServerCertificate=True"
dotnet test Tests/Tests.Infrastructure/Tests.Infrastructure.csproj
```

> **인터넷을 건너가므로 `Encrypt=True`를 뺀 채로 붙이지 않는다.** 빼면 쿼리와 결과가
> 평문으로 흐른다. 서버 인증서가 자체 서명이라 `TrustServerCertificate=True`가 함께 필요하다
> (중간자 공격을 막지는 못하지만 평문보다는 낫다). `sqlcmd`의 `-C`가 같은 뜻이다.

### 4. 서버측 설치 + 첫 배포

**4-1. 스크립트·설정 파일을 서버로 올린다** (로컬 PowerShell):

```powershell
$key = "C:\Workspace\Keys\playground-prod.pem"
scp -i $key Deploy/deploy-app.sh Deploy/backup-database.sh `
    Deploy/playground.service Deploy/playground.conf ubuntu@<EIP>:/tmp/
```

**4-2. 서버에서 제자리에 놓는다** (SSH 접속 후):

```bash
sudo install -m 750 /tmp/deploy-app.sh      /usr/local/bin/playground-deploy
sudo install -m 750 /tmp/backup-database.sh /usr/local/bin/playground-backup
sudo install -m 644 /tmp/playground.service /etc/systemd/system/playground.service
sudo install -m 644 /tmp/playground.conf    /etc/nginx/sites-available/playground

sudo ln -sf /etc/nginx/sites-available/playground /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl reload nginx
```

**4-3. 환경변수 파일** — 시크릿이라 파일로만 둔다:

```bash
sudo mkdir -p /etc/playground
sudo nano /etc/playground/playground.env
```

내용 (`__`가 계층 구분자 — `Jwt__Key` = `Jwt:Key`):

```
Jwt__Key=<32자 이상 임의 문자열>
Jwt__Issuer=playground
Jwt__Audience=playground-client
OAuth__Google__ClientId=...
OAuth__Google__ClientSecret=...
OAuth__Kakao__ClientId=...
OAuth__Naver__ClientId=...
OAuth__Naver__ClientSecret=...
OAuth__Apple__ClientId=...
DatabaseConfiguration__Databases__Account__ConnectionString=Server=localhost,47821;Database=PlayGround_Account;User Id=pgadmin;Password=<비번>;TrustServerCertificate=True
DatabaseConfiguration__Databases__Soccer__ConnectionString=Server=localhost,47821;Database=PlayGround_Soccer;User Id=pgadmin;Password=<비번>;TrustServerCertificate=True
RedisConfig__Connections__0__ConnectionString=localhost:6379
```

> **`localhost` 뒤에도 `,47821`이 필요하다.** SQL Server는 지정한 포트 하나만 듣기 때문에
> 1433은 로컬에서도 닫혀 있다. 이걸 빠뜨리면 앱이 "DB 연결 실패"로 기동에 실패한다.

```bash
sudo chmod 600 /etc/playground/playground.env
sudo systemctl daemon-reload
sudo systemctl enable playground
```

> **OAuth 리다이렉트 URI는 `appsettings.json`이 아니라 각 소셜 콘솔에 등록한다**(6단계).
> 위 파일에는 ClientId/Secret만 넣는다.

**4-4. 백업 설정**:

```bash
sudo tee /etc/playground/backup.env > /dev/null <<'EOF'
DB_USER=pgadmin
DB_PASSWORD=<비번>
S3_BUCKET=<버킷이름>
EOF
sudo chmod 600 /etc/playground/backup.env
sudo apt-get install -y awscli

sudo crontab -e
# 아래 한 줄 추가 (새벽 4시)
# 0 4 * * * /usr/local/bin/playground-backup >> /var/log/playground/backup.log 2>&1
```

**한 번 수동으로 돌려 S3에 올라가는지 확인한다** — 백업은 "돌아간다고 믿는 것"이 가장 위험하다:

```bash
sudo /usr/local/bin/playground-backup
aws s3 ls s3://<버킷이름>/db/ --recursive
```

**4-5. 첫 배포** — GitHub → Settings → Environments → `Production`에 시크릿 4개 등록:

| 시크릿 | 값 |
|---|---|
| `DEPLOY_HOST` | `<EIP>` |
| `DEPLOY_USER` | `ubuntu` |
| `DEPLOY_SSH_KEY` | `playground-prod.pem` **파일 내용 전체** |
| `DEPLOY_KNOWN_HOSTS` | 아래 명령의 출력 |

```powershell
ssh-keyscan <EIP>
```

등록 후 GitHub **Actions → Deploy → Run workflow**로 수동 실행한다.

> **첫 배포는 `deploy.yml`의 마지막 스모크 체크(공개 URL)가 실패한다** — 아직 HTTPS가 없기 때문이다.
> 앱 자체는 올라갔는지 `curl http://<EIP>/api/soccer/landing/contents`로 확인하고,
> 5단계(HTTPS)를 마친 뒤 다시 실행하면 통과한다.

### 5. HTTPS 발급

A 레코드는 콘솔 단계(`AwsSetup.md` 9)에서 이미 걸었다. 전파를 확인한 뒤:

```bash
sudo certbot --nginx -d playgroundsport.com -d www.playgroundsport.com
```

certbot이 443 서버 블록과 80→443 리다이렉트를 `playground.conf`에 자동으로 추가한다.
갱신은 systemd 타이머가 알아서 한다(`systemctl list-timers | grep certbot`으로 확인).

> **도메인·HTTPS 전에 `http://<Elastic IP>`로 앱이 뜨는지 먼저 확인한다.**
> HTTPS·OAuth 문제와 앱 자체 문제가 섞이면 원인을 가리기 어려워진다.

### 6. OAuth 리다이렉트 URI 등록

도메인이 붙은 뒤 **4곳 모두** 등록해야 소셜 로그인이 동작한다:

```
https://playgroundsport.com/api/auth/social/google/callback
https://playgroundsport.com/api/auth/social/kakao/callback
https://playgroundsport.com/api/auth/social/naver/callback
https://playgroundsport.com/api/auth/social/apple/callback
```

## SSMS로 붙기

- 서버 이름: `<Elastic IP>,47821` (쉼표다 — 콜론이 아니다)
- 인증: **SQL Server 인증** (Linux는 Windows 인증을 쓰지 않는다)
- 로그인: `pgadmin` (`sa`는 잠가 뒀다 — `AwsSetup.md` 7절)
- **옵션 → 연결 속성 → "서버 인증서 신뢰" 체크** (자체 서명 인증서라 안 하면 거부된다)

> **붙던 게 갑자기 안 붙으면 십중팔구 내 IP가 바뀐 것이다.**
> 보안 그룹 → 인바운드 규칙 편집 → 47821 규칙의 소스를 **내 IP로 다시 선택**.

### 대안 — SSH 터널 (포트를 열지 않는 방법)

카페·출장 등 IP가 자주 바뀌면 보안 그룹의 47821 규칙을 **지우고** 터널을 쓴다.
열어 두는 문이 22 하나로 줄어든다.

```powershell
ssh -i <키.pem> -L 14330:localhost:47821 ubuntu@<Elastic IP> -N
```

이때 SSMS 서버 이름은 `127.0.0.1,14330`이다 (`14330`은 내 PC에서만 쓰는 번호라 아무 값이나 된다).

## 환경은 1단 (Production)

투자 유치용 서비스 구축 단계라 **서버 한 대 · 파이프라인 1단**으로 간다.
정식 스펙 재구성 시 **Local(개발자) · Dev(공동 테스트) · Staging(정식 QA) · Production(라이브)**
4단으로 확장할 예정이고, 설정 레이어(`appsettings.{Environment}.json`)는 이미 그 구조다.

> **서버가 한 대여도 `ASPNETCORE_ENVIRONMENT`는 `Production`이다.**
> 환경 "개수"와 환경 "이름"은 별개 결정이다. `Development`로 두면
> OpenAPI 노출 · WASM 디버깅 프록시 · **상세 예외 페이지**가 공개 URL에 켜지고 HSTS가 빠진다.
> 투자자에게 스택 트레이스가 한 번 뜨면 그걸로 인상이 결정된다.

## 배포 후 확인 (첫 배포 직후 한 번)

- [ ] `https://playgroundsport.com` 접속 — 랜딩이 뜨는가
- [ ] **상세 예외 페이지가 안 뜨는가** — 없는 경로(`/api/nope`)로 404 확인.
      스택 트레이스가 보이면 `ASPNETCORE_ENVIRONMENT`가 `Production`이 아니다
- [ ] 소셜 로그인 4종 — 리다이렉트 URI 등록이 빠지면 여기서 드러난다
- [ ] **로그아웃 후 같은 토큰으로 API 호출 → 401** (Redis 무효화가 서버에서 도는지)
- [ ] OG 카드 한글 — `https://playgroundsport.com/og/brand.png` 이미지에 글자가 깨지지 않는가
- [ ] 이미지 업로드 → 표시 (현재는 로컬 디스크. S3 전환은 H2)
- [ ] **롤백 1회 연습** — 이전 커밋으로 워크플로를 재실행해 되돌아가는지 본다.
      되돌릴 수 있다는 걸 사고 전에 확인해 둔다

## 알려진 한계 (첫 배포 시점)

- **이메일 발송이 로그로만 나간다**(`LogOnlyEmailSender`) — 데이터 내려받기 완료 알림의
  이메일 채널이 비어 있다. 알림 센터로는 정상 도착한다. D3 결정 후 어댑터 교체(H1).
- **이미지가 서버 로컬 디스크에 저장된다** — 인스턴스를 재생성하면 사라진다.
  S3 전환(H2) 전까지는 업로드 이미지도 백업 대상으로 볼 것.
- **export 큐가 인메모리** — 서버 재시작 시 진행 중이던 내려받기 작업이 유실된다(H3).
- **환경 이름 분기** — `Program.cs`가 `IsDevelopment()`로 OpenAPI·디버깅을 켠다.
  1단에서는 문제없지만, 4단으로 갈 때 `Dev`는 이 조건에 안 걸려 혼란을 부른다.
  그때 설정 플래그로 바꾸는 편이 낫다.
