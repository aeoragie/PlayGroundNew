namespace PlayGround.Shared.Result;

/// <summary>
/// DetailCode 값 범위를 정의하는 상수 클래스
/// </summary>
public static class DetailCodeRange
{
    #region Success Codes (0-999)

    public static class Success
    {
        public const int Min = 0;
        public const int Max = 999;

        public static class Basic
        {
            public const int Min = 0;
            public const int Max = 99;
        }

        public static class Crud
        {
            public const int Min = 100;
            public const int Max = 199;
        }

        public static class Auth
        {
            public const int Min = 200;
            public const int Max = 299;
        }

        public static class User
        {
            public const int Min = 300;
            public const int Max = 399;
        }

        public static class File
        {
            public const int Min = 400;
            public const int Max = 499;
        }

        public static class Communication
        {
            public const int Min = 500;
            public const int Max = 599;
        }

        public static class Sports
        {
            public const int Min = 600;
            public const int Max = 699;
        }

        public static class Process
        {
            public const int Min = 700;
            public const int Max = 799;
        }

        public static class System
        {
            public const int Min = 800;
            public const int Max = 899;
        }

        public static class Data
        {
            public const int Min = 900;
            public const int Max = 999;
        }
    }

    #endregion

    #region Error Codes (1000-3999)

    public static class Error
    {
        public const int Min = 1000;
        public const int Max = 3999;

        public static class Client
        {
            public const int Min = 1000;
            public const int Max = 1099;
        }

        public static class Auth
        {
            public const int Min = 1100;
            public const int Max = 1199;
        }

        public static class Resource
        {
            public const int Min = 1200;
            public const int Max = 1299;
        }

        public static class Business
        {
            public const int Min = 2000;
            public const int Max = 2099;
        }

        public static class Sports
        {
            public const int Min = 2100;
            public const int Max = 2199;
        }

        public static class Database
        {
            public const int Min = 3000;
            public const int Max = 3099;
        }

        public static class Network
        {
            public const int Min = 3100;
            public const int Max = 3199;
        }

        public static class ExternalService
        {
            public const int Min = 3200;
            public const int Max = 3299;
        }

        public static class Cache
        {
            public const int Min = 3300;
            public const int Max = 3399;
        }

        public static class Messaging
        {
            public const int Min = 3400;
            public const int Max = 3499;
        }

        public static class Configuration
        {
            public const int Min = 3500;
            public const int Max = 3599;
        }

        public static class Service
        {
            public const int Min = 3600;
            public const int Max = 3699;
        }

        public static class Maintenance
        {
            public const int Min = 3700;
            public const int Max = 3799;
        }

        public static class Processing
        {
            public const int Min = 3800;
            public const int Max = 3899;
        }

        public static class Internal
        {
            public const int Min = 3900;
            public const int Max = 3999;
        }
    }

    #endregion

    #region Warning Codes (5000-5999)

    public static class Warning
    {
        public const int Min = 5000;
        public const int Max = 5999;

        public static class General
        {
            public const int Min = 5000;
            public const int Max = 5099;
        }

        public static class Data
        {
            public const int Min = 5100;
            public const int Max = 5199;
        }

        public static class Security
        {
            public const int Min = 5200;
            public const int Max = 5299;
        }

        public static class Performance
        {
            public const int Min = 5300;
            public const int Max = 5399;
        }

        public static class Business
        {
            public const int Min = 5400;
            public const int Max = 5499;
        }

        public static class Sports
        {
            public const int Min = 5500;
            public const int Max = 5599;
        }

        public static class System
        {
            public const int Min = 5600;
            public const int Max = 5699;
        }

        public static class Integration
        {
            public const int Min = 5700;
            public const int Max = 5799;
        }

        public static class UserExperience
        {
            public const int Min = 5800;
            public const int Max = 5899;
        }
    }

    #endregion

    #region Information Codes (6000-6999)

    public static class Information
    {
        public const int Min = 6000;
        public const int Max = 6999;

        public static class Crud
        {
            public const int Min = 6000;
            public const int Max = 6099;
        }

        public static class Process
        {
            public const int Min = 6100;
            public const int Max = 6199;
        }

        public static class User
        {
            public const int Min = 6200;
            public const int Max = 6299;
        }

        public static class File
        {
            public const int Min = 6300;
            public const int Max = 6399;
        }

        public static class Communication
        {
            public const int Min = 6400;
            public const int Max = 6499;
        }

        public static class Sports
        {
            public const int Min = 6500;
            public const int Max = 6599;
        }

        public static class System
        {
            public const int Min = 6600;
            public const int Max = 6699;
        }

        public static class Status
        {
            public const int Min = 6700;
            public const int Max = 6799;
        }
    }

    #endregion

    #region Utility Methods

    public static bool IsInRange(int value, int min, int max)
    {
        return value >= min && value <= max;
    }

    public static bool IsSuccessCode(int value) => IsInRange(value, Success.Min, Success.Max);
    public static bool IsErrorCode(int value) => IsInRange(value, Error.Min, Error.Max);
    public static bool IsWarningCode(int value) => IsInRange(value, Warning.Min, Warning.Max);
    public static bool IsInformationCode(int value) => IsInRange(value, Information.Min, Information.Max);

    // 세분화된 에러 카테고리 (1000-1299: 사용자/클라이언트 측 에러)
    public static bool IsClientError(int value) => IsInRange(value, Error.Client.Min, Error.Client.Max);
    public static bool IsAuthError(int value) => IsInRange(value, Error.Auth.Min, Error.Auth.Max);
    public static bool IsResourceError(int value) => IsInRange(value, Error.Resource.Min, Error.Resource.Max);

    // 비즈니스 에러 (2000-2199)
    public static bool IsBusinessError(int value) => IsInRange(value, Error.Business.Min, Error.Business.Max);
    public static bool IsSportsError(int value) => IsInRange(value, Error.Sports.Min, Error.Sports.Max);

    // 시스템/인프라 에러 (3000-3999)
    public static bool IsSystemError(int value) => IsInRange(value, Error.Database.Min, Error.Internal.Max);
    public static bool IsDatabaseError(int value) => IsInRange(value, Error.Database.Min, Error.Database.Max);
    public static bool IsNetworkError(int value) => IsInRange(value, Error.Network.Min, Error.Network.Max);
    public static bool IsExternalServiceError(int value) => IsInRange(value, Error.ExternalService.Min, Error.ExternalService.Max);

    // 상위 카테고리 (여러 하위 카테고리 포함)
    public static bool IsUserError(int value) => IsInRange(value, Error.Client.Min, Error.Resource.Max);
    public static bool IsBusinessLogicError(int value) => IsInRange(value, Error.Business.Min, Error.Sports.Max);

    public static string GetCategoryName(int value)
    {
        return value switch
        {
            >= Success.Min and <= Success.Max => "Success",
            >= Error.Client.Min and <= Error.Resource.Max => "ClientError",
            >= Error.Business.Min and <= Error.Sports.Max => "BusinessError",
            >= Error.Database.Min and <= Error.Internal.Max => "SystemError",
            >= Warning.Min and <= Warning.Max => "Warning",
            >= Information.Min and <= Information.Max => "Information",
            _ => "Unknown"
        };
    }

    #endregion
}
