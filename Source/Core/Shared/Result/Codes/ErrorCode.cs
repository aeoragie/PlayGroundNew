using System.Collections.Concurrent;

namespace PlayGround.Shared.Result;

public sealed class ErrorCode : DetailCode
{
    private static readonly ConcurrentDictionary<int, ErrorCode> ErrorCodes = new();

    private ErrorCode(int value, string name, string message)
        : base(ResultCodes.Error, value, name, message)
    {
        if (!ErrorCodes.TryAdd(value, this))
        {
            throw new InvalidOperationException($"Error code value {value} is already defined.");
        }
    }

    /// <summary>
    /// 외부 프로젝트에서 도메인 특화 ErrorCode를 등록하기 위한 팩토리 메서드
    /// </summary>
    public static ErrorCode Register(int value, string name, string message)
    {
        return new ErrorCode(value, name, message);
    }

    #region Client Errors

    public static readonly ErrorCode InvalidInput = new(DetailCodeRange.Error.Client.Min, "InvalidInput", "The input value is invalid.");
    public static readonly ErrorCode InvalidFormat = new(DetailCodeRange.Error.Client.Min + 1, "InvalidFormat", "The format of the input value is incorrect.");
    public static readonly ErrorCode MissingRequired = new(DetailCodeRange.Error.Client.Min + 2, "MissingRequired", "A required input value is missing.");
    public static readonly ErrorCode OutOfRange = new(DetailCodeRange.Error.Client.Min + 3, "OutOfRange", "The input value is out of the allowed range.");
    public static readonly ErrorCode DuplicateValue = new(DetailCodeRange.Error.Client.Min + 4, "DuplicateValue", "The input value is duplicated.");
    public static readonly ErrorCode InvalidFileType = new(DetailCodeRange.Error.Client.Min + 5, "InvalidFileType", "The file type is not supported.");
    public static readonly ErrorCode FileSizeExceeded = new(DetailCodeRange.Error.Client.Min + 6, "FileSizeExceeded", "The file size exceeds the maximum allowed size.");
    public static readonly ErrorCode InvalidOperation = new(DetailCodeRange.Error.Client.Min + 7, "InvalidOperation", "The requested operation is not valid.");
    public static readonly ErrorCode BadRequest = new(DetailCodeRange.Error.Client.Min + 8, "BadRequest", "The request is malformed or invalid.");
    public static readonly ErrorCode InvalidJson = new(DetailCodeRange.Error.Client.Min + 9, "InvalidJson", "The JSON format is invalid.");
    public static readonly ErrorCode InvalidXml = new(DetailCodeRange.Error.Client.Min + 10, "InvalidXml", "The XML format is invalid.");
    public static readonly ErrorCode InvalidUrl = new(DetailCodeRange.Error.Client.Min + 11, "InvalidUrl", "The URL format is invalid.");
    public static readonly ErrorCode InvalidEmail = new(DetailCodeRange.Error.Client.Min + 12, "InvalidEmail", "The email format is invalid.");
    public static readonly ErrorCode InvalidPhoneNumber = new(DetailCodeRange.Error.Client.Min + 13, "InvalidPhoneNumber", "The phone number format is invalid.");
    public static readonly ErrorCode InvalidDateFormat = new(DetailCodeRange.Error.Client.Min + 14, "InvalidDateFormat", "The date format is invalid.");
    public static readonly ErrorCode InvalidTimeFormat = new(DetailCodeRange.Error.Client.Min + 15, "InvalidTimeFormat", "The time format is invalid.");
    public static readonly ErrorCode InvalidGuid = new(DetailCodeRange.Error.Client.Min + 16, "InvalidGuid", "The GUID format is invalid.");
    public static readonly ErrorCode InvalidBase64 = new(DetailCodeRange.Error.Client.Min + 17, "InvalidBase64", "The Base64 format is invalid.");
    public static readonly ErrorCode TooManyItems = new(DetailCodeRange.Error.Client.Min + 18, "TooManyItems", "Too many items in the request.");
    public static readonly ErrorCode InvalidPagination = new(DetailCodeRange.Error.Client.Min + 19, "InvalidPagination", "Invalid pagination parameters.");

