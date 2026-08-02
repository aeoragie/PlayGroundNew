# 서버 목록 · 자주 쓰는 명령

> **비밀번호·키·토큰은 여기 적지 않는다.** 이 파일은 git에 올라간다.
> 시크릿은 비밀번호 관리자 / 서버의 `/etc/playground/*.env` / GitHub Environment 시크릿에 둔다.
>
> 구성·설치는 `README.md`, 콘솔 작업은 `AwsSetup.md`, 장애 대응은 `Runbook.md`.

## 지금 어디까지 왔나 (2026-08-02)

**네트워크는 검증 끝, 인스턴스는 AMI를 잘못 골라 재생성 대기.**

| 끝난 것 | 상태 |
|---|---|
| VPC·서브넷·IGW·라우팅 | ✅ `playground-vpc` — 퍼블릭 서브넷 접속까지 확인 |
| 보안 그룹 | ✅ `playground-prod-sg` (22·80·443·47821) |
| 키 페어 | ✅ `playground-prod.pem` (권한 정리 완료) |
| Elastic IP | ✅ `54.180.64.167` |
| SSH 접속 | ✅ 성공 |

**다음에 할 일 — 인스턴스를 다시 만든다:**

1. 현재 인스턴스가 **Ubuntu 26.04**로 만들어졌다. Microsoft가 26.04용 저장소를 주지 않아
   **SQL Server·.NET을 설치할 수 없다.** 종료하고 다시 만든다.
2. AMI는 **`모든 AMI 찾아보기` → `ubuntu-jammy-22.04`** (소유자 Canonical).
   빠른 시작의 `Ubuntu` 타일은 최신 LTS가 기본이라 또 잘못 잡힌다.
3. 네트워크 설정은 **`편집`을 눌러** 서브넷 `…-public1-…` · 퍼블릭 IP **활성화**.
4. 탄력적 IP 재연결 → 접속 후 `lsb_release -a`로 **jammy** 확인.
5. 그다음 **`ManualSetup.md` 1단계**부터 (직접 설치해 보는 중).

> 이후 절차는 `AwsSetup.md` "SQL Server 초기 설정" → S3/IAM → Route 53 순서.

## 서버 목록

### prod-01 — 앱 + DB + 캐시 (전부 한 대)

| 항목 | 값 |
|---|---|
| 역할 | PlayGround 웹 앱 · SQL Server 2022 Express · Redis · Nginx |
| **공개 IP** | `54.180.64.167` (Elastic IP · `eipalloc-0f8a638601ba852b0`) |
| **비공개 IP** | `10.0.1.77` (**퍼블릭 서브넷 = `10.0.0~31.x`** 여야 한다) |
| VPC | `playground-vpc` (`vpc-06c3c73221934327b`, 10.0.0.0/16) |
| 서브넷 | `playground-subnet-public1-ap-northeast-2a` (10.0.0.0/20) |
| 보안 그룹 | `playground-prod-sg` (`sg-0d7ff3505cee59980`) |
| OS | Ubuntu 22.04 LTS |
| 인스턴스 타입 | t3.medium (2 vCPU / 4GB) |
| 리전 | **ap-northeast-2 (서울)** |
| 인스턴스 ID | `i-0bb7075d9a4c359e5` |
| 도메인 | playgroundsport.com · www |
| SSH 키 | `C:\…\playground-prod.pem` |
| 시간대 | **UTC** (호스트 TZ로 로직을 맞추지 않는다) |

> **공개 IP는 Elastic IP인지 반드시 확인한다.** 아니면 인스턴스를 중지·시작할 때
> 바뀌고 DNS·보안 그룹이 전부 어긋난다.

#### 리전 — 반드시 서울(ap-northeast-2)

**한 번 버지니아(us-east-1)에 잘못 만들어 다시 세웠다** (2026-08-02).
콘솔 우측 상단 리전이 다른 상태에서 만들면 조용히 그쪽에 생긴다.

- 서울이 아니면 한국 사용자 왕복 지연이 **10ms대 → 200ms대**가 된다.
- **인스턴스·EBS·S3 버킷·Elastic IP는 리전에 묶인다** — 나중엔 전부 다시 만들어야 한다.

