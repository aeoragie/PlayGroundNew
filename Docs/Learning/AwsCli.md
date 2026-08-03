# AWS CLI — 설치 · 자격 증명 · 기본 사용

> 콘솔 클릭 대신 명령으로 AWS를 다루기 위한 문서다. `InvalidClientTokenId` 오류에서
> 막힌 이유(자격 증명이 없거나 만료)를 이해하고 처음부터 설정한다.
> 네트워크 개념은 `AwsNetwork.md`, 실제 구축 절차는 `Deploy/AwsSetup.md`.

## 왜 필요한가

콘솔만으로도 구축은 된다. CLI가 필요해지는 순간은:

- **확인이 잦을 때** — "인스턴스가 지금 어느 리전에, 어떤 AMI로 떠 있나"를
  콘솔 로그인 → 리전 전환 → 화면 탐색 없이 한 줄로 본다.
- **기록이 남아야 할 때** — 콘솔 클릭은 재현이 안 되지만 명령은 문서에 붙여넣으면 절차가 된다.
- **자동화할 때** — 배포 스크립트·백업 스크립트가 S3에 올리려면 결국 CLI(또는 SDK)다.

## 큰 그림 — 세 가지가 맞아야 명령이 통한다

```
① 프로그램        aws-cli 실행 파일 (이 PC에 v2 설치됨 — aws --version)
② 자격 증명       "너는 누구냐" — 액세스 키 (IAM 사용자에게 발급)
③ 리전            "어느 서울/버지니아에게 묻느냐" — 기본값 설정 or --region
```

`InvalidClientTokenId`는 **②가 없거나·틀렸거나·만료**됐다는 뜻이다.
프로그램(①)은 멀쩡해도 자격 증명이 죽으면 모든 명령이 이 오류를 낸다.

---

## 1. 설치 확인

이 PC에는 이미 설치되어 있다:

```powershell
aws --version        # aws-cli/2.x.x ... 이면 설치 OK
```

없다면 <https://aws.amazon.com/cli/> 에서 Windows용 MSI를 받는다. **v2를 쓴다**
(v1은 구형 — 명령 체계가 일부 다르고 지원이 끊기는 중).

## 2. 자격 증명 — 액세스 키를 만든다

### 어떤 계정의 키인가 — 루트 키는 절대 만들지 않는다

| 주체 | 액세스 키 | 이유 |
|---|---|---|
| **루트 계정** (가입 이메일) | **금지** | 유출되면 계정 전체 탈취 — 삭제·과금 무제한. AWS도 만들지 말라고 경고한다 |
| **IAM 사용자** | **이걸 쓴다** | 권한을 부여한 만큼만 할 수 있고, 유출 시 그 키만 폐기하면 된다 |

> 우리는 콘솔 작업도 IAM 사용자(관리자 권한 부여)로 하는 것이 원칙이다.
> 루트는 결제 설정·계정 폐쇄 같은 루트 전용 작업에만 쓴다.

### 콘솔에서 키 발급 (최초 1회)

1. 콘솔 로그인 → 우측 상단 계정 메뉴 → **보안 자격 증명** (또는 IAM → 사용자 → 본인 선택)
2. **액세스 키** 섹션 → **액세스 키 만들기**
3. 용도는 **Command Line Interface (CLI)** 선택 → 확인 체크 → 생성
4. **액세스 키 ID**(`AKIA...`)와 **비밀 액세스 키** 두 값을 받는다.
   - **비밀 액세스 키는 이 화면에서만 보여준다.** 창을 닫으면 다시 못 보고 재발급해야 한다.
   - 비밀번호 관리자에 저장한다. **파일로 저장해 git 근처에 두지 않는다.**

### 이 PC에 등록

```powershell
aws configure
```

4개를 묻는다:

| 질문 | 입력 |
|---|---|
| AWS Access Key ID | 발급받은 `AKIA...` |
| AWS Secret Access Key | 발급받은 비밀 키 |
| Default region name | **`ap-northeast-2`** (서울 — 아래 함정 참조) |
| Default output format | `json` (표로 보고 싶을 때만 명령에 `--output table`) |

저장 위치는 홈 폴더다 (프로젝트 폴더가 아니다 — git에 안 들어간다):

