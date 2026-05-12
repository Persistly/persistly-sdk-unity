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

        public string UserAgent { get; set; } = "Persistly Unity SDK/0.10.0";

        public string SdkName { get; set; } = "unity";

        public string SdkVersion { get; set; } = "0.10.0";

        public string Platform { get; set; } = "unity";

        public string? EngineVersion { get; set; }

        public string? ClientVersion { get; set; }
    }

    public sealed class PersistlyCreateSaveRequest
    {
        public PersistlyCreateSaveRequest(string stateJson, string? metadataJson = null, string? playerRef = null)
        {
            StateJson = PersistlyJson.CanonicalizeObjectJson(stateJson, "state");
            MetadataJson = metadataJson == null ? null : PersistlyJson.CanonicalizeObjectJson(metadataJson, "metadata");
            PlayerRef = string.IsNullOrWhiteSpace(playerRef) ? null : playerRef.Trim();
        }

        public string? PlayerRef { get; }

        public string? MetadataJson { get; }

        public string StateJson { get; }
    }

    public sealed class PersistlySyncSaveRequest
    {
        public PersistlySyncSaveRequest(string stateJson, int? baseVersion = null, string? metadataJson = null)
        {
            BaseVersion = baseVersion;
            StateJson = PersistlyJson.CanonicalizeObjectJson(stateJson, "state");
            MetadataJson = metadataJson == null ? null : PersistlyJson.CanonicalizeObjectJson(metadataJson, "metadata");
        }

        public int? BaseVersion { get; }

        public string? MetadataJson { get; }

        public string StateJson { get; }
    }

    public sealed class PersistlyCreateProfileRequest
    {
        public PersistlyCreateProfileRequest(
            string? accountDataJson = null,
            string? profileMetadataJson = null,
            string? playerRef = null,
            string? externalProfileRefJson = null,
            PersistlyCreateProfileInitialCharacterRequest? character = null)
        {
            AccountDataJson = PersistlyJson.CanonicalizeObjectJson(string.IsNullOrWhiteSpace(accountDataJson) ? "{}" : accountDataJson, "accountData");
            ProfileMetadataJson = profileMetadataJson == null ? null : PersistlyJson.CanonicalizeObjectJson(profileMetadataJson, "profileMetadata");
            PlayerRef = string.IsNullOrWhiteSpace(playerRef) ? null : playerRef.Trim();
            ExternalProfileRefJson = externalProfileRefJson == null ? null : PersistlyJson.CanonicalizeObjectJson(externalProfileRefJson, "externalProfileRef");
            Character = character;
        }

        public string AccountDataJson { get; }

        public string? ProfileMetadataJson { get; }

        public string? PlayerRef { get; }

        public string? ExternalProfileRefJson { get; }

        public PersistlyCreateProfileInitialCharacterRequest? Character { get; }
    }

    public sealed class PersistlyCreateProfileInitialCharacterRequest
    {
        public PersistlyCreateProfileInitialCharacterRequest(string slotKey, string metadataJson, string stateJson)
        {
            SlotKey = PersistlySlotKey.Normalize(slotKey);
            MetadataJson = PersistlySlotKey.BuildMetadataJson(SlotKey, metadataJson);
            StateJson = PersistlyJson.CanonicalizeObjectJson(stateJson, "characterState");
        }

        public string SlotKey { get; }

        public string MetadataJson { get; }

        public string StateJson { get; }
    }

    public sealed class PersistlyCreateProfileCharacterRequest
    {
        public PersistlyCreateProfileCharacterRequest(string slotKey, string characterMetadataJson, string characterStateJson)
        {
            SlotKey = PersistlySlotKey.Normalize(slotKey);
            CharacterMetadataJson = PersistlySlotKey.BuildMetadataJson(SlotKey, characterMetadataJson);
            CharacterStateJson = PersistlyJson.CanonicalizeObjectJson(characterStateJson, "characterState");
        }

        public string SlotKey { get; }

        public string CharacterMetadataJson { get; }

        public string CharacterStateJson { get; }
    }

    public sealed class PersistlySave
    {
        public PersistlySave(
            string saveId,
            string? playerRef,
            string metadataJson,
            string stateJson,
            int version,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            SaveId = saveId;
            PlayerRef = playerRef;
            MetadataJson = metadataJson;
            StateJson = stateJson;
            Version = version;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public string SaveId { get; }

        public string? PlayerRef { get; }

        public string MetadataJson { get; }

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

    public sealed class PersistlyProfileEnvelope
    {
        public PersistlyProfileEnvelope(string profileSaveId, string? profileSessionToken, PersistlySave profile, PersistlySyncPolicy? syncPolicy = null)
        {
            ProfileSaveId = profileSaveId;
            ProfileSessionToken = profileSessionToken;
            Profile = profile;
            SyncPolicy = syncPolicy;
        }

        public string ProfileSaveId { get; }

        public string? ProfileSessionToken { get; }

        public PersistlySave Profile { get; }

        public PersistlySave Save => Profile;

        public PersistlySyncPolicy? SyncPolicy { get; }

        public PersistlyProfileState ProfileState => PersistlyProfileState.Parse(Profile.StateJson);
    }

    public sealed class PersistlyCharacterEnvelope
    {
        public PersistlyCharacterEnvelope(PersistlySave save)
        {
            Save = save;
        }

        public PersistlySave Save { get; }
    }

    public sealed class PersistlyCreateProfileResponse
    {
        public PersistlyCreateProfileResponse(string profileSaveId, string? profileSessionToken, PersistlySave profile, PersistlySave? character, PersistlySyncPolicy syncPolicy)
        {
            ProfileSaveId = profileSaveId;
            ProfileSessionToken = profileSessionToken;
            Profile = profile;
            Character = character;
            SyncPolicy = syncPolicy;
        }

        public string ProfileSaveId { get; }

        public string? ProfileSessionToken { get; }

        public PersistlySave Profile { get; }

        public PersistlySave? Character { get; }

        public PersistlySyncPolicy SyncPolicy { get; }

        public PersistlyProfileState ProfileState => PersistlyProfileState.Parse(Profile.StateJson);
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

    public sealed class PersistlySyncProfileAccountDataRequest
    {
        public PersistlySyncProfileAccountDataRequest(
            int baseVersion,
            string? accountDataJson = null,
            string? accountDataPatchJson = null,
            string? metadataJson = null,
            bool clearMetadata = false)
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
            MetadataJson = metadataJson == null ? null : PersistlyJson.CanonicalizeObjectJson(metadataJson, "metadata");
            ClearMetadata = clearMetadata;

            if (AccountDataJson == null && AccountDataPatchJson == null && MetadataJson == null && !ClearMetadata)
            {
                throw new PersistlyConfigurationError("At least one of accountDataJson, accountDataPatchJson, metadataJson, or clearMetadata must be set.");
            }
        }

        public int BaseVersion { get; }

        public string? AccountDataJson { get; }

        public string? AccountDataPatchJson { get; }

        public string? MetadataJson { get; }

        public bool ClearMetadata { get; }
    }

    public sealed class PersistlyProfileState
    {
        public const string Schema = "persistly.profile.v1";

        private PersistlyProfileState(string accountDataJson, IReadOnlyList<PersistlyCharacterSlotRef> characterSlots)
        {
            AccountDataJson = accountDataJson;
            CharacterSlots = characterSlots;
        }

        public string AccountDataJson { get; }

        public IReadOnlyList<PersistlyCharacterSlotRef> CharacterSlots { get; }

        public static PersistlyProfileState Parse(string stateJson)
        {
            var root = PersistlyJson.ParseJsonValue(stateJson, "profile state") as Dictionary<string, object?>;
            if (root == null)
            {
                throw new PersistlyConfigurationError("profile state must be a JSON object.");
            }

            if (!root.TryGetValue("schema", out var schemaRaw) || !(schemaRaw is string schema) || !string.Equals(schema, Schema, StringComparison.Ordinal))
            {
                throw new PersistlyConfigurationError("profile state schema must be " + Schema + ".");
            }

            if (!root.TryGetValue("accountData", out var accountData) || !(accountData is Dictionary<string, object?>))
            {
                throw new PersistlyConfigurationError("profile state accountData must be a JSON object.");
            }

            if (!root.TryGetValue("characterSlots", out var slotsRaw) || !(slotsRaw is List<object?> rawSlots))
            {
                throw new PersistlyConfigurationError("profile state characterSlots must be an array.");
            }

            var slots = new List<PersistlyCharacterSlotRef>();
            foreach (var rawSlot in rawSlots)
            {
                var slotObject = rawSlot as Dictionary<string, object?>;
                if (slotObject == null)
                {
                    throw new PersistlyConfigurationError("profile state characterSlots entries must be objects.");
                }

                slots.Add(PersistlyCharacterSlotRef.Parse(slotObject));
            }

            return new PersistlyProfileState(PersistlyJson.Serialize(accountData), slots);
        }
    }

    public sealed class PersistlyCharacterSlotRef
    {
        private PersistlyCharacterSlotRef(string slotKey, string characterSaveId, string metadataJson, bool archived, string? archivedAt)
        {
            SlotKey = slotKey;
            CharacterSaveId = characterSaveId;
            MetadataJson = metadataJson;
            Archived = archived;
            ArchivedAt = archivedAt;
        }

        public string SlotKey { get; }

        public string CharacterSaveId { get; }

        public string MetadataJson { get; }

        public bool Archived { get; }

        public string? ArchivedAt { get; }

        public static PersistlyCharacterSlotRef Parse(Dictionary<string, object?> slotObject)
        {
            var slotKey = RequireString(slotObject, "slotKey");
            var characterSaveId = RequireString(slotObject, "characterSaveId");
            if (!slotObject.TryGetValue("metadata", out var metadata) || !(metadata is Dictionary<string, object?>))
            {
                throw new PersistlyConfigurationError("character slot metadata must be a JSON object.");
            }

            var archived = slotObject.TryGetValue("archived", out var archivedRaw) && archivedRaw is bool archivedBool && archivedBool;
            var archivedAt = slotObject.TryGetValue("archivedAt", out var archivedAtRaw) ? archivedAtRaw as string : null;
            return new PersistlyCharacterSlotRef(slotKey, characterSaveId, PersistlyJson.Serialize(metadata), archived, archivedAt);
        }

        private static string RequireString(Dictionary<string, object?> source, string key)
        {
            if (!source.TryGetValue(key, out var raw) || !(raw is string value) || string.IsNullOrWhiteSpace(value))
            {
                throw new PersistlyConfigurationError("character slot is missing " + key + ".");
            }

            return value;
        }
    }

    public static class PersistlySlotKey
    {
        private static readonly Regex SlotKeyPattern = new Regex("^[A-Za-z0-9_.-]{1,64}$", RegexOptions.Compiled);

        public static string Normalize(string slotKey)
        {
            if (string.IsNullOrWhiteSpace(slotKey))
            {
                throw new PersistlyConfigurationError("slotKey must be set.");
            }

            var normalized = slotKey.Trim();
            if (!SlotKeyPattern.IsMatch(normalized))
            {
                throw new PersistlyConfigurationError("slotKey must match ^[A-Za-z0-9_.-]{1,64}$.");
            }

            return normalized;
        }

        public static string BuildMetadataJson(string slotKey, string? developerMetadataJson)
        {
            var metadata = string.IsNullOrWhiteSpace(developerMetadataJson)
                ? new Dictionary<string, object?>(StringComparer.Ordinal)
                : PersistlyJson.ParseJsonValue(developerMetadataJson!, "metadata") as Dictionary<string, object?>;
            if (metadata == null)
            {
                throw new PersistlyConfigurationError("metadata must be a JSON object.");
            }

            if (metadata.ContainsKey("_persistly"))
            {
                throw new PersistlyConfigurationError("metadata._persistly is reserved for Persistly slot metadata.");
            }

            metadata["_persistly"] = new Dictionary<string, object?>
            {
                { "slotKey", Normalize(slotKey) }
            };
            return PersistlyJson.Serialize(metadata);
        }
    }
}
