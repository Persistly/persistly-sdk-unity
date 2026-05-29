#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Persistly.Unity
{
    public interface IPersistlySaveCache
    {
        bool TryGet(string saveId, out PersistlySave save);

        void Store(PersistlySave save);

        void Clear(string saveId);
    }

    public sealed class InMemoryPersistlySaveCache : IPersistlySaveCache
    {
        private readonly Dictionary<string, PersistlySave> _saves = new Dictionary<string, PersistlySave>(StringComparer.Ordinal);
        private readonly object _gate = new object();

        public bool TryGet(string saveId, out PersistlySave save)
        {
            lock (_gate)
            {
                return _saves.TryGetValue(saveId, out save);
            }
        }

        public void Store(PersistlySave save)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            lock (_gate)
            {
                _saves[save.SaveId] = save;
            }
        }

        public void Clear(string saveId)
        {
            lock (_gate)
            {
                _saves.Remove(saveId);
            }
        }
    }

    public sealed class PersistlyAutosaveDraft
    {
        public PersistlyAutosaveDraft(
            string accountId,
            string accountSessionToken,
            string slotId,
            string slotInfoJson,
            string stateJson,
            int? baseVersion,
            DateTimeOffset updatedAt)
        {
            AccountId = accountId;
            AccountSessionToken = accountSessionToken;
            SlotId = slotId;
            SlotInfoJson = slotInfoJson;
            StateJson = stateJson;
            BaseVersion = baseVersion;
            UpdatedAt = updatedAt;
        }

        public string AccountId { get; }

        public string AccountSessionToken { get; }

        public string SlotId { get; }

        public string SlotInfoJson { get; }

        public string StateJson { get; }

        public int? BaseVersion { get; }

        public DateTimeOffset UpdatedAt { get; }
    }

    public interface IPersistlyAutosaveDraftStore
    {
        bool TryLoad(string slotId, out PersistlyAutosaveDraft draft);

        void Store(PersistlyAutosaveDraft draft);

        void Clear(string slotId);
    }

    public sealed class InMemoryPersistlyAutosaveDraftStore : IPersistlyAutosaveDraftStore
    {
        private readonly Dictionary<string, PersistlyAutosaveDraft> _drafts = new Dictionary<string, PersistlyAutosaveDraft>(StringComparer.Ordinal);
        private readonly object _gate = new object();

        public bool TryLoad(string slotId, out PersistlyAutosaveDraft draft)
        {
            lock (_gate)
            {
                return _drafts.TryGetValue(slotId, out draft);
            }
        }

        public void Store(PersistlyAutosaveDraft draft)
        {
            if (draft == null)
            {
                throw new ArgumentNullException(nameof(draft));
            }

            lock (_gate)
            {
                _drafts[draft.SlotId] = draft;
            }
        }

        public void Clear(string slotId)
        {
            lock (_gate)
            {
                _drafts.Remove(slotId);
            }
        }
    }

    public sealed class FilePersistlyAutosaveDraftStore : IPersistlyAutosaveDraftStore
    {
        private readonly string _rootDirectory;

        public FilePersistlyAutosaveDraftStore(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new PersistlyConfigurationError("FilePersistlyAutosaveDraftStore rootDirectory must be set.");
            }

            _rootDirectory = rootDirectory;
            Directory.CreateDirectory(_rootDirectory);
        }

        public bool TryLoad(string slotId, out PersistlyAutosaveDraft draft)
        {
            var path = DraftPath(slotId);
            if (!File.Exists(path))
            {
                draft = default!;
                return false;
            }

            try
            {
                var parsed = PersistlyJson.ParseJsonValue(File.ReadAllText(path), "autosave draft") as Dictionary<string, object?>;
                if (parsed == null)
                {
                    draft = default!;
                    return false;
                }

                draft = new PersistlyAutosaveDraft(
                    RequireString(parsed, "accountId"),
                    RequireString(parsed, "accountSessionToken"),
                    RequireString(parsed, "slotId"),
                    PersistlyJson.Serialize(RequireObject(parsed, "slotInfo")),
                    PersistlyJson.Serialize(RequireObject(parsed, "state")),
                    OptionalInt(parsed, "baseVersion"),
                    DateTimeOffset.Parse(RequireString(parsed, "updatedAt"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
                return true;
            }
            catch
            {
                draft = default!;
                return false;
            }
        }

        public void Store(PersistlyAutosaveDraft draft)
        {
            if (draft == null)
            {
                throw new ArgumentNullException(nameof(draft));
            }

            Directory.CreateDirectory(_rootDirectory);
            var payload = new Dictionary<string, object?>
            {
                { "accountId", draft.AccountId },
                { "accountSessionToken", draft.AccountSessionToken },
                { "slotId", draft.SlotId },
                { "slotInfo", PersistlyJson.ParseJsonValue(draft.SlotInfoJson, "slotInfo") },
                { "state", PersistlyJson.ParseJsonValue(draft.StateJson, "state") },
                { "baseVersion", draft.BaseVersion },
                { "updatedAt", draft.UpdatedAt.ToString("O", CultureInfo.InvariantCulture) }
            };
            File.WriteAllText(DraftPath(draft.SlotId), PersistlyJson.Serialize(payload));
        }

        public void Clear(string slotId)
        {
            var path = DraftPath(slotId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private string DraftPath(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                throw new PersistlyConfigurationError("slotId must be set.");
            }

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                slotId = slotId.Replace(invalid, '_');
            }

            return Path.Combine(_rootDirectory, slotId + ".json");
        }

        private static string RequireString(Dictionary<string, object?> source, string key)
        {
            if (!source.TryGetValue(key, out var raw) || !(raw is string value) || string.IsNullOrWhiteSpace(value))
            {
                throw new PersistlyConfigurationError("autosave draft is missing " + key + ".");
            }

            return value;
        }

        private static Dictionary<string, object?> RequireObject(Dictionary<string, object?> source, string key)
        {
            if (!source.TryGetValue(key, out var raw) || !(raw is Dictionary<string, object?> value))
            {
                throw new PersistlyConfigurationError("autosave draft is missing " + key + ".");
            }

            return value;
        }

        private static int? OptionalInt(Dictionary<string, object?> source, string key)
        {
            if (!source.TryGetValue(key, out var raw) || raw == null)
            {
                return null;
            }

            if (raw is long longValue)
            {
                return checked((int)longValue);
            }

            if (raw is int intValue)
            {
                return intValue;
            }

            if (raw is double doubleValue)
            {
                return checked((int)Math.Round(doubleValue));
            }

            return null;
        }
    }

    public enum PersistlyAutosaveSkippedReason
    {
        None,
        NoDraft,
        ForceSyncCooldown,
        RemoteSyncInterval
    }

    public sealed class PersistlyAutosaveSyncResult
    {
        public PersistlyAutosaveSyncResult(bool syncedRemotely, int? syncedVersion = null, PersistlyAutosaveSkippedReason skippedReason = PersistlyAutosaveSkippedReason.None)
        {
            SyncedRemotely = syncedRemotely;
            SyncedVersion = syncedVersion;
            SkippedReason = skippedReason;
        }

        public bool SyncedRemotely { get; }

        public int? SyncedVersion { get; }

        public PersistlyAutosaveSkippedReason SkippedReason { get; }
    }

    public delegate Task<PersistlyAutosaveSyncResult> PersistlyAutosaveSyncDelegate(PersistlyAutosaveDraft draft, bool force, CancellationToken cancellationToken);

    public sealed class PersistlyAutosaveManager
    {
        private readonly IPersistlyAutosaveDraftStore _store;
        private readonly PersistlySyncPolicy _syncPolicy;
        private readonly PersistlyAutosaveSyncDelegate _syncRemote;
        private readonly Dictionary<string, DateTimeOffset> _lastRemoteSyncBySlot = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);

        public PersistlyAutosaveManager(IPersistlyAutosaveDraftStore store, PersistlySyncPolicy syncPolicy, PersistlyAutosaveSyncDelegate syncRemote)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _syncPolicy = syncPolicy ?? throw new ArgumentNullException(nameof(syncPolicy));
            _syncRemote = syncRemote ?? throw new ArgumentNullException(nameof(syncRemote));
        }

        public Task RecordLocalChangeAsync(
            string accountId,
            string accountSessionToken,
            string slotId,
            string slotInfoJson,
            string stateJson,
            int? baseVersion = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new PersistlyConfigurationError("accountId must be set.");
            }

            if (string.IsNullOrWhiteSpace(accountSessionToken))
            {
                throw new PersistlyConfigurationError("accountSessionToken must be set.");
            }

            if (string.IsNullOrWhiteSpace(slotId))
            {
                throw new PersistlyConfigurationError("slotId must be set.");
            }

            var canonicalSlotInfo = PersistlyJson.CanonicalizeObjectJson(slotInfoJson, "slotInfo");
            var canonicalState = PersistlyJson.CanonicalizeObjectJson(stateJson, "state");
            PersistlyJson.ValidatePayloadSizes(canonicalSlotInfo, canonicalState);
            _store.Store(new PersistlyAutosaveDraft(
                accountId,
                accountSessionToken,
                slotId,
                canonicalSlotInfo,
                canonicalState,
                baseVersion,
                DateTimeOffset.UtcNow));
            return Task.CompletedTask;
        }

        public Task<PersistlyAutosaveSyncResult> SyncIfDueAsync(string slotId, CancellationToken cancellationToken = default)
        {
            return SyncInternalAsync(slotId, false, cancellationToken);
        }

        public Task<PersistlyAutosaveSyncResult> ForceSyncAsync(string slotId, CancellationToken cancellationToken = default)
        {
            return SyncInternalAsync(slotId, true, cancellationToken);
        }

        private async Task<PersistlyAutosaveSyncResult> SyncInternalAsync(string slotId, bool force, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            if (_lastRemoteSyncBySlot.TryGetValue(slotId, out var lastSync))
            {
                var elapsedSeconds = (now - lastSync).TotalSeconds;
                if (force && elapsedSeconds < _syncPolicy.ForceSyncCooldownSeconds)
                {
                    return new PersistlyAutosaveSyncResult(false, null, PersistlyAutosaveSkippedReason.ForceSyncCooldown);
                }

                if (!force && elapsedSeconds < _syncPolicy.MinRemoteSyncIntervalSeconds)
                {
                    return new PersistlyAutosaveSyncResult(false, null, PersistlyAutosaveSkippedReason.RemoteSyncInterval);
                }
            }

            if (!_store.TryLoad(slotId, out var draft))
            {
                return new PersistlyAutosaveSyncResult(false, null, PersistlyAutosaveSkippedReason.NoDraft);
            }

            var result = await _syncRemote(draft, force, cancellationToken);
            if (result.SyncedRemotely)
            {
                _lastRemoteSyncBySlot[slotId] = now;
                _store.Clear(slotId);
            }

            return result;
        }
    }
}
