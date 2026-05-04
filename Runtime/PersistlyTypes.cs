#nullable enable
using System;

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

        public string UserAgent { get; set; } = "Persistly Unity SDK/0.9.1";
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
            string accountDataJson,
            string characterMetadataJson,
            string characterStateJson,
            string? profileMetadataJson = null,
            string? playerRef = null,
            string? externalProfileRefJson = null)
        {
            AccountDataJson = PersistlyJson.CanonicalizeObjectJson(accountDataJson, "accountData");
            CharacterMetadataJson = PersistlyJson.CanonicalizeObjectJson(characterMetadataJson, "characterMetadata");
            CharacterStateJson = PersistlyJson.CanonicalizeObjectJson(characterStateJson, "characterState");
            ProfileMetadataJson = profileMetadataJson == null ? null : PersistlyJson.CanonicalizeObjectJson(profileMetadataJson, "profileMetadata");
            PlayerRef = string.IsNullOrWhiteSpace(playerRef) ? null : playerRef.Trim();
            ExternalProfileRefJson = externalProfileRefJson == null ? null : PersistlyJson.CanonicalizeObjectJson(externalProfileRefJson, "externalProfileRef");
        }

        public string AccountDataJson { get; }

        public string CharacterMetadataJson { get; }

        public string CharacterStateJson { get; }

        public string? ProfileMetadataJson { get; }

        public string? PlayerRef { get; }

        public string? ExternalProfileRefJson { get; }
    }

    public sealed class PersistlyCreateProfileCharacterRequest
    {
        public PersistlyCreateProfileCharacterRequest(string characterMetadataJson, string characterStateJson)
        {
            CharacterMetadataJson = PersistlyJson.CanonicalizeObjectJson(characterMetadataJson, "characterMetadata");
            CharacterStateJson = PersistlyJson.CanonicalizeObjectJson(characterStateJson, "characterState");
        }

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
        public PersistlyProfileEnvelope(string profileSaveId, string profileSessionToken, PersistlySave save)
        {
            ProfileSaveId = profileSaveId;
            ProfileSessionToken = profileSessionToken;
            Save = save;
        }

        public string ProfileSaveId { get; }

        public string ProfileSessionToken { get; }

        public PersistlySave Save { get; }
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
        public PersistlyCreateProfileResponse(PersistlyProfileEnvelope profile, PersistlyCharacterEnvelope character)
        {
            Profile = profile;
            Character = character;
        }

        public PersistlyProfileEnvelope Profile { get; }

        public PersistlyCharacterEnvelope Character { get; }

        public string ProfileSaveId => Profile.ProfileSaveId;

        public string ProfileSessionToken => Profile.ProfileSessionToken;
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
        public PersistlyRuntimeConfig(PersistlySyncPolicy syncPolicy)
        {
            SyncPolicy = syncPolicy;
        }

        public PersistlySyncPolicy SyncPolicy { get; }
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
        public PersistlySyncResponse(PersistlySyncStatus status, PersistlySave save, PersistlySyncConflictDetails? details = null)
        {
            Status = status;
            Save = save;
            Details = details;
        }

        public PersistlySyncStatus Status { get; }

        public PersistlySave Save { get; }

        public PersistlySyncConflictDetails? Details { get; }
    }
}
