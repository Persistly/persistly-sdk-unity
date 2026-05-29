#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Persistly.Unity
{
    public sealed class PersistlyClientOptions
    {
        public const string DefaultBaseUrl = "https://api.persistly.app";

        public PersistlyClientOptions(string runtimeKey)
            : this(DefaultBaseUrl, runtimeKey)
        {
        }

        public PersistlyClientOptions(string baseUrl, string runtimeKey)
        {
            BaseUrl = baseUrl;
            RuntimeKey = runtimeKey;
        }

        public string BaseUrl { get; }

        public string RuntimeKey { get; }

        public IPersistlyTransport? Transport { get; set; }

        public IPersistlySaveCache? Cache { get; set; }

        public int TimeoutSeconds { get; set; } = 30;

        public string UserAgent { get; set; } = "Persistly Unity SDK/1.0.0";

        public string SdkName { get; set; } = "unity";

        public string SdkVersion { get; set; } = "1.0.0";

        public string Platform { get; set; } = "unity";

        public string? EngineVersion { get; set; }

        public string? ClientVersion { get; set; }
    }

    public sealed class PersistlyCreateSaveRequest
    {
        public PersistlyCreateSaveRequest(string stateJson, string? slotInfoJson = null, string? playerRef = null)
        {
            StateJson = PersistlyJson.CanonicalizeObjectJson(stateJson, "state");
            SlotInfoJson = slotInfoJson == null ? null : PersistlyJson.CanonicalizeObjectJson(slotInfoJson, "slotInfo");
            PlayerRef = string.IsNullOrWhiteSpace(playerRef) ? null : playerRef.Trim();
        }

        public string? PlayerRef { get; }

        public string? SlotInfoJson { get; }

        public string StateJson { get; }
    }

    public sealed class PersistlySyncSaveRequest
    {
        public PersistlySyncSaveRequest(string stateJson, int? baseVersion = null, string? slotInfoJson = null)
        {
            BaseVersion = baseVersion;
            StateJson = PersistlyJson.CanonicalizeObjectJson(stateJson, "state");
            SlotInfoJson = slotInfoJson == null ? null : PersistlyJson.CanonicalizeObjectJson(slotInfoJson, "slotInfo");
        }

        public int? BaseVersion { get; }

        public string? SlotInfoJson { get; }

        public string StateJson { get; }
    }

    public sealed class PersistlyCreateAccountRequest
    {
        public PersistlyCreateAccountRequest(
            string? accountDataJson = null,
            string? playerRef = null,
            string? externalAccountRefJson = null,
            PersistlyCreateAccountInitialSlotRequest? slot = null)
        {
            AccountDataJson = PersistlyJson.CanonicalizeObjectJson(string.IsNullOrWhiteSpace(accountDataJson) ? "{}" : accountDataJson, "accountData");
            PlayerRef = string.IsNullOrWhiteSpace(playerRef) ? null : playerRef.Trim();
            ExternalAccountRefJson = externalAccountRefJson == null ? null : PersistlyJson.CanonicalizeObjectJson(externalAccountRefJson, "externalAccountRef");
            Slot = slot;
        }

        public string AccountDataJson { get; }

        public string? PlayerRef { get; }

        public string? ExternalAccountRefJson { get; }

        public PersistlyCreateAccountInitialSlotRequest? Slot { get; }
    }

    public sealed class PersistlyCreateAccountInitialSlotRequest
    {
        public PersistlyCreateAccountInitialSlotRequest(string slotId, string slotInfoJson, string dataJson)
        {
            SlotId = PersistlySlotId.Normalize(slotId);
            SlotInfoJson = PersistlyJson.CanonicalizeObjectJson(slotInfoJson, "slotInfo");
            DataJson = PersistlyJson.CanonicalizeObjectJson(dataJson, "data");
        }

        public string SlotId { get; }

        public string SlotInfoJson { get; }

        public string DataJson { get; }
    }

    public sealed class PersistlyCreateAccountSlotRequest
    {
        public PersistlyCreateAccountSlotRequest(string slotId, string slotInfoJson, string slotDataJson)
        {
            SlotId = PersistlySlotId.Normalize(slotId);
            SlotInfoJson = PersistlyJson.CanonicalizeObjectJson(slotInfoJson, "slotInfo");
            SlotDataJson = PersistlyJson.CanonicalizeObjectJson(slotDataJson, "data");
        }

        public string SlotId { get; }

        public string SlotInfoJson { get; }

        public string SlotDataJson { get; }
    }

    public sealed class PersistlySave
    {
        public PersistlySave(
            string saveId,
            string? playerRef,
            string slotInfoJson,
            string stateJson,
            int version,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            SaveId = saveId;
            PlayerRef = playerRef;
            SlotInfoJson = slotInfoJson;
            StateJson = stateJson;
            Version = version;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public string SaveId { get; }

        public string? PlayerRef { get; }

        public string SlotInfoJson { get; }

        public string StateJson { get; }

        public int Version { get; }

        public DateTimeOffset CreatedAt { get; }

        public DateTimeOffset UpdatedAt { get; }
    }

    public sealed class PersistlySaveEnvelope
    {
        public PersistlySaveEnvelope(PersistlySave save)
        {
            Save = save;
        }

        public PersistlySave Save { get; }
    }

    public sealed class PersistlyAccountEnvelope
    {
        public PersistlyAccountEnvelope(string accountId, string? accountSessionToken, PersistlySave account, PersistlySyncPolicy? syncPolicy = null)
        {
            AccountId = accountId;
            AccountSessionToken = accountSessionToken;
            Account = account;
            SyncPolicy = syncPolicy;
        }

        public string AccountId { get; }

        public string? AccountSessionToken { get; }

        public PersistlySave Account { get; }

        public PersistlySave Save => Account;

        public PersistlySyncPolicy? SyncPolicy { get; }

        public PersistlyAccountState AccountState => PersistlyAccountState.Parse(Account.StateJson);
    }

    public sealed class PersistlySlotEnvelope
    {
        public PersistlySlotEnvelope(PersistlySave save)
        {
            Save = save;
        }

        public PersistlySave Save { get; }
    }

    public sealed class PersistlyCreateAccountResponse
    {
        public PersistlyCreateAccountResponse(string accountId, string? accountSessionToken, PersistlySave account, PersistlySave? slot, PersistlySyncPolicy syncPolicy)
        {
            AccountId = accountId;
            AccountSessionToken = accountSessionToken;
            Account = account;
            Slot = slot;
            SyncPolicy = syncPolicy;
        }

        public string AccountId { get; }

        public string? AccountSessionToken { get; }

        public PersistlySave Account { get; }

        public PersistlySave? Slot { get; }

        public PersistlySyncPolicy SyncPolicy { get; }

        public PersistlyAccountState AccountState => PersistlyAccountState.Parse(Account.StateJson);
    }

    public sealed class PersistlyDeleteAccountResponse
    {
        public PersistlyDeleteAccountResponse(string accountId, DateTimeOffset deletedAt, int deletedSlotCount, bool alreadyDeleted, bool cleanupQueued)
        {
            AccountId = accountId;
            DeletedAt = deletedAt;
            DeletedSlotCount = deletedSlotCount;
            AlreadyDeleted = alreadyDeleted;
            CleanupQueued = cleanupQueued;
        }

        public string AccountId { get; }

        public DateTimeOffset DeletedAt { get; }

        public int DeletedSlotCount { get; }

        public bool AlreadyDeleted { get; }

        public bool CleanupQueued { get; }
    }

    public sealed class PersistlyDeleteSlotResponse
    {
        public PersistlyDeleteSlotResponse(
            string accountId,
            string slotId,
            DateTimeOffset deletedAt,
            bool alreadyDeleted,
            bool cleanupQueued,
            PersistlySave? account = null)
        {
            AccountId = accountId;
            SlotId = slotId;
            DeletedAt = deletedAt;
            AlreadyDeleted = alreadyDeleted;
            CleanupQueued = cleanupQueued;
            Account = account;
        }

        public string AccountId { get; }

        public string SlotId { get; }

        public DateTimeOffset DeletedAt { get; }

        public bool AlreadyDeleted { get; }

        public bool CleanupQueued { get; }

        public PersistlySave? Account { get; }
    }

    public sealed class PersistlySyncPolicy
    {
        public PersistlySyncPolicy(
            int minRemoteSyncIntervalSeconds,
            int forceSyncCooldownSeconds,
            bool syncOnBackground,
            bool syncOnForeground,
            bool syncOnReconnect,
            int maxQueuedLocalSnapshots)
        {
            MinRemoteSyncIntervalSeconds = minRemoteSyncIntervalSeconds;
            ForceSyncCooldownSeconds = forceSyncCooldownSeconds;
            SyncOnBackground = syncOnBackground;
            SyncOnForeground = syncOnForeground;
            SyncOnReconnect = syncOnReconnect;
            MaxQueuedLocalSnapshots = maxQueuedLocalSnapshots;
        }

        public int MinRemoteSyncIntervalSeconds { get; }

        public int ForceSyncCooldownSeconds { get; }

        public bool SyncOnBackground { get; }

        public bool SyncOnForeground { get; }

        public bool SyncOnReconnect { get; }

        public int MaxQueuedLocalSnapshots { get; }
    }

    public sealed class PersistlyRuntimeConfig
    {
        public PersistlyRuntimeConfig(PersistlySyncPolicy syncPolicy, PersistlyRuntimeGameConfig? gameConfig = null)
        {
            SyncPolicy = syncPolicy;
            GameConfig = gameConfig;
        }

        public PersistlySyncPolicy SyncPolicy { get; }

        public PersistlyRuntimeGameConfig? GameConfig { get; }
    }

    public sealed class PersistlyRuntimeGameConfig
    {
        public PersistlyRuntimeGameConfig(
            bool enabled,
            int? version = null,
            bool unchanged = false,
            int? sizeBytes = null,
            bool hasData = false,
            string? eventName = null,
            string configJson = "{}")
        {
            Enabled = enabled;
            Version = version;
            Unchanged = unchanged;
            SizeBytes = sizeBytes;
            HasData = hasData;
            EventName = eventName;
            ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson;
        }

        public bool Enabled { get; }

        public int? Version { get; }

        public bool Unchanged { get; }

        public int? SizeBytes { get; }

        public bool HasData { get; }

        public string? EventName { get; }

        public string ConfigJson { get; }
    }

    public enum PersistlySyncStatus
    {
        Accepted,
        Conflict
    }

    public enum PersistlySyncConflictReason
    {
        BaseVersionMismatch
    }

    public sealed class PersistlySyncConflictDetails
    {
        public PersistlySyncConflictDetails(PersistlySyncConflictReason reason)
        {
            Reason = reason;
        }

        public PersistlySyncConflictReason Reason { get; }
    }

    public sealed class PersistlySyncResponse
    {
        public PersistlySyncResponse(
            PersistlySyncStatus status,
            PersistlySave save,
            PersistlySyncConflictDetails? details = null,
            bool historyRetained = false,
            IReadOnlyList<string>? warnings = null)
        {
            Status = status;
            Save = save;
            Details = details;
            HistoryRetained = historyRetained;
            Warnings = warnings ?? Array.Empty<string>();
        }

        public PersistlySyncStatus Status { get; }

        public PersistlySave Save { get; }

        public PersistlySyncConflictDetails? Details { get; }

        public bool HistoryRetained { get; }

        public IReadOnlyList<string> Warnings { get; }
    }

    public sealed class PersistlySyncAccountDataRequest
    {
        public PersistlySyncAccountDataRequest(
            int baseVersion,
            string? accountDataJson = null,
            string? accountDataPatchJson = null)
        {
            if (baseVersion < 1)
            {
                throw new PersistlyConfigurationError("baseVersion must be greater than zero.");
            }

            if (accountDataJson != null && accountDataPatchJson != null)
            {
                throw new PersistlyConfigurationError("Only one of accountDataJson or accountDataPatchJson can be set.");
            }

            BaseVersion = baseVersion;
            AccountDataJson = accountDataJson == null ? null : PersistlyJson.CanonicalizeObjectJson(accountDataJson, "accountData");
            AccountDataPatchJson = accountDataPatchJson == null ? null : PersistlyJson.CanonicalizeObjectJson(accountDataPatchJson, "accountDataPatch");

            if (AccountDataJson == null && AccountDataPatchJson == null)
            {
                throw new PersistlyConfigurationError("At least one of accountDataJson or accountDataPatchJson must be set.");
            }
        }

        public int BaseVersion { get; }

        public string? AccountDataJson { get; }

        public string? AccountDataPatchJson { get; }
    }

    public sealed class PersistlyAccountState
    {
        public const string Schema = "persistly.account.v1";

        private PersistlyAccountState(string accountDataJson, IReadOnlyList<PersistlySlotRef> slots)
        {
            AccountDataJson = accountDataJson;
            Slots = slots;
        }

        public string AccountDataJson { get; }

        public IReadOnlyList<PersistlySlotRef> Slots { get; }

        public static PersistlyAccountState Parse(string stateJson)
        {
            var root = PersistlyJson.ParseJsonValue(stateJson, "account state") as Dictionary<string, object?>;
            if (root == null)
            {
                throw new PersistlyConfigurationError("account state must be a JSON object.");
            }

            if (!root.TryGetValue("schema", out var schemaRaw) || !(schemaRaw is string schema) || !string.Equals(schema, Schema, StringComparison.Ordinal))
            {
                throw new PersistlyConfigurationError("account state schema must be " + Schema + ".");
            }

            if (!root.TryGetValue("accountData", out var accountData) || !(accountData is Dictionary<string, object?>))
            {
                throw new PersistlyConfigurationError("account state accountData must be a JSON object.");
            }

            if (!root.TryGetValue("slots", out var slotsRaw) || !(slotsRaw is List<object?> rawSlots))
            {
                throw new PersistlyConfigurationError("account.slots must be an array.");
            }

            var slots = new List<PersistlySlotRef>();
            foreach (var rawSlot in rawSlots)
            {
                var slotObject = rawSlot as Dictionary<string, object?>;
                if (slotObject == null)
                {
                    throw new PersistlyConfigurationError("account.slots entries must be objects.");
                }

                slots.Add(PersistlySlotRef.Parse(slotObject));
            }

            return new PersistlyAccountState(PersistlyJson.Serialize(accountData), slots);
        }
    }

    public sealed class PersistlySlotRef
    {
        private PersistlySlotRef(string slotId, string slotInfoJson, bool archived, string? archivedAt)
        {
            SlotId = slotId;
            SlotInfoJson = slotInfoJson;
            Archived = archived;
            ArchivedAt = archivedAt;
        }

        public string SlotId { get; }

        public string SlotInfoJson { get; }

        public bool Archived { get; }

        public string? ArchivedAt { get; }

        public static PersistlySlotRef Parse(Dictionary<string, object?> slotObject)
        {
            var slotId = RequireString(slotObject, "slotId");
            if (!slotObject.TryGetValue("slotInfo", out var slotInfo) || !(slotInfo is Dictionary<string, object?>))
            {
                throw new PersistlyConfigurationError("slotInfo must be a JSON object.");
            }

            var archived = slotObject.TryGetValue("archived", out var archivedRaw) && archivedRaw is bool archivedBool && archivedBool;
            var archivedAt = slotObject.TryGetValue("archivedAt", out var archivedAtRaw) ? archivedAtRaw as string : null;
            return new PersistlySlotRef(slotId, PersistlyJson.Serialize(slotInfo), archived, archivedAt);
        }

        private static string RequireString(Dictionary<string, object?> source, string key)
        {
            if (!source.TryGetValue(key, out var raw) || !(raw is string value) || string.IsNullOrWhiteSpace(value))
            {
                throw new PersistlyConfigurationError("slot is missing " + key + ".");
            }

            return value;
        }
    }

    public static class PersistlySlotId
    {
        private static readonly Regex SlotIdPattern = new Regex("^[A-Za-z0-9_.-]{1,64}$", RegexOptions.Compiled);

        public static string Normalize(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                throw new PersistlyConfigurationError("slotId must be set.");
            }

            var normalized = slotId.Trim();
            if (!SlotIdPattern.IsMatch(normalized))
            {
                throw new PersistlyConfigurationError("slotId must match ^[A-Za-z0-9_.-]{1,64}$.");
            }

            return normalized;
        }

        public static string BuildSlotInfoJson(string slotId, string? developerSlotInfoJson)
        {
            var slotInfo = string.IsNullOrWhiteSpace(developerSlotInfoJson)
                ? new Dictionary<string, object?>(StringComparer.Ordinal)
                : PersistlyJson.ParseJsonValue(developerSlotInfoJson!, "slotInfo") as Dictionary<string, object?>;
            if (slotInfo == null)
            {
                throw new PersistlyConfigurationError("slotInfo must be a JSON object.");
            }

            return PersistlyJson.Serialize(slotInfo);
        }
    }
}