    #endregion

    #region Authentication / Authorization Errors

    public static readonly ErrorCode Unauthorized = new(DetailCodeRange.Error.Auth.Min, "Unauthorized", "Unauthorized request.");
    public static readonly ErrorCode Forbidden = new(DetailCodeRange.Error.Auth.Min + 1, "Forbidden", "Access to the requested resource is forbidden.");
    public static readonly ErrorCode TokenExpired = new(DetailCodeRange.Error.Auth.Min + 2, "TokenExpired", "The authentication token has expired.");
    public static readonly ErrorCode InvalidCredentials = new(DetailCodeRange.Error.Auth.Min + 3, "InvalidCredentials", "Invalid login credentials.");
    public static readonly ErrorCode SessionExpired = new(DetailCodeRange.Error.Auth.Min + 4, "SessionExpired", "The user session has expired.");
    public static readonly ErrorCode AccountLocked = new(DetailCodeRange.Error.Auth.Min + 5, "AccountLocked", "The user account is locked.");
    public static readonly ErrorCode AccountDisabled = new(DetailCodeRange.Error.Auth.Min + 6, "AccountDisabled", "The user account is disabled.");
    public static readonly ErrorCode InvalidToken = new(DetailCodeRange.Error.Auth.Min + 7, "InvalidToken", "The authentication token is invalid.");
    public static readonly ErrorCode PasswordExpired = new(DetailCodeRange.Error.Auth.Min + 8, "PasswordExpired", "The password has expired and must be changed.");
    public static readonly ErrorCode TwoFactorRequired = new(DetailCodeRange.Error.Auth.Min + 9, "TwoFactorRequired", "Two-factor authentication is required.");
    public static readonly ErrorCode InvalidApiKey = new(DetailCodeRange.Error.Auth.Min + 10, "InvalidApiKey", "The API key is invalid.");
    public static readonly ErrorCode ApiKeyExpired = new(DetailCodeRange.Error.Auth.Min + 11, "ApiKeyExpired", "The API key has expired.");
    public static readonly ErrorCode InsufficientPermissions = new(DetailCodeRange.Error.Auth.Min + 12, "InsufficientPermissions", "Insufficient permissions for this operation.");
    public static readonly ErrorCode RefreshTokenExpired = new(DetailCodeRange.Error.Auth.Min + 13, "RefreshTokenExpired", "The refresh token has expired.");
    public static readonly ErrorCode InvalidRefreshToken = new(DetailCodeRange.Error.Auth.Min + 14, "InvalidRefreshToken", "The refresh token is invalid.");
    public static readonly ErrorCode MultipleSessionsNotAllowed = new(DetailCodeRange.Error.Auth.Min + 15, "MultipleSessionsNotAllowed", "Multiple sessions are not allowed for this user.");
    public static readonly ErrorCode AccountNotVerified = new(DetailCodeRange.Error.Auth.Min + 16, "AccountNotVerified", "The user account is not verified.");
    public static readonly ErrorCode PasswordResetRequired = new(DetailCodeRange.Error.Auth.Min + 17, "PasswordResetRequired", "Password reset is required.");

    #endregion

    #region Resource Errors

