# 리눅스 기본 — 패키지 관리 · 자주 쓰는 명령

> `ManualSetup.md`를 손으로 따라가기 전에, 거기 나오는 명령이 **무엇을 하는 건지** 알기 위한
> 문서다. 우리 서버(Ubuntu 22.04)에서 실제로 쓰는 것만 담는다. AWS 쪽은 `AwsCli.md`.

## 프롬프트 읽는 법 — 지금 내가 어디서 누구인가

```
ubuntu@ip-10-0-4-155:~$
  │        │          │└ $ = 일반 사용자 (# 이면 root)
  │        │          └ 현재 위치 (~ = 내 홈 폴더 /home/ubuntu)
  │        └ 서버 이름 (비공개 IP가 이름에 들어 있다)
  └ 로그인한 사용자
```

- **`PS C:\…>` 가 보이면 내 PC다** — 서버 명령을 치기 전에 프롬프트부터 본다
  (EC2 재생성 날 실제로 겪은 혼동).
- 서버에서 나가기: `exit` (또는 Ctrl+D).

## sudo — 관리자 권한으로 실행

리눅스는 시스템 변경(패키지 설치, 서비스 제어, `/etc` 수정)에 **root** 권한을 요구한다.
root로 상시 로그인하는 대신, 허용된 사용자가 **명령 단위로** 권한을 빌리는 게 `sudo`다.

```bash
apt-get install nginx          # 실패 — Permission denied
sudo apt-get install nginx     # 성공 — 이 명령만 root 권한으로
```

- `ubuntu` 계정은 sudo가 허용돼 있다 (비밀번호 없이 — EC2 기본 설정).
- **함정**: `sudo`로 만든 파일은 소유자가 root라, 나중에 일반 권한으로 못 고친다.
  파일 편집에 습관적으로 sudo를 붙이지 말고, **시스템 영역을 만질 때만** 쓴다.

---

## 패키지 관리 (apt) — 프로그램은 저장소에서 받는다

### 개념 — Windows와 뭐가 다른가

Windows는 설치 파일(msi/exe)을 사이트에서 받아 실행하지만, 리눅스는
**저장소(repository)** 라는 공식 패키지 서버에서 명령으로 받는다.
**가게에 비유하면 전체 흐름이 잡힌다:**

| 비유 | 명령 (ManualSetup의 실제 예) | 이 시점에 서버에 생기는 것 |
|---|---|---|
| 가게를 단골 목록에 등록 | `echo "deb …" \| sudo tee /etc/apt/sources.list.d/…` | 주소가 적힌 **텍스트 파일 하나** — 프로그램 없음 |
| 가게들의 **상품 카탈로그** 수집 | `sudo apt-get update` | 카탈로그(`/var/lib/apt/lists`) — **아직 프로그램 없음** |
| 실제 **구매·설치** | `sudo apt-get install -y mssql-server` | **여기서 처음으로** 프로그램 본체가 내려와 설치된다 |

**저장소 등록도, update도 프로그램을 받지 않는다.** SQL Server 저장소를 등록하고
update까지 마쳐도 서버에는 "packages.microsoft.com에 mssql-server라는 패키지가 있다"는
**정보만** 있다. 본체를 내려받는 건 `install`이 유일하다.

### install이 실제로 하는 일 — 5단계

```
sudo apt-get install -y mssql-server
  ① 의존성 계산     — 카탈로그를 보고 "이걸 설치하려면 저것도 필요하다" 목록 완성
  ② 내려받기       — 패키지 파일(.deb)들을 저장소에서 다운로드
  ③ 배치           — 압축을 풀어 파일들을 제자리에 (/opt/mssql, /usr/bin, /etc …)
  ④ 설치 스크립트   — 패키지에 든 후처리 실행 (서비스 등록, 계정 생성 등)
  ⑤ 대장 기록       — "설치됨"을 dpkg 데이터베이스에 기록 (제거·업그레이드의 근거)
```

- **의존성 자동 해결(①)이 apt의 핵심 가치**다 — nginx 하나를 설치하면 nginx가
  필요로 하는 라이브러리들이 알아서 딸려 온다. Windows처럼 "OO 런타임을 먼저
  설치하세요"를 사람이 챙기지 않는다.
- **설치 ≠ 실행.** `mssql-server`는 설치가 끝나도 **안 떠 있는 게 정상**이다
  (`systemctl status mssql-server` → not running). 에디션·sa 비밀번호를 정하는
  초기 설정을 마쳐야 서비스가 시작된다 — "설치는 apt의 일, 실행은 systemd의 일"로
  역할이 나뉘어 있다.
- 어디에 설치됐는지 보려면: `dpkg -L mssql-server` (그 패키지가 배치한 파일 전체 목록).

### 기본 4명령

