// 토스 결제위젯 래퍼 — PG의 JS SDK를 아는 유일한 파일.
// SDK는 window 전역(TossPayments)을 등록하는 클래식 스크립트라 script 태그 주입으로 로드한다.
// 카드 입력·간편결제 UI는 토스 도메인 iframe 안에서 그려진다 (보안 책임이 토스에 남는 구조).
let sdkPromise = null;

function loadSdk() {
  if (window.TossPayments) {
    return Promise.resolve();
  }
  if (!sdkPromise) {
    sdkPromise = new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = 'https://js.tosspayments.com/v2/standard';
      script.onload = resolve;
      script.onerror = () => {
        sdkPromise = null;
        reject(new Error('failed to load TossPayments SDK'));
      };
      document.head.appendChild(script);
    });
  }
  return sdkPromise;
}

// 결제수단 선택 + 약관 위젯을 렌더하고, 결제 요청·정리 핸들을 돌려준다.
// requestPayment는 토스 결제창으로 이동(전체 리로드)하므로 성공 시 이 페이지로 돌아오지 않는다.
export async function initWidget(clientKey, customerKey, amount, methodSelector, agreementSelector) {
  await loadSdk();
  const widgets = window.TossPayments(clientKey).widgets({ customerKey });
  await widgets.setAmount({ currency: 'KRW', value: amount });
  await Promise.all([
    widgets.renderPaymentMethods({ selector: methodSelector }),
    widgets.renderAgreement({ selector: agreementSelector }),
  ]);

  return {
    requestPayment: (orderId, orderName, successUrl, failUrl) =>
      widgets.requestPayment({ orderId, orderName, successUrl, failUrl }),
    dispose: () => {},
  };
}
