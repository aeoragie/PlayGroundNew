using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Contracts.Notification;
using PlayGround.Application.Interfaces;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>알림 센터 저장소 (Soccer DB). 다중 결과셋(카운트+목록) — MultiQueryReader 소비.</summary>
    public class SoccerNotificationRepository : RepositoryBase, INotificationRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public SoccerNotificationRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<NotificationsResponse>> GetByUserAsync(Guid userId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Notifications requested", ("UserId", userId));

            var procedure = new UspGetSoccerNotificationsByUser(this) { UserId = userId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Notifications query failed", ("DetailCode", opened.ResultData.DetailCode));
                return Result<NotificationsResponse>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            int unread = await reader.ReadSingleOrDefaultAsync<int>();
            var rows = (await reader.ReadAsync<SoccerNotificationRecord>()).ToList();

            var response = new NotificationsResponse
            {
                UnreadCount = unread,
                Items = rows
                    .Select(n => new NotificationDto
                    {
                        NotificationId = n.NotificationId,
                        Type = n.NotificationType,
                        RefId = n.RefId,
                        TargetPlayerId = n.TargetPlayerId,
                        ActorName = NullIfEmpty(n.ActorName),
                        PlayerName = NullIfEmpty(n.PlayerName),
                        TeamName = NullIfEmpty(n.TeamName),
                        MetaText = NullIfEmpty(n.MetaText),
                        SubText = NullIfEmpty(n.SubText),
                        Relation = NullIfEmpty(n.Relation),
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt,
                        RequestStatus = NullIfEmpty(n.Status)
                    })
                    .ToList()
            };

            Logger.InfoWith("Notifications received", ("UserId", userId), ("Unread", unread), ("Items", response.Items.Count));
            return Result<NotificationsResponse>.Success(response);
        }

        public async Task<Result<bool>> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Notification read requested", ("UserId", userId), ("NotificationId", notificationId));

            var procedure = new UspMarkSoccerNotificationRead(this) { UserId = userId, NotificationId = notificationId };
            var queryResult = await procedure.QueryAsync<SoccerNotificationReadRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "MarkNotificationRead");
            }

            return Result<bool>.Success(queryResult.Values1.Count > 0);
        }

        public async Task<Result<List<NotificationRecipient>>> GetMatchResultRecipientsAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerMatchResultRecipients(this) { ManagerUserId = managerUserId };
            var queryResult = await procedure.QueryAsync<SoccerMatchResultRecipientRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<List<NotificationRecipient>>.Error(ErrorCode.DatabaseError, "GetMatchResultRecipients");
            }

            List<NotificationRecipient> recipients = queryResult.Values1
                .Where(r => r.UserId is not null)
                .Select(r => new NotificationRecipient
                {
                    UserId = r.UserId!.Value,
                    PlayerId = r.PlayerId,
                    PlayerName = r.Name,
                    TeamName = r.TeamName
                })
                .ToList();

            return Result<List<NotificationRecipient>>.Success(recipients);
        }

        public async Task<Result<bool>> CreateAsync(Guid recipientUserId, string type, Guid refId, Guid? targetPlayerId,
            string? actorName, string? playerName, string? teamName, string? metaText, string? subText,
            CancellationToken cancellation = default)
        {
            Logger.InfoWith("Notification creation requested", ("RecipientUserId", recipientUserId), ("Type", type));

            var procedure = new UspCreateSoccerNotification(this)
            {
                RecipientUserId = recipientUserId,
                NotificationType = type,
                RefId = refId,
                TargetPlayerId = targetPlayerId,
                ActorName = actorName!,
                PlayerName = playerName!,
                TeamName = teamName!,
                MetaText = metaText!,
                SubText = subText!
            };
            var queryResult = await procedure.QueryAsync<SoccerNotificationCreatedRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "CreateNotification");
            }

            return Result<bool>.Success(queryResult.Values1.Count > 0);
        }

        private static string? NullIfEmpty(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
