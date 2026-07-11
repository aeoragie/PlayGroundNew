# PlayGround (리뉴얼)

유소년 축구(U12~U18) 선수 이력관리 · 팀관리 플랫폼 — 신규 구축 리포지토리.

- .NET 10 / Blazor WebAssembly / ASP.NET Core Web API / SQL Server
- 클린 아키텍처 (Core 범용 레이어 + PlayGround 전용 레이어)
- 프로젝트 구조·역할·작업 규칙: [CLAUDE.md](CLAUDE.md) 참조

## 빌드 & 실행

```bash
dotnet build PlayGround.slnx
dotnet test PlayGround.slnx
dotnet run --project Source/PlayGround/PlayGround.Server
```