    public static readonly ErrorCode NotFound = new(DetailCodeRange.Error.Resource.Min, "NotFound", "The requested resource could not be found.");
    public static readonly ErrorCode AlreadyExists = new(DetailCodeRange.Error.Resource.Min + 1, "AlreadyExists", "The resource already exists.");
    public static readonly ErrorCode Conflict = new(DetailCodeRange.Error.Resource.Min + 2, "Conflict", "A resource conflict has occurred.");
    public static readonly ErrorCode Gone = new(DetailCodeRange.Error.Resource.Min + 3, "Gone", "The requested resource is no longer available.");
    public static readonly ErrorCode TooManyRequests = new(DetailCodeRange.Error.Resource.Min + 4, "TooManyRequests", "Too many requests. Please try again later.");
    public static readonly ErrorCode ResourceLocked = new(DetailCodeRange.Error.Resource.Min + 5, "ResourceLocked", "The resource is currently locked.");
    public static readonly ErrorCode DependencyNotFound = new(DetailCodeRange.Error.Resource.Min + 6, "DependencyNotFound", "A required dependency resource was not found.");
    public static readonly ErrorCode ResourceExhausted = new(DetailCodeRange.Error.Resource.Min + 7, "ResourceExhausted", "Resource pool exhausted.");
    public static readonly ErrorCode ResourceModified = new(DetailCodeRange.Error.Resource.Min + 8, "ResourceModified", "The resource has been modified by another process.");
    public static readonly ErrorCode ResourceCorrupted = new(DetailCodeRange.Error.Resource.Min + 9, "ResourceCorrupted", "The resource is corrupted.");
    public static readonly ErrorCode ResourceTooLarge = new(DetailCodeRange.Error.Resource.Min + 10, "ResourceTooLarge", "The resource is too large to process.");
    public static readonly ErrorCode ResourceUnavailable = new(DetailCodeRange.Error.Resource.Min + 11, "ResourceUnavailable", "The resource is temporarily unavailable.");
    public static readonly ErrorCode DependencyFailed = new(DetailCodeRange.Error.Resource.Min + 12, "DependencyFailed", "A dependency operation failed.");
    public static readonly ErrorCode CircularDependency = new(DetailCodeRange.Error.Resource.Min + 13, "CircularDependency", "Circular dependency detected.");

    #endregion

    #region Business Logic Errors

    public static readonly ErrorCode BusinessRuleViolation = new(DetailCodeRange.Error.Business.Min, "BusinessRuleViolation", "A business rule has been violated.");
    public static readonly ErrorCode InvalidState = new(DetailCodeRange.Error.Business.Min + 1, "InvalidState", "The current state does not allow this operation.");
    public static readonly ErrorCode InsufficientFunds = new(DetailCodeRange.Error.Business.Min + 2, "InsufficientFunds", "Insufficient funds.");
    public static readonly ErrorCode QuotaExceeded = new(DetailCodeRange.Error.Business.Min + 3, "QuotaExceeded", "The quota has been exceeded.");
    public static readonly ErrorCode OperationNotAllowed = new(DetailCodeRange.Error.Business.Min + 4, "OperationNotAllowed", "The operation is not allowed in the current context.");
    public static readonly ErrorCode WorkflowError = new(DetailCodeRange.Error.Business.Min + 5, "WorkflowError", "An error occurred in the workflow process.");
    public static readonly ErrorCode ValidationFailed = new(DetailCodeRange.Error.Business.Min + 6, "ValidationFailed", "Business validation failed.");
    public static readonly ErrorCode OperationFailed = new(DetailCodeRange.Error.Business.Min + 7, "OperationFailed", "Business operation failed.");
    public static readonly ErrorCode TransactionFailed = new(DetailCodeRange.Error.Business.Min + 8, "TransactionFailed", "Transaction processing failed.");
    public static readonly ErrorCode PaymentRequired = new(DetailCodeRange.Error.Business.Min + 9, "PaymentRequired", "Payment is required to proceed.");
    public static readonly ErrorCode SubscriptionExpired = new(DetailCodeRange.Error.Business.Min + 10, "SubscriptionExpired", "The subscription has expired.");
    public static readonly ErrorCode LicenseExpired = new(DetailCodeRange.Error.Business.Min + 11, "LicenseExpired", "The license has expired.");
    public static readonly ErrorCode TrialExpired = new(DetailCodeRange.Error.Business.Min + 12, "TrialExpired", "The trial period has expired.");
    public static readonly ErrorCode PlanLimitExceeded = new(DetailCodeRange.Error.Business.Min + 13, "PlanLimitExceeded", "The plan limit has been exceeded.");
    public static readonly ErrorCode FeatureNotAvailable = new(DetailCodeRange.Error.Business.Min + 14, "FeatureNotAvailable", "This feature is not available in your plan.");
    public static readonly ErrorCode DuplicateOperation = new(DetailCodeRange.Error.Business.Min + 15, "DuplicateOperation", "This operation has already been performed.");
    public static readonly ErrorCode ConcurrentModification = new(DetailCodeRange.Error.Business.Min + 16, "ConcurrentModification", "The resource was modified during this operation.");
    public static readonly ErrorCode ApprovalRequired = new(DetailCodeRange.Error.Business.Min + 17, "ApprovalRequired", "This operation requires approval.");
    public static readonly ErrorCode SchedulingConflict = new(DetailCodeRange.Error.Business.Min + 18, "SchedulingConflict", "A scheduling conflict has occurred.");

