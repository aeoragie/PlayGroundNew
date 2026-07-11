using System.Collections.Concurrent;

namespace PlayGround.Shared.Result;

public sealed class SuccessCode : DetailCode
{
    private static readonly ConcurrentDictionary<int, SuccessCode> SuccessCodes = new();

    private SuccessCode(int value, string name, string message)
        : base(ResultCodes.Success, value, name, message)
    {
        if (!SuccessCodes.TryAdd(value, this))
        {
            throw new InvalidOperationException($"Success code value {value} is already defined.");
        }
    }

    /// <summary>
    /// 외부 프로젝트에서 도메인 특화 SuccessCode를 등록하기 위한 팩토리 메서드
    /// </summary>
    public static SuccessCode Register(int value, string name, string message)
    {
        return new SuccessCode(value, name, message);
    }

    #region Basic Success Codes

    public static readonly SuccessCode Ok = new(DetailCodeRange.Success.Basic.Min, "Ok", "The operation completed successfully.");
    public static readonly SuccessCode Accepted = new(DetailCodeRange.Success.Basic.Min + 1, "Accepted", "The request has been accepted for processing.");
    public static readonly SuccessCode NoContent = new(DetailCodeRange.Success.Basic.Min + 2, "NoContent", "The operation completed successfully with no content to return.");
    public static readonly SuccessCode PartialContent = new(DetailCodeRange.Success.Basic.Min + 3, "PartialContent", "The operation completed with partial content.");

    #endregion

    #region CRUD Operations

    public static readonly SuccessCode Created = new(DetailCodeRange.Success.Crud.Min, "Created", "Resource successfully created.");
    public static readonly SuccessCode Updated = new(DetailCodeRange.Success.Crud.Min + 1, "Updated", "Resource successfully updated.");
    public static readonly SuccessCode Deleted = new(DetailCodeRange.Success.Crud.Min + 2, "Deleted", "Resource successfully deleted.");
    public static readonly SuccessCode Retrieved = new(DetailCodeRange.Success.Crud.Min + 3, "Retrieved", "Resource successfully retrieved.");
    public static readonly SuccessCode Listed = new(DetailCodeRange.Success.Crud.Min + 4, "Listed", "Resources successfully listed.");
    public static readonly SuccessCode Restored = new(DetailCodeRange.Success.Crud.Min + 5, "Restored", "Resource successfully restored.");
    public static readonly SuccessCode Archived = new(DetailCodeRange.Success.Crud.Min + 6, "Archived", "Resource successfully archived.");
    public static readonly SuccessCode Duplicated = new(DetailCodeRange.Success.Crud.Min + 7, "Duplicated", "Resource successfully duplicated.");
    public static readonly SuccessCode Merged = new(DetailCodeRange.Success.Crud.Min + 8, "Merged", "Resources successfully merged.");

    #endregion

    #region Authentication & Authorization

    public static readonly SuccessCode Authenticated = new(DetailCodeRange.Success.Auth.Min, "Authenticated", "User successfully authenticated.");
    public static readonly SuccessCode Authorized = new(DetailCodeRange.Success.Auth.Min + 1, "Authorized", "User successfully authorized.");
    public static readonly SuccessCode LoggedIn = new(DetailCodeRange.Success.Auth.Min + 2, "LoggedIn", "User successfully logged in.");
    public static readonly SuccessCode LoggedOut = new(DetailCodeRange.Success.Auth.Min + 3, "LoggedOut", "User successfully logged out.");
    public static readonly SuccessCode TokenRefreshed = new(DetailCodeRange.Success.Auth.Min + 4, "TokenRefreshed", "Authentication token successfully refreshed.");
    public static readonly SuccessCode PasswordChanged = new(DetailCodeRange.Success.Auth.Min + 5, "PasswordChanged", "Password successfully changed.");
    public static readonly SuccessCode PasswordReset = new(DetailCodeRange.Success.Auth.Min + 6, "PasswordReset", "Password successfully reset.");
    public static readonly SuccessCode EmailVerified = new(DetailCodeRange.Success.Auth.Min + 7, "EmailVerified", "Email address successfully verified.");
    public static readonly SuccessCode TwoFactorEnabled = new(DetailCodeRange.Success.Auth.Min + 8, "TwoFactorEnabled", "Two-factor authentication successfully enabled.");
    public static readonly SuccessCode TwoFactorDisabled = new(DetailCodeRange.Success.Auth.Min + 9, "TwoFactorDisabled", "Two-factor authentication successfully disabled.");

