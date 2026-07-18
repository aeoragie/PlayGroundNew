using System;
using System.Threading.Tasks;

namespace PlayGround.Client.Services.Feedback
{
    /// <summary>확인 모달 단계 (Design.FeedbackPatterns B).</summary>
    public enum ConfirmKind
    {
        /// <summary>일반 — 네이비 주 버튼. 공개 전환·승인 등.</summary>
        Normal,
        /// <summary>파괴적 — 레드 주 버튼, 기본 포커스=취소. 제목에 대상 이름 명시.</summary>
        Destructive,
        /// <summary>고위험 — 문구 입력 일치 전 버튼 비활성. 계정·팀 삭제 전용.</summary>
        HighRisk,
    }

    /// <summary>확인 모달 요청. 되돌릴 수 없는 일의 **사전** 확인에만 쓴다 — 그 외 모달 금지.</summary>
    public sealed class ConfirmRequest
    {
        public ConfirmKind Kind { get; init; } = ConfirmKind.Normal;

        /// <summary>파괴적이면 대상 이름을 명시 ("김민준 선수를 선수단에서 삭제할까요?").</summary>
        public required string Title { get; init; }

        /// <summary>결과 설명 1줄. 파괴적이면 "되돌릴 수 없어요"를 포함한다.</summary>
        public string? Description { get; init; }

        /// <summary>**동사형** 레이블 — "확인" 금지 ("공개하기", "삭제").</summary>
        public required string ConfirmLabel { get; init; }

        public string CancelLabel { get; init; } = "취소";

        /// <summary>HighRisk 전용 — 이 문구를 정확히 입력해야 주 버튼이 활성화된다.</summary>
        public string? RequiredPhrase { get; init; }
    }

    /// <summary>전역 확인 모달 — 호출측은 `await Confirm.ShowAsync(...)`로 bool을 받는다.
    /// 모바일은 바텀시트로 변형(파괴 버튼이 위, 취소가 아래).</summary>
    public sealed class ConfirmService
    {
        private TaskCompletionSource<bool>? mCompletion;

        /// <summary>현재 열린 요청 (없으면 null). ConfirmDialogHost가 구독해 렌더한다.</summary>
        public ConfirmRequest? Current { get; private set; }

        public event Action? OnChanged;

        /// <summary>모달을 띄우고 사용자의 선택을 기다린다 — 주 버튼 true, 취소·Esc false.</summary>
        public Task<bool> ShowAsync(ConfirmRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            // 이미 열려 있으면 이전 요청은 취소로 마감 (모달 중첩 금지)
            mCompletion?.TrySetResult(false);

            Current = request;
            mCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            OnChanged?.Invoke();

            return mCompletion.Task;
        }

        /// <summary>호스트가 사용자 선택을 알린다.</summary>
        public void Complete(bool confirmed)
        {
            if (Current is null)
            {
                return;
            }

            Current = null;
            OnChanged?.Invoke();

            mCompletion?.TrySetResult(confirmed);
            mCompletion = null;
        }
    }
}
