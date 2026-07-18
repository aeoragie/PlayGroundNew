using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PlayGround.Client.Components.Shared.Forms
{
    /// <summary>폼 한 개의 검증 상태를 모으는 컨텍스트 (Design.FormPatterns 검증 타이밍).
    /// 필드가 자기 자신을 등록하고, 제출 시 전체 검증 → 첫 오류 필드로 스크롤·포커스한다.
    /// CascadingValue로 필드들에 내려준다. 폼 오류는 인라인만 — 토스트 금지(FeedbackPatterns 결정표).</summary>
    public sealed class FormFieldContext
    {
        private readonly List<IFormFieldRegistration> mFields = new();

        /// <summary>제출 진행 중 — 이중 제출 잠금(SubmitButton이 구독).</summary>
        public bool IsSubmitting { get; private set; }

        /// <summary>필드 등록 (컴포넌트 초기화 시 1회). 등록 순서가 곧 화면 순서 = 첫 오류 판정 순서.</summary>
        public void Register(IFormFieldRegistration field)
        {
            ArgumentNullException.ThrowIfNull(field);
            Debug.Assert(field is not null);

            if (!mFields.Contains(field))
            {
                mFields.Add(field);
            }
        }

        public void Unregister(IFormFieldRegistration field)
        {
            mFields.Remove(field);
        }

        /// <summary>제출 시 전체 검증. 오류가 있으면 첫 오류 필드로 스크롤·포커스 후 false.
        /// 미입력 상태에서도 제출 버튼을 누를 수 있게 두고 여기서 인라인 오류로 안내한다(사전 비활성 금지).</summary>
        public async Task<bool> ValidateAllAsync()
        {
            IFormFieldRegistration? firstInvalid = null;
            foreach (IFormFieldRegistration field in mFields)
            {
                if (!field.ValidateOnSubmit() && firstInvalid is null)
                {
                    firstInvalid = field;
                }
            }

            if (firstInvalid is null)
            {
                return true;
            }

            await firstInvalid.FocusAsync();
            return false;
        }

        /// <summary>제출 실행 — 검증 통과 시에만 onValid 호출. 진행 중에는 재진입하지 않는다.</summary>
        public async Task SubmitAsync(Func<Task> onValid, Func<Task>? onStateChanged = null)
        {
            ArgumentNullException.ThrowIfNull(onValid);

            if (IsSubmitting)
            {
                return;
            }

            if (!await ValidateAllAsync())
            {
                return;
            }

            IsSubmitting = true;
            if (onStateChanged is not null)
            {
                await onStateChanged();
            }

            try
            {
                await onValid();
            }
            finally
            {
                IsSubmitting = false;
                if (onStateChanged is not null)
                {
                    await onStateChanged();
                }
            }
        }

        /// <summary>등록된 필드 중 오류 표시 중인 개수 (데모·디버깅용).</summary>
        public int ErrorCount => mFields.Count(f => f.HasVisibleError);
    }

    /// <summary>컨텍스트가 필드를 다루기 위한 최소 계약. 각 폼 컴포넌트가 구현한다.</summary>
    public interface IFormFieldRegistration
    {
        /// <summary>제출 시 검증 — 유효하면 true, 아니면 오류를 표시하고 false.</summary>
        bool ValidateOnSubmit();

        /// <summary>첫 오류 필드로 스크롤·포커스 (모바일 고정 바 오프셋은 JS에서 처리).</summary>
        Task FocusAsync();

        bool HasVisibleError { get; }
    }
}
