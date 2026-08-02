# AWS 콘솔 셋업 가이드

> 대상: **playgroundsport.com** · 서울 리전 · EC2 t3.medium 한 대
> 결정 근거는 `Deploy/README.md`. 이 문서는 **콘솔에서 실제로 무엇을 누르는지**만 다룬다.
>
> 콘솔 UI는 자주 바뀌므로 버튼 위치가 아니라 **이름·기능**으로 적었다.

## 시작 전 확인

**오른쪽 위 리전이 `아시아 태평양(서울) ap-northeast-2`인지 먼저 본다.**
AWS 자원은 리전마다 따로 존재해서, 다른 리전에 만들면 목록에 아무것도 안 보이고
"분명히 만들었는데 없다"로 30분을 쓴다. **가장 흔한 첫 실수다.**

### 비용 알림부터 걸어 두기 (5분)

t3는 CPU를 오래 쓰면 초과 크레딧 요금이 별도로 붙는다. 예상 밖 청구를 막는다.

1. 콘솔 → **Billing and Cost Management** → **Budgets** → 예산 생성
2. 템플릿 **월별 비용 예산** → 금액 예: `$70` → 이메일 입력
3. 실제 사용액이 예산의 85%에 도달하면 메일이 온다

---

## 1. 키 페어 만들기

SSH 접속에 쓸 열쇠다. **다운로드는 이때 한 번뿐이고 다시 받을 수 없다.**

1. **EC2** → 왼쪽 메뉴 **네트워크 및 보안** → **키 페어** → **키 페어 생성**
2. 이름: `playground-prod`
3. 키 페어 유형 **RSA** · 프라이빗 키 형식 **`.pem`**
4. 생성 → `playground-prod.pem`이 자동 다운로드된다

**받은 파일을 안전한 곳으로 옮긴다** (예: `C:\Workspace\Keys\playground-prod.pem`).
이 파일을 잃어버리면 서버에 못 들어간다. 저장소에 넣지 말 것.

### Windows에서 키 권한 고치기 (안 하면 접속이 막힌다)

Windows에서 받은 `.pem`은 권한이 너무 열려 있어 SSH가 **거부**한다
(`UNPROTECTED PRIVATE KEY FILE`). PowerShell에서 한 번만 실행한다:

```powershell
$key = "C:\Workspace\Keys\playground-prod.pem"
icacls $key /inheritance:r                    # 상속된 권한 제거
icacls $key /grant:r "$($env:USERNAME):(R)"   # 나만 읽기
```

---

## 2. 보안 그룹 만들기

방화벽이다.

1. **EC2** → **네트워크 및 보안** → **보안 그룹** → **보안 그룹 생성**
2. 이름 `playground-prod-sg` · 설명 `PlayGround production`
3. **인바운드 규칙** 4개 추가:

| 유형 | 포트 | 소스 | 설명 |
|---|---|---|---|
| SSH | 22 | **내 IP** | 관리 접속 |
| HTTP | 80 | Anywhere-IPv4 (`0.0.0.0/0`) | 웹 + certbot 갱신 |
| HTTPS | 443 | Anywhere-IPv4 (`0.0.0.0/0`) | 웹 |
| **사용자 지정 TCP** | **47821** | **내 IP** | SQL Server (SSMS·sqlcmd·스키마 배포) |

4. 아웃바운드는 기본값(전체 허용) 그대로 — 패키지 설치·S3 업로드에 필요하다

> **"내 IP"는 지금 이 순간의 IP다.** 인터넷 회선이 재접속되면 바뀌어서 SSH와 SQL이 막힌다.
> 그때는 보안 그룹에서 두 규칙의 소스를 **내 IP로 다시 선택**하면 된다. 당황할 일이 아니다.

### SQL 포트에 대해 — 무엇이 지켜 주고 무엇이 아닌지

`1433` 대신 `47821`을 쓰는 이유는 **1433만 노리는 자동 스캔 봇의 소음을 줄이는 것**이다.
그게 전부다. 포트를 바꿔도 숨겨지지 않는다 — 인터넷 전 대역 전 포트 스캔은 몇 분이면 끝나고,
SQL Server는 접속하면 TDS 핸드셰이크로 스스로를 밝힌다.

**실제로 지키는 것은 소스 = "내 IP"다.** 이 한 줄이 전부이므로:

- **소스를 `0.0.0.0/0`으로 바꾸지 않는다.** 그 순간 전 세계가 `sa` 비밀번호를 무제한으로
  때려 볼 수 있고, **SQL Server on Linux에는 계정 잠금이 기본으로 없다.**
- 규칙 설명에 `SQL Server`라고 쓰는 건 상관없다 — 콘솔은 우리만 본다.
- 포트 번호를 다른 값으로 하고 싶으면 **이 문서와 `Deploy/README.md`의 `47821`을 일괄 치환**한다.
  1433에서 유도되는 번호(`14330`·`1533` 등)는 피한다. `1434`는 **SQL Browser 포트라 쓰면 안 된다.**

> 카페·출장 등 IP가 자주 바뀌는 환경이면 이 규칙을 지우고 **SSH 터널**을 쓰는 편이 낫다
> (`Deploy/README.md` "SSMS로 붙기"에 방법을 남겨 뒀다). 터널은 22 하나만 쓰므로
> 열어 두는 문이 하나 줄어든다.

---

## 3. EC2 인스턴스 시작

1. **EC2** → **인스턴스** → **인스턴스 시작**
2. **이름**: `playground-prod`
3. **AMI**: 검색창에 `Ubuntu` → **Ubuntu Server 22.04 LTS (HVM), SSD Volume Type** · 아키텍처 **64비트(x86)**
   - **24.04가 아니라 22.04다.** SQL Server 2022가 24.04용 저장소를 제공하지 않는다.
4. **인스턴스 유형**: `t3.medium`
5. **키 페어**: 1단계에서 만든 `playground-prod`
6. **네트워크 설정** → **기존 보안 그룹 선택** → `playground-prod-sg`
7. **스토리지 구성**: **50 GiB · gp3**
   - **기본값이 8GiB라 반드시 바꿔야 한다.** SQL Server + 백업 임시 공간이 들어간다.
8. **고급 세부 정보**(맨 아래, 접혀 있음) → **사용자 데이터** 칸에
   `Deploy/ec2-setup.sh` **전체 내용을 붙여 넣는다**
   - 시크릿이 없어 안전하다(사용자 데이터는 인스턴스 메타데이터로 조회 가능)
9. **인스턴스 시작**

> **직접 쳐 보며 이해하고 싶다면** 8번을 건너뛰고 인스턴스를 만든 뒤
> **`ManualSetup.md`** 를 따라간다 — 스크립트를 한 덩어리씩 설명과 함께 실행한다.
> **최초 1회만 그렇게 하고, 이후에는 user-data를 쓴다** (손으로 한 설치는 재현되지 않는다).

### 설치가 끝날 때까지 기다린다

사용자 데이터 스크립트가 SQL Server·Redis·.NET·Nginx를 받는다. **5~10분 걸린다.**
인스턴스 상태가 "실행 중"이어도 스크립트는 아직 도는 중일 수 있다.

---

## 4. Elastic IP 할당·연결

**도메인을 붙이려면 IP가 고정이어야 한다.** 안 하면 인스턴스를 중지·시작할 때마다 IP가 바뀐다.

1. **EC2** → **네트워크 및 보안** → **탄력적 IP** → **탄력적 IP 주소 할당** → 할당
2. 만들어진 주소 선택 → **작업** → **탄력적 IP 주소 연결**
3. 인스턴스 `playground-prod` 선택 → 연결

> **연결하지 않고 방치하면 요금이 붙는다.** 쓰지 않는 탄력적 IP에는 시간당 과금이 있다.
> 인스턴스를 지울 때는 탄력적 IP도 **해제(release)** 해야 한다.

이 주소를 적어 둔다 — 앞으로 계속 쓴다. 아래에서는 `<EIP>`로 표기한다.

---

## 5. 첫 SSH 접속

PowerShell에서 (Windows 10 이상은 SSH가 기본 내장):

```powershell
ssh -i C:\Workspace\Keys\playground-prod.pem ubuntu@<EIP>
```

- 처음엔 `Are you sure you want to continue connecting?` → `yes`
- 사용자 이름은 **`ubuntu`** (Ubuntu AMI의 기본 계정)

**막히면:**

| 증상 | 원인·해결 |
|---|---|
| `UNPROTECTED PRIVATE KEY FILE` | 1단계의 `icacls` 명령을 실행하지 않았다 |
| 연결 시간 초과 | 보안 그룹 SSH 소스가 현재 내 IP가 아니다 → 규칙에서 "내 IP" 다시 선택 |
| `Permission denied (publickey)` | 사용자 이름이 틀렸다 (`ubuntu`가 맞다) 또는 키 파일이 다른 것 |