    #endregion

    #region User Management

    public static readonly SuccessCode UserRegistered = new(DetailCodeRange.Success.User.Min, "UserRegistered", "User successfully registered.");
    public static readonly SuccessCode UserActivated = new(DetailCodeRange.Success.User.Min + 1, "UserActivated", "User account successfully activated.");
    public static readonly SuccessCode UserDeactivated = new(DetailCodeRange.Success.User.Min + 2, "UserDeactivated", "User account successfully deactivated.");
    public static readonly SuccessCode ProfileUpdated = new(DetailCodeRange.Success.User.Min + 3, "ProfileUpdated", "User profile successfully updated.");
    public static readonly SuccessCode PreferencesUpdated = new(DetailCodeRange.Success.User.Min + 4, "PreferencesUpdated", "User preferences successfully updated.");
    public static readonly SuccessCode RoleAssigned = new(DetailCodeRange.Success.User.Min + 5, "RoleAssigned", "Role successfully assigned to user.");
    public static readonly SuccessCode RoleRevoked = new(DetailCodeRange.Success.User.Min + 6, "RoleRevoked", "Role successfully revoked from user.");
    public static readonly SuccessCode PermissionGranted = new(DetailCodeRange.Success.User.Min + 7, "PermissionGranted", "Permission successfully granted to user.");
    public static readonly SuccessCode PermissionRevoked = new(DetailCodeRange.Success.User.Min + 8, "PermissionRevoked", "Permission successfully revoked from user.");

    #endregion

    #region File Operations

    public static readonly SuccessCode FileUploaded = new(DetailCodeRange.Success.File.Min, "FileUploaded", "File successfully uploaded.");
    public static readonly SuccessCode FileDownloaded = new(DetailCodeRange.Success.File.Min + 1, "FileDownloaded", "File successfully downloaded.");
    public static readonly SuccessCode FileProcessed = new(DetailCodeRange.Success.File.Min + 2, "FileProcessed", "File successfully processed.");
    public static readonly SuccessCode FileConverted = new(DetailCodeRange.Success.File.Min + 3, "FileConverted", "File successfully converted.");
    public static readonly SuccessCode FileCompressed = new(DetailCodeRange.Success.File.Min + 4, "FileCompressed", "File successfully compressed.");
    public static readonly SuccessCode FileDecompressed = new(DetailCodeRange.Success.File.Min + 5, "FileDecompressed", "File successfully decompressed.");
    public static readonly SuccessCode FileMoved = new(DetailCodeRange.Success.File.Min + 6, "FileMoved", "File successfully moved.");
    public static readonly SuccessCode FileCopied = new(DetailCodeRange.Success.File.Min + 7, "FileCopied", "File successfully copied.");
    public static readonly SuccessCode FileRenamed = new(DetailCodeRange.Success.File.Min + 8, "FileRenamed", "File successfully renamed.");

    #endregion

    #region Communication

