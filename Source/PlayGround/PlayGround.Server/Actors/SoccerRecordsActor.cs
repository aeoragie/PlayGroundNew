using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Actor;
using PlayGround.Contracts.Records;
using PlayGround.Application.Records.Commands;

namespace PlayGround.Server.Actors
{
    /// <summary>공개 경기기록(Records) 읽기 액터. Controller → 액터 → 유즈케이스 → DB.</summary>
    public sealed class SoccerRecordsActor : ReceiveActorBase
    {
        public SoccerRecordsActor(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RegisterHandlerAsync<GetSoccerRecordsTournamentsMessage>(HandleGetTournamentsAsync);
            RegisterHandlerAsync<GetSoccerRecordsTournamentDetailMessage>(HandleGetTournamentDetailAsync);
        }

        private async Task HandleGetTournamentsAsync(GetSoccerRecordsTournamentsMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerRecordsTournamentsCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerRecordsTournamentsCommand>();
            Result<RecordsTournamentsResponse> result = await useCase.ExecuteAsync(message.SeasonYear);
            sender.Tell(result);
        }

        private async Task HandleGetTournamentDetailAsync(GetSoccerRecordsTournamentDetailMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerRecordsTournamentDetailCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerRecordsTournamentDetailCommand>();
            Result<RecordsTournamentDetailResponse> result = await useCase.ExecuteAsync(message.TournamentId);
            sender.Tell(result);
        }
    }
}
