# AWS 콘솔 셋업 가이드

> 대상: **playgroundsport.com** · 서울 리전 · EC2 t3.medium 한 대
> 결정 근거는 `Docs/Development/Deployment.md`. 이 문서는 **콘솔에서 실제로 무엇을 누르는지**만 다룬다.
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

방화벽이다. **1433(SQL)은 열지 않는다** — SSMS는 SSH 터널로 붙는다.

1. **EC2** → **네트워크 및 보안** → **보안 그룹** → **보안 그룹 생성**
2. 이름 `playground-prod-sg` · 설명 `PlayGround production`
3. **인바운드 규칙** 3개 추가:

| 유형 | 포트 | 소스 | 설명 |
|---|---|---|---|
| SSH | 22 | **내 IP** | 관리 접속 |
| HTTP | 80 | Anywhere-IPv4 (`0.0.0.0/0`) | 웹 + certbot 갱신 |
| HTTPS | 443 | Anywhere-IPv4 (`0.0.0.0/0`) | 웹 |

4. 아웃바운드는 기본값(전체 허용) 그대로 — 패키지 설치·S3 업로드에 필요하다

> **"내 IP"는 지금 이 순간의 IP다.** 인터넷 회선이 재접속되면 바뀌어서 SSH가 막힌다.
> 그때는 보안 그룹에서 SSH 규칙의 소스를 **내 IP로 다시 선택**하면 된다. 당황할 일이 아니다.

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
timedatectl | grep "Time zone"             # Asia/Seoul 이어야 한다
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

메모리 상한을 잡는다. 앱·Redis 몫을 남기기 위함이다:

```bash
sudo /opt/mssql/bin/mssql-conf set memory.memorylimitmb 2048
sudo systemctl restart mssql-server
```

동작 확인:

```bash
sqlcmd -S localhost -U sa -P '<비밀번호>' -C -Q "SELECT @@VERSION"
```

### DB 두 개 만들기

콜레이션이 중요하다 — 우리 스키마 규칙(UTF-8)이다:

```bash
sqlcmd -S localhost -U sa -P '<비밀번호>' -C -Q "
CREATE DATABASE PlayGround_Account COLLATE Latin1_General_100_CI_AS_SC_UTF8;
CREATE DATABASE PlayGround_Soccer  COLLATE Latin1_General_100_CI_AS_SC_UTF8;"
```

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

여기까지가 콘솔 작업이다. 이후는 `Docs/Development/Deployment.md`의 3~6단계:

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
| 사용자 데이터가 안 돈 것 같다 | `sudo cat /var/log/cloud-init-output.log` 끝부분 |
| 디스크가 8GB로 만들어졌다 | 인스턴스를 다시 만드는 편이 빠르다 (아직 아무것도 없으므로) |
