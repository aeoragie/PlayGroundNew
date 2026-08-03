# 런북 — 문제가 생겼을 때

> **증상으로 찾는 문서다.** 셋업은 `AwsSetup.md`, 구성·규칙은 `README.md`.
> 리눅스가 익숙하지 않아도 따라갈 수 있게 명령을 그대로 적었다.

## 먼저 — 상태 한 번에 보기

접속해서 이것부터 친다. 대부분 여기서 원인이 좁혀진다.

```bash
ssh -i <키.pem> ubuntu@<EIP>

systemctl status playground nginx mssql-server redis-server --no-pager
df -h /                      # 디스크
free -h                      # 메모리
journalctl -u playground -n 50 --no-pager    # 앱 최근 로그
```

`q`로 빠져나온다. **`active (running)`이 아닌 것**이 범인일 가능성이 높다.

---

## 사이트가 안 뜬다

### 브라우저에 502 Bad Gateway

Nginx는 살아 있는데 **앱이 죽었다.** 가장 흔한 경우다.

```bash
systemctl status playground --no-pager
journalctl -u playground -n 100 --no-pager
```

로그에서 예외를 확인한 뒤:

```bash
sudo systemctl restart playground
```

**다시 죽는다면** 설정 문제일 가능성이 크다 — 아래 "앱이 계속 재시작한다"로.

### 연결 자체가 안 된다 (시간 초과)

앱이 아니라 앞단 문제다.

1. **EC2 인스턴스가 실행 중인가** — AWS 콘솔에서 확인
2. **Elastic IP가 연결돼 있는가** — 중지·시작 후 IP가 바뀌었을 수 있다
3. **보안 그룹 80·443이 열려 있는가**
4. Nginx가 살아 있는가:

```bash
systemctl status nginx --no-pager
sudo nginx -t          # 설정 문법 검사
sudo systemctl restart nginx
```

### 404만 나온다 (API는 되는데 화면이 안 뜬다)

배포 산출물에 WASM 클라이언트가 빠졌다.

```bash
ls /var/www/playground/wwwroot/_framework | head
```

비어 있으면 산출물이 잘못된 것이다. 워크플로를 다시 돌린다.

---

## 앱이 계속 재시작한다

systemd가 `Restart=always`라 죽어도 계속 살리려 시도한다. 로그가 반복되면 이 상태다.

```bash
journalctl -u playground -n 200 --no-pager | grep -i "error\|exception\|fatal"
```

흔한 원인:

| 로그에 보이는 것 | 원인 |
|---|---|
| `Jwt:Key is not configured` | `/etc/playground/playground.env`에 `Jwt__Key`가 없다 |
| DB 연결 실패 | SQL Server가 안 떴거나 커넥션 문자열·비밀번호가 틀렸다 |
| `Address already in use` | 5000 포트를 다른 프로세스가 쓴다 — `sudo lsof -i :5000` |
| 아무 로그 없이 죽음 | 메모리 부족일 수 있다 → 아래 "메모리" |

환경변수 파일을 고쳤으면 **반드시 재시작**한다 (systemd가 기동 시점에만 읽는다):

```bash
sudo systemctl restart playground
```

---

## 배포가 실패했다

`deploy-app.sh`는 기동 확인에 실패하면 **스스로 직전 버전으로 되돌린다.**
즉 워크플로가 빨개도 **서비스는 이전 버전으로 살아 있다.**

```bash
journalctl -u playground -n 100 --no-pager     # 왜 새 버전이 안 떴는지
ls -la /var/www/playground.prev                 # 직전 버전 보관 위치
```

### 수동으로 되돌리기

자동 롤백까지 실패한 경우에만:

```bash
sudo systemctl stop playground
sudo rm -rf /var/www/playground
sudo mv /var/www/playground.prev /var/www/playground
sudo systemctl start playground
```

### 특정 커밋으로 되돌리기

GitHub **Actions → Deploy → Run workflow**에서 브랜치 대신 이전 커밋을 고를 수 없다면,
`main`을 그 커밋으로 되돌린 뒤(revert 커밋 권장) 워크플로가 자동으로 돈다.

> **되돌릴 수 없는 것은 DB 마이그레이션이다.** 컬럼을 지웠다면 코드를 되돌려도 데이터는 안 돌아온다.
> 파괴적 마이그레이션 전에는 반드시 백업을 먼저 받는다.

---

## 디스크가 찼다

```bash
df -h /
sudo du -sh /var/log/* /var/backups/playground/* /var/www/* 2>/dev/null | sort -h | tail
```

