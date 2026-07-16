# Handoff: 브랜드 로고 (Brand)

> 대상 저장소: `C:\Workspace\PlayGroundNew` · 압축 해제 위치: `Handoff\Design.Brand`
> `Brand Logo.dc.html`이 규정 원본입니다 — 브라우저로 열어 모든 수치·색을 확인하세요.

## 핵심 원칙
**텍스트 전용 브랜드.** 심볼·아이콘·이모지 없이 타이포그래피만 사용. 서체는 UI와 동일한 Plus Jakarta Sans.

## 1. 워드마크 (화면 내 UI 표준)
- 구성: `PlayGround`(800, letter-spacing -0.035em) + 종목명 `Soccer`(700, -0.02em), 베이스라인 정렬, 간격 = 대문자 높이의 약 28%
- 색 — 라이트 배경: PlayGround `#23408e` / 종목명 `#b6bdc9`
- 색 — 다크(네이비) 배경: PlayGround `#FFFFFF` / 종목명 흰색 45%
- 크기: 17px(PC GNB) · 15px(모바일 상단 바) · **최소 12px** (미만 사용 금지, 공간 부족 시 종목명 생략)
- 클리어 스페이스: 사방 대문자 P 높이 1배
- 종목 확장: 접미어만 교체(Soccer/Baseball/…). 종목명 없는 단독형은 전사 커뮤니케이션(약관·채용·투자)에만
- 금지 4종: 아이콘·이모지 결합 / 임의 색 변경(오렌지는 CTA 전용) / 웨이트 변경·타 서체 / 어순 변경·종목명 강조

## 2. 앱 아이콘 · 파비콘 (두 줄 워드마크 아이콘)
- 구성: `Play` / `Ground` 두 줄, 좌측 정렬, 800 + **텍스트 스트로크로 두께 강화**(대형 1.1px / 48px 0.5px / 24px 0.4px, currentColor), letter-spacing -0.03em
- 기본(네이비 타일): 그라디언트 `160deg #1c2b4a→#23408e`, Play 흰색 / Ground `#FF6B35`, radius = 타일의 약 23%
- 라이트(흰 타일): Play `#23408e` / Ground 오렌지 — 밝은 표면·문서용
- **24px 미만: "PG" 축약형만** (P 흰색 + G 오렌지, 두 줄 판독 불가)
- 화면 내 UI에는 항상 한 줄 워드마크 — 아이콘형은 앱 아이콘·파비콘·소셜 프로필 전용

## 3. 표면별 규칙
- 서비스 페이지(랜딩·Records·프로필): 워드마크 좌측 + 세로 구분선 + 서비스 레이블
- 대시보드(네이비 GNB): 다크 워드마크 + 역할 뱃지
- **팀 홈페이지: 팀 엠블럼이 좌측(팀 자체 사이트 원칙), PlayGround는 우측 `#b6bdc9` 11.5px 텍스트만**

## Files
- `Brand Logo.dc.html` — 규정 원본(7개 섹션: 기본형/다크/종목 확장/크기·여백/금지/앱 아이콘/실사용)
- `support.js` — 레퍼런스 실행용 (구현 대상 아님)

## Claude Code 적용 지시 예시
```
Handoff/Design.Brand/README.md를 읽고 Brand Logo.dc.html을 브라우저로 확인해.
공용 로고 컴포넌트(BrandWordmark: variant=light|dark, sport 파라미터)를 만들고
모든 페이지의 로고를 이 컴포넌트로 통일해. 파비콘은 24px PG 축약형 SVG로 생성.
```
