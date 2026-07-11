using System.Collections.Concurrent;

namespace PlayGround.Shared.Result;

public sealed class WarningCode : DetailCode
{
    private static readonly ConcurrentDictionary<int, WarningCode> WarningCodes = new();

    private WarningCode(int value, string name, string message)
        : base(ResultCodes.Warning, value, name, message)
    {
        if (!WarningCodes.TryAdd(value, this))
        {
            throw new InvalidOperationException($"Warning code value {value} is already defined.");
        }
    }

    /// <summary>
    /// 외부 프로젝트에서 도메인 특화 WarningCode를 등록하기 위한 팩토리 메서드
    /// </summary>
    public static WarningCode Register(int value, string name, string message)
    {
        return new WarningCode(value, name, message);
    }

    #region General Warnings

    public static readonly WarningCode DeprecatedFeature = new(DetailCodeRange.Warning.General.Min, "DeprecatedFeature", "The requested feature is deprecated.");
    public static readonly WarningCode PerformanceIssue = new(DetailCodeRange.Warning.General.Min + 1, "PerformanceIssue", "A performance issue was detected during request processing.");
    public static readonly WarningCode PartialSuccess = new(DetailCodeRange.Warning.General.Min + 2, "PartialSuccess", "The operation was partially successful.");
    public static readonly WarningCode DataIncomplete = new(DetailCodeRange.Warning.General.Min + 3, "DataIncomplete", "The returned data may be incomplete.");
    public static readonly WarningCode LegacyApiUsed = new(DetailCodeRange.Warning.General.Min + 4, "LegacyApiUsed", "A legacy API endpoint was used.");
    public static readonly WarningCode FeatureNotSupported = new(DetailCodeRange.Warning.General.Min + 5, "FeatureNotSupported", "The requested feature is not fully supported.");
    public static readonly WarningCode ConfigurationIssue = new(DetailCodeRange.Warning.General.Min + 6, "ConfigurationIssue", "A configuration issue was detected.");
    public static readonly WarningCode ResourceLimitApproaching = new(DetailCodeRange.Warning.General.Min + 7, "ResourceLimitApproaching", "Resource limit is approaching the threshold.");
    public static readonly WarningCode RetryRecommended = new(DetailCodeRange.Warning.General.Min + 8, "RetryRecommended", "Operation completed but retry is recommended.");

    #endregion

    #region Data Warnings

    public static readonly WarningCode DataQualityIssue = new(DetailCodeRange.Warning.Data.Min, "DataQualityIssue", "Data quality issue detected.");
    public static readonly WarningCode DataOutdated = new(DetailCodeRange.Warning.Data.Min + 1, "DataOutdated", "The data may be outdated.");
    public static readonly WarningCode DataInconsistency = new(DetailCodeRange.Warning.Data.Min + 2, "DataInconsistency", "Data inconsistency detected.");
    public static readonly WarningCode MissingOptionalData = new(DetailCodeRange.Warning.Data.Min + 3, "MissingOptionalData", "Some optional data is missing.");
    public static readonly WarningCode DataTruncated = new(DetailCodeRange.Warning.Data.Min + 4, "DataTruncated", "Data was truncated due to size limits.");
    public static readonly WarningCode DuplicateDataFound = new(DetailCodeRange.Warning.Data.Min + 5, "DuplicateDataFound", "Duplicate data was found and handled.");
    public static readonly WarningCode DefaultValueUsed = new(DetailCodeRange.Warning.Data.Min + 6, "DefaultValueUsed", "Default value was used due to missing data.");
    public static readonly WarningCode DataValidationWarning = new(DetailCodeRange.Warning.Data.Min + 7, "DataValidationWarning", "Data validation warning detected.");

    #endregion

    #region Security Warnings

