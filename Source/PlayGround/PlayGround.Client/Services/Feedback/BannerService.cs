using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayGround.Client.Services.Feedback
{
    /// <summary>배너 심각도 — 색·아이콘·닫기 가능 여부가 달라진다 (Design.BannerStepper §1).
    /// **teal(첫 사용 힌트)은 여기 없다** — 학습용은 TooltipHelp, 운영용은 이 3톤으로 구분한다.</summary>
    public enum BannerSeverity
    {
        /// <summary>정보(회색 ⓘ) — 서버 점검·정책 공지. **X로 닫을 수 있다**(닫음 상태는 로컬 저장).</summary>
        Info,
        /// <summary>주의(오렌지 !) — Claim 장기 대기 등. 닫기 없음 — 원인이 해소돼야 사라진다.</summary>
        Warning,
        /// <summary>오류(연레드 !) — 결제 만료 등. 닫기 없음 + 해결 링크.</summary>
        Error,
    }

    /// <summary>화면 최상단에 떠 있는 배너 1개.</summary>
    public sealed class BannerNotice
    {
        /// <summary>닫음 상태를 로컬에 저장하는 키 — 정보 배너를 다시 띄우지 않으려면 안정적이어야 한다.</summary>
        public required string Id { get; init; }

        public required BannerSeverity Severity { get; init; }

        /// <summary>굵은 접두어 — "정보"·"주의"·"오류" 대신 맥락 단어를 줄 수 있다.</summary>
        public required string Prefix { get; init; }

        /// <summary>본문 문장. 접두어 뒤에 이어 붙는다.</summary>
        public required string Message { get; init; }

        public string? LinkLabel { get; init; }

        public string? LinkHref { get; init; }

        /// <summary>정보 배너만 닫을 수 있다 — 주의·오류는 원인이 해소돼야 소멸한다.</summary>
        public bool CanDismiss => Severity == BannerSeverity.Info;

        public bool HasLink => LinkLabel is not null && LinkHref is not null;
    }

    /// <summary>시스템·운영 배너 — **콘텐츠 최상단 풀폭, 동시 1개(심각도 우선: 오류 > 주의 > 정보)**.
    /// 여러 배너가 올라와도 가장 심각한 하나만 보여준다. 정보 배너는 사용자가 닫으면 로컬에
    /// 기록해 다시 띄우지 않는다(닫음 판정은 호스트가 localStorage로 확인한다).</summary>
    public sealed class BannerService
    {
        private readonly List<BannerNotice> mNotices = new();

        public event Action? OnChanged;

        /// <summary>표시 후보를 등록한다. 같은 Id가 있으면 교체한다(중복 누적 방지).</summary>
        public void Publish(BannerNotice notice)
        {
            ArgumentNullException.ThrowIfNull(notice);

            mNotices.RemoveAll(n => n.Id == notice.Id);
            mNotices.Add(notice);
            OnChanged?.Invoke();
        }

        /// <summary>원인이 해소된 배너를 내린다(주의·오류의 정상 소멸 경로).</summary>
        public void Withdraw(string id)
        {
            if (mNotices.RemoveAll(n => n.Id == id) > 0)
            {
                OnChanged?.Invoke();
            }
        }

        public void Clear()
        {
            if (mNotices.Count == 0)
            {
                return;
            }

            mNotices.Clear();
            OnChanged?.Invoke();
        }

        /// <summary>닫힌 정보 배너(dismissedIds)를 뺀 뒤 심각도 우선으로 하나만 고른다.</summary>
        public BannerNotice? Resolve(ISet<string> dismissedIds)
        {
            ArgumentNullException.ThrowIfNull(dismissedIds);

            return mNotices
                .Where(n => !(n.CanDismiss && dismissedIds.Contains(n.Id)))
                .OrderByDescending(n => n.Severity)
                .FirstOrDefault();
        }
    }
}
