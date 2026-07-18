namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 피드백(토스트·확인 모달) 클래스 — Design.FeedbackPatterns 기준.
        /// 오렌지 사용 금지(성공 teal / 오류 danger / 정보 navy).
        /// ※ Tailwind content 글롭이 Styles/**/*.cs만 스캔하므로 클래스 문자열은 여기에 둔다.
        /// </summary>
        public static class Feedback
        {
            //.// 토스트

            /// <summary>토스트 위치 — PC 좌하단(24px) / 모바일 하단 중앙(탭바 위).</summary>
            public const string ToastLayer =
                "fixed z-[100] left-1/2 -translate-x-1/2 bottom-[76px] " +
                "md:left-6 md:translate-x-0 md:bottom-6 px-4 md:px-0 w-full md:w-auto flex justify-center md:block " +
                "pointer-events-none";

            /// <summary>캡슐 — 다크 네이비, radius 13, 최대 340px, 슬라이드 업 + 페이드.</summary>
            public const string ToastCapsule =
                "pointer-events-auto max-w-[340px] w-full md:w-auto bg-navy-deep rounded-[13px] px-4 py-[13px] " +
                "flex items-center gap-[11px] shadow-toast animate-toast-in";

            /// <summary>상태 원 26px — 종류별 배경은 아래 메서드.</summary>
            private const string ToastDotBase =
                "w-[26px] h-[26px] rounded-full flex items-center justify-center shrink-0 " +
                "text-[13px] font-extrabold text-white";

            public const string ToastMessage = "text-[13px] font-semibold text-white break-keep flex-1";

            /// <summary>액션 — teal 텍스트 버튼, 최대 1개.</summary>
            public const string ToastAction =
                "text-[12.5px] font-extrabold text-teal whitespace-nowrap shrink-0 pl-1 border-0 bg-transparent cursor-pointer";

            /// <summary>닫기(오류 전용 — 자동 소멸하지 않으므로 수동 닫기 제공).</summary>
            public const string ToastClose =
                "text-white/50 hover:text-white shrink-0 border-0 bg-transparent cursor-pointer transition-colors";

            public static string ToastDot(bool isSuccess, bool isError)
            {
                if (isError)
                {
                    return $"{ToastDotBase} bg-danger";
                }

                return isSuccess ? $"{ToastDotBase} bg-teal" : $"{ToastDotBase} bg-navy";
            }

            //.// 확인 모달 · 바텀시트

            /// <summary>오버레이 — rgba(28,43,74,.45).</summary>
            public const string Overlay = "fixed inset-0 z-[110] bg-navy-deep/45 flex items-end md:items-center md:justify-center";

            /// <summary>카드 — PC 중앙 radius 18 / 모바일 바텀시트(상단만 라운드).</summary>
            public const string Card =
                "relative w-full md:w-auto md:max-w-[420px] bg-white rounded-t-[18px] md:rounded-[18px] " +
                "px-5 pt-3 pb-5 md:px-[22px] md:py-6 flex flex-col shadow-modal " +
                "pb-[calc(20px+env(safe-area-inset-bottom))] md:pb-6";

            /// <summary>모바일 그랩바 36×4.</summary>
            public const string GrabBar = "w-9 h-1 rounded-full bg-border mx-auto md:hidden";

            public const string Title = "text-[14.5px] md:text-[15.5px] font-extrabold text-navy-deep break-keep mt-3.5 md:mt-0";

            public const string Description = "text-xs md:text-[12.5px] text-text-body leading-[1.6] break-keep mt-1.5 md:mt-[7px]";

            /// <summary>입력 잠금형 확인 입력칸.</summary>
            public const string PhraseInput =
                "h-[46px] md:h-[42px] border-1.5 border-border rounded-[11px] px-[13px] mt-3.5 " +
                "text-base md:text-[13px] text-navy-deep placeholder:text-text-muted outline-none " +
                "focus:border-navy focus:ring-[3px] focus:ring-navy/[.12] transition-colors";

            /// <summary>버튼 영역 — PC 가로 2열 / 모바일 세로 스택(파괴 버튼이 위).</summary>
            public const string Actions = "flex flex-col-reverse md:flex-row gap-1 md:gap-[9px] mt-4 md:mt-5";

            private const string ButtonBase =
                "h-[46px] md:h-[42px] md:flex-1 rounded-xl md:rounded-[11px] text-[13.5px] md:text-[13px] " +
                "font-bold whitespace-nowrap transition-colors";

            /// <summary>취소 — 모바일은 배경 흰색(보더 없음), PC는 아웃라인.</summary>
            public const string CancelButton =
                ButtonBase + " bg-white text-text-strong border-0 md:border-1.5 md:border-border cursor-pointer";

            /// <summary>주 버튼 — 일반 네이비 / 파괴 레드 / 입력 미일치 시 비활성(#e8b0a8).</summary>
            public static string ConfirmButton(bool isDestructive, bool isLocked)
            {
                if (isLocked)
                {
                    return $"{ButtonBase} border-0 bg-danger-muted text-white cursor-default";
                }

                return isDestructive
                    ? $"{ButtonBase} border-0 bg-danger hover:bg-danger/90 text-white cursor-pointer"
                    : $"{ButtonBase} border-0 bg-navy hover:bg-navy-deep text-white cursor-pointer";
            }
        }
    }
}
