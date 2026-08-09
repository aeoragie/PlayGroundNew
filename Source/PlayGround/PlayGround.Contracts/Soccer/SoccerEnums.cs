using System.Text.Json.Serialization;
using PlayGround.Shared.Http;

namespace PlayGround.Contracts.Soccer
{
    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerAgeGroup>))]
    public enum SoccerAgeGroup
    {
        Unknown = 0,
        U12,
        U15,
        U18,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerPosition>))]
    public enum SoccerPosition
    {
        Unknown = 0,
        GK,
        DF,
        MF,
        FW,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerGrade>))]
    public enum SoccerGrade
    {
        Unknown = 0,
        U7,
        U8,
        U9,
        U10,
        U11,
        U12,
        U13,
        U14,
        U15,
        U16,
        U17,
        U18,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<SoccerPreferredFoot>))]
    public enum SoccerPreferredFoot
    {
        Unknown = 0,
        Left,
        Right,
        Both,
    }
}