    #endregion

    #region Database Errors

    public static readonly ErrorCode DatabaseError = new(DetailCodeRange.Error.Database.Min, "DatabaseError", "An error occurred during database processing.");
    public static readonly ErrorCode DatabaseConnectionFailed = new(DetailCodeRange.Error.Database.Min + 1, "DatabaseConnectionFailed", "Failed to connect to the database.");
    public static readonly ErrorCode DatabaseTimeout = new(DetailCodeRange.Error.Database.Min + 2, "DatabaseTimeout", "Database operation timed out.");
    public static readonly ErrorCode DatabaseConstraintViolation = new(DetailCodeRange.Error.Database.Min + 3, "DatabaseConstraintViolation", "Database constraint violation occurred.");
    public static readonly ErrorCode DatabaseDeadlock = new(DetailCodeRange.Error.Database.Min + 4, "DatabaseDeadlock", "Database deadlock detected.");
    public static readonly ErrorCode DatabaseTransactionFailed = new(DetailCodeRange.Error.Database.Min + 5, "DatabaseTransactionFailed", "Database transaction failed.");
    public static readonly ErrorCode DatabaseMigrationFailed = new(DetailCodeRange.Error.Database.Min + 6, "DatabaseMigrationFailed", "Database migration failed.");
    public static readonly ErrorCode DatabaseBackupFailed = new(DetailCodeRange.Error.Database.Min + 7, "DatabaseBackupFailed", "Database backup failed.");

    #endregion

    #region Network Errors

    public static readonly ErrorCode NetworkError = new(DetailCodeRange.Error.Network.Min, "NetworkError", "A network communication error occurred.");
    public static readonly ErrorCode NetworkTimeout = new(DetailCodeRange.Error.Network.Min + 1, "NetworkTimeout", "Network operation timed out.");
    public static readonly ErrorCode ConnectionLost = new(DetailCodeRange.Error.Network.Min + 2, "ConnectionLost", "Network connection was lost.");
    public static readonly ErrorCode DnsResolutionFailed = new(DetailCodeRange.Error.Network.Min + 3, "DnsResolutionFailed", "DNS resolution failed.");
    public static readonly ErrorCode SslHandshakeFailed = new(DetailCodeRange.Error.Network.Min + 4, "SslHandshakeFailed", "SSL handshake failed.");
    public static readonly ErrorCode ProxyError = new(DetailCodeRange.Error.Network.Min + 5, "ProxyError", "Proxy server error occurred.");

    #endregion

    #region External Service Errors

    public static readonly ErrorCode ExternalServiceError = new(DetailCodeRange.Error.ExternalService.Min, "ExternalServiceError", "An error occurred while communicating with an external service.");
    public static readonly ErrorCode ExternalServiceUnavailable = new(DetailCodeRange.Error.ExternalService.Min + 1, "ExternalServiceUnavailable", "External service is currently unavailable.");
    public static readonly ErrorCode ExternalServiceTimeout = new(DetailCodeRange.Error.ExternalService.Min + 2, "ExternalServiceTimeout", "External service request timed out.");
    public static readonly ErrorCode ExternalServiceRateLimited = new(DetailCodeRange.Error.ExternalService.Min + 3, "ExternalServiceRateLimited", "External service rate limit exceeded.");
    public static readonly ErrorCode ExternalServiceAuthFailed = new(DetailCodeRange.Error.ExternalService.Min + 4, "ExternalServiceAuthFailed", "External service authentication failed.");
    public static readonly ErrorCode ExternalServiceMaintenance = new(DetailCodeRange.Error.ExternalService.Min + 5, "ExternalServiceMaintenance", "External service is under maintenance.");

    #endregion

    #region Cache Errors