```bash
sudo apt-get update              # ① 카탈로그 갱신 (설치·변경 없음)
sudo apt-get upgrade -y          # ② 설치된 패키지 전부를 새 버전으로 업그레이드
sudo apt-get install -y nginx    # ③ 설치
sudo apt-get remove nginx        # ④ 제거 (설정 파일까지 지우려면 purge)
```

- **`update`는 "업데이트"가 아니다** — 카탈로그만 갱신한다. 실제 업그레이드는 `upgrade`.
  이름이 오해를 부르는 대표 사례.
- `-y` = 중간의 "계속할까요? [Y/n]" 질문에 전부 yes. **스크립트에선 필수**
  (질문에서 멈추면 자동화가 죽는다). 손으로 칠 때는 빼고 물어보게 두는 것도 좋다.
- 부속 명령: `apt-get autoremove` (의존성으로 딸려왔다가 이제 안 쓰는 것 정리),
  `apt list --installed | grep nginx` (설치 확인), `apt-cache search 키워드` (검색),
  `apt-cache policy 패키지` (어느 저장소의 어느 버전이 잡히는지 — ManualSetup 3단계의 확인 명령).

### ManualSetup에서 보게 되는 변형들

```bash
# 한 번에 여러 개 — 나열하면 전부 설치된다 (2단계 기본 패키지)
sudo apt-get install -y curl gnupg ca-certificates apt-transport-https software-properties-common unzip

# 환경변수를 앞에 붙이는 형태 — "이 명령 한 번만" 적용되는 설정
sudo ACCEPT_EULA=Y apt-get install -y mssql-tools18 unixodbc-dev
export DEBIAN_FRONTEND=noninteractive   # 이건 세션 전체에 적용 (설치 중 파란 설정 화면 억제)
```

- `ACCEPT_EULA=Y` — 라이선스 동의 질문에 미리 답해 두는 것. 없으면 동의 화면에서 멈춘다.
- `명령 앞의 변수=값`은 그 명령 **한 번에만** 적용되고, `export`는 **현재 접속이 끝날 때까지**
  적용된다는 차이가 있다.

### 함정 ① — install 전에 update

새 서버(또는 오랜만에 접속한 서버)에서 **`update` 없이 `install` 하면 404가 난다.**
AMI에 구워진 시점의 목록으로 지금은 사라진 옛 버전 URL을 찾아가기 때문이다.
**모든 설치 세션은 `sudo apt-get update`로 시작**한다고 외워 둔다.

### 함정 ② — apt vs apt-get

같은 시스템의 두 얼굴이다. `apt`는 사람용(진행바·색상, 출력 형식이 버전마다 바뀔 수 있음),
`apt-get`은 스크립트용(출력이 안정적). **문서·스크립트에는 `apt-get`**을 쓰고,
손으로 칠 때는 아무거나 써도 된다.

### 함정 ③ — lock 오류

```
Could not get lock /var/lib/dpkg/lock-frontend
```

다른 apt가 이미 돌고 있다는 뜻이다. 부팅 직후엔 **자동 업데이트가 백그라운드로 돌고
있을 때가 많다** — 몇 분 기다렸다 다시 하면 된다. 강제로 lock 파일을 지우는 해법이
검색에 많이 나오는데, **진행 중인 설치를 깨뜨릴 수 있으니 기다리는 게 우선**이다.

### 서드파티 저장소 추가 — SQL Server·.NET이 이 방식이다

우분투 공식 저장소에 없는 프로그램(Microsoft 제품 등)은 **제조사 저장소를 등록**하고 받는다.
등록은 두 가지로 이루어진다: **① 서명 키**("이 패키지가 진짜 Microsoft 것인가" 검증용) +
**② 저장소 주소**. 그 뒤에는 평소처럼 `update` → `install`.

#### ① 서명 키 등록 — 명령 해부

```bash
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
  | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
```

한 줄씩이 아니라 **한 조각씩** 읽는다:

| 조각 | 뜻 |
|---|---|
| `curl` | URL로 HTTP 요청을 보내는 도구 — 여기선 키 파일을 내려받는다 |
| `-f` | **fail** — 404 같은 HTTP 오류면 오류 페이지(HTML)를 출력하지 말고 실패로 끝내라. 없으면 **오류 페이지가 키 파일로 저장되는 사고**가 난다 |
| `-s` | **silent** — 진행바·통계 숨김 (파이프로 넘길 때 잡음 제거) |
| `-S` | **show-error** — silent여도 진짜 오류 메시지는 보여라 (`-s` 단독이면 실패도 조용해서 원인을 모른다) |
| `-L` | **location** — 서버가 "주소 옮겼음(리다이렉트)"이라 답하면 따라가라 |
| `\` (줄 끝) | 명령이 다음 줄로 이어진다는 표시 — 두 줄이지만 한 명령이다. **`\` 뒤에 공백이 붙으면 깨진다** |
| `\|` | 파이프 — curl이 받은 내용을 화면이 아니라 **다음 명령의 입력으로** 넘긴다 |
| `sudo gpg --dearmor` | 키를 텍스트 형식에서 **바이너리 형식으로 변환**. `.asc`는 이메일에 붙일 수 있게 만든 텍스트 포장(`-----BEGIN PGP PUBLIC KEY BLOCK-----`)이고, apt는 바이너리(`.gpg`)를 요구한다. "armor(포장)를 벗긴다(de-)"는 이름 그대로다 |
| `-o /usr/share/keyrings/microsoft-prod.gpg` | 변환 결과를 이 파일로 저장. `/usr/share/keyrings/`가 **저장소별 키를 두는 관례 위치**다 |

- **sudo가 `gpg` 쪽에만 붙은 이유** — 내려받기(curl)는 아무나 해도 되지만,
  `/usr/share/keyrings/`는 시스템 영역이라 **쓰는 쪽만** root가 필요하다.
  파이프 조합에서 sudo는 "권한이 필요한 조각에만" 붙인다 (최소 권한 원칙).
- **왜 키를 저장소마다 따로 두나** — 옛 방식(`apt-key`)은 키를 전역 신뢰 목록에
  넣어서, 등록한 키가 **모든 저장소의 패키지에 서명할 수 있는** 과도한 권한을 가졌다.
  지금 방식은 저장소 주소(②)에 `signed-by=키경로`를 붙여 **"이 저장소는 이 키만"**으로
  범위를 좁힌다. `apt-key`는 폐기(deprecated)됐다 — 검색하면 아직 많이 나오니 주의.

#### ② 저장소 주소 등록

```bash
echo "deb [arch=amd64,arm64,armhf signed-by=/usr/share/keyrings/microsoft-prod.gpg] \
https://packages.microsoft.com/ubuntu/22.04/mssql-server-2022 jammy main" \
  | sudo tee /etc/apt/sources.list.d/mssql-server-2022.list
```

`echo "저장소 정의 한 줄"`을 파이프로 넘겨 파일에 쓰는 구조다. 조각별로:

- `sudo tee 파일` — 파이프로 받은 내용을 **root 권한으로 파일에 쓴다**.
  `sudo … > 파일`이 안 되는 자리의 관용구다 (`>` 리다이렉트는 sudo 밖에서 일어나기 때문).
- `/etc/apt/sources.list.d/` — 추가 저장소 목록을 **파일 하나당 저장소 하나**로 두는 위치.
  나중에 `ls`로 "이 서버에 어떤 서드파티 저장소를 등록했더라"를 볼 수 있다.

따옴표 안의 **`deb` 줄이 저장소 정의의 본체**다:

```
deb [arch=amd64,... signed-by=키경로] https://packages.microsoft.com/ubuntu/22.04/mssql-server-2022 jammy main
 │        │              │                │                                                          │     │
 │        │              │                │                                                          │     └ 컴포넌트(구획)
 │        │              │                │                                                          └ 코드네임 — 22.04=jammy (버전 종속!)
 │        │              │                └ 저장소 루트 URL
 │        │              └ 이 저장소는 ①의 키가 한 서명만 인정 (범위 제한)
 │        └ 지원 CPU 목록 — 우리 서버는 amd64만 해당, 나머지는 무시된다
 └ "바이너리 패키지 저장소" 선언 (deb-src는 소스코드용)
