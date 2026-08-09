using System.Text.Json.Serialization;
using PlayGround.Shared.Http;

namespace PlayGround.Contracts.Soccer
{
    // 와이어(JSON)는 멤버 이름 문자열, DB도 멤버 이름 문자열(변환은 Persistence 경계에서만).
    // Unknown(0)은 저장·전송 값이 아니다 — 미지 값 폴백 전용이라 쓰는 쪽이 "값 없음"으로 다룬다.

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

    /// <summary>학년 — 국가 학제 대신 나이 기준 U표기. 화면 표시도 당분간 이 표기 그대로다(국가별 표기는 추후 결정).</summary>
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
