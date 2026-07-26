namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 선수 지원(Application) — 팀 대시보드 선수 모집 섹션의 지원자 리스트.
        /// Design.Application §3 (SPEC.TEAMDASHBOARD §6 자리 확장) — 행은 액션형(행 탭 없음, TableList 규칙).
        /// 상태 뱃지 톤·세그먼트는 공용(Css.Badge·Css.Segment) 재사용 — 여기엔 배치·버튼만.
        /// (Tailwind content 글롭이 Styles/**/*.cs만 스캔 — 클래스 문자열은 반드시 여기에.)
        /// </summary>
        public static class Application
        {
            /// <summary>지원자 리스트를 공고 카드 아래에 얹을 때의 상단 여백·구분.</summary>
            public const string Wrapper = "flex flex-col gap-3 mt-1";

            /// <summary>서브 섹션 제목("지원자") — 선수 모집 섹션 안의 하위 블록이라 SectionTitle보다 작다.</summary>
            public const string SubTitle = "m-0 text-base font-extrabold text-navy-deep";

            /// <summary>지원자 한 행 — 행 탭 없는 액션형(오른쪽에 버튼·⋯).</summary>
            public const string Row =
                "flex items-center gap-3 border border-border-soft rounded-[12px] px-3.5 py-3";

            /// <summary>이름 줄 — 이름(굵게) + 회색 메타(연령·희망 포지션)를 한 줄에.</summary>
            public const string Name = "text-[13px] font-bold text-navy-deep whitespace-nowrap";

            public const string NameMeta = "font-semibold text-text-muted";

            /// <summary>보조 메타 줄 — 공고 제목·경로·상대시각.</summary>
            public const string Meta = "text-[11px] text-text-muted whitespace-nowrap overflow-hidden text-ellipsis";

            //.// 행 액션 버튼 (작은 크기 — 행 높이에 맞춘 32px)

            /// <summary>[테스트 요청] — 네이비(오렌지 아님: CTA 아니라 상태 전환).</summary>
            public const string TestButton =
                "h-8 px-3 rounded-[9px] bg-navy hover:bg-navy-deep text-white text-[12.5px] font-bold " +
                "whitespace-nowrap transition-colors cursor-pointer";

            /// <summary>[프로필] — 자기소개·메타를 펼치는 정보 토글(아웃라인).</summary>
            public const string ProfileButton =
                "h-8 px-3 rounded-[9px] bg-white border-1.5 border-border hover:border-navy text-text-strong " +
                "text-[12.5px] font-bold whitespace-nowrap transition-colors cursor-pointer";

            /// <summary>펼친 자기소개 패널 — 행 아래 옅은 배경 블록.</summary>
            public const string ProfilePanel =
                "bg-surface-soft rounded-[10px] px-3.5 py-3 flex flex-col gap-1.5";

            public const string ProfilePanelLabel = "text-[11px] font-bold text-text-muted";

            public const string ProfilePanelBody = "text-[12.5px] text-text-body leading-[1.6] break-keep";

            /// <summary>세그먼트에 항목이 하나도 없을 때의 한 줄 안내(전체 빈 상태는 EmptySlot).</summary>
            public const string SegmentEmpty =
                "text-[12.5px] text-text-muted text-center py-6 break-keep";
        }
    }
}