흔한 범인 셋:

```bash
# 1) 로컬 백업이 쌓임 — 스크립트가 7일치만 남기지만 실패했을 수 있다
sudo find /var/backups/playground -name '*.bak' -mtime +7 -delete

# 2) journald 로그
sudo journalctl --vacuum-time=14d

# 3) 이전 배포 잔재
sudo rm -rf /var/www/playground.new
```

> **업로드 이미지도 여기 쌓인다** (`/var/www/playground/wwwroot/uploads`).
> S3 전환(H2) 전까지는 디스크를 함께 본다.

---

## DB에 연결이 안 된다

```bash
systemctl status mssql-server --no-pager
sudo journalctl -u mssql-server -n 50 --no-pager
sqlcmd -S localhost,47821 -U playgroundadmin -P '<비번>' -C -Q "SELECT 1"
```

| 증상 | 대응 |
|---|---|
| 서비스가 안 뜬다 | 메모리 부족이 대부분 — `free -h` 확인 후 `memory.memorylimitmb` 조정 |
| 로그인 실패 | `playground` 비밀번호 확인. `playground.env`의 커넥션 문자열과 같은지 |
| 시작 직후 죽음 | `sudo journalctl -u mssql-server -n 100`에 원인이 찍힌다 |
| 연결 자체가 안 됨 | **포트를 빠뜨렸다.** `localhost`가 아니라 `localhost,47821`이다 |

> **1433은 열려 있지 않다.** `mssql-conf set network.tcpport 47821`로 바꿔 뒀고,
> SQL Server는 지정한 포트 **하나만** 듣는다. 실제 수신 포트 확인:
>
> ```bash
> sudo /opt/mssql/bin/mssql-conf get network.tcpport
> sudo ss -lntp | grep sqlservr
> ```

### Express 10GB 한계

Express는 **DB당 10GB**를 넘으면 쓰기가 실패한다. 미리 본다:

```bash
sqlcmd -S localhost,47821 -U playgroundadmin -P '<비번>' -C -Q "
SELECT DB_NAME(database_id) AS DB,
       CAST(SUM(size) * 8.0 / 1024 AS DECIMAL(10,1)) AS MB
FROM sys.master_files
WHERE database_id > 4 GROUP BY database_id;"
```

7GB를 넘기 시작하면 대응을 준비한다 — 오래된 데이터 정리, 또는 Standard/RDS로 이전.

---

## 내 PC에서 SSMS가 안 붙는다

**대부분 내 IP가 바뀐 것이다.** 보안 그룹의 47821 규칙 소스가 "내 IP"라서,
공유기 재접속·회선 변경만으로도 막힌다.

로컬 PowerShell에서 포트가 열려 있는지부터 본다:

```powershell
Test-NetConnection <Elastic IP> -Port 47821
```

| 결과 | 원인 |
|---|---|
| `TcpTestSucceeded : False` | **보안 그룹** → 47821 규칙 소스를 **내 IP로 다시 선택** |
| 보안 그룹은 맞는데 여전히 False | **호스트 방화벽(ufw)** — 아래 |
| True인데 로그인 실패 | 계정은 `playgroundadmin`이다 — `sa`는 잠가 뒀다 |
| True인데 인증서 오류 | SSMS **연결 속성 → "서버 인증서 신뢰"** 체크 |

### 보안 그룹은 맞는데 안 붙는다 — 방화벽이 둘이다

**보안 그룹(AWS)과 ufw(호스트) 양쪽을 통과해야 한다.** 둘 중 하나만 막혀도 증상은 같다(시간 초과).
서버에서:

```bash
sudo ufw status numbered
```

목록에 `47821/tcp`가 없으면 추가한다:

```bash
sudo ufw allow 47821/tcp comment 'SQL Server'
```

> 오래된 `ec2-setup.sh`로 인스턴스를 만들었으면 이 규칙이 빠져 있다.
> **접속 IP 제한은 보안 그룹이 한다** — ufw는 포트 개폐만 담당하는 2차 방어다.

### 로그인 실패가 쌓이고 있지 않은지 (열어 둔 포트의 대가)

**SQL Server on Linux에는 계정 잠금이 없다.** 소스 IP 제한이 유일한 방어이므로,
가끔 실패 로그를 확인해 대입 시도가 도달하는지 본다.

```bash
sudo grep -i "Login failed" /var/opt/mssql/log/errorlog | tail -20
```