    public static readonly WarningCode WeakPassword = new(DetailCodeRange.Warning.Security.Min, "WeakPassword", "Password strength is below recommended level.");
    public static readonly WarningCode InsecureConnection = new(DetailCodeRange.Warning.Security.Min + 1, "InsecureConnection", "Connection is not using secure protocol.");
    public static readonly WarningCode PermissionElevated = new(DetailCodeRange.Warning.Security.Min + 2, "PermissionElevated", "Operation performed with elevated permissions.");
    public static readonly WarningCode UnusualActivity = new(DetailCodeRange.Warning.Security.Min + 3, "UnusualActivity", "Unusual activity pattern detected.");
    public static readonly WarningCode TokenNearExpiry = new(DetailCodeRange.Warning.Security.Min + 4, "TokenNearExpiry", "Authentication token is near expiry.");
    public static readonly WarningCode UntrustedSource = new(DetailCodeRange.Warning.Security.Min + 5, "UntrustedSource", "Request originated from untrusted source.");
    public static readonly WarningCode SecurityPolicyViolation = new(DetailCodeRange.Warning.Security.Min + 6, "SecurityPolicyViolation", "Minor security policy violation detected.");

    #endregion

    #region Performance Warnings

    public static readonly WarningCode SlowResponse = new(DetailCodeRange.Warning.Performance.Min, "SlowResponse", "Response time exceeded expected threshold.");
    public static readonly WarningCode HighMemoryUsage = new(DetailCodeRange.Warning.Performance.Min + 1, "HighMemoryUsage", "High memory usage detected.");
    public static readonly WarningCode HighCpuUsage = new(DetailCodeRange.Warning.Performance.Min + 2, "HighCpuUsage", "High CPU usage detected.");
    public static readonly WarningCode LargeResultSet = new(DetailCodeRange.Warning.Performance.Min + 3, "LargeResultSet", "Result set is larger than recommended.");
    public static readonly WarningCode CacheMiss = new(DetailCodeRange.Warning.Performance.Min + 4, "CacheMiss", "Cache miss occurred, data retrieved from source.");
    public static readonly WarningCode ConnectionPoolExhausted = new(DetailCodeRange.Warning.Performance.Min + 5, "ConnectionPoolExhausted", "Connection pool is near exhaustion.");
    public static readonly WarningCode QueryOptimizationNeeded = new(DetailCodeRange.Warning.Performance.Min + 6, "QueryOptimizationNeeded", "Query may benefit from optimization.");

    #endregion

    #region Business Logic Warnings

    public static readonly WarningCode BusinessRuleBypass = new(DetailCodeRange.Warning.Business.Min, "BusinessRuleBypass", "Business rule was bypassed with authorization.");
    public static readonly WarningCode ApprovalRequired = new(DetailCodeRange.Warning.Business.Min + 1, "ApprovalRequired", "Operation completed but requires approval.");
    public static readonly WarningCode ThresholdExceeded = new(DetailCodeRange.Warning.Business.Min + 2, "ThresholdExceeded", "Business threshold was exceeded.");
    public static readonly WarningCode UnusualBehavior = new(DetailCodeRange.Warning.Business.Min + 3, "UnusualBehavior", "Unusual business behavior detected.");
    public static readonly WarningCode RecommendationIgnored = new(DetailCodeRange.Warning.Business.Min + 4, "RecommendationIgnored", "System recommendation was ignored.");
    public static readonly WarningCode PolicyException = new(DetailCodeRange.Warning.Business.Min + 5, "PolicyException", "Operation performed as policy exception.");

    #endregion

    #region System Warnings

    public static readonly WarningCode ServiceDegradation = new(DetailCodeRange.Warning.System.Min, "ServiceDegradation", "Service performance degradation detected.");
    public static readonly WarningCode MaintenanceScheduled = new(DetailCodeRange.Warning.System.Min + 1, "MaintenanceScheduled", "System maintenance is scheduled soon.");
    public static readonly WarningCode VersionMismatch = new(DetailCodeRange.Warning.System.Min + 2, "VersionMismatch", "Client and server version mismatch detected.");
    public static readonly WarningCode CapacityWarning = new(DetailCodeRange.Warning.System.Min + 3, "CapacityWarning", "System capacity approaching limits.");
    public static readonly WarningCode BackupDelayed = new(DetailCodeRange.Warning.System.Min + 4, "BackupDelayed", "Scheduled backup was delayed.");
    public static readonly WarningCode LogRotationNeeded = new(DetailCodeRange.Warning.System.Min + 5, "LogRotationNeeded", "Log rotation is needed.");
    public static readonly WarningCode DiskSpaceLow = new(DetailCodeRange.Warning.System.Min + 6, "DiskSpaceLow", "Disk space is running low.");
    public static readonly WarningCode DatabaseConnectionSlow = new(DetailCodeRange.Warning.System.Min + 7, "DatabaseConnectionSlow", "Database connection is slow.");

