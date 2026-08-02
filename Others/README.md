# Others — 로컬 개발용 외부 실행물

제품 소스가 아니라 **로컬에서 앱을 돌리는 데 필요한 도구**를 둔다.
운영 환경에서는 관리형 서비스를 쓰므로(예: Redis → AWS ElastiCache) 여기 있는 것은 배포되지 않는다.

## Redis (Windows)

- 버전: **8.10.0** (`Redis-8.10.0-Windows-x64-msys2-with-Service`)
- 출처: <https://github.com/redis-windows/redis-windows> — GitHub Actions로 공개 빌드,
  릴리스 페이지에 해시가 공개된다.
- 받은 파일 SHA256: `525B2AC16F45943973F44B7C49A5DCE2FF5A21B8C1C93EC059F4902B1C5BD622`
  (릴리스 노트의 `Redis-8.10.0-Windows-x64-msys2-with-Service.zip` 값과 일치 확인)

> 공식 Redis는 Windows 네이티브 빌드를 제공하지 않는다. 이건 커뮤니티 빌드다.
> **로컬 개발 전용** — 운영은 ElastiCache를 쓴다.

### 무엇에 쓰나

로그아웃·탈퇴 시 **발급된 JWT를 만료 전에 끊는 데** 쓴다(`ITokenRevocationStore`).
Redis가 없으면 앱은 정상 동작하지만 **무효화만 비활성**된다(fail-open — 설계 결정과 근거는
`Docs/Architecture/DeploymentAndConfiguration.md` 예 3).

### 실행

```bat
Others\Redis\start.bat
```

또는 직접:

```bat
cd Others\Redis
redis-server.exe --port 6379 --save ""
```

부팅 시 자동 실행이 편하면 서비스로 등록한다(관리자 권한):

```bat
sc.exe create Redis binpath=C:\Workspace\PlayGroundNew\Others\Redis\RedisService.exe start= auto
net start Redis
```

### 앱 연결

`appsettings.Development.json`의 `RedisConfig:Connections[0].ConnectionString = "localhost:6379"`로
이미 잡혀 있다. 커넥션 문자열이 비어 있으면 기동 시 경고만 남기고 무효화가 꺼진다.

### 동작 확인

```bat
Others\Redis\redis-cli.exe -p 6379 PING
Others\Redis\redis-cli.exe -p 6379 KEYS auth:*
```

무효화가 동작하면 아래 형태의 키가 보인다:

- `auth:revoked:token:{jti}` — 로그아웃한 토큰 하나 (TTL = 토큰 잔여 수명)
- `auth:revoked:user:{userId}` — 탈퇴 기준 시각 (TTL = 액세스 토큰 수명 + 여유)

### 버전 올리기

릴리스 페이지에서 `*-msys2-with-Service.zip`을 받아 이 폴더를 통째로 교체하고,
**릴리스 노트의 SHA256과 대조한 뒤** 위 버전·해시 기록을 갱신한다.