만들고 나서 한 번 확인한다:

```bash
curl -s http://169.254.169.254/latest/meta-data/placement/region   # ap-northeast-2
```

### 비공개 IP는 어디에 쓰나

같은 VPC 안에서만 유효하다(인터넷에서 접근 불가). 지금은 한 대에 전부 올라가 있어
앱은 `localhost`로 붙으므로 **쓸 일이 없다.** DB를 별도 인스턴스/RDS로 분리하면
그때 앱의 커넥션 문자열이 이 주소를 가리키게 된다.

## 접속

```powershell
ssh -i C:\…\playground-prod.pem ubuntu@3.224.104.65
```

매번 치기 번거로우면 `~/.ssh/config`에 등록해 두고 `ssh playground`로 붙는다:

```
Host playground
    HostName 3.224.104.65
    User ubuntu
    IdentityFile C:/…/playground-prod.pem
```

## 포트

| 포트 | 무엇 | 열린 범위 |
|---|---|---|
| 22 | SSH | **내 IP만** |
| 80 · 443 | 웹 (80은 certbot 갱신에도 필요) | 전체 |
| 47821 | SQL Server | **내 IP만** |
| 5000 | 앱 (Kestrel) | 서버 내부만 — Nginx가 프록시 |
| 6379 | Redis | 서버 내부만 (`bind 127.0.0.1`) |

> **보안 그룹(AWS)과 ufw(호스트) 둘 다 통과해야 한다.** 하나만 막혀도 증상은 시간 초과로 같다.

## 자주 쓰는 명령

### 상태 보기

```bash
systemctl status playground nginx mssql-server redis-server --no-pager
df -h / ; free -h
```

### 로그

```bash
journalctl -u playground -f                  # 앱 실시간
journalctl -u playground -n 100 --no-pager   # 최근 100줄
tail -f /var/log/nginx/error.log             # Nginx 오류
tail -30 /var/log/playground/backup.log      # 백업 결과
sudo tail -30 /var/log/cloud-init-output.log # 최초 셋업(user-data)
```

### 재시작

```bash
sudo systemctl restart playground     # 앱만
sudo systemctl reload nginx           # Nginx 설정 반영 (무중단)
sudo systemctl restart mssql-server
sudo systemctl restart redis-server
```

> 환경변수(`/etc/playground/playground.env`)를 고쳤으면 **앱 재시작이 필요하다** —
> systemd는 기동 시점에만 읽는다.

### DB

```bash
# 서버 안에서
sqlcmd -S localhost,47821 -U pgadmin -P '<비번>' -C -Q "SELECT @@VERSION"

# 내 PC에서 (SSMS 서버 이름도 같은 형식)
sqlcmd -S 3.224.104.65,47821 -U pgadmin -P '<비번>' -C -Q "SELECT 1"
```

### 배포·백업

```bash
sudo /usr/local/bin/playground-backup        # 백업 수동 실행
sudo crontab -l                              # 백업 스케줄 확인
```

배포는 **GitHub Actions → Deploy → Run workflow**로 한다. 서버에서 직접 하지 않는다.

## 경로·계정

| 무엇 | 어디 |
|---|---|
| 앱 실행 파일 | `/var/www/playground` |
| 직전 버전 (롤백용) | `/var/www/playground.prev` |
| 앱 로그 (NLog) | `/var/log/playground/` |
| 시크릿 (앱) | `/etc/playground/playground.env` |
| 시크릿 (백업) | `/etc/playground/backup.env` |
| DB 백업 | `/var/backups/playground/` → S3 |
| Nginx 설정 | `/etc/nginx/sites-available/playground` |
| systemd 유닛 | `/etc/systemd/system/playground.service` |
| 업로드 이미지 | `/var/www/playground/wwwroot/uploads` (S3 전환 전) |

| 계정 | 용도 |
|---|---|
| `ubuntu` | SSH 로그인 (sudo 가능) |
| `playground` | 앱 실행 전용 — 로그인 불가(`nologin`) |
| `pgadmin` | SQL Server 관리 (`sa`는 잠가 뒀다) |