    #endregion

    #region Integration Warnings

    public static readonly WarningCode ExternalServiceSlow = new(DetailCodeRange.Warning.Integration.Min, "ExternalServiceSlow", "External service response is slower than expected.");
    public static readonly WarningCode ApiRateLimitApproaching = new(DetailCodeRange.Warning.Integration.Min + 1, "ApiRateLimitApproaching", "API rate limit is approaching.");
    public static readonly WarningCode ExternalDataStale = new(DetailCodeRange.Warning.Integration.Min + 2, "ExternalDataStale", "External data source appears stale.");
    public static readonly WarningCode SyncDelayed = new(DetailCodeRange.Warning.Integration.Min + 3, "SyncDelayed", "Data synchronization is delayed.");
    public static readonly WarningCode WebhookRetry = new(DetailCodeRange.Warning.Integration.Min + 4, "WebhookRetry", "Webhook delivery required retry.");
    public static readonly WarningCode IntegrationDeprecated = new(DetailCodeRange.Warning.Integration.Min + 5, "IntegrationDeprecated", "Integration endpoint is deprecated.");

    #endregion

    #region User Experience Warnings

    public static readonly WarningCode BrowserNotSupported = new(DetailCodeRange.Warning.UserExperience.Min, "BrowserNotSupported", "Browser version may not be fully supported.");
    public static readonly WarningCode FeatureLimitedOnDevice = new(DetailCodeRange.Warning.UserExperience.Min + 1, "FeatureLimitedOnDevice", "Feature functionality is limited on this device.");
    public static readonly WarningCode OfflineMode = new(DetailCodeRange.Warning.UserExperience.Min + 2, "OfflineMode", "Operating in offline mode with limited functionality.");
    public static readonly WarningCode SessionExpiringSoon = new(DetailCodeRange.Warning.UserExperience.Min + 3, "SessionExpiringSoon", "User session will expire soon.");
    public static readonly WarningCode UnsavedChanges = new(DetailCodeRange.Warning.UserExperience.Min + 4, "UnsavedChanges", "There are unsaved changes.");
    public static readonly WarningCode AccessibilityIssue = new(DetailCodeRange.Warning.UserExperience.Min + 5, "AccessibilityIssue", "Accessibility feature may not work properly.");

    #endregion

    #region Utility Methods

    public static WarningCode? GetByValue(int value)
    {
        return WarningCodes.TryGetValue(value, out var warningCode) ? warningCode : null;
    }

    public static IEnumerable<WarningCode> GetAll()
    {
        return WarningCodes.Values.OrderBy(x => x.Value);
    }

    public static IEnumerable<WarningCode> GetByRange(int minValue, int maxValue)
    {
        return WarningCodes.Values.Where(x => x.Value >= minValue && x.Value <= maxValue).OrderBy(x => x.Value);
    }

    public bool IsGeneralWarning => DetailCodeRange.IsInRange(Value, DetailCodeRange.Warning.General.Min, DetailCodeRange.Warning.General.Max);
    public bool IsDataWarning => DetailCodeRange.IsInRange(Value, DetailCodeRange.Warning.Data.Min, DetailCodeRange.Warning.Data.Max);
    public bool IsSecurityWarning => DetailCodeRange.IsInRange(Value, DetailCodeRange.Warning.Security.Min, DetailCodeRange.Warning.Security.Max);
    public bool IsPerformanceWarning => DetailCodeRange.IsInRange(Value, DetailCodeRange.Warning.Performance.Min, DetailCodeRange.Warning.Performance.Max);
    public bool IsBusinessWarning => DetailCodeRange.IsInRange(Value, DetailCodeRange.Warning.Business.Min, DetailCodeRange.Warning.Business.Max);
    public bool IsSystemWarning => DetailCodeRange.IsInRange(Value, DetailCodeRange.Warning.System.Min, DetailCodeRange.Warning.System.Max);
    public bool IsIntegrationWarning => DetailCodeRange.IsInRange(Value, DetailCodeRange.Warning.Integration.Min, DetailCodeRange.Warning.Integration.Max);
    public bool IsUserExperienceWarning => DetailCodeRange.IsInRange(Value, DetailCodeRange.Warning.UserExperience.Min, DetailCodeRange.Warning.UserExperience.Max);

    #endregion
}
