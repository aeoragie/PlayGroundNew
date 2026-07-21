using System.Net.Http.Json;
using PlayGround.Shared.Http;
using PlayGround.Contracts.Notification;

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

        /// <summary>읽음 처리 — 이동형 클릭 시.</summary>
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