## 6. 셋업 완료 확인

접속했으면 사용자 데이터 스크립트가 끝났는지 본다:

```bash
sudo tail -30 /var/log/cloud-init-output.log
```

마지막에 `[setup] 완료`가 보이면 성공이다. 아직이면 몇 분 더 기다린다.

설치 상태를 개별 확인:

```bash
systemctl status mssql-server --no-pager   # SQL Server
systemctl status redis-server --no-pager   # Redis
systemctl status nginx --no-pager          # Nginx
dotnet --list-runtimes | grep AspNetCore   # .NET 런타임
timedatectl | grep "Time zone"             # UTC 여야 한다
```

`active (running)`이면 정상이다. **`q`를 눌러 빠져나온다.**

---

## 7. SQL Server 초기 설정

sa 비밀번호가 들어가므로 사용자 데이터가 아니라 여기서 한다.

```bash
sudo MSSQL_PID=Express /opt/mssql/bin/mssql-conf setup
```

- 에디션에서 **Express**를 고른다 (무료)
- sa 비밀번호: **8자 이상 + 대문자·소문자·숫자·기호** — 안 지키면 조용히 실패한다
- **이 비밀번호를 안전한 곳에 적어 둔다** (비밀번호 관리자)

메모리 상한과 **수신 포트**를 잡는다. 앱·Redis 몫을 남기고, 보안 그룹에 연 포트와 맞춘다:

```bash
sudo /opt/mssql/bin/mssql-conf set memory.memorylimitmb 2048
sudo /opt/mssql/bin/mssql-conf set network.tcpport 47821
sudo systemctl restart mssql-server
```

> **이후 `localhost` 접속도 포트를 붙여야 한다.** SQL Server는 지정한 포트 하나만 듣는다 —
> 1433은 더 이상 열려 있지 않다. 이 문서와 스크립트의 `sqlcmd`·커넥션 문자열이
> 전부 `localhost,47821` 형태인 이유다.

동작 확인:

```bash
sqlcmd -S localhost,47821 -U sa -P '<비밀번호>' -C -Q "SELECT @@VERSION"
```

### 관리 계정을 따로 만들고 `sa`를 잠근다

**포트를 여는 이상 `sa`를 그대로 두면 안 된다.** 공격자가 아는 유일한 사용자 이름이고,
SQL Server on Linux는 **로그인 실패 잠금이 기본으로 없어** 무제한 대입이 가능하다.
이름을 모르면 비밀번호 대입 자체가 시작되지 않는다.

```bash
sqlcmd -S localhost,47821 -U sa -P '<sa비밀번호>' -C -Q "
CREATE LOGIN pgadmin WITH PASSWORD = '<새 비밀번호>', CHECK_POLICY = ON;
ALTER SERVER ROLE sysadmin ADD MEMBER pgadmin;"
```

새 계정으로 붙는지 **먼저 확인**한다 (여기서 실패한 채 sa를 잠그면 들어갈 길이 없다):

```bash
sqlcmd -S localhost,47821 -U pgadmin -P '<새 비밀번호>' -C -Q "SELECT SUSER_NAME()"
```

`pgadmin`이 출력되면 sa를 잠근다:

```bash
sqlcmd -S localhost,47821 -U pgadmin -P '<새 비밀번호>' -C -Q "
ALTER LOGIN sa DISABLE;"
```

> **이후 모든 접속(앱·백업·SSMS)은 `pgadmin`을 쓴다.** sa 비밀번호도 버리지 말고
> 비밀번호 관리자에 남겨 둔다 — 잠금을 되돌려야 할 때가 있다
> (`ALTER LOGIN sa ENABLE;`, 로컬에서 `pgadmin`으로 실행).
>
> 앱 전용으로 권한을 더 좁힌 계정(`db_owner`만)을 두는 것이 원칙이지만,
> 지금은 계정 하나로 간다. 사용자 트래픽이 붙는 시점의 하드닝 항목이다.

### DB 두 개 만들기

콜레이션이 중요하다 — 우리 스키마 규칙(UTF-8)이다:

```bash
sqlcmd -S localhost,47821 -U pgadmin -P '<비밀번호>' -C -Q "
CREATE DATABASE PlayGround_Account COLLATE Latin1_General_100_CI_AS_SC_UTF8;
CREATE DATABASE PlayGround_Soccer  COLLATE Latin1_General_100_CI_AS_SC_UTF8;"
```

