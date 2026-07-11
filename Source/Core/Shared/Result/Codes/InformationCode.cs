using System.Collections.Concurrent;

namespace PlayGround.Shared.Result;

public sealed class InformationCode : DetailCode
{
    private static readonly ConcurrentDictionary<int, InformationCode> InformationCodes = new();

    private InformationCode(int value, string name, string message)
        : base(ResultCodes.Information, value, name, message)
    {
        if (!InformationCodes.TryAdd(value, this))
        {
            throw new InvalidOperationException($"Information code value {value} is already defined.");
        }
    }

    /// <summary>
    /// 외부 프로젝트에서 도메인 특화 InformationCode를 등록하기 위한 팩토리 메서드
    /// </summary>
    public static InformationCode Register(int value, string name, string message)
    {
        return new InformationCode(value, name, message);
    }

    #region CRUD Operations

    public static readonly InformationCode Created = new(DetailCodeRange.Information.Crud.Min, "Created", "Resource successfully created.");
    public static readonly InformationCode Updated = new(DetailCodeRange.Information.Crud.Min + 1, "Updated", "Resource successfully updated.");
    public static readonly InformationCode Deleted = new(DetailCodeRange.Information.Crud.Min + 2, "Deleted", "Resource successfully deleted.");
    public static readonly InformationCode Retrieved = new(DetailCodeRange.Information.Crud.Min + 3, "Retrieved", "Resource successfully retrieved.");
    public static readonly InformationCode Listed = new(DetailCodeRange.Information.Crud.Min + 4, "Listed", "Resources successfully listed.");
    public static readonly InformationCode Restored = new(DetailCodeRange.Information.Crud.Min + 5, "Restored", "Resource successfully restored.");
    public static readonly InformationCode Archived = new(DetailCodeRange.Information.Crud.Min + 6, "Archived", "Resource successfully archived.");

    #endregion

    #region Process Operations

    public static readonly InformationCode ProcessStarted = new(DetailCodeRange.Information.Process.Min, "ProcessStarted", "Process has been started successfully.");
    public static readonly InformationCode ProcessCompleted = new(DetailCodeRange.Information.Process.Min + 1, "ProcessCompleted", "Process completed successfully.");
    public static readonly InformationCode ProcessPaused = new(DetailCodeRange.Information.Process.Min + 2, "ProcessPaused", "Process has been paused.");
    public static readonly InformationCode ProcessResumed = new(DetailCodeRange.Information.Process.Min + 3, "ProcessResumed", "Process has been resumed.");
    public static readonly InformationCode ProcessCancelled = new(DetailCodeRange.Information.Process.Min + 4, "ProcessCancelled", "Process has been cancelled.");
    public static readonly InformationCode ProcessScheduled = new(DetailCodeRange.Information.Process.Min + 5, "ProcessScheduled", "Process has been scheduled successfully.");
    public static readonly InformationCode BatchProcessCompleted = new(DetailCodeRange.Information.Process.Min + 6, "BatchProcessCompleted", "Batch process completed successfully.");

    #endregion

    #region User Operations

    public static readonly InformationCode UserRegistered = new(DetailCodeRange.Information.User.Min, "UserRegistered", "User successfully registered.");
    public static readonly InformationCode UserLoggedIn = new(DetailCodeRange.Information.User.Min + 1, "UserLoggedIn", "User successfully logged in.");
    public static readonly InformationCode UserLoggedOut = new(DetailCodeRange.Information.User.Min + 2, "UserLoggedOut", "User successfully logged out.");
    public static readonly InformationCode PasswordChanged = new(DetailCodeRange.Information.User.Min + 3, "PasswordChanged", "Password successfully changed.");
    public static readonly InformationCode ProfileUpdated = new(DetailCodeRange.Information.User.Min + 4, "ProfileUpdated", "User profile successfully updated.");
    public static readonly InformationCode EmailVerified = new(DetailCodeRange.Information.User.Min + 5, "EmailVerified", "Email address successfully verified.");
    public static readonly InformationCode AccountActivated = new(DetailCodeRange.Information.User.Min + 6, "AccountActivated", "User account successfully activated.");
    public static readonly InformationCode AccountDeactivated = new(DetailCodeRange.Information.User.Min + 7, "AccountDeactivated", "User account successfully deactivated.");

    #endregion

    #region File Operations

    public static readonly InformationCode FileUploaded = new(DetailCodeRange.Information.File.Min, "FileUploaded", "File successfully uploaded.");
    public static readonly InformationCode FileDownloaded = new(DetailCodeRange.Information.File.Min + 1, "FileDownloaded", "File successfully downloaded.");
    public static readonly InformationCode FileDeleted = new(DetailCodeRange.Information.File.Min + 2, "FileDeleted", "File successfully deleted.");
    public static readonly InformationCode FileProcessed = new(DetailCodeRange.Information.File.Min + 3, "FileProcessed", "File successfully processed.");
    public static readonly InformationCode FileConverted = new(DetailCodeRange.Information.File.Min + 4, "FileConverted", "File successfully converted.");
    public static readonly InformationCode FileCompressed = new(DetailCodeRange.Information.File.Min + 5, "FileCompressed", "File successfully compressed.");

