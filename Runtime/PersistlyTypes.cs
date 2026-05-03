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

        public string UserAgent { get; set; } = "Persistly Unity SDK/0.9.0";
    }

    public sealed class PersistlyCreateSaveRequest
    {
        public PersistlyCreateSaveRequest(string stateJson, string? metadataJson = null, string? externalUserId = null)
        {
            StateJson = PersistlyJson.CanonicalizeObjectJson(stateJson, "state");
            MetadataJson = metadataJson == null ? null : PersistlyJson.CanonicalizeObjectJson(metadataJson, "metadata");
            ExternalUserId = string.IsNullOrWhiteSpace(externalUserId) ? null : externalUserId.Trim();
        }

        public string? ExternalUserId { get; }

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

    public sealed class PersistlySave
    {
        public PersistlySave(
            string saveId,
            string? externalUserId,
            string metadataJson,
            string stateJson,
            int version,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            SaveId = saveId;
            ExternalUserId = externalUserId;
            MetadataJson = metadataJson;
            StateJson = stateJson;
            Version = version;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public string SaveId { get; }

        public string? ExternalUserId { get; }

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