### 원격에서 붙는지 확인 (로컬 PowerShell)

```powershell
Test-NetConnection <Elastic IP> -Port 47821
```

`TcpTestSucceeded : True`면 보안 그룹·포트 설정이 맞다. 실패하면
**보안 그룹 소스가 현재 내 IP인지**부터 본다.

---

## 8. 백업용 S3 + IAM 역할

### 8-1. 버킷 만들기

1. **S3** → **버킷 만들기**
2. 이름: `playground-backup-<임의문자>` (버킷 이름은 전 세계에서 유일해야 한다)
3. 리전: **아시아 태평양(서울)**
4. **퍼블릭 액세스 차단: 모두 차단** (기본값 유지 — 백업은 절대 공개하면 안 된다)
5. 만들기

### 8-2. 인스턴스에 쓰기 권한 주기 (액세스 키를 서버에 두지 않는다)

1. **IAM** → **역할** → **역할 생성**
2. 신뢰할 수 있는 개체: **AWS 서비스** → **EC2**
3. 권한: 일단 건너뛰고 생성 → 이름 `playground-ec2-role`
4. 만든 역할 → **권한 추가** → **인라인 정책 생성** → JSON 탭에 붙여 넣기
   (`<버킷이름>`을 실제 이름으로 바꾼다):

```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Action": ["s3:PutObject"],
    "Resource": "arn:aws:s3:::<버킷이름>/*"
  }]
}
```

5. **EC2** → 인스턴스 선택 → **작업** → **보안** → **IAM 역할 수정** → `playground-ec2-role` 적용

---

## 9. Route 53 — 도메인 연결

1. **Route 53** → **호스팅 영역** → `playgroundsport.com`
2. **레코드 생성** 2개:

| 레코드 이름 | 유형 | 값 |
|---|---|---|
| (비움 = 루트) | A | `<EIP>` |
| `www` | A | `<EIP>` |

3. 전파 확인 (로컬 PowerShell):

```powershell
nslookup playgroundsport.com
```

`<EIP>`가 나오면 다음 단계로 간다. 몇 분 걸릴 수 있다.

---

## 다음 — 서버 안에서 할 일

여기까지가 콘솔 작업이다. 이후는 `Deploy/README.md`의 3~6단계:

1. **스키마 배포** — 로컬에서 SSH 터널 + `sqlcmd`
2. **Nginx 설정 · systemd 유닛 설치** — `playground.conf` · `playground.service`
3. **환경변수 파일** `/etc/playground/playground.env` 작성
4. **첫 배포** — GitHub Environment 시크릿 등록 후 워크플로 실행
5. **HTTPS** — `sudo certbot --nginx -d playgroundsport.com -d www.playgroundsport.com`
6. **OAuth 리다이렉트 URI 4곳 등록**

> **권고: 도메인·HTTPS 전에 `http://<EIP>`로 앱이 뜨는지 먼저 확인한다.**
> HTTPS·OAuth 문제와 앱 자체 문제가 섞이면 원인을 가리기 어려워진다.

---

## 자주 막히는 것 정리

| 증상 | 확인할 것 |
|---|---|
| 콘솔에 만든 자원이 안 보인다 | **리전이 서울인가** |
| SSH 시간 초과 | 보안 그룹 SSH 소스 = 현재 내 IP |
| SSH 키 거부 | `icacls`로 권한 정리했는가 |
| 재시작 후 IP가 바뀜 | Elastic IP를 **연결**했는가 |
| SQL Server가 안 뜬다 | 메모리 부족일 수 있다 — `free -h`로 확인, 상한 2048 적용 여부 |
| SSMS 연결 시간 초과 | 보안 그룹 47821 소스 = 현재 내 IP. `Test-NetConnection <EIP> -Port 47821` |
| `sqlcmd`가 서버를 못 찾는다 | **포트를 빠뜨렸다** — `localhost`가 아니라 `localhost,47821` |
| SSMS 인증서 오류 | 연결 속성 → **"서버 인증서 신뢰"** 체크 (자체 서명 인증서) |
| 사용자 데이터가 안 돈 것 같다 | `sudo cat /var/log/cloud-init-output.log` 끝부분 |
| 디스크가 8GB로 만들어졌다 | 인스턴스를 다시 만드는 편이 빠르다 (아직 아무것도 없으므로) |
