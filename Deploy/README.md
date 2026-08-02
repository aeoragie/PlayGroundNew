# Deploy — 서버에 올라가는 실행물

이 폴더에는 **파일만** 있다. 절차와 근거는 문서에 있다:

- **`Docs/Development/Deployment.md`** — 확정된 선택 · 전체 순서 · 배포 후 확인
- **`Docs/Development/AwsSetup.md`** — AWS 콘솔에서 무엇을 누르는지 (처음이라면 여기부터)

| 파일 | 어디로 가나 |
|---|---|
| `ec2-setup.sh` | EC2 **user-data**에 붙여 넣는다 (시크릿 없음) |
| `deploy-app.sh` | `/usr/local/bin/playground-deploy` |
| `backup-database.sh` | `/usr/local/bin/playground-backup` |
| `playground.service` | `/etc/systemd/system/playground.service` |
| `playground.conf` | `/etc/nginx/sites-available/playground` |

> 파일명이 소문자·하이픈인 이유: **리눅스에서 실행·설치되는 것**이라 그 생태계 관례를 따른다.
> 설치 이름과도 같아진다. 네이밍 규칙 전체는 CLAUDE.md "파일 네이밍".
