// 랜딩 하이브리드 헤더: 스크롤 임계값(PC 40px / 모바일 30px) 통과 시에만 .NET에 통지
export function initHeaderScroll(dotnetRef) {
  const mobileQuery = window.matchMedia('(max-width: 767px)');
  const threshold = () => (mobileQuery.matches ? 30 : 40);

  let solid = window.scrollY > threshold();

  const evaluate = () => {
    const next = window.scrollY > threshold();
    if (next !== solid) {
      solid = next;
      dotnetRef.invokeMethodAsync('OnScrollStateChanged', solid);
    }
  };

  window.addEventListener('scroll', evaluate, { passive: true });
  mobileQuery.addEventListener('change', evaluate);
  dotnetRef.invokeMethodAsync('OnScrollStateChanged', solid);

  return {
    dispose: () => {
      window.removeEventListener('scroll', evaluate);
      mobileQuery.removeEventListener('change', evaluate);
    },
  };
}
