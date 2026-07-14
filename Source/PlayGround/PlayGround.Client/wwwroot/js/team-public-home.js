// 공개 팀 홈페이지 GNB 팀명 규칙: 히어로 통과(PC 300px / 모바일 170px) 시에만 .NET에 통지
export function initHeroScroll(dotnetRef) {
  const mobileQuery = window.matchMedia('(max-width: 767px)');
  const threshold = () => (mobileQuery.matches ? 170 : 300);

  let past = window.scrollY > threshold();

  const evaluate = () => {
    const next = window.scrollY > threshold();
    if (next !== past) {
      past = next;
      dotnetRef.invokeMethodAsync('OnHeroScrollChanged', past);
    }
  };

  window.addEventListener('scroll', evaluate, { passive: true });
  mobileQuery.addEventListener('change', evaluate);
  dotnetRef.invokeMethodAsync('OnHeroScrollChanged', past);

  return {
    dispose: () => {
      window.removeEventListener('scroll', evaluate);
      mobileQuery.removeEventListener('change', evaluate);
    },
  };
}
