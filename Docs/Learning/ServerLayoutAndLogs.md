# 서버 배치 지도 · 4-2/4-3 명령 해설 · 로그 보는 법

> 첫 배포(R4)에서 실행한 명령들이 "무엇을 어디에 왜" 놓는지, 그리고 문제가 생겼을 때
> **스스로** 로그를 찾아 읽는 방법. 명령 문법 자체는 `LinuxBasics.md`, 절차는 `Deploy/README.md`.

## 1. 리눅스 폴더 역할 — 자리가 정해져 있다 (FHS)

리눅스는 FHS(Filesystem Hierarchy Standard)라는 관례로 폴더마다 역할이 정해져 있다.
"아무 데나 놓아도 돌아가지만", 관례를 따르면 어떤 서버에 들어가도 뭐가 어디 있는지 안다.

| 폴더 | 역할 | 우리 서버에서 |
|---|---|---|
| `/etc` | **설정** (editable text config) — 프로그램 말고 프로그램의 설정 | `/etc/playground/playground.env`(앱 시크릿), `/etc/nginx/`(웹서버 설정), `/etc/systemd/system/`(서비스 정의) |
| `/usr` | 배포판·패키지가 설치한 **프로그램** (읽기 전용 성격) | `/usr/bin/dotnet` |
| `/usr/local` | 패키지 관리자 밖에서 **관리자가 직접** 설치한 것 — apt가 절대 건드리지 않는 안전지대 | `/usr/local/bin/playground-deploy`·`playground-backup` (우리 스크립트) |
| `/opt` | 자기 폴더 하나를 통째로 쓰는 서드파티 | `/opt/mssql/`(SQL Server), `/opt/mssql-tools18/` |
| `/var` | **변하는 데이터** (variable) — 로그·DB 파일·앱 데이터 | `/var/www/playground/`(앱 본체), `/var/log/`(로그), `/var/opt/mssql/`(DB 데이터 파일) |
| `/tmp` | 임시 파일 — 재부팅하면 사라져도 되는 것만 | scp로 올린 zip·스크립트의 첫 착지점. 그래서 받은 뒤 `install`로 제자리에 옮긴다 |
| `/home` | 사용자 홈 | `/home/ubuntu` — SSH로 들어가면 여기서 시작 |

요약: **프로그램은 /usr, 설정은 /etc, 데이터·로그는 /var, 임시는 /tmp.**
"어디에 있을까?"가 떠오르면 역할부터 생각하면 된다 — 앱 로그는 변하는 데이터니까 /var 근처다.

## 2. 4-2 명령 해설 — 제자리 배치 + nginx 교체

```bash
sudo install -m 750 /tmp/deploy-app.sh /usr/local/bin/playground-deploy
```
- `install` = 복사(cp) + 권한 설정(chmod) + 소유자 설정을 **한 번에**. 배치 전용 cp라고 보면 된다.
- `-m 750` = 권한 8진수. 자리마다 소유자/그룹/기타 — `7`(rwx)·`5`(r-x)·`0`(권한 없음).
  스크립트라 실행(x)이 필요하고, 아무나 읽을 필요는 없다.
- `/tmp`의 파일은 착지점일 뿐이니 `/usr/local/bin`(관리자가 직접 설치하는 명령 자리)으로 옮기며
  이름도 `playground-deploy`로 바꾼다 — PATH에 있는 폴더라 어디서든 명령처럼 부를 수 있게 된다.

```bash
sudo install -m 644 /tmp/playground.service /etc/systemd/system/playground.service
```
- systemd 서비스 정의 = "이 앱을 어떻게 켜고, 죽으면 어떻게 하고, 환경변수는 어디서 읽나"의 명세.
  `/etc/systemd/system/`이 관리자가 만든 서비스 정의의 자리다.
- `644` = 소유자만 쓰고(6=rw-) 모두 읽기(4=r--). 실행 파일이 아니라 설정이니 x가 없다.

```bash
sudo ln -sf /etc/nginx/sites-available/playground /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
```
- nginx의 on/off 구조: 설정 원본은 전부 `sites-available`(보관함)에 두고,
  **켤 것만 `sites-enabled`에 심볼릭 링크**로 건다. 끄고 싶으면 링크만 지우면 된다(원본 보존).
- `default`를 지워야 하는 이유 — 우리가 겪은 그대로다: default가 살아 있으면 도메인 접속이
  앱이 아니라 "Welcome to nginx" 페이지로 간다.

```bash
sudo nginx -t && sudo systemctl reload nginx
```
- `nginx -t` = 설정 문법 검사. **`&&` 때문에 검사를 통과해야만** reload가 실행된다 —
  문법이 깨진 설정으로 reload하면 웹서버가 안 뜨는 사고를 막는 안전벨트.
- `reload` vs `restart`: reload는 연결을 끊지 않고 설정만 다시 읽는다.

## 3. 4-3 명령 해설 — 환경변수 파일