```

SQL Server 설치 때는 같은 패턴으로 **저장소를 둘** 등록한다 —
`mssql-server-2022`(SQL Server 엔진 전용)와 `prod`(Microsoft 공용 제품:
`sqlcmd`가 든 mssql-tools, .NET 런타임 등). 엔진과 도구가 딴 창고에 있다.

#### ③ `update`가 등록을 "반영"한다 — 원리

```bash
sudo apt-get update
sudo apt-get install -y mssql-server
```

②까지는 **파일을 써 놓았을 뿐, apt는 아직 아무것도 모른다.** `update`가 하는 일:

```
① sources.list + sources.list.d/*.list 전부 읽기   ← 방금 쓴 파일이 처음 읽히는 순간
② 각 저장소 URL에서 패키지 인덱스(카탈로그) 내려받기
③ 인덱스 서명을 signed-by 키로 검증                ← ①의 microsoft-prod.gpg가 쓰인다
④ /var/lib/apt/lists/ 에 저장                      ← install은 이 로컬 카탈로그를 본다
```

- 출력을 읽는 법: 우분투 공식 저장소들은 `Hit`(변한 것 없음), 방금 등록한
  저장소는 `Get`(카탈로그 첫 수집)으로 나온다.
- **등록(파일 쓰기)과 반영(카탈로그 수집)이 분리**돼 있어서, `update`를 빼먹으면
  `install`이 "Unable to locate package"로 실패한다 — 저장소에 뭐가 있는지
  아직 모르는 상태이기 때문. "install 전에 update" 규칙이 여기서도 같은 원리로 적용된다.

> **26.04 AMI를 버리고 22.04로 재생성한 이유가 바로 ②다** — URL의 `ubuntu/22.04/`와
> 코드네임 `jammy`가 전부 OS 버전에 묶여 있는데, Microsoft가 26.04용 경로를
> 제공하지 않아 ②에서 막힌다. 저장소 등록형 설치는 **OS 버전에 종속**된다.

---

## 자주 쓰는 명령

### 이동·확인

```bash
pwd                    # 지금 어디인가 (print working directory)
ls                     # 목록 (-l 상세, -a 숨김 포함 — 합쳐서 ls -la)
cd /var/www            # 이동 (cd 만 치면 홈으로, cd .. 은 한 단계 위로)
```

- 경로: `/`로 시작하면 **절대 경로**, 아니면 현재 위치 기준 **상대 경로**. `~` = 내 홈.
- **리눅스는 대소문자를 구분한다** — `README.md`와 `readme.md`는 다른 파일.
  (CLAUDE.md 파일 네이밍 규칙에서 "Windows에서 멀쩡하던 참조가 서버에서 깨진다"가 이것.)
- **Tab 자동완성을 쓴다** — 경로를 끝까지 치지 않는다. 두 번 누르면 후보 목록.
  오타 방지가 아니라 **"그 파일이 실제로 있다"는 확인**이 된다.

### 파일 내용 보기

```bash
cat 파일               # 전체 출력 (짧은 파일용)
less 파일              # 페이지 단위로 보기 (q 종료, / 검색, G 끝으로)
head -5 파일           # 처음 5줄 — 파이프 뒤에 붙여 "앞부분만" 볼 때 많이 쓴다
tail -30 파일          # 마지막 30줄 — 로그 확인의 기본
tail -f 파일           # 실시간 따라가기 (Ctrl+C로 중단) — 로그 지켜볼 때
grep "문자열" 파일      # 파일 안 검색 (-r 폴더 재귀, -i 대소문자 무시, -n 줄번호)
```

### 파일 조작

```bash
mkdir -p a/b/c         # 폴더 생성 (-p: 중간 경로까지 한 번에, 있어도 오류 없음)
cp 원본 대상            # 복사 (-r 폴더째)
mv 원본 대상            # 이동 = 이름 변경 (같은 명령이다)
rm 파일                # 삭제 (-r 폴더째)
```

> **`rm`에는 휴지통이 없다 — 즉시, 영구히 지워진다.** 특히 `sudo rm -r`은
> 경로 오타 하나로 시스템을 부술 수 있다. 치기 전에 `ls`로 대상을 먼저 확인하는
> 습관을 들인다.

### 편집 — nano

```bash
sudo nano /etc/nginx/sites-available/playground
```

서버에서 설정 파일을 고칠 때 쓴다. 화면 하단에 단축키가 늘 표시된다:
**Ctrl+O 저장 → Enter → Ctrl+X 종료**. (vim이 표준 교양이지만 배움 비용이 있어,
당장은 nano로 충분하다. 실수로 vim이 열렸다면 `:q!` + Enter가 "저장 없이 탈출"이다.)

### 서비스 (systemd) — 배포 후 매일 쓰게 된다

서버 프로그램(앱·nginx·SQL Server·Redis)은 켜 놓고 잊는 게 아니라 **서비스**로 등록해
**systemd**가 관리한다. systemd는 부팅 때 가장 먼저 뜨는 관리자 프로세스로,
등록된 서비스들의 **시작 순서·자동 시작·죽으면 재시작·로그 수집**을 책임진다.
`systemctl`은 그 관리자에게 말을 거는 명령이다.

등록은 **유닛 파일**(서비스 정의서)로 한다 — 무엇을 실행하고, 어느 계정으로, 죽으면
어떻게 할지가 적힌 텍스트다. apt로 설치한 것들은 자동 등록되고(`nginx`, `mssql-server`,
`redis-server`), 우리 앱은 직접 만든 `playground.service`를 `/etc/systemd/system/`에 둔다.

#### 두 축을 구분하는 게 전부다 — "지금" vs "부팅할 때"

| | 지금 당장 | 부팅할 때 |
|---|---|---|
| 켜기 | `sudo systemctl start nginx` | `sudo systemctl enable nginx` |
| 끄기 | `sudo systemctl stop nginx` | `sudo systemctl disable nginx` |

**`enable`은 지금 시작하지 않고, `start`는 부팅 등록을 하지 않는다.** 완전히 독립이다.
ManualSetup 7단계에서 Redis에 `enable`과 `restart`를 **둘 다** 치는 이유가 이것이다 —
하나만 하면 "지금은 도는데 재부팅하면 죽어 있는" 또는 그 반대 상태가 된다.
(`enable --now`로 한 번에 할 수도 있다.)

그 외 상태 변경:

```bash
sudo systemctl restart playground   # 껐다 켠다 — 순간 끊긴다. 코드·환경변수 반영은 이것
sudo systemctl reload nginx         # 설정 파일만 다시 읽는다 — 무중단. Nginx 설정 반영은 이것
```

`reload`는 프로그램이 지원할 때만 있다 (Nginx는 지원, 우리 앱은 restart만).

#### status 출력 읽는 법

```bash
systemctl status mssql-server --no-pager
```

```
● mssql-server.service - Microsoft SQL Server Database Engine
     Loaded: loaded (/lib/systemd/system/mssql-server.service; enabled; ...)
     Active: active (running) since Mon 2026-08-03 07:12:01 UTC; 2h ago
   Main PID: 1234 (sqlservr)
     Memory: 1.1G
     CGroup: ...
   ... (마지막에 최근 로그 몇 줄)
```

| 줄 | 보는 것 |
|---|---|
| `Loaded:` | 유닛 파일 경로 + **`enabled`/`disabled`** — 부팅 시 자동 시작 여부는 여기서 본다 |
| `Active:` | **현재 상태** + 언제부터. `since`가 방금이면 "최근에 (재)시작됐거나 죽었다 살아났다"는 단서 |
| `Main PID:` | 실제 프로세스 — `ps`에서 보이는 그 번호 |
| `Memory:` | 이 서비스가 쓰는 메모리 — t3.medium 4GB에서 누가 얼마나 먹는지 볼 때 |

`Active:` 값 세 가지를 구분한다:

- `active (running)` — 정상 가동
- `inactive (dead)` — 꺼져 있음 (설치 직후의 mssql-server처럼 **의도된 정지일 수도** 있다)
- **`failed`** — 시작하려다 죽었다. 아래 journalctl로 원인을 본다

부속 명령:

- `--no-pager` — 출력을 페이저(q로 닫는 화면)에 가두지 말고 그대로 쏟아라.
  **스크립트나 여러 서비스를 한 번에 볼 때** 필수 (`systemctl status nginx redis-server --no-pager`).
- `systemctl is-active nginx` / `is-enabled nginx` — 답이 단어 하나(`active`/`enabled`)라
  스크립트 판정용.
- `status`는 sudo 없이 된다. **상태를 바꾸는 것**(start/stop/restart/enable)만 sudo.

#### 로그 — journalctl

systemd가 모든 서비스의 표준 출력을 모아 둔다. `-u 서비스명`으로 골라 본다:

```bash
journalctl -u playground -n 100 --no-pager   # 최근 100줄 — "왜 failed지?"의 첫 명령
journalctl -u playground -f                  # 실시간 따라가기 (tail -f 의 서비스판, Ctrl+C 중단)
journalctl -u mssql-server --since "10 min ago"
```

> 서비스가 `failed`일 때의 순서: `systemctl status 서비스` (마지막 로그 몇 줄에 힌트)
> → 부족하면 `journalctl -u 서비스 -n 100`. 증상별 대응은 `Deploy/Runbook.md`.

#### 유닛 파일을 고쳤다면 — daemon-reload

`/etc/systemd/system/playground.service` 자체를 수정한 경우, systemd는 파일을
다시 읽지 않는다. **정의서 변경은 별도 신고가 필요하다:**

```bash
sudo systemctl daemon-reload          # 유닛 파일 재독취 (서비스 재시작은 아님)
sudo systemctl restart playground     # 그다음 재시작해야 반영
```

`reload`(서비스의 설정 파일 재독취)와 `daemon-reload`(systemd의 유닛 파일 재독취)는
이름이 비슷하지만 **대상이 다르다** — 전자는 nginx.conf, 후자는 playground.service.

### 자원·프로세스 — "서버가 왜 느리지"의 출발점

```bash
df -h /                # 디스크 여유 (-h: GB 단위로 사람이 읽게)
free -h                # 메모리 여유
top                    # 실시간 CPU·메모리 순위 (q 종료)
ps aux | grep dotnet   # 특정 프로세스가 떠 있나
```

`|`(파이프)는 **앞 명령의 출력을 뒤 명령의 입력으로** 넘긴다. `ps aux`(전체 프로세스)를
`grep dotnet`(dotnet 포함 줄만)으로 걸러 보는 식 — 리눅스 명령 조합의 기본 문법이다.

### 네트워크 확인

```bash
ss -tlnp                          # 어떤 포트가 열려 있고 누가 잡고 있나
curl http://localhost:5000        # 서버 안에서 앱이 응답하는지 (브라우저 없이 HTTP 요청)
```

**"밖에서 안 열려요" 디버깅의 첫 단계가 `ss`다** — 포트에 프로세스가 없으면 앱 문제,
있는데 밖에서 안 되면 방화벽(보안 그룹·ufw) 문제로 갈린다.

---

## 시스템 관리 — ManualSetup에서 만나는 명령들

### `~ctl` 가족 — systemd 계열의 제어 명령

이름이 `ctl`(control)로 끝나는 명령들은 **systemd가 관리하는 영역별 제어 도구**다.
`systemctl`(서비스)을 배웠으면 나머지는 같은 감각으로 읽힌다:

```bash
timedatectl                        # 시간·시간대 보기 (인자 없이 치면 "보기"다)
sudo timedatectl set-timezone UTC  # 시간대 변경 — ManualSetup 1단계
hostnamectl                        # 서버 이름·OS·커널 버전 한눈에
journalctl -u playground -n 100    # 로그 (위 systemd 절 참조)
```

- 공통 패턴: **인자 없이 치면 현재 상태 보기**(sudo 불필요), `set-…` 등
  바꾸는 동사가 붙으면 변경(sudo 필요).
- 서버 시간대를 UTC로 두는 이유는 `Servers.md`·CLAUDE.md 참조 — 호스트 TZ에
  로직을 맞추지 않는다는 프로젝트 규칙과 연결되어 있다.

### 사용자와 권한 — `ls -l` 읽는 법부터

```bash
ls -l /var/www
-rw-r--r-- 1 playground playground  1234 Aug  3 07:00 appsettings.json
│└┬┘└┬┘└┬┘   └─ 소유자    └─ 그룹
│ │  │  └ 나머지 모두(other)의 권한: r--  (읽기만)
│ │  └ 그룹의 권한: r--
│ └ 소유자의 권한: rw-  (읽기+쓰기)
└ 종류: - 파일, d 폴더, l 링크
```

`r`(read) · `w`(write) · `x`(execute)의 3벌 구성이다. 숫자로도 쓴다 —
r=4, w=2, x=1을 더해서 `rw-r--r--` = `644`, `rwx------` = `700`.

```bash
chmod 400 키.pem                       # 소유자 읽기만 — SSH가 키에 요구하는 상태
                                       # (Windows에서 icacls로 한 것의 리눅스판)
sudo chown -R playground:playground /var/www/playground   # 소유자:그룹 변경 (-R 하위 전부)
```

**서비스용 계정 만들기** (ManualSetup 10단계):

```bash
sudo useradd --system --no-create-home --shell /usr/sbin/nologin playground
```

- `--system` — 사람용이 아니라 서비스용 계정
- `--no-create-home` — 홈 폴더를 만들지 않는다 (로그인할 일이 없으니 필요도 없다)
- `--shell nologin` — **이 계정으로는 로그인 자체가 불가**. 앱이 뚫려도 셸을 못 얻는다
- 앱을 root로 돌리지 않기 위한 장치다 — 계정을 따로 만들고 `chown`으로
  앱 폴더만 그 계정 소유로 준다. "뚫려도 그 폴더까지만"

### 심볼릭 링크 — 파일인 척하는 이정표

#### 무엇인가

심볼릭 링크(symlink)는 **내용 대신 "다른 파일의 경로"를 담고 있는 특수 파일**이다.
열면 운영체제가 그 경로를 따라가 **원본을 연 것과 완전히 같게** 동작한다 —
실행하면 원본이 실행되고, 읽고 쓰면 원본이 읽히고 써진다.

```bash
ls -l /usr/local/bin/sqlcmd
lrwxrwxrwx 1 root root 30 ... /usr/local/bin/sqlcmd -> /opt/mssql-tools18/bin/sqlcmd
│                                                    └ 화살표가 "어디를 가리키는지"
└ 첫 글자 l = 링크
```

- Windows 바로가기(.lnk)와 비슷하지만 더 강력하다 — 바로가기는 탐색기만 이해하는
  파일이지만, 심볼릭 링크는 **운영체제 수준에서 투명**해서 모든 프로그램이
  원본처럼 취급한다.
- **복사본이 아니다.** 원본이 바뀌면 링크로 여는 내용도 즉시 같이 바뀐다.
  용량도 사실상 0 (경로 문자열 길이만큼).
- 원본이 지워지면 링크는 **끊어진 이정표**(dangling link)로 남는다 — 열면
  "No such file or directory". `ls -l`로 화살표 끝이 실재하는지 확인한다.

#### 만드는 법 — 인자 순서가 전부다

```bash
sudo ln -sf /opt/mssql-tools18/bin/sqlcmd /usr/local/bin/sqlcmd
#           └ ① 원본 (실재하는 파일)      └ ② 만들 링크 (새 이름)
sudo ln -sf /opt/mssql-tools18/bin/bcp /usr/local/bin/bcp
```

- **"원본 먼저, 링크 나중"** — `cp 원본 대상`과 같은 어순이라 외우기 쉽다.
  거꾸로 쓰면 오류 없이 **엉뚱한 방향의 링크**가 생기니 주의.
- `-s` — **s**ymbolic. 이걸 빼면 하드 링크라는 다른 것이 생긴다
  (같은 실체에 이름을 하나 더 붙이는 것 — 쓸 일이 오면 그때 배워도 된다).
- `-f` — **f**orce. 같은 이름이 이미 있으면 지우고 다시 만든다.
  설치 스크립트를 **두 번 돌려도 안전**하게 만드는 장치다.
- `readlink -f /usr/local/bin/sqlcmd` — 링크를 끝까지 따라간 실제 경로 확인.

#### 왜 여기서 쓰나 — PATH 문제의 해법

명령을 치면 셸은 아무 데서나 찾는 게 아니라 **PATH 환경변수에 등록된 폴더들에서만**
실행 파일을 찾는다:

```bash
echo $PATH        # /usr/local/bin:/usr/bin:/bin:...  ← 이 폴더들만 뒤진다
```

mssql-tools는 `/opt/mssql-tools18/bin`에 설치되는데 **그 폴더는 PATH에 없다.**
그래서 설치가 멀쩡히 끝났는데도 `sqlcmd`를 치면 "command not found"가 난다 —
파일이 없는 게 아니라 **셸이 안 찾아보는 곳에 있는 것**이다. 해법은 둘:

| 방법 | 단점 |
|---|---|
| PATH에 폴더 추가 (`~/.bashrc` 수정) | **그 사용자 한정** — playground 계정·root·스크립트에는 적용 안 됨 |
| **PATH 안 폴더에 링크 생성** ← 채택 | 없음에 가깝다 — 모든 사용자·스크립트에 즉시 적용 |

`/usr/local/bin`은 "관리자가 직접 추가한 명령"을 두는 관례 위치로 **모든 사용자의
PATH에 기본 포함**되어 있다. 여기에 이정표만 세우면 끝난다:

```bash
which sqlcmd      # /usr/local/bin/sqlcmd — 셸이 이제 찾는다 (설치 검증 명령이기도)
```

#### 리눅스 전반에서 어떻게 쓰이나 — 우리 서버의 다른 예

심볼릭 링크는 "PATH 해결" 전용이 아니라 **"실체는 한 곳에, 참조는 여러 곳에"**가
필요한 자리 어디에나 쓰인다:

- **Nginx 사이트 켜기/끄기** (우리 배포 구성이 이 방식) — 설정 원본은
  `sites-available/playground`에 두고, `sites-enabled/`에 **링크가 있으면 활성**.
  끌 때는 원본을 지우는 게 아니라 **링크만 지운다**. 파일 삭제 없이 on/off가 된다.

  ```bash
  sudo ln -s /etc/nginx/sites-available/playground /etc/nginx/sites-enabled/
  ```

- **버전 전환** — `dotnet` 같은 도구들이 `tool → tool-10.0.2` 형태로 링크를 걸어 두고,
  업그레이드 때 **링크만 새 버전으로 바꿔치기**한다. 호출하는 쪽은 경로를 바꿀 필요가 없다.
  우리 배포의 `/var/www/playground.prev`(롤백용 직전 버전) 같은 구조도 같은 발상이다.

### 파일 안 문자열을 명령으로 치환 — `sed -i`

설정 파일 한 줄을 고치자고 nano를 여는 대신, 치환을 명령으로 한다 (스크립트화 가능):

```bash
sudo sed -i 's/^supervised .*/supervised systemd/' /etc/redis/redis.conf
#            └ s/찾을패턴/바꿀내용/ — ^는 "줄 시작", .*는 "그 뒤 전부"
```

- `-i` (in-place) — 결과를 화면에 뿌리지 않고 **파일 자체를 수정**한다.
- ManualSetup 7단계에서 Redis의 `bind` 줄을 두 번 치환하는 이유: 원본에서 그 줄이
  **주석(`# bind …`)인 경우와 아닌 경우가 둘 다 있어서** 패턴을 두 벌 돌린다.
