/** @type {import('tailwindcss').Config} */
// PlayGround 디자인 토큰 — SPEC.LANDING.md (Handoff/Design.Landing.Phase0) 기준.
// 색상 하드코딩 금지: 아래 토큰만 사용한다. 오렌지는 CTA 전용(전체의 5~10%).
// 색상 값은 Styles/app.tailwind.css의 CSS 변수(RGB 트리플릿)에서 정의.
module.exports = {
  content: [
    './**/*.razor',
    './**/*.html',
    './Pages/**/*.razor',
    './Components/**/*.razor',
    './Layout/**/*.razor',
    './Styles/**/*.cs',
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        // CTA 전용 (5~10% 사용 제한)
        orange: {
          DEFAULT: 'rgb(var(--color-orange) / <alpha-value>)',
          hover: 'rgb(var(--color-orange-hover) / <alpha-value>)',
          ink: 'rgb(var(--color-orange-ink) / <alpha-value>)',
        },
        // Trust — hero·역할카드·CTA 배경(deep), 그라디언트 끝·보조 버튼·아이콘 배경(DEFAULT)
        navy: {
          DEFAULT: 'rgb(var(--color-navy) / <alpha-value>)',
          deep: 'rgb(var(--color-navy-deep) / <alpha-value>)',
          muted: 'rgb(var(--color-navy-muted) / <alpha-value>)',
        },
        // 폼 오류 상태 (Design.FormPatterns) — 파괴적 액션에도 사용
        danger: {
          DEFAULT: 'rgb(var(--color-danger) / <alpha-value>)',
          muted: 'rgb(var(--color-danger-muted) / <alpha-value>)',
        },
        // 빈 상태 Tier A 일러스트 외곽선 (Design.EmptyStates)
        illustration: 'rgb(var(--color-illustration) / <alpha-value>)',
        // 날짜 선택 (Design.DatePicker) — 주말 요일 색·선택 불가 날짜
        weekend: {
          sun: 'rgb(var(--color-weekend-sun) / <alpha-value>)',
          sat: 'rgb(var(--color-weekend-sat) / <alpha-value>)',
        },
        'picker-disabled': 'rgb(var(--color-picker-disabled) / <alpha-value>)',
        // 이미지 업로더 빈 드롭존 (Design.ImageUploader)
        dropzone: 'rgb(var(--color-dropzone) / <alpha-value>)',
        // 뱃지·체크·오버라인 포인트
        teal: {
          DEFAULT: 'rgb(var(--color-teal) / <alpha-value>)',
          ink: 'rgb(var(--color-teal-ink) / <alpha-value>)',
        },
        bg: 'rgb(var(--color-bg) / <alpha-value>)',
        surface: {
          alt: 'rgb(var(--color-surface-alt) / <alpha-value>)',
          icon: 'rgb(var(--color-surface-icon) / <alpha-value>)',
          teal: 'rgb(var(--color-surface-teal) / <alpha-value>)',
          orange: 'rgb(var(--color-surface-orange) / <alpha-value>)',
          soft: 'rgb(var(--color-surface-soft) / <alpha-value>)',
          'orange-badge': 'rgb(var(--color-surface-orange-badge) / <alpha-value>)',
          // 친선경기 행 배경·세그먼트 트랙 (Design.FriendlyMatch)
          friendly: 'rgb(var(--color-surface-friendly) / <alpha-value>)',
          segment: 'rgb(var(--color-surface-segment) / <alpha-value>)',
        },
        border: {
          DEFAULT: 'rgb(var(--color-border) / <alpha-value>)',
          soft: 'rgb(var(--color-border-soft) / <alpha-value>)',
          friendly: 'rgb(var(--color-border-friendly) / <alpha-value>)',
        },
        text: {
          body: 'rgb(var(--color-text-body) / <alpha-value>)',
          strong: 'rgb(var(--color-text-strong) / <alpha-value>)',
          muted: 'rgb(var(--color-text-muted) / <alpha-value>)',
          faint: 'rgb(var(--color-text-faint) / <alpha-value>)',
        },
        agent: 'rgb(var(--color-agent) / <alpha-value>)',
      },
      fontFamily: {
        sans: ['"Plus Jakarta Sans"', 'Pretendard', '-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'sans-serif'],
      },
      borderRadius: {
        card: '20px',
        'card-sm': '18px',
        'card-xs': '16px',
        btn: '9px',
        'btn-lg': '12px',
        wrap: '24px',
        'wrap-sm': '20px',
      },
      borderWidth: {
        1.5: '1.5px',
      },
      maxWidth: {
        content: '1200px',
      },
      backgroundImage: {
        // hero·CTA 공통 그라디언트
        'gradient-navy': 'linear-gradient(160deg, #1c2b4a 0%, #23408e 100%)',
        // hero 배경 사진 위 오버레이
        'hero-overlay':
          'linear-gradient(160deg, rgba(30,35,45,.9) 0%, rgba(28,43,74,.84) 55%, rgba(35,64,142,.76) 100%)',
        // 공개 팀 홈페이지 히어로 커버 오버레이 (PC / 모바일)
        'team-cover-overlay':
          'linear-gradient(180deg, rgba(28,43,74,.25) 0%, rgba(28,43,74,.78) 100%)',
        'team-cover-overlay-m':
          'linear-gradient(180deg, rgba(28,43,74,.2) 0%, rgba(28,43,74,.7) 100%)',
        // 로딩 스켈레톤 시머 (Design.LoadingStates) — deep는 썸네일·커버 전용
        shimmer:
          'linear-gradient(90deg, rgb(var(--color-skeleton)) 25%, rgb(var(--color-skeleton-lit)) 50%, rgb(var(--color-skeleton)) 75%)',
        'shimmer-deep':
          'linear-gradient(90deg, rgb(var(--color-skeleton-deep)) 25%, rgb(var(--color-skeleton-deep-lit)) 50%, rgb(var(--color-skeleton-deep)) 75%)',
      },
      boxShadow: {
        // 스크롤 시 solid 헤더
        header: '0 1px 0 #e6e8ee, 0 4px 16px rgba(28,43,74,.06)',
        // 모바일 드롭다운 메뉴
        menu: '0 12px 24px rgba(28,43,74,.1)',
        // 오렌지 CTA 버튼 (히어로·CTA 섹션 / 헤더 소형)
        'cta-orange': '0 6px 20px rgba(255,107,53,.35)',
        'cta-orange-sm': '0 2px 8px rgba(255,107,53,.3)',
        // 인증·온보딩 카드 / 주 버튼
        authcard: '0 8px 32px rgba(28,43,74,.06)',
        authbtn: '0 4px 14px rgba(255,107,53,.3)',
        // 공개 팀 홈페이지 히어로 엠블럼 박스
        'team-emblem': '0 8px 24px rgba(28,43,74,.25)',
        // 피드백 (Design.FeedbackPatterns) — 토스트 캡슐 / 확인 모달 카드
        // 날짜·시간 팝오버 (Design.DatePicker)
        picker: '0 10px 30px rgba(28,43,74,.12)',
        // 카메라 뱃지 (Design.ImageUploader)
        badge: '0 2px 6px rgba(28,43,74,.12)',
        toast: '0 8px 24px rgba(28,43,74,.25)',
        modal: '0 20px 50px rgba(28,43,74,.3)',
      },
      keyframes: {
        // 토스트 등장 — 슬라이드 업 + 페이드
        'toast-in': {
          from: { opacity: '0', transform: 'translateY(8px)' },
          to: { opacity: '1', transform: 'translateY(0)' },
        },
        // 스켈레톤 시머 — 좌→우 1.6s 무한 루프
        shimmer: {
          from: { backgroundPosition: '200% 0' },
          to: { backgroundPosition: '-200% 0' },
        },
      },
      animation: {
        'toast-in': 'toast-in .18s ease-out',
        shimmer: 'shimmer 1.6s linear infinite',
      },
    },
  },
  plugins: [],
};
