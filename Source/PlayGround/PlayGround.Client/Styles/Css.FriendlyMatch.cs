namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 친선경기 구분 — Design.FriendlyMatch.
        /// **공식은 기본 형태 그대로 두고 친선에만 마킹한다** (다수가 공식이므로 "공식" 뱃지를 달지 않는다).
        /// 구분은 뱃지 추가가 아니라 행 스타일 + 메타 라벨로 한다 — 행당 뱃지 1개 규칙 유지.
        /// ※ Tailwind content 글롭이 Styles/**/*.cs만 스캔하므로 클래스 문자열은 여기에 둔다.
        /// </summary>
        public static class FriendlyMatch
        {
            //.// 결과 행

            /// <summary>친선 행 = 점선 보더 + 옅은 배경. 공식 행은 기존 실선 그대로다.</summary>
            public static string Row(bool isFriendly, string baseClass)
            {
                return isFriendly
                    ? $"{baseClass} border border-dashed border-border-friendly bg-surface-friendly"
                    : baseClass;
            }

            /// <summary>대회명 자리에 들어가는 회색 "친선경기" 라벨 (아이콘 11px + 텍스트).</summary>
            public const string Label =
                "inline-flex items-center gap-[5px] text-[11px] text-text-muted whitespace-nowrap";

            /// <summary>친선 행의 스코어는 한 톤 죽인다 (공식이 눈에 먼저 들어오도록).</summary>
            public const string Score = "text-text-body";

            //.// 세그먼트 (전체 / 공식 / 친선경기) — 모양은 공용 Css.Segment에 있다(자녀 전환과 공유)

            public const string SegmentTrack = Segment.Track;

            public static string SegmentItem(bool isActive) => Segment.Item(isActive);

            //.// 집계 경계 표기

            /// <summary>친선 보조 카드 — 공식 카드와 같은 틀에 회색으로. 합산하지 않는다는 신호.</summary>
            public const string SupplementValue = "text-text-muted";

            /// <summary>목록 아래 한 줄 안내 — 왜 요약과 목록의 수가 다른지 설명한다.</summary>
            public const string Note =
                "text-[11px] text-text-muted leading-[1.6] break-keep";
        }
    }
}