```
C:\Users\<나>\.aws\credentials    ← 키 (비밀)
C:\Users\<나>\.aws\config         ← 리전·출력 형식
```

### 확인

```powershell
aws sts get-caller-identity
```

```json
{
    "UserId": "AIDA...",
    "Account": "123456789012",
    "Arn": "arn:aws:iam::123456789012:user/<IAM사용자명>"
}
```

- **Arn이 `user/...`로 끝나면 정상.** `root`면 루트 키를 등록한 것 — 폐기하고 다시.
- 여기서 `InvalidClientTokenId`면 키를 잘못 붙여넣었거나 비활성화된 키다.

---

## 3. 함정

### 리전 기본값 — 버지니아 사건의 CLI 버전

콘솔에서 리전을 잘못 두고 만들면 그쪽에 생기듯, **CLI도 `--region`을 안 주면
기본 리전으로 간다.** 기본값을 `ap-northeast-2`로 박아두는 이유다.
그래도 "조회했는데 아무것도 없다"면 리전부터 의심한다:

```powershell
aws configure get region                      # 기본 리전 확인
aws ec2 describe-instances --region us-east-1 # 버지니아에 남은 게 있나 확인
```

### 비밀 키는 절대 저장소에 넣지 않는다

`.aws\credentials`는 홈 폴더라 안전하지만, **명령을 문서·스크립트에 붙여넣을 때
키가 딸려 들어가는 사고**가 흔하다. GitHub에 올라간 AWS 키는 **수 분 안에**
봇이 수집해 채굴 인스턴스를 돌린다. 키가 노출됐다 싶으면 고민 없이
IAM에서 **비활성화 → 재발급**한다 (무료·즉시).

### 만료 — 언제 다시 `InvalidClientTokenId`를 보나

- `aws configure`로 등록한 IAM 사용자 키는 **만료가 없다** (폐기 전까지 유효).
- 만료되는 것은 **임시 자격 증명**(SSO 로그인, MFA 세션, 역할 전환)이다.
  회사·팀 환경에서 SSO(`aws sso login`)를 쓰면 몇 시간마다 재로그인이 필요하다.
- **우리 경우**: 1인 계정이라 IAM 사용자 키 방식이면 충분하다. 팀이 생기면
  IAM Identity Center(SSO)로 전환을 검토한다 — 키 파일이 각 PC에 남지 않는 장점.

### 프로필 — 계정이 둘 이상일 때

개인/회사 계정을 오가면 `--profile`로 구분한다. 지금은 계정 하나라 불필요.

```powershell
aws configure --profile work          # work라는 이름으로 별도 저장
aws s3 ls --profile work              # 명령마다 지정
```

---

## 4. 명령은 어디서 찾나

CLI 명령은 외우는 게 아니라 **찾아서 조립**한다. 구조를 알면 찾는 곳이 정해진다.

### 명령의 구조 — 3계층

```
aws  ec2  describe-images  --owners ... --filters ... --query ...
 │    │         │                │
 │    │         │                └ 옵션 (명령마다 다르다 — 레퍼런스에서 확인)
 │    │         └ 동작 (동사-명사, 소문자-하이픈)
 │    └ 서비스 (콘솔의 서비스 이름과 거의 일치: ec2, s3, rds, iam, route53)
 └ 프로그램
```

동작 이름에는 관례가 있어서 절반은 추측이 된다:

| 접두어 | 의미 | 예 |
|---|---|---|
| `describe-` | 조회 (EC2 계열) | `describe-instances`, `describe-subnets` |
| `list-` / `get-` | 조회 (S3·IAM 등 신형 서비스) | `list-buckets`, `get-caller-identity` |
| `create-` / `delete-` | 생성 / 삭제 | `create-tags`, `delete-volume` |
| 예외 | 역사적 이름 | 인스턴스 생성은 `create-instances`가 아니라 **`run-instances`**, 종료는 **`terminate-instances`** |

### 찾는 곳 ① — 터미널 `help` (가장 빠르다)

**각 계층 뒤에 `help`를 붙이면 그 계층의 목록/설명이 나온다.**