- 함정: 패턴이 안 맞으면 **조용히 아무것도 안 바꾼다** (오류 없음). 치환 후
  `grep`으로 결과를 확인하는 습관이 필요하다.

### 호스트 방화벽 — `ufw`

```bash
sudo ufw allow OpenSSH        # 규칙 추가를 먼저!
sudo ufw show added           # OpenSSH가 있는지 눈으로 확인한 뒤
sudo ufw --force enable       # 켜는 건 마지막
sudo ufw status numbered      # 현재 규칙
```

- **allow보다 enable을 먼저 치면 SSH가 끊겨 못 들어온다** — ManualSetup 11단계가
  "유일하게 위험한 단계"인 이유. 순서가 전부다.
- **방화벽이 둘이다** — AWS 보안 그룹(밖)과 ufw(호스트 안). 둘 다 통과해야 하고,
  어느 쪽이 막혀도 증상은 똑같이 "시간 초과"다.

### 내 PC ↔ 서버 파일 복사 — `scp`

ssh와 같은 키로 파일을 나른다 (배포 전 수동 확인 등):

```powershell
# 내 PC(PowerShell)에서 — 순서는 "원본 대상", 서버 쪽은 주소:경로 형태
scp -i D:\...\playground-prod.pem 파일.txt ubuntu@54.180.64.167:/home/ubuntu/
scp -i D:\...\playground-prod.pem ubuntu@54.180.64.167:/var/log/playground/app.log .
```

