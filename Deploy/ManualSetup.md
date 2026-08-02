# 서버 설치를 손으로 따라가기 (최초 1회, 이해용)

> **정본은 `ec2-setup.sh`다.** 이 문서는 그 스크립트가 하는 일을 **한 덩어리씩 직접 쳐 보며
> 이해하기 위한 안내**다. 무엇이 왜 필요한지 알아야 나중에 문제가 났을 때 어디를 볼지 알 수 있다.
>
> **두 번째부터는 user-data에 스크립트를 붙여 넣는다.** 손으로 하면 매번 달라지고,
> 인스턴스를 잃었을 때 같은 서버를 다시 만들 수 없다.
>
> `ec2-setup.sh`를 고쳤다면 이 문서도 함께 본다 — 두 곳에 같은 명령이 있다.

## 시작 전

- EC2 인스턴스를 **사용자 데이터 없이** 만든다 (`AwsSetup.md` "EC2 인스턴스 시작" 절에서 **사용자 데이터** 항목만 건너뛴다)
- SSH로 접속한다 (`AwsSetup.md` "첫 SSH 접속" 절)

```bash
ssh -i <키.pem> ubuntu@<Elastic IP>
```

### 첫 줄부터 확인한다 — **Ubuntu 22.04인가**

접속하면 `Welcome to Ubuntu …` 가 뜬다. **22.04가 아니면 여기서 멈춘다.**

```bash
lsb_release -a          # Codename: jammy  /  Release: 22.04
```

`jammy`가 아니면 3단계(Microsoft 저장소)부터 막힌다 — **다른 버전용 저장소가 없다.**
AMI를 잘못 고른 것이므로 인스턴스를 다시 만든다(`AwsSetup.md` "EC2 인스턴스 시작").
**여기서 확인 안 하고 진행하면 4단계까지 가서야 실패한다.**

> **user-data는 root로 돌지만 지금 나는 `ubuntu`다.** 그래서 아래 명령에는 전부 `sudo`가 붙는다.
> 스크립트에 `sudo`가 없는 이유가 이것이다.

---

## 1. 시간대

```bash
sudo timedatectl set-timezone UTC
timedatectl | grep "Time zone"
```

**왜 UTC인가** — 서버 시간대에 애플리케이션 로직을 맞추면, "어느 장비에서 도는가"에 따라
결과가 달라지는데 그 의존이 코드 어디에도 안 보인다. 한국 시각은 코드·DB의 명시적 래퍼가
책임진다 (`Docs/Development/ReleasePlan.md` H7).

---

## 2. 기본 패키지

```bash
export DEBIAN_FRONTEND=noninteractive
sudo apt-get update
sudo apt-get install -y curl gnupg ca-certificates apt-transport-https software-properties-common unzip
```

- `apt-get update` — 설치가 아니라 **패키지 목록 갱신**이다. 저장소를 추가할 때마다 다시 친다.
- `DEBIAN_FRONTEND=noninteractive` — 설치 중 파란 설정 화면이 떠서 멈추는 걸 막는다.
- 여기 것들은 그 자체가 목적이 아니라 **다음 단계(외부 저장소 추가)의 준비물**이다.

---

## 3. Microsoft 저장소 등록

Ubuntu 기본 저장소에는 SQL Server도 .NET도 없다. Microsoft 저장소를 **믿을 수 있는 출처로
등록**해야 `apt-get install`로 받을 수 있다.

**3-1. 서명 키** — 받은 패키지가 진짜 Microsoft 것인지 검증하는 열쇠다.

```bash
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
  | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
```

**3-2. SQL Server 2022 저장소**

```bash
echo "deb [arch=amd64,arm64,armhf signed-by=/usr/share/keyrings/microsoft-prod.gpg] \
https://packages.microsoft.com/ubuntu/22.04/mssql-server-2022 jammy main" \
  | sudo tee /etc/apt/sources.list.d/mssql-server-2022.list
```

> `22.04`·`jammy`가 박혀 있다. **Ubuntu 24.04에는 SQL Server 2022 저장소가 없어서**(2025만 제공)
> 22.04를 쓴다.

**3-3. prod 저장소** — sqlcmd·bcp·.NET 런타임이 여기 있다.

```bash
echo "deb [arch=amd64,arm64,armhf signed-by=/usr/share/keyrings/microsoft-prod.gpg] \
https://packages.microsoft.com/ubuntu/22.04/prod jammy main" \
  | sudo tee /etc/apt/sources.list.d/microsoft-prod.list

sudo apt-get update
```

확인 — 목록에 `packages.microsoft.com`이 보이면 성공이다:

```bash
apt-cache policy mssql-server | head -5
```

---

## 4. SQL Server 설치

```bash
sudo apt-get install -y mssql-server
```

**설치만 하고 아직 안 뜬다.** 에디션과 sa 비밀번호를 정해야 시작할 수 있고,
그건 `AwsSetup.md` "SQL Server 초기 설정" 절에서 대화형으로 한다 — **비밀번호를 스크립트에 넣지 않기 위해서다**
(user-data는 인스턴스 메타데이터로 조회할 수 있다).

```bash
systemctl status mssql-server --no-pager    # 아직 not running이 정상
```

---

## 5. sqlcmd · bcp

```bash
sudo ACCEPT_EULA=Y apt-get install -y mssql-tools18 unixodbc-dev
sudo ln -sf /opt/mssql-tools18/bin/sqlcmd /usr/local/bin/sqlcmd
sudo ln -sf /opt/mssql-tools18/bin/bcp /usr/local/bin/bcp
```

