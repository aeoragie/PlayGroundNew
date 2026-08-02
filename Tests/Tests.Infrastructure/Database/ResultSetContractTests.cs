using System.Data;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;
using PlayGround.Infrastructure.Database;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;

namespace PlayGround.Tests.Infrastructure.Database
{
    /// <summary>
    /// **다중 결과셋 프로시저 ↔ 읽는 쪽 타입 순서**의 계약.
    ///
    /// 파라미터 계약(<see cref="ProcedureContractTests"/>)이 통과해도 결과셋은 여전히 어긋날 수 있다:
    /// SELECT를 하나 끼워 넣으면 그 뒤 순서가 통째로 밀리고, 컬럼을 개명하면 Dapper가 조용히
    /// 기본값을 채운다(예외가 아니라 **잘못된 값**이 화면에 나온다).
    ///
    /// 결과가 없는 파라미터로 실행해 **스키마만** 확인하므로 시드 데이터가 필요 없다
    /// (대상 프로시저들은 조기 반환이 없어 빈 결과에서도 모든 SELECT를 낸다).
    /// </summary>
    [Collection(LocalDatabaseCollection.Name)]
    public class ResultSetContractTests(LocalDatabaseFixture fixture)
    {
        /// <summary>결과셋 순서 = 저장소가 reader에서 읽는 순서. 바꾸면 여기도 바꾼다.</summary>
        public static TheoryData<string, string, Type[]> Contracts => new()
        {
            {
                // SoccerRecordsRepository.GetMatchDetailAsync
                "UspGetSoccerMatchDetail", "MatchId",
                [
                    typeof(SoccerMatchesEntity),
                    typeof(SoccerTournamentsEntity),
                    typeof(SoccerMatchEventsEntity),
                    typeof(SoccerMatchAppearancesEntity),
                    typeof(SoccerPlayersEntity),          // PlayerId·Slug만 부분 매핑
                ]
            },
            {
                // SoccerRecordsRepository.GetTournamentDetailAsync — ⑧·⑨는 스칼라라 별도 검증
                "UspGetSoccerTournamentDetail", "TournamentId",
                [
                    typeof(SoccerTournamentsEntity),
                    typeof(SoccerTournamentStandingsEntity),
                    typeof(SoccerMatchesEntity),
                    typeof(SoccerTournamentAwardsEntity),
                    typeof(SoccerSeriesChampionRecord),
                    typeof(SoccerMatchVideosEntity),
                    typeof(SoccerTournamentNewsEntity),
                    typeof(SoccerTeamsEntity),            // TeamId·Slug만 부분 매핑
                ]
            },
        };

        [Theory]
        [MemberData(nameof(Contracts))]
        public void 결과셋_개수가_읽는_순서와_같다(string procedure, string idParameter, Type[] expected)
        {
            fixture.SkipIfUnavailable();

            List<string[]> sets = ReadResultSetColumns(procedure, idParameter);

            sets.Count.Should().BeGreaterThanOrEqualTo(expected.Length,
                $"{procedure}의 결과셋이 읽는 쪽보다 적다 — SELECT가 빠졌거나 순서가 바뀌었다");
        }

        [Theory]
        [MemberData(nameof(Contracts))]
        public void 각_결과셋의_컬럼을_매핑_타입이_받을_수_있다(string procedure, string idParameter, Type[] expected)
        {
            fixture.SkipIfUnavailable();

            List<string[]> sets = ReadResultSetColumns(procedure, idParameter);

            for (int i = 0; i < expected.Length && i < sets.Count; i++)
            {
                var properties = expected[i]
                    .GetProperties()
                    .Select(p => p.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // 프로시저가 내는데 타입에 없는 컬럼 → Dapper가 버린다. 컬럼 개명·추가가 여기서 걸린다.
                // (반대 방향은 부분 매핑이 정상이라 검사하지 않는다 — 슬림 조회는 일부 컬럼만 낸다.)
                sets[i].Where(column => !properties.Contains(column))
                    .Should().BeEmpty(
                        $"{procedure} {i + 1}번째 결과셋의 컬럼을 {expected[i].Name}이(가) 받지 못한다 " +
                        "— 제너레이터를 다시 돌리거나 읽는 타입을 맞춘다");
            }
        }

        [Fact]
        public void 경기_상세는_스코어보드에_필요한_컬럼을_낸다()
        {
            // 공식 경기 상세(전후반·PK·주심·감독)는 대회 서비스 선반영 스키마다 — 조용히 빠지면
            // 화면에서 값이 사라지는 것으로만 드러난다.
            fixture.SkipIfUnavailable();

            string[] first = ReadResultSetColumns("UspGetSoccerMatchDetail", "MatchId")[0];

            first.Should().Contain(["FirstHalfHomeScore", "FirstHalfAwayScore", "HomePkScore", "AwayPkScore",
                "RefereeName", "MatchSequence", "HomeCoachName", "AwayCoachName"]);
        }

        /// <summary>결과가 없는 식별자로 실행해 각 결과셋의 컬럼 이름만 뽑는다.</summary>
        private static List<string[]> ReadResultSetColumns(string procedure, string idParameter)
        {
            using SqlConnection connection = new(LocalDatabaseFixture.ConnectionStringFor(DatabaseTypes.Soccer));
            connection.Open();

            using SqlCommand command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedure;
            command.Parameters.Add(new SqlParameter($"@{idParameter}", SqlDbType.UniqueIdentifier)
            {
                Value = Guid.Empty,
            });

            var sets = new List<string[]>();
            using SqlDataReader reader = command.ExecuteReader();
            do
            {
                if (reader.FieldCount > 0)
                {
                    sets.Add(Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray());
                }
            }
            while (reader.NextResult());

            return sets;
        }
    }
}
