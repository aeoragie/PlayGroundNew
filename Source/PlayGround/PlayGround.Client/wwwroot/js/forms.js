// 폼 공용 — 제출 시 첫 오류 필드로 스크롤·포커스 (Design.FormPatterns 검증 타이밍 3)
// 모바일은 하단 고정 제출 바에 가리므로 여백을 두고 멈춘다.
const MOBILE_BREAKPOINT = 767;
const MOBILE_BOTTOM_BAR_OFFSET = 96; // 고정 바(48px) + 여유
const DESKTOP_OFFSET = 24;

export function scrollIntoViewAndFocus(element) {
  if (!element) {
    return;
  }

  const isMobile = window.matchMedia(`(max-width: ${MOBILE_BREAKPOINT}px)`).matches;
  const offset = isMobile ? MOBILE_BOTTOM_BAR_OFFSET : DESKTOP_OFFSET;
  const top = element.getBoundingClientRect().top + window.scrollY - offset;

  window.scrollTo({ top: Math.max(top, 0), behavior: 'smooth' });

  // 스크롤과 동시에 포커스하면 브라우저가 위치를 다시 잡아채므로 한 프레임 뒤에 준다.
  requestAnimationFrame(() => element.focus({ preventScroll: true }));
}

// 팝오버(캘린더·시간 목록)가 스크롤 컨테이너 안에서 잘리지 않도록 한 프레임 뒤 스크롤한다.
// 모달 본문처럼 overflow 되는 부모 안에서는 absolute 팝오버가 아래로 잘려 퀵버튼이 가려진다.
export function revealPopover(element) {
  if (!element) {
    return;
  }

  requestAnimationFrame(() => element.scrollIntoView({ block: 'nearest', behavior: 'smooth' }));
}