### 검증용 단발 명령들 — "설치가 잘 됐나"

ManualSetup의 각 단계 끝에는 확인 명령이 하나씩 붙어 있다. **설치 후 즉시 검증**이
습관이 되면, 문제가 몇 단계 뒤에서 터져 원인을 못 찾는 상황이 없어진다:

```bash
lsb_release -a                        # OS 버전·코드네임 (Codename: jammy) — 모든 것의 전제
redis-cli ping                        # Redis 왕복 확인 — PONG이 오면 살아 있는 것
dotnet --list-runtimes                # 설치된 .NET 런타임 목록
fc-list :lang=ko | head -3            # 한국어 지원 폰트 목록 — 몇 개만 나와도 OK
sudo fc-cache -f                      # 폰트 캐시 재생성 — 폰트 "설치 직후" 한 번 (f=강제)
apt-cache policy mssql-server         # 이 패키지가 어느 저장소의 어느 버전으로 잡히나
```

- 패턴이 보일 것이다 — **설치한 것에게 직접 말을 걸어 본다** (`redis-cli ping`,
  `dotnet --list-runtimes`, `which sqlcmd`). 프로그램마다 자기 확인 명령이 있고,
  없으면 `systemctl status`가 대타다.
- `fc-*`는 폰트 캐시(fontconfig) 도구다. 폰트 파일을 넣어도 캐시를 갱신해야
  프로그램(SkiaSharp)이 인식한다 — "설치 ≠ 반영"의 또 다른 예.
