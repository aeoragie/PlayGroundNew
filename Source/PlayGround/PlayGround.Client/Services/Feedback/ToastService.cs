using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlayGround.Client.Services.Feedback
{
    /// <summary>토스트 종류 — 상태 원 색이 달라진다 (Design.FeedbackPatterns A).</summary>
    public enum ToastKind
    {
        /// <summary>완료 통지 (teal ✓) — 저장·전송·복사.</summary>
        Success,
        /// <summary>실패 (danger !) — 자동 소멸하지 않는다.</summary>
        Error,
        /// <summary>중립 통지 (navy i) — 남용 금지.</summary>
        Info,
    }

    /// <summary>화면에 떠 있는 토스트 1개. 액션은 최대 1개(teal 텍스트 버튼).</summary>
    public sealed class ToastMessage
    {
        public required ToastKind Kind { get; init; }

        /// <summary>"~됐어요" 완료형. 오류는 원인보다 다음 행동("다시 시도해 주세요").</summary>
        public required string Message { get; init; }

        public string? ActionLabel { get; init; }

        public Func<Task>? OnAction { get; init; }

        public bool HasAction => ActionLabel is not null && OnAction is not null;
    }

    /// <summary>전역 토스트 — **동시 1개(교체식)**. 새 토스트가 오면 이전 것을 대체한다.
    /// 사용 결정표: 저장·복사·전송 완료 = 토스트 / 폼 검증 오류 = 인라인(토스트 금지) /
    /// 서버 오류 = 오류 토스트 + 재시도 / 화면 내 변화가 보이면 생략 · 알림 센터와 중복 발송 금지.</summary>
    public sealed class ToastService
    {
        private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(3.5);
        private static readonly TimeSpan ActionDuration = TimeSpan.FromSeconds(5);

        private CancellationTokenSource? mDismissSource;

        /// <summary>현재 표시 중인 토스트 (없으면 null). ToastHost가 구독해 렌더한다.</summary>
        public ToastMessage? Current { get; private set; }

        public event Action? OnChanged;

        /// <summary>완료 통지 — 3.5초(액션 있으면 5초) 후 자동 소멸.</summary>
        public void ShowSuccess(string message, string? actionLabel = null, Func<Task>? onAction = null)
        {
            Show(new ToastMessage
            {
                Kind = ToastKind.Success,
                Message = message,
                ActionLabel = actionLabel,
                OnAction = onAction,
            });
        }

        /// <summary>실패 — **자동 소멸하지 않는다**(수동 닫기·재시도).</summary>
        public void ShowError(string message, string? actionLabel = null, Func<Task>? onAction = null)
        {
            Show(new ToastMessage
            {
                Kind = ToastKind.Error,
                Message = message,
                ActionLabel = actionLabel,
                OnAction = onAction,
            });
        }

        /// <summary>중립 통지 — 화면 내 변화가 보이면 쓰지 않는다.</summary>
        public void ShowInfo(string message)
        {
            Show(new ToastMessage { Kind = ToastKind.Info, Message = message });
        }

        public void Show(ToastMessage toast)
        {
            ArgumentNullException.ThrowIfNull(toast);

            // 교체식 — 이전 토스트의 자동 소멸 타이머를 취소하고 새 것으로 갈아끼운다
            mDismissSource?.Cancel();
            mDismissSource?.Dispose();
            mDismissSource = null;

            Current = toast;
            OnChanged?.Invoke();

            // 오류는 자동 소멸 없음
            if (toast.Kind == ToastKind.Error)
            {
                return;
            }

            var source = new CancellationTokenSource();
            mDismissSource = source;
            _ = DismissAfterAsync(toast, toast.HasAction ? ActionDuration : DefaultDuration, source.Token);
        }

        public void Dismiss()
        {
            mDismissSource?.Cancel();
            mDismissSource?.Dispose();
            mDismissSource = null;

            if (Current is not null)
            {
                Current = null;
                OnChanged?.Invoke();
            }
        }

        private async Task DismissAfterAsync(ToastMessage toast, TimeSpan delay, CancellationToken cancellation)
        {
            try
            {
                await Task.Delay(delay, cancellation);
            }
            catch (TaskCanceledException)
            {
                return; // 교체되거나 수동으로 닫힘
            }

            // 그사이 다른 토스트로 교체됐으면 건드리지 않는다
            if (ReferenceEquals(Current, toast))
            {
                Current = null;
                OnChanged?.Invoke();
            }
        }
    }
}