    public static readonly ErrorCode CacheError = new(DetailCodeRange.Error.Cache.Min, "CacheError", "An error occurred with the cache system.");
    public static readonly ErrorCode CacheUnavailable = new(DetailCodeRange.Error.Cache.Min + 1, "CacheUnavailable", "Cache system is unavailable.");
    public static readonly ErrorCode CacheTimeout = new(DetailCodeRange.Error.Cache.Min + 2, "CacheTimeout", "Cache operation timed out.");
    public static readonly ErrorCode CacheKeyNotFound = new(DetailCodeRange.Error.Cache.Min + 3, "CacheKeyNotFound", "Cache key not found.");
    public static readonly ErrorCode CacheSerializationError = new(DetailCodeRange.Error.Cache.Min + 4, "CacheSerializationError", "Cache serialization error occurred.");

    #endregion

    #region Messaging Errors

    public static readonly ErrorCode MessagingError = new(DetailCodeRange.Error.Messaging.Min, "MessagingError", "An error occurred in the messaging system.");
    public static readonly ErrorCode MessageQueueFull = new(DetailCodeRange.Error.Messaging.Min + 1, "MessageQueueFull", "Message queue is full.");
    public static readonly ErrorCode MessageSerializationError = new(DetailCodeRange.Error.Messaging.Min + 2, "MessageSerializationError", "Message serialization error occurred.");
    public static readonly ErrorCode MessageDeliveryFailed = new(DetailCodeRange.Error.Messaging.Min + 3, "MessageDeliveryFailed", "Message delivery failed.");
    public static readonly ErrorCode MessageTimeout = new(DetailCodeRange.Error.Messaging.Min + 4, "MessageTimeout", "Message processing timed out.");
    public static readonly ErrorCode MessageDuplicateDetected = new(DetailCodeRange.Error.Messaging.Min + 5, "MessageDuplicateDetected", "Duplicate message detected.");
    public static readonly ErrorCode MessageFormatError = new(DetailCodeRange.Error.Messaging.Min + 6, "MessageFormatError", "Message format error.");

    #endregion

    #region Configuration Errors

    public static readonly ErrorCode ConfigurationError = new(DetailCodeRange.Error.Configuration.Min, "ConfigurationError", "A configuration error occurred.");
    public static readonly ErrorCode ConfigurationMissing = new(DetailCodeRange.Error.Configuration.Min + 1, "ConfigurationMissing", "Required configuration is missing.");
    public static readonly ErrorCode ConfigurationInvalid = new(DetailCodeRange.Error.Configuration.Min + 2, "ConfigurationInvalid", "Configuration value is invalid.");
    public static readonly ErrorCode EnvironmentVariableMissing = new(DetailCodeRange.Error.Configuration.Min + 3, "EnvironmentVariableMissing", "Required environment variable is missing.");

    #endregion

    #region Service Errors

    public static readonly ErrorCode ServiceUnavailable = new(DetailCodeRange.Error.Service.Min, "ServiceUnavailable", "The service is temporarily unavailable.");
    public static readonly ErrorCode ServiceOverloaded = new(DetailCodeRange.Error.Service.Min + 1, "ServiceOverloaded", "The service is overloaded.");
    public static readonly ErrorCode ServiceDeprecated = new(DetailCodeRange.Error.Service.Min + 2, "ServiceDeprecated", "The service version is deprecated.");

    #endregion

    #region Maintenance Errors

    public static readonly ErrorCode MaintenanceMode = new(DetailCodeRange.Error.Maintenance.Min, "MaintenanceMode", "The system is currently under maintenance.");
    public static readonly ErrorCode ScheduledMaintenance = new(DetailCodeRange.Error.Maintenance.Min + 1, "ScheduledMaintenance", "The system is under scheduled maintenance.");

    #endregion

    #region Processing Errors