```powershell
aws help                          # 서비스 전체 목록
aws ec2 help                      # ec2의 동작 전체 목록 — 여기서 이름을 찾는다
aws ec2 describe-images help      # 이 명령의 옵션 전부 + 사용 예제(EXAMPLES 섹션)
```

- 출력이 페이저(`q`로 종료)로 열린다. 길면 `| Select-String owners`처럼 걸러도 된다.
- **EXAMPLES 섹션이 제일 유용하다** — 실제 조합 예가 있어 복사해서 고치면 된다.

### 찾는 곳 ② — 공식 CLI 레퍼런스 (웹)

<https://docs.aws.amazon.com/cli/latest/reference/>

`help`와 같은 내용의 웹 버전. **서비스 → 동작** 순으로 내려가며,
검색은 사이트 검색보다 **구글에 `aws cli describe-images`처럼 명령명으로** 치는 게 빠르다.
페이지 하단 **Examples**부터 본다.

### 찾는 곳 ③ — `--query`는 CLI 문법이 아니라 JMESPath다

`--query "sort_by(Images,&CreationDate)[-1].[ImageId,Name]"` 같은 것은
AWS가 만든 문법이 아니라 **JMESPath**라는 별도 JSON 질의 언어다.

- 튜토리얼·실습장: <https://jmespath.org> (브라우저에서 바로 실험 가능)
- `--filters`와의 차이를 알아야 헤매지 않는다:

| | 어디서 거르나 | 문법 |
|---|---|---|
| `--filters` | **서버**(AWS)가 걸러서 보낸다 | `Name=...,Values=...` (명령마다 지원 필터가 다름 — help에서 확인) |
| `--query` | **내 PC**에서 받은 JSON을 후처리 | JMESPath (모든 명령에서 동일하게 동작) |

전량을 받아 `--query`로만 거르면 느리고, `--filters`만으로는 출력 모양을 못 다듬는다.
**거르기는 filters, 모양 만들기는 query**가 기본 조합이다.

### 찾는 곳 ④ — AI에게 물을 때

명령 조립을 AI에게 시키는 건 빠르지만, **실행 전에 `help`로 옵션이 실재하는지 확인**한다.
버전에 따라 없는 옵션을 만들어내는 일이 있고, 특히 **변경 명령(create/delete/run)은
옵션 하나 차이로 결과가 달라진다.** 조회 명령은 틀려도 오류만 나니 부담 없이 실험해도 된다.

---

## 5. 우리가 당장 쓰는 명령

전부 **조회(읽기)** 명령이다 — 실행해도 아무것도 바뀌지 않는다.

### 인스턴스 상태 — "지금 뭐가 떠 있나"

```powershell
aws ec2 describe-instances --output table --query `
  "Reservations[].Instances[].[InstanceId,State.Name,InstanceType,PublicIpAddress,ImageId]"
```

### AMI 확인 — "이 인스턴스가 정말 22.04(jammy)인가"

```powershell
aws ec2 describe-images --image-ids <위에서 나온 ImageId> `
  --query "Images[].[Name,Description]" --output table
# Name에 ubuntu-jammy-22.04 가 들어 있어야 한다 (noble=24.04, resolute=26.04면 잘못)
```

### Elastic IP — "고정 IP가 어느 인스턴스에 붙어 있나"

```powershell
aws ec2 describe-addresses --output table --query `
  "Addresses[].[PublicIp,InstanceId,AllocationId]"
# InstanceId가 비어 있으면 미연결 상태 — 연결 전까지도 과금된다
```

### 22.04 AMI ID 찾기 — 콘솔에서 헤매지 않는 법

```powershell
aws ec2 describe-images --owners 099720109477 `
  --filters "Name=name,Values=ubuntu/images/hvm-ssd/ubuntu-jammy-22.04-amd64-server-*" `
  --query "sort_by(Images,&CreationDate)[-1].[ImageId,Name]" --output table
# 099720109477 = Canonical 공식 계정. 최신 jammy AMI ID가 나온다
```

> 인스턴스 **생성·종료 같은 변경 작업은 당분간 콘솔로 한다** — 화면에서 무엇을
> 고르는지 눈으로 확인하며 배우는 단계이기 때문. CLI 변경 명령은 절차가
> 손에 익은 뒤 `Deploy/` 스크립트로 옮길 때 도입한다.
