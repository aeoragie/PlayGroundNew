# 기능 백로그 (기록만 — 착수 시점 미정)

> 2026-08-09 작성. 배포 이후를 위한 기능 아이디어를 잃어버리지 않게 기록한다.
> **여기 있는 것은 약속이 아니다** — 착수를 결정할 때 범위·우선순위를 다시 정한다.
> 배포 전 필수 항목은 여기가 아니라 `ReleasePlan.md`(R1~R5)에 있다.

## 외부 연동 축

| 기능 | 내용 | 메모 |
|---|---|---|
| 이메일 실제 발송 | `LogOnlyEmailSender` → 실물 어댑터 | **ReleasePlan H1·D3에서 추적 중** (SES 제안) — 여기서는 중복 관리하지 않는다 |
| 핸드폰 인증 + 문자 발송 | 가입·보호자 확인용 본인 인증(SMS OTP 또는 PASS류) + 알림 문자 발송 | 미성년자 서비스라 보호자 실명 확인 요구가 생길 수 있다. 후보: 국내 SENS·해외 SNS/Twilio. `IEmailSender`처럼 포트 뒤 어댑터로 |
| 푸시 알림 | 현재 알림 센터는 인앱 전용 — 브라우저 웹푸시/FCM으로 앱 밖 도달 | 알림 설정의 `PushChannel` 항목이 이미 자리를 잡아 둔 상태(현재는 인앱 스위치) |
| 결제 | 회비·구독 등 결제 플로우 | PG 선정(토스페이먼츠·아임포트 등)부터. 팀 회비(`MonthlyFee`) 컬럼이 이미 있으나 표시 전용 |
| 유튜브·인스타 연동 | API 키/유저 토큰을 받아 **본인이 올린 게시물·영상 일부를 가져와 랜덤 노출** | 팀 채널(`SoccerTeamChannels`)·선수 포트폴리오가 URL을 이미 보관 — 그 URL의 콘텐츠를 읽어오는 확장. YouTube Data API·Instagram Graph API, 토큰 보관·갱신 설계 필요. KFA 어댑터처럼 `IExternalContentProvider`류 포트로 |

## 참고 — 이미 자리가 있는 것들

- 알림 설정 어휘: `NotificationPreferenceItem`(PushChannel·EmailChannel·MatchResult·Recruit·Review·VisitSummary)
- 외부 데이터 어댑터 선례: `IExternalMatchProvider`(KFA, 설계 결정 5), `IObjectStore`(스토리지 벤더 중립)
- 발송 채널 선례: `IEmailSender` — 실패는 부가 작업으로 삼키는 규약