    public static readonly ErrorCode InvalidParameter = new(DetailCodeRange.Error.Processing.Min, "InvalidParameter", "Parameter is null or invalid.");
    public static readonly ErrorCode SerializationError = new(DetailCodeRange.Error.Processing.Min + 1, "SerializationError", "Serialization error occurred.");
    public static readonly ErrorCode DeserializationError = new(DetailCodeRange.Error.Processing.Min + 2, "DeserializationError", "Deserialization error occurred.");
    public static readonly ErrorCode CompressionError = new(DetailCodeRange.Error.Processing.Min + 3, "CompressionError", "Compression error occurred.");
    public static readonly ErrorCode EncryptionError = new(DetailCodeRange.Error.Processing.Min + 4, "EncryptionError", "Encryption error occurred.");
    public static readonly ErrorCode DecryptionError = new(DetailCodeRange.Error.Processing.Min + 5, "DecryptionError", "Decryption error occurred.");

    #endregion

    #region Internal Errors

    public static readonly ErrorCode InternalError = new(DetailCodeRange.Error.Internal.Min, "InternalError", "An internal error occurred during processing.");
    public static readonly ErrorCode UnknownError = new(DetailCodeRange.Error.Internal.Max, "UnknownError", "An unknown error has occurred.");

    #endregion

    #region Utility Methods

    public static ErrorCode? GetByValue(int value)
    {
        return ErrorCodes.TryGetValue(value, out var errorCode) ? errorCode : null;
    }

    public static IEnumerable<ErrorCode> GetAll()
    {
        return ErrorCodes.Values.OrderBy(x => x.Value);
    }

    public static IEnumerable<ErrorCode> GetByRange(int minValue, int maxValue)
    {
        return ErrorCodes.Values.Where(x => x.Value >= minValue && x.Value <= maxValue).OrderBy(x => x.Value);
    }

    public static IEnumerable<ErrorCode> GetByCategory(string category)
    {
        return category.ToLower() switch
        {
            "client" => GetClientErrors(),
            "auth" => GetAuthErrors(),
            "resource" => GetResourceErrors(),
            "business" => GetBusinessErrors(),
            "system" => GetSystemErrors(),
            _ => Enumerable.Empty<ErrorCode>()
        };
    }

    public static IEnumerable<ErrorCode> GetClientErrors() => GetByRange(DetailCodeRange.Error.Client.Min, DetailCodeRange.Error.Client.Max);
    public static IEnumerable<ErrorCode> GetAuthErrors() => GetByRange(DetailCodeRange.Error.Auth.Min, DetailCodeRange.Error.Auth.Max);
    public static IEnumerable<ErrorCode> GetResourceErrors() => GetByRange(DetailCodeRange.Error.Resource.Min, DetailCodeRange.Error.Resource.Max);
    public static IEnumerable<ErrorCode> GetUserErrors() => GetByRange(DetailCodeRange.Error.Client.Min, DetailCodeRange.Error.Resource.Max);
    public static IEnumerable<ErrorCode> GetBusinessErrors() => GetByRange(DetailCodeRange.Error.Business.Min, DetailCodeRange.Error.Business.Max);
    public static IEnumerable<ErrorCode> GetBusinessLogicErrors() => GetByRange(DetailCodeRange.Error.Business.Min, DetailCodeRange.Error.Sports.Max);
    public static IEnumerable<ErrorCode> GetSystemErrors() => GetByRange(DetailCodeRange.Error.Database.Min, DetailCodeRange.Error.Internal.Max);

    public bool IsClientError => DetailCodeRange.IsClientError(Value);
    public bool IsAuthError => DetailCodeRange.IsAuthError(Value);
    public bool IsResourceError => DetailCodeRange.IsResourceError(Value);
    public bool IsBusinessError => DetailCodeRange.IsBusinessError(Value);
    public bool IsSportsError => DetailCodeRange.IsSportsError(Value);
    public bool IsSystemError => DetailCodeRange.IsSystemError(Value);

    public bool IsUserError => DetailCodeRange.IsUserError(Value);
    public bool IsBusinessLogicError => DetailCodeRange.IsBusinessLogicError(Value);

    public bool IsRetryable => this == NetworkTimeout || this == DatabaseTimeout ||
                              this == ExternalServiceTimeout || this == ServiceUnavailable ||
                              this == MessageTimeout || this == CacheTimeout;

    public bool IsCritical => this == DatabaseError || this == InternalError ||
                             this == UnknownError || this == DatabaseDeadlock;

    #endregion
}