- `fc-cache: command not found`가 나면 fontconfig 패키지가 없는 것이다
  (`sudo apt-get install -y fontconfig`). 우분투는 이럴 때 **"어느 패키지를 설치하면
  되는지" 힌트를 같이 출력**한다 — 낯선 명령이 없다고 하면 그 힌트부터 읽는다.

---

## 마무리 — 명령이 기억 안 날 때

```bash
man ls                 # 매뉴얼 (q 종료) — 모든 명령의 원본 문서
ls --help              # 짧은 요약판
history                # 내가 쳤던 명령 목록 (!번호 로 재실행)
```

- **Ctrl+R** — 친 적 있는 명령을 검색해서 재사용. 긴 명령을 다시 칠 일이 없어진다.
- **위/아래 화살표** — 직전 명령 재호출.

---

## 부록 — ManualSetup 명령 색인

`Deploy/ManualSetup.md`를 따라가다 명령이 낯설면 여기서 찾는다.

| ManualSetup 단계 | 명령 | 이 문서의 설명 위치 |
|---|---|---|
| 시작 전 | `ssh -i` | 프롬프트 읽는 법 (접속 값·config 등록은 `Deploy/Servers.md`) |
| 시작 전 | `lsb_release -a` | 검증용 단발 명령들 |
| 1. 시간대 | `timedatectl set-timezone` | `~ctl` 가족 |
| 2. 기본 패키지 | `export DEBIAN_FRONTEND=…` · `apt-get update` · `install -y` | 패키지 관리 — 기본 4명령 · ManualSetup에서 보게 되는 변형들 |
| 3. 저장소 등록 | `curl -fsSL \| gpg --dearmor` · `echo \| tee` · `deb …` 줄 · `apt-cache policy` | 서드파티 저장소 추가 ①②③ |
| 4. SQL Server | `apt-get install mssql-server` · `systemctl status --no-pager` | install이 실제로 하는 일 5단계 ("설치 ≠ 실행") · status 출력 읽는 법 |
| 5. sqlcmd·bcp | `ACCEPT_EULA=Y …` · `ln -sf` · `which` | 변형들 (명령 앞 변수=값) · 심볼릭 링크 |
| 6. .NET 런타임 | `dotnet --list-runtimes \| grep` | 검증용 단발 명령들 · 파이프는 "자원·프로세스" 절 |
| 7. Redis | `sed -i` · `systemctl enable`+`restart` · `redis-cli ping` | sed -i · systemd "두 축" (enable≠start) · 검증용 단발 명령들 |
| 8. Nginx | `apt-get install` · `systemctl enable` | 패키지 관리 · systemd. 사이트 on/off 구조는 "심볼릭 링크 — 리눅스 전반" |
| 9. 한글 폰트 | `fc-cache -f` · `fc-list :lang=ko \| head` | 검증용 단발 명령들 |
| 10. 앱 계정 | `useradd --system --shell nologin` · `mkdir -p` · `chown -R` | 사용자와 권한 · 파일 조작 |
| 11. 방화벽 | `ufw allow` → `show added` → `--force enable` → `status` | 호스트 방화벽 ufw (allow 먼저, enable 마지막) |
| 끝 확인 | `systemctl status … --no-pager` 묶음 | status 출력 읽는 법 · 검증용 단발 명령들 |
