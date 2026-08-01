// Records 대회 상세 ↔ 경기 상세 왕복 시 스크롤 위치 보존 (Design.Records 돌아가기 플로우).
export function getScrollY() {
  return window.scrollY || window.pageYOffset || 0;
}

export function scrollToY(y) {
  window.scrollTo(0, y || 0);
}