    public static readonly SuccessCode MessageSent = new(DetailCodeRange.Success.Communication.Min, "MessageSent", "Message successfully sent.");
    public static readonly SuccessCode MessageDelivered = new(DetailCodeRange.Success.Communication.Min + 1, "MessageDelivered", "Message successfully delivered.");
    public static readonly SuccessCode MessageRead = new(DetailCodeRange.Success.Communication.Min + 2, "MessageRead", "Message successfully marked as read.");
    public static readonly SuccessCode NotificationSent = new(DetailCodeRange.Success.Communication.Min + 3, "NotificationSent", "Notification successfully sent.");
    public static readonly SuccessCode EmailSent = new(DetailCodeRange.Success.Communication.Min + 4, "EmailSent", "Email successfully sent.");
    public static readonly SuccessCode InvitationSent = new(DetailCodeRange.Success.Communication.Min + 5, "InvitationSent", "Invitation successfully sent.");
    public static readonly SuccessCode InvitationAccepted = new(DetailCodeRange.Success.Communication.Min + 6, "InvitationAccepted", "Invitation successfully accepted.");
    public static readonly SuccessCode InvitationDeclined = new(DetailCodeRange.Success.Communication.Min + 7, "InvitationDeclined", "Invitation successfully declined.");

    #endregion

    #region Process Operations

    public static readonly SuccessCode ProcessStarted = new(DetailCodeRange.Success.Process.Min, "ProcessStarted", "Process successfully started.");
    public static readonly SuccessCode ProcessCompleted = new(DetailCodeRange.Success.Process.Min + 1, "ProcessCompleted", "Process successfully completed.");
    public static readonly SuccessCode ProcessPaused = new(DetailCodeRange.Success.Process.Min + 2, "ProcessPaused", "Process successfully paused.");
    public static readonly SuccessCode ProcessResumed = new(DetailCodeRange.Success.Process.Min + 3, "ProcessResumed", "Process successfully resumed.");
    public static readonly SuccessCode ProcessCancelled = new(DetailCodeRange.Success.Process.Min + 4, "ProcessCancelled", "Process successfully cancelled.");
    public static readonly SuccessCode ProcessScheduled = new(DetailCodeRange.Success.Process.Min + 5, "ProcessScheduled", "Process successfully scheduled.");
    public static readonly SuccessCode BatchProcessCompleted = new(DetailCodeRange.Success.Process.Min + 6, "BatchProcessCompleted", "Batch process successfully completed.");
    public static readonly SuccessCode TaskCompleted = new(DetailCodeRange.Success.Process.Min + 7, "TaskCompleted", "Task successfully completed.");
    public static readonly SuccessCode TaskScheduled = new(DetailCodeRange.Success.Process.Min + 8, "TaskScheduled", "Task successfully scheduled.");
    public static readonly SuccessCode QueueProcessed = new(DetailCodeRange.Success.Process.Min + 9, "QueueProcessed", "Queue successfully processed.");

    #endregion

    #region System Operations

    public static readonly SuccessCode SystemStarted = new(DetailCodeRange.Success.System.Min, "SystemStarted", "System successfully started.");
    public static readonly SuccessCode SystemShutdown = new(DetailCodeRange.Success.System.Min + 1, "SystemShutdown", "System successfully shut down.");
    public static readonly SuccessCode DatabaseConnected = new(DetailCodeRange.Success.System.Min + 2, "DatabaseConnected", "Database successfully connected.");
    public static readonly SuccessCode DatabaseMigrated = new(DetailCodeRange.Success.System.Min + 3, "DatabaseMigrated", "Database successfully migrated.");
    public static readonly SuccessCode CacheCleared = new(DetailCodeRange.Success.System.Min + 4, "CacheCleared", "Cache successfully cleared.");
    public static readonly SuccessCode CacheWarmed = new(DetailCodeRange.Success.System.Min + 5, "CacheWarmed", "Cache successfully warmed up.");
    public static readonly SuccessCode ConfigurationLoaded = new(DetailCodeRange.Success.System.Min + 6, "ConfigurationLoaded", "Configuration successfully loaded.");
    public static readonly SuccessCode ConfigurationUpdated = new(DetailCodeRange.Success.System.Min + 7, "ConfigurationUpdated", "Configuration successfully updated.");
    public static readonly SuccessCode BackupCreated = new(DetailCodeRange.Success.System.Min + 8, "BackupCreated", "Backup successfully created.");
    public static readonly SuccessCode BackupRestored = new(DetailCodeRange.Success.System.Min + 9, "BackupRestored", "Backup successfully restored.");
    public static readonly SuccessCode MaintenanceStarted = new(DetailCodeRange.Success.System.Min + 10, "MaintenanceStarted", "Maintenance mode successfully started.");
    public static readonly SuccessCode MaintenanceCompleted = new(DetailCodeRange.Success.System.Min + 11, "MaintenanceCompleted", "Maintenance successfully completed.");
    public static readonly SuccessCode HealthCheckPassed = new(DetailCodeRange.Success.System.Min + 12, "HealthCheckPassed", "Health check successfully passed.");
    public static readonly SuccessCode SyncCompleted = new(DetailCodeRange.Success.System.Min + 13, "SyncCompleted", "Synchronization successfully completed.");
    public static readonly SuccessCode IndexRebuilt = new(DetailCodeRange.Success.System.Min + 14, "IndexRebuilt", "Index successfully rebuilt.");

