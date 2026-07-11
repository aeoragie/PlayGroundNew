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
        },
        // Trust — hero·역할카드·CTA 배경(deep), 그라디언트 끝·보조 버튼·아이콘 배경(DEFAULT)
        navy: {
          DEFAULT: 'rgb(var(--color-navy) / <alpha-value>)',
          deep: 'rgb(var(--color-navy-deep) / <alpha-value>)',
        },
        // 뱃지·체크·오버라인 포인트
        teal: {
          DEFAULT: 'rgb(var(--color-teal) / <alpha-value>)',
        },
        bg: 'rgb(var(--color-bg) / <alpha-value>)',
        surface: {
          alt: 'rgb(var(--color-surface-alt) / <alpha-value>)',
          icon: 'rgb(var(--color-surface-icon) / <alpha-value>)',
        },
        border: {
          DEFAULT: 'rgb(var(--color-border) / <alpha-value>)',
        },
        text: {
          body: 'rgb(var(--color-text-body) / <alpha-value>)',
          strong: 'rgb(var(--color-text-strong) / <alpha-value>)',
          muted: 'rgb(var(--color-text-muted) / <alpha-value>)',
        },
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
      },
      boxShadow: {
        // 스크롤 시 solid 헤더
        header: '0 1px 0 #e6e8ee, 0 4px 16px rgba(28,43,74,.06)',
        // 모바일 드롭다운 메뉴
        menu: '0 12px 24px rgba(28,43,74,.1)',
        // 오렌지 CTA 버튼 (히어로·CTA 섹션 / 헤더 소형)
        'cta-orange': '0 6px 20px rgba(255,107,53,.35)',
        'cta-orange-sm': '0 2px 8px rgba(255,107,53,.3)',
      },
    },
  },
  plugins: [],
};