    #endregion

    #region Communication Operations

    public static readonly InformationCode MessageSent = new(DetailCodeRange.Information.Communication.Min, "MessageSent", "Message successfully sent.");
    public static readonly InformationCode MessageDelivered = new(DetailCodeRange.Information.Communication.Min + 1, "MessageDelivered", "Message successfully delivered.");
    public static readonly InformationCode NotificationSent = new(DetailCodeRange.Information.Communication.Min + 2, "NotificationSent", "Notification successfully sent.");
    public static readonly InformationCode EmailSent = new(DetailCodeRange.Information.Communication.Min + 3, "EmailSent", "Email successfully sent.");
    public static readonly InformationCode InvitationSent = new(DetailCodeRange.Information.Communication.Min + 4, "InvitationSent", "Invitation successfully sent.");

    #endregion

    #region System Operations

    public static readonly InformationCode SystemStarted = new(DetailCodeRange.Information.System.Min, "SystemStarted", "System has started successfully.");
    public static readonly InformationCode SystemShutdown = new(DetailCodeRange.Information.System.Min + 1, "SystemShutdown", "System is shutting down.");
    public static readonly InformationCode DatabaseConnected = new(DetailCodeRange.Information.System.Min + 2, "DatabaseConnected", "Database connection established.");
    public static readonly InformationCode DatabaseDisconnected = new(DetailCodeRange.Information.System.Min + 3, "DatabaseDisconnected", "Database connection closed.");
    public static readonly InformationCode CacheCleared = new(DetailCodeRange.Information.System.Min + 4, "CacheCleared", "Cache successfully cleared.");
    public static readonly InformationCode ConfigurationLoaded = new(DetailCodeRange.Information.System.Min + 5, "ConfigurationLoaded", "Configuration successfully loaded.");
    public static readonly InformationCode BackupCompleted = new(DetailCodeRange.Information.System.Min + 6, "BackupCompleted", "Backup completed successfully.");
    public static readonly InformationCode MaintenanceStarted = new(DetailCodeRange.Information.System.Min + 7, "MaintenanceStarted", "Maintenance mode activated.");
    public static readonly InformationCode MaintenanceEnded = new(DetailCodeRange.Information.System.Min + 8, "MaintenanceEnded", "Maintenance mode deactivated.");

    #endregion

    #region Status Updates

    public static readonly InformationCode StatusChanged = new(DetailCodeRange.Information.Status.Min, "StatusChanged", "Status successfully changed.");
    public static readonly InformationCode QueueProcessed = new(DetailCodeRange.Information.Status.Min + 1, "QueueProcessed", "Queue item successfully processed.");
    public static readonly InformationCode TaskCompleted = new(DetailCodeRange.Information.Status.Min + 2, "TaskCompleted", "Task completed successfully.");
    public static readonly InformationCode TaskScheduled = new(DetailCodeRange.Information.Status.Min + 3, "TaskScheduled", "Task scheduled successfully.");
    public static readonly InformationCode HealthCheckPassed = new(DetailCodeRange.Information.Status.Min + 4, "HealthCheckPassed", "Health check passed successfully.");
    public static readonly InformationCode SyncCompleted = new(DetailCodeRange.Information.Status.Min + 5, "SyncCompleted", "Synchronization completed successfully.");

    #endregion

    #region Utility Methods

    public static InformationCode? GetByValue(int value)
    {
        return InformationCodes.TryGetValue(value, out var informationCode) ? informationCode : null;
    }

    public static IEnumerable<InformationCode> GetAll()
    {
        return InformationCodes.Values.OrderBy(x => x.Value);
    }

    public static IEnumerable<InformationCode> GetByRange(int minValue, int maxValue)
    {
        return InformationCodes.Values.Where(x => x.Value >= minValue && x.Value <= maxValue).OrderBy(x => x.Value);
    }

    public bool IsCrudOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Information.Crud.Min, DetailCodeRange.Information.Crud.Max);
    public bool IsProcessOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Information.Process.Min, DetailCodeRange.Information.Process.Max);
    public bool IsUserOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Information.User.Min, DetailCodeRange.Information.User.Max);
    public bool IsFileOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Information.File.Min, DetailCodeRange.Information.File.Max);
    public bool IsCommunicationOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Information.Communication.Min, DetailCodeRange.Information.Communication.Max);
    public bool IsSystemOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Information.System.Min, DetailCodeRange.Information.System.Max);
    public bool IsStatusOperation => DetailCodeRange.IsInRange(Value, DetailCodeRange.Information.Status.Min, DetailCodeRange.Information.Status.Max);

    #endregion
}
