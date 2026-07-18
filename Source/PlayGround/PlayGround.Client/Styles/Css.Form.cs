namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 폼 공용 클래스 — Design.FormPatterns 기준.
        /// PC 44px / 모바일 46px·16px(iOS 자동 줌 방지) · radius 11 · 보더 1.5px.
        /// (색상은 토큰만, 크기·radius는 arbitrary 값)
        /// </summary>
        public static class Form
        {
            /// <summary>레이블 — 12.5px 800 상단 고정 (placeholder로 대체 금지).</summary>
            public const string Label =
                "text-xs md:text-[12.5px] font-extrabold text-text-strong whitespace-nowrap";

            /// <summary>"(선택)" 접미 — 필수 표시(*) 대신 선택 항목에만.</summary>
            public const string OptionalMark = "font-semibold text-text-muted";

            /// <summary>헬퍼 문구 — 11.5px, 한글 keep-all.</summary>
            public const string Helper = "text-[11.5px] text-text-muted leading-[1.6] break-keep";

            /// <summary>오류 인라인 메시지 — 11.5px 700 danger.</summary>
            public const string ErrorMessage = "text-[11.5px] font-bold text-danger break-keep";

            /// <summary>글자수 카운터 — 레이블 우측. 초과 시 CounterOver로 교체(입력은 막지 않음).</summary>
            public const string Counter = "text-[11px] font-bold text-text-muted whitespace-nowrap";
            public const string CounterOver = "text-[11px] font-bold text-danger whitespace-nowrap";

            /// <summary>필드 래퍼 — 레이블·컨트롤·헬퍼 세로 스택.</summary>
            public const string FieldWrapper = "flex flex-col gap-1.5";

            /// <summary>입력 컨트롤 공통 골격 (높이·radius·폰트). 상태별 보더/배경은 아래 메서드에서.</summary>
            private const string ControlBase =
                "w-full h-[46px] md:h-11 rounded-[11px] border-1.5 px-[13px] text-base md:text-[13px] " +
                "text-navy-deep placeholder:text-text-muted outline-none transition-colors";

            private const string TextAreaBase =
                "w-full min-h-[88px] rounded-[11px] border-1.5 px-[13px] py-[11px] text-base md:text-[13px] " +
                "leading-[1.6] break-keep text-navy-deep placeholder:text-text-muted outline-none transition-colors resize-y";

            private const string RadioCardBase =
                "h-[46px] md:h-[42px] rounded-[10px] border-1.5 flex items-center justify-center " +
                "text-[13px] md:text-[12.5px] whitespace-nowrap transition-colors";

            private const string CheckBoxBase =
                "w-[22px] h-[22px] md:w-5 md:h-5 rounded-[7px] md:rounded-md flex items-center justify-center " +
                "shrink-0 mt-px border-1.5 transition-colors";

            private const string SubmitBase =
                "w-full h-12 md:h-[46px] rounded-btn-lg border-0 text-[15px] md:text-[13.5px] font-bold text-white " +
                "whitespace-nowrap flex items-center justify-center gap-[9px] transition-colors";

            // Tailwind JIT가 스캔하도록 상태별 클래스를 리터럴로 나열한다.
            private const string StateDisabled = "bg-surface-soft border-border-soft text-text-faint cursor-not-allowed";
            private const string StateError = "bg-white border-danger focus:ring-[3px] focus:ring-danger/[.12]";
            private const string StateDefault = "bg-white border-border focus:border-navy focus:ring-[3px] focus:ring-navy/[.12]";

            /// <summary>텍스트 입력 — 상태 5종(기본/포커스/채움/오류/비활성).</summary>
            public static string Input(bool hasError, bool isDisabled)
            {
                if (isDisabled)
                {
                    return $"{ControlBase} {StateDisabled}";
                }

                return hasError ? $"{ControlBase} {StateError}" : $"{ControlBase} {StateDefault}";
            }

            /// <summary>여러 줄 입력 — 높이 대신 min-height.</summary>
            public static string TextArea(bool hasError, bool isDisabled)
            {
                if (isDisabled)
                {
                    return $"{TextAreaBase} {StateDisabled}";
                }

                return hasError ? $"{TextAreaBase} {StateError}" : $"{TextAreaBase} {StateDefault}";
            }

            /// <summary>라디오 카드 한 칸 — 선택 = 네이비 보더 + 틴트.</summary>
            public static string RadioCard(bool isSelected, bool isDisabled)
            {
                if (isDisabled)
                {
                    return $"{RadioCardBase} bg-surface-soft border-border-soft text-text-faint cursor-not-allowed";
                }

                return isSelected
                    ? $"{RadioCardBase} border-navy bg-navy/[.06] text-navy font-extrabold"
                    : $"{RadioCardBase} border-border bg-white text-text-body font-bold hover:border-navy/40";
            }

            /// <summary>체크박스 박스 — 체크 시 네이비 채움. 기본 체크 금지.</summary>
            public static string CheckBox(bool isChecked, bool isDisabled)
            {
                if (isDisabled)
                {
                    return $"{CheckBoxBase} bg-surface-soft border-border-soft cursor-not-allowed";
                }

                return isChecked
                    ? $"{CheckBoxBase} bg-navy border-navy"
                    : $"{CheckBoxBase} bg-white border-border hover:border-navy/40";
            }

            /// <summary>제출 버튼 — 기본(네이비) / 비활성(제출 직후에만, navy-muted).</summary>
            public static string Submit(bool isDisabled)
            {
                return isDisabled
                    ? $"{SubmitBase} bg-navy-muted cursor-default"
                    : $"{SubmitBase} bg-navy hover:bg-navy-deep cursor-pointer";
            }
        }
    }
}
