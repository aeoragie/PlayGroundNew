using PlayGround.Contracts.Notification;
using PlayGround.Shared.Http;
using System.Net.Http.Json;

namespace PlayGround.Client.Services
{
    /// <summary>알림 센터 API — 벨 카운트와 목록이 같은 응답을 공유한다(수가 어긋나면 안 된다).</summary>
    public class NotificationClient
    {
        private readonly HttpClient mHttp;

        public NotificationClient(HttpClient http)
        {
            mHttp = http ?? throw new ArgumentNullException(nameof(http));
        }

        /// <summary>미읽음 카운트 + 최근 목록. 오류 시 null.</summary>
        public async Task<NotificationsResponse?> GetAsync()
        {
            try
            {
                Envelope<NotificationsResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<NotificationsResponse>>("api/soccer/notifications/me");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>알림 센터 페이지 — 세그먼트 필터(all|action|unread) + 페이지네이션. 오류 시 null.</summary>
        public async Task<NotificationPageResponse?> GetPageAsync(string filter, int offset, int limit)
        {
            try
            {
                Envelope<NotificationPageResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<NotificationPageResponse>>(
                        $"api/soccer/notifications/me/page?filter={filter}&offset={offset}&limit={limit}");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>여러 건 읽음 처리 — 페이지 진입 시 화면에 보인 알림. 성공 여부만.</summary>
        public async Task<bool> MarkReadBulkAsync(IReadOnlyCollection<Guid> notificationIds)
        {
            if (notificationIds.Count == 0)
            {
                return true;
            }

            try
            {
                HttpResponseMessage response = await mHttp.PutAsJsonAsync(
                    "api/soccer/notifications/me/read", new MarkNotificationsReadRequest { NotificationIds = notificationIds.ToList() });
                Envelope<int>? envelope = await response.Content.ReadFromJsonAsync<Envelope<int>>();
                return envelope is { IsSuccess: true };
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MarkReadAsync(Guid notificationId)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PutAsync($"api/soccer/notifications/me/{notificationId}/read", null);
                Envelope<bool>? envelope = await response.Content.ReadFromJsonAsync<Envelope<bool>>();
                return envelope is { IsSuccess: true };
            }
            catch
            {
                return false;
            }
        }
    }
}