    #endregion

    #region Data Operations

    public static readonly SuccessCode DataImported = new(DetailCodeRange.Success.Data.Min, "DataImported", "Data successfully imported.");
    public static readonly SuccessCode DataExported = new(DetailCodeRange.Success.Data.Min + 1, "DataExported", "Data successfully exported.");
    public static readonly SuccessCode DataMigrated = new(DetailCodeRange.Success.Data.Min + 2, "DataMigrated", "Data successfully migrated.");
    public static readonly SuccessCode DataValidated = new(DetailCodeRange.Success.Data.Min + 3, "DataValidated", "Data successfully validated.");
    public static readonly SuccessCode DataCleaned = new(DetailCodeRange.Success.Data.Min + 4, "DataCleaned", "Data successfully cleaned.");
    public static readonly SuccessCode DataTransformed = new(DetailCodeRange.Success.Data.Min + 5, "DataTransformed", "Data successfully transformed.");
    public static readonly SuccessCode DataArchived = new(DetailCodeRange.Success.Data.Min + 6, "DataArchived", "Data successfully archived.");
    public static readonly SuccessCode DataPurged = new(DetailCodeRange.Success.Data.Min + 7, "DataPurged", "Data successfully purged.");

    #endregion

    #region Utility Methods

    public static SuccessCode? GetByValue(int value)
    {
        return SuccessCodes.TryGetValue(value, out var successCode) ? successCode : null;
    }

    public static IEnumerable<SuccessCode> GetAll()
    {
        return SuccessCodes.Values.OrderBy(x => x.Value);
    }

    public static IEnumerable<SuccessCode> GetByRange(int minValue, int maxValue)
    {
        return SuccessCodes.Values.Where(x => x.Value >= minValue && x.Value <= maxValue).OrderBy(x => x.Value);
    }

    public bool IsBasicSuccess => DetailCodeRange.IsInRange(Value, DetailCodeRange.Success.Basic.Min, DetailCodeRange.Success.Basic.Max);
    public bool IsCrudOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Success.Crud.Min, DetailCodeRange.Success.Crud.Max);
    public bool IsAuthOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Success.Auth.Min, DetailCodeRange.Success.Auth.Max);
    public bool IsUserOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Success.User.Min, DetailCodeRange.Success.User.Max);
    public bool IsFileOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Success.File.Min, DetailCodeRange.Success.File.Max);
    public bool IsCommunicationOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Success.Communication.Min, DetailCodeRange.Success.Communication.Max);
    public bool IsProcessOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Success.Process.Min, DetailCodeRange.Success.Process.Max);
    public bool IsSystemOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Success.System.Min, DetailCodeRange.Success.System.Max);
    public bool IsDataOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Success.Data.Min, DetailCodeRange.Success.Data.Max);

    #endregion
}