- `ACCEPT_EULA=Y` — 없으면 라이선스 동의 화면에서 멈춘다.
- `ln -sf` — **심볼릭 링크**다. `/opt/mssql-tools18/bin`은 기본 PATH에 없어서
  그냥은 `sqlcmd`가 "command not found"가 난다. PATH에 있는 곳으로 바로가기를 만드는 것.

```bash
which sqlcmd      # /usr/local/bin/sqlcmd
```

---

## 6. ASP.NET Core 런타임

```bash
sudo apt-get install -y aspnetcore-runtime-10.0
dotnet --list-runtimes | grep AspNetCore
```

**SDK가 아니라 런타임이다.** 빌드는 GitHub Actions가 하고, 서버는 만들어진 결과물을
실행만 하면 된다. SDK를 깔면 용량만 크고 쓸 일이 없다.

---

## 7. Redis

```bash
sudo apt-get install -y redis-server
```

**외부에 열지 않는다** — 같은 서버의 앱만 쓴다. 인터넷에 노출된 Redis는 비밀번호가 없으면
누구나 읽고 쓸 수 있다.

```bash
sudo sed -i 's/^# *bind .*/bind 127.0.0.1 ::1/' /etc/redis/redis.conf
sudo sed -i 's/^bind .*/bind 127.0.0.1 ::1/' /etc/redis/redis.conf
sudo sed -i 's/^supervised .*/supervised systemd/' /etc/redis/redis.conf

sudo systemctl enable redis-server
sudo systemctl restart redis-server
```

- `sed -i` — 파일을 그 자리에서 치환한다. 두 줄인 이유는 `bind` 줄이
  **주석 처리된 경우와 아닌 경우**가 둘 다 있어서다.
- `supervised systemd` — Redis가 systemd에 "나 준비됐다"를 알리게 한다.
- `enable`(부팅 시 자동 시작)과 `restart`(지금 시작)는 **다른 명령**이다. 둘 다 필요하다.

```bash
redis-cli ping        # PONG
```

---

## 8. Nginx · certbot

```bash
sudo apt-get install -y nginx
sudo systemctl enable nginx

sudo apt-get install -y certbot python3-certbot-nginx
```

Nginx는 **리버스 프록시**다. 앱은 5000 포트에서 돌고, Nginx가 80/443을 받아 넘긴다.
HTTPS 인증서도 Nginx가 들고 있다(앱이 아니라).

지금 `http://<Elastic IP>`를 브라우저로 열면 Nginx 기본 페이지가 뜬다 — 여기까지 왔다는 뜻이다.

---

## 9. 한글 폰트

```bash
sudo apt-get install -y fonts-nanum fonts-noto-cjk
sudo fc-cache -f
```

**OG 카드(공유 미리보기 이미지)를 SkiaSharp가 서버에서 그린다.** 폰트가 없으면 한글이 통째로
깨진다. Windows 개발 PC에는 폰트가 있어서 **개발 중에는 절대 드러나지 않는 문제**다.

```bash
fc-list :lang=ko | head -3
```

---

## 10. 앱 계정과 디렉터리

```bash
sudo useradd --system --no-create-home --shell /usr/sbin/nologin playground
sudo mkdir -p /var/www/playground /var/log/playground /var/backups/playground
sudo chown -R playground:playground /var/www/playground /var/log/playground
```

**앱을 root로 돌리지 않는다.** 앱이 뚫려도 서버 전체를 잃지 않게 하려는 것이다.

- `--system` — 사람이 아니라 서비스용 계정
- `--shell /usr/sbin/nologin` — **이 계정으로는 로그인 자체가 안 된다**

---

## 11. 방화벽 (ufw)

> **여기가 유일하게 위험한 단계다.** 순서를 지키지 않으면 **SSH가 끊겨 다시 못 들어온다.**
> `allow`를 **먼저** 하고 `enable`을 **마지막에** 한다.

```bash
sudo ufw allow OpenSSH                              # 22 — 이걸 빼먹으면 잠긴다
sudo ufw allow 'Nginx Full'                         # 80 + 443
sudo ufw allow 47821/tcp comment 'SQL Server'
```

규칙을 먼저 확인하고,

```bash
sudo ufw show added
```

`OpenSSH`가 보이는 것을 **눈으로 확인한 뒤** 켠다:

```bash
sudo ufw --force enable
sudo ufw status numbered
```

**방화벽이 둘이라는 점이 중요하다** — AWS 보안 그룹과 호스트 ufw를 **둘 다** 통과해야 한다.
하나만 막혀도 증상은 똑같이 "시간 초과"라 원인을 헷갈리기 쉽다.
접속 IP 제한은 보안 그룹이 하고, ufw는 포트 개폐만 담당한다.

> 잠겼다면 — SSH로는 못 들어간다. AWS 콘솔의 **EC2 Instance Connect**나
> 인스턴스 중지 → 볼륨 분리 순서로 복구해야 한다. 그래서 순서를 지키는 것이다.

---

## 끝 — 스크립트와 같은 상태인지 확인

```bash
systemctl status nginx redis-server --no-pager   # active (running)
systemctl status mssql-server --no-pager         # 아직 안 뜬 게 정상 (AwsSetup에서 설정)
dotnet --list-runtimes | grep AspNetCore
which sqlcmd
sudo ufw status
timedatectl | grep "Time zone"                   # UTC
```

여기까지가 `ec2-setup.sh`와 같은 상태다. 이어서 **`AwsSetup.md` "SQL Server 초기 설정" 절** 로 간다.

## 이제 무엇이 달라졌나

각 서비스가 **왜** 필요한지 알게 됐으므로, 장애가 났을 때 `Runbook.md`의 명령이
무엇을 확인하는 것인지 읽힌다. 다음 인스턴스부터는 user-data를 쓴다 —
**손으로 한 설치는 재현되지 않기 때문이다.**
