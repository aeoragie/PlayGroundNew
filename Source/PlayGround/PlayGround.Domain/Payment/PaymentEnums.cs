using System.Text.Json.Serialization;
using PlayGround.Shared.Http;

namespace PlayGround.Domain.Payment
{
    // 결제는 종목 공통 기능이라 Soccer 어휘 파일에 두지 않는다. 종목 구분은 Sport 값으로 한다.

    [JsonConverter(typeof(LenientEnumJsonConverter<PaymentStatus>))]
    public enum PaymentStatus
    {
        Unknown = 0,
        Pending,
        Approved,
        Failed,
        Canceled,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<PaymentProvider>))]
    public enum PaymentProvider
    {
        Unknown = 0,
        Toss,
    }

    [JsonConverter(typeof(LenientEnumJsonConverter<Sport>))]
    public enum Sport
    {
        Unknown = 0,
        Soccer,
    }
}