내 것이 아닌 IP나 모르는 사용자 이름(`sa`·`admin`·`test`)이 보이면
**소스가 `0.0.0.0/0`으로 열려 있다는 뜻이다.** 즉시 보안 그룹을 확인한다.

---

## 메모리가 부족하다

t3.medium은 4GB다. SQL Server 상한을 잡아 두지 않으면 앱이 밀려난다.

```bash
free -h
ps aux --sort=-%mem | head -5
```

SQL Server 상한 확인·조정:

```bash
sudo /opt/mssql/bin/mssql-conf get memory.memorylimitmb
sudo /opt/mssql/bin/mssql-conf set memory.memorylimitmb 2048
sudo systemctl restart mssql-server
```

**계속 부족하면** 인스턴스를 t3.large로 올린다 — 중지 → 인스턴스 타입 변경 → 시작.
Elastic IP를 붙여 뒀으면 IP는 그대로다.

---

## HTTPS 인증서

Let's Encrypt는 90일마다 갱신된다. 타이머가 자동으로 한다.

```bash
systemctl list-timers | grep certbot     # 타이머가 살아 있는지
sudo certbot certificates                # 만료일 확인
sudo certbot renew --dry-run             # 갱신이 될지 미리 시험
```

**갱신 실패의 대부분은 80 포트가 막힌 것이다** — 보안 그룹에서 80이 열려 있어야 한다
(HTTPS만 쓰니 닫아도 될 것 같지만, 갱신에 필요하다).

수동 갱신:

```bash
sudo certbot renew --force-renewal
sudo systemctl reload nginx
```

---

## 로그아웃해도 계속 로그인 상태다

Redis가 죽었을 때 나타난다. **설계상 이 경우 토큰을 막지 않는다**(fail-open) —
캐시 장애가 전체 로그인 불가로 번지는 것을 막기 위해서다.

```bash
systemctl status redis-server --no-pager
redis-cli ping                                   # PONG이어야 한다
journalctl -u playground | grep "Revocation check skipped"
```

`Revocation check skipped` 경고가 보이면 그 상태가 맞다.

```bash
sudo systemctl restart redis-server
```

> 앱은 자동으로 재연결한다(`AbortOnConnectFail=false`). 앱 재시작은 필요 없다.

---

## 백업이 돌고 있는지

**"돌아간다고 믿는 백업"이 가장 위험하다.** 주기적으로 실제 파일을 확인한다.

```bash
sudo crontab -l                                  # 등록돼 있는지
tail -30 /var/log/playground/backup.log          # 최근 실행 결과
aws s3 ls s3://<버킷>/db/ --recursive | tail     # S3에 실제로 올라갔는지
```

수동 실행:

```bash
sudo /usr/local/bin/playground-backup
```

---

## 최악 — 인스턴스를 잃었을 때

EBS까지 잃으면 **S3 백업이 유일한 자산이다.**

1. `AwsSetup.md`대로 인스턴스를 새로 만든다 (user-data 포함)
2. Elastic IP를 **새 인스턴스에 연결**한다 → DNS는 그대로 동작한다
3. SQL Server 초기 설정 + DB 2개 생성
4. S3에서 최신 백업을 받아 복원:

```bash
aws s3 cp s3://<버킷>/db/PlayGround_Soccer/<최신>.bak /var/backups/playground/
sudo chown mssql:mssql /var/backups/playground/<최신>.bak

sqlcmd -S localhost,47821 -U playgroundadmin -P '<비번>' -C -Q "
RESTORE DATABASE [PlayGround_Soccer]
FROM DISK = N'/var/backups/playground/<최신>.bak' WITH REPLACE;"
```

Account DB도 같은 방식으로. 이후 서버측 설치(`README.md` 4단계) → 워크플로 재실행.

> **업로드 이미지는 복원되지 않는다** — 로컬 디스크에만 있었기 때문이다.
> S3 전환(H2)이 미뤄질수록 이 손실 범위가 커진다.

---

## 로그는 어디에 있나

| 대상 | 명령 |
|---|---|
| 앱 | `journalctl -u playground -f` (실시간) |
| 앱 파일 로그 | `/var/log/playground/` (NLog) |
| Nginx 접근·오류 | `/var/log/nginx/access.log` · `error.log` |
| SQL Server | `journalctl -u mssql-server` · `/var/opt/mssql/log/errorlog` |
| 최초 셋업(user-data) | `/var/log/cloud-init-output.log` |
| 백업 | `/var/log/playground/backup.log` |

특정 시간대만 보기:

```bash
journalctl -u playground --since "2026-08-02 14:00" --until "2026-08-02 15:00"
```
