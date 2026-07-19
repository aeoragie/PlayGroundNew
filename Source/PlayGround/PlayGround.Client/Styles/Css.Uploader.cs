namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 이미지 업로더 — Design.ImageUploader. 아바타(원형 1:1) · 커버(3:1).
        /// 실패는 인라인 카드(토스트 금지 — 자리 유지가 복구에 유리).
        /// ※ Tailwind content 글롭이 Styles/**/*.cs만 스캔하므로 클래스 문자열은 여기에 둔다.
        /// </summary>
        public static class Uploader
        {
            //.// 아바타 · 엠블럼

            public const string AvatarWrap = "relative w-[84px] h-[84px] shrink-0";

            public const string AvatarImage = "w-[84px] h-[84px] rounded-full object-cover bg-surface-icon";

            /// <summary>빈 상태 — 이니셜/실드 placeholder. "빈 이미지 상태"를 만들지 않는다.</summary>
            public const string AvatarPlaceholder =
                "w-[84px] h-[84px] rounded-full bg-navy flex items-center justify-center " +
                "text-[26px] font-extrabold text-white select-none";

            //.// 사각 프레임 (선수 사진 3:4 등) — SPEC이 정사각/원형이 아닌 슬롯을 요구하는 곳

            /// <summary>프레임 래퍼. 크기는 호출부가 Class로 준다(SPEC이 PC 120px·모바일 92px로 다르다).</summary>
            public const string FrameWrap = "relative shrink-0";

            public const string FrameImage = "w-full h-full object-cover rounded-[14px] bg-surface-icon";

            /// <summary>사각 프레임의 빈 상태 — 이니셜 아바타가 안에 들어간다(Design.AvatarBadge).</summary>
            public const string FramePlaceholder =
                "w-full h-full rounded-[14px] bg-surface-icon flex items-center justify-center select-none";

            /// <summary>우하단 카메라 뱃지 30px.</summary>
            public const string CameraBadge =
                "absolute -right-0.5 -bottom-0.5 w-[30px] h-[30px] rounded-full bg-white border-1.5 border-border " +
                "flex items-center justify-center cursor-pointer shadow-badge text-text-strong " +
                "hover:border-navy transition-colors";

            public const string AvatarActions = "flex gap-2.5 mt-2 justify-center";

            public const string ActionReplace =
                "text-[11px] font-bold text-navy hover:text-navy-deep border-0 bg-transparent cursor-pointer whitespace-nowrap";

            public const string ActionDelete =
                "text-[11px] font-bold text-danger hover:opacity-80 border-0 bg-transparent cursor-pointer whitespace-nowrap";

            //.// 커버 (3:1)

            /// <summary>빈 상태 드롭존 — PC 전용(모바일은 버튼으로 대체).</summary>
            public static string DropZone(bool isDragOver)
            {
                string tone = isDragOver
                    ? "border-navy bg-surface-alt"
                    : "border-illustration bg-dropzone";

                return "hidden md:flex border-1.5 border-dashed rounded-[13px] px-4 py-[26px] " +
                       "flex-col items-center gap-2 text-center transition-colors " + tone;
            }

            public const string DropZoneText = "text-[12.5px] font-bold text-text-strong break-keep";

            public const string DropZonePick = "text-navy cursor-pointer";

            public const string DropZoneHint = "text-[11px] text-text-muted whitespace-nowrap";

            /// <summary>모바일 대체 버튼 — OS 시트에 위임(자체 시트 만들지 않는다).</summary>
            public const string MobilePickButton =
                "md:hidden w-full h-[46px] rounded-xl border-1.5 border-border bg-white " +
                "text-sm font-bold text-text-strong whitespace-nowrap cursor-pointer";

            /// <summary>업로드된 커버 — 3:1 고정.</summary>
            public const string CoverFrame =
                "relative w-full aspect-[3/1] rounded-[13px] overflow-hidden bg-surface-icon";

            public const string CoverImage = "w-full h-full object-cover select-none";

            /// <summary>우하단 반투명 캡슐.</summary>
            public const string CoverControls = "absolute right-2.5 bottom-2.5 flex gap-1.5";

            public const string CoverButton =
                "h-[30px] px-3 rounded-lg bg-navy-deep/65 hover:bg-navy-deep/80 text-[11px] font-bold text-white " +
                "whitespace-nowrap border-0 cursor-pointer transition-colors";

            /// <summary>위치 조정 중 — 세로 드래그만.</summary>
            public const string CoverAdjustHint =
                "absolute inset-x-0 top-2.5 text-center text-[11px] font-bold text-white " +
                "drop-shadow-[0_1px_3px_rgba(28,43,74,.8)] pointer-events-none";

            //.// 진행 · 실패 (공통)

            public const string ProgressRow =
                "flex items-center gap-3 border border-border-soft rounded-xl px-3.5 py-3 mt-2";

            public const string ProgressIcon =
                "w-[38px] h-[38px] rounded-[10px] bg-surface-icon flex items-center justify-center shrink-0";

            public const string Spinner =
                "w-[18px] h-[18px] rounded-full border-[2.5px] border-navy/20 border-t-navy animate-spin";

            public const string ProgressBody = "flex flex-col gap-1.5 flex-1 min-w-0";

            public const string ProgressName = "text-[12.5px] font-bold text-navy-deep whitespace-nowrap truncate";

            /// <summary>실패 카드 — 원인 + 해결 + "다시 선택".</summary>
            public const string ErrorRow =
                "flex items-center gap-3 border border-danger/25 rounded-xl px-3.5 py-3 mt-2 bg-danger/5";

            public const string ErrorIcon =
                "w-[38px] h-[38px] rounded-[10px] bg-danger/10 flex items-center justify-center shrink-0 " +
                "text-[15px] font-extrabold text-danger";

            public const string ErrorTitle = "text-[12.5px] font-bold text-danger whitespace-nowrap";

            public const string ErrorHint = "text-[11px] text-text-muted break-keep";

            public const string ErrorRetry =
                "h-8 px-[13px] rounded-[9px] border-1.5 border-border bg-white text-[11.5px] font-bold " +
                "text-text-strong whitespace-nowrap cursor-pointer shrink-0 hover:border-navy transition-colors";

            //.// 크롭 모달 (원형 1:1)

            public const string CropOverlay =
                "fixed inset-0 z-[125] bg-navy-deep/70 flex items-center justify-center p-4";

            public const string CropCard =
                "w-full md:w-[400px] bg-white rounded-[18px] p-5 flex flex-col gap-3.5 shadow-modal";

            public const string CropTitle = "text-[15px] font-extrabold text-navy-deep";

            /// <summary>크롭 무대 — 원형 마스크를 씌운 정사각 영역.</summary>
            public const string CropStage =
                "relative w-full aspect-square rounded-[12px] overflow-hidden bg-navy-deep touch-none cursor-move select-none";

            /// <summary>크롭 무대(3:4) — 선수 사진처럼 세로 슬롯일 때. 잘릴 영역을 그대로 보여준다.</summary>
            public const string CropStagePortrait =
                "relative w-[270px] mx-auto aspect-[3/4] rounded-[12px] overflow-hidden bg-navy-deep touch-none cursor-move select-none";

            /// <summary>원형 마스크 — 바깥을 어둡게 덮는다.</summary>
            public const string CropMask =
                "absolute inset-0 pointer-events-none " +
                "[background:radial-gradient(circle_at_center,transparent_calc(50%_-_1px),rgba(28,43,74,.6)_50%)]";

            public const string CropRing =
                "absolute inset-0 m-auto w-full h-full rounded-full border-2 border-white/70 pointer-events-none";

            /// <summary>사각 크롭에는 마스크 대신 테두리만 — 무대 전체가 곧 결과다.</summary>
            public const string CropRingRect =
                "absolute inset-0 rounded-[12px] border-2 border-white/70 pointer-events-none";

            public const string ZoomRow = "flex items-center gap-2.5";

            public const string ZoomLabel = "text-[11.5px] font-bold text-text-muted whitespace-nowrap";

            public const string ZoomSlider = "flex-1 accent-navy cursor-pointer";

            public const string CropActions = "flex gap-2.5";

            public const string CropCancel =
                "flex-1 h-11 rounded-[11px] bg-white border-1.5 border-border text-[13px] font-bold " +
                "text-text-strong whitespace-nowrap cursor-pointer";

            public const string CropConfirm =
                "flex-1 h-11 rounded-[11px] bg-navy hover:bg-navy-deep text-[13px] font-bold text-white " +
                "whitespace-nowrap border-0 cursor-pointer transition-colors disabled:bg-navy-muted";

            /// <summary>파일 input은 숨기고 버튼으로 연다.</summary>
            public const string HiddenInput = "hidden";
        }
    }
}