```bash
sudo tee /etc/playground/playground.env > /dev/null <<'EOF'
...내용...
EOF
```
- `sudo nano`로 열어 손으로 쳐도 같다. `tee`는 "표준입력을 파일에 쓰는" 명령이라
  붙여넣기 배치에 쓴 것뿐이다. (`sudo echo > 파일`은 리다이렉트가 sudo 밖이라 안 된다 —
  그래서 tee 관용구를 쓴다.) `<<'EOF'`는 히어독 — EOF가 나올 때까지를 통째로 입력으로.
- 이 파일이 앱 시크릿의 **유일한** 자리다. 코드·저장소에는 절대 없다.

```bash
sudo chmod 600 /etc/playground/playground.env
```
- `600` = 소유자(root)만 읽고 쓴다. DB 비번이 든 파일의 기본기 —
  다른 계정으로 침입당해도 이 파일은 못 읽는다.

```bash
sudo systemctl daemon-reload && sudo systemctl enable playground
```
- `daemon-reload` = systemd에게 "서비스 정의 파일이 바뀌었으니 다시 읽어라".
  `.service` 파일을 만들거나 고치면 항상 이걸 해야 반영된다.
- `enable` = **부팅 시 자동 시작** 등록. 지금 켜는 게 아니다(그건 `start`).
  enable 없이 start만 하면 서버 재부팅 후 앱이 안 떠 있는 사고가 난다.

## 4. 로그 보는 법 — 어디에 무엇이 있고, 어떻게 읽나

우리 서버의 로그는 세 갈래다. **"누가 찍은 로그인가"로 자리를 찾는다.**

| 갈래 | 자리 | 보는 명령 |
|---|---|---|
| ① systemd(서비스 기동·중단) + 앱 콘솔 출력 | journal (systemd 저널) | `journalctl -u playground` |
| ② 앱 파일 로그 (NLog) | `/var/www/playground/Logs/*.log` | `tail`·`less`·`grep` |
| ③ nginx 접속·오류 | `/var/log/nginx/access.log`·`error.log` | `tail -f` |

### ① journalctl — 기동 실패는 여기부터

```bash
sudo systemctl status playground --no-pager     # 지금 상태 요약 + 마지막 로그 몇 줄
sudo journalctl -u playground -n 50 --no-pager  # 이 서비스의 마지막 50줄
sudo journalctl -u playground -f                # 실시간으로 흘려 보기 (Ctrl+C로 끝)
sudo journalctl -u playground --since "10 min ago"
```

- `-u playground` = 이 유닛(서비스)의 로그만. 이게 없으면 시스템 전체가 섞여 나온다.
- 읽는 요령 — **에러의 "단계"를 구분한다**:
  - `Failed to locate executable ...: status=203/EXEC` → 앱 코드가 돌기 **전**,
    실행 파일 자체를 못 찾음(환경 문제 — 우리가 겪은 dotnet 미설치가 이것).
  - 앱이 찍은 스택트레이스가 보이면 → 앱은 떴는데 초기화 중 죽음(설정·DB 연결 등).
  - `Scheduled restart job, restart counter is at N` → 서비스 정의의 자동 재시작이 돌고 있다는 뜻.
    문제를 고치면 스스로 살아나기도 한다.

### ② 앱 파일 로그 — 운영 중 API 오류는 여기

NLog가 `${basedir}/Logs`에 쓴다 → 앱이 `/var/www/playground`에 있으니:

```bash
ls /var/www/playground/Logs/
tail -n 100 /var/www/playground/Logs/PlayGround.Server.log
tail -f /var/www/playground/Logs/PlayGround.Server.log          # 실시간
grep '\[ERROR\]' /var/www/playground/Logs/PlayGround.Server.log | tail -20
```

로그 포맷은 우리 규칙대로 `문장. { Key:Value }` — UserId 같은 식별자 필드로 grep하면 된다.

### ③ nginx — "요청이 앱까지 왔는가"를 가르는 곳

```bash
sudo tail -f /var/log/nginx/access.log   # 요청이 들어오긴 하나? 상태코드는?
sudo tail -n 50 /var/log/nginx/error.log # 502면 대부분 여기 (앱 5000이 죽어 있음)
```

판별 요령 — **502 Bad Gateway** = nginx는 살았는데 뒤의 앱(5000)이 죽음 → ①로 가서 앱을 본다.
**연결 자체가 안 됨** = nginx나 방화벽/보안 그룹 → `systemctl status nginx`와 SG 확인.

### 진단 순서 요약 (밖→안)

```
브라우저/curl 실패
 → nginx access.log에 요청이 찍히나?   (안 찍힘 = DNS·SG·nginx 죽음)
 → 상태코드가 502인가?                  (그렇다 = 앱이 죽음 → journalctl)
 → journalctl에서 203/EXEC류인가 앱 에러인가?
 → 앱 에러면 파일 로그(Logs/)에서 ERROR를 grep
```
