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
            string profileSaveId,
            string profileSessionToken,
            string characterSaveId,
            string metadataJson,
            string stateJson,
            int? baseVersion,
            DateTimeOffset updatedAt)
        {
            ProfileSaveId = profileSaveId;
            ProfileSessionToken = profileSessionToken;
            CharacterSaveId = characterSaveId;
            MetadataJson = metadataJson;
            StateJson = stateJson;
            BaseVersion = baseVersion;
            UpdatedAt = updatedAt;
        }

        public string ProfileSaveId { get; }

        public string ProfileSessionToken { get; }

        public string CharacterSaveId { get; }

        public string MetadataJson { get; }

        public string StateJson { get; }

        public int? BaseVersion { get; }

        public DateTimeOffset UpdatedAt { get; }
    }

    public interface IPersistlyAutosaveDraftStore
    {
        bool TryLoad(string characterSaveId, out PersistlyAutosaveDraft draft);

        void Store(PersistlyAutosaveDraft draft);

        void Clear(string characterSaveId);
    }

    public sealed class InMemoryPersistlyAutosaveDraftStore : IPersistlyAutosaveDraftStore
    {
        private readonly Dictionary<string, PersistlyAutosaveDraft> _drafts = new Dictionary<string, PersistlyAutosaveDraft>(StringComparer.Ordinal);
        private readonly object _gate = new object();

        public bool TryLoad(string characterSaveId, out PersistlyAutosaveDraft draft)
        {
            lock (_gate)
            {
                return _drafts.TryGetValue(characterSaveId, out draft);
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
                _drafts[draft.CharacterSaveId] = draft;
            }
        }

        public void Clear(string characterSaveId)
        {
            lock (_gate)
            {
                _drafts.Remove(characterSaveId);
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

        public bool TryLoad(string characterSaveId, out PersistlyAutosaveDraft draft)
        {
            var path = DraftPath(characterSaveId);
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
                    RequireString(parsed, "profileSaveId"),
                    RequireString(parsed, "profileSessionToken"),
                    RequireString(parsed, "characterSaveId"),
                    PersistlyJson.Serialize(RequireObject(parsed, "metadata")),
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
                { "profileSaveId", draft.ProfileSaveId },
                { "profileSessionToken", draft.ProfileSessionToken },
                { "characterSaveId", draft.CharacterSaveId },
                { "metadata", PersistlyJson.ParseJsonValue(draft.MetadataJson, "metadata") },
                { "state", PersistlyJson.ParseJsonValue(draft.StateJson, "state") },
                { "baseVersion", draft.BaseVersion },
                { "updatedAt", draft.UpdatedAt.ToString("O", CultureInfo.InvariantCulture) }
            };
            File.WriteAllText(DraftPath(draft.CharacterSaveId), PersistlyJson.Serialize(payload));
        }

        public void Clear(string characterSaveId)
        {
            var path = DraftPath(characterSaveId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private string DraftPath(string characterSaveId)
        {
            if (string.IsNullOrWhiteSpace(characterSaveId))
            {
                throw new PersistlyConfigurationError("characterSaveId must be set.");
            }

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                characterSaveId = characterSaveId.Replace(invalid, '_');
            }

            return Path.Combine(_rootDirectory, characterSaveId + ".json");
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
        private readonly Dictionary<string, DateTimeOffset> _lastRemoteSyncByCharacter = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);

        public PersistlyAutosaveManager(IPersistlyAutosaveDraftStore store, PersistlySyncPolicy syncPolicy, PersistlyAutosaveSyncDelegate syncRemote)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _syncPolicy = syncPolicy ?? throw new ArgumentNullException(nameof(syncPolicy));
            _syncRemote = syncRemote ?? throw new ArgumentNullException(nameof(syncRemote));
        }

        public Task RecordLocalChangeAsync(
            string profileSaveId,
            string profileSessionToken,
            string characterSaveId,
            string metadataJson,
            string stateJson,
            int? baseVersion = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(profileSaveId))
            {
                throw new PersistlyConfigurationError("profileSaveId must be set.");
            }

            if (string.IsNullOrWhiteSpace(profileSessionToken))
            {
                throw new PersistlyConfigurationError("profileSessionToken must be set.");
            }

            if (string.IsNullOrWhiteSpace(characterSaveId))
            {
                throw new PersistlyConfigurationError("characterSaveId must be set.");
            }

            var canonicalMetadata = PersistlyJson.CanonicalizeObjectJson(metadataJson, "metadata");
            var canonicalState = PersistlyJson.CanonicalizeObjectJson(stateJson, "state");
            PersistlyJson.ValidatePayloadSizes(canonicalMetadata, canonicalState);
            _store.Store(new PersistlyAutosaveDraft(
                profileSaveId,
                profileSessionToken,
                characterSaveId,
                canonicalMetadata,
                canonicalState,
                baseVersion,
                DateTimeOffset.UtcNow));
            return Task.CompletedTask;
        }

        public Task<PersistlyAutosaveSyncResult> SyncIfDueAsync(string characterSaveId, CancellationToken cancellationToken = default)
        {
            return SyncInternalAsync(characterSaveId, false, cancellationToken);
        }

        public Task<PersistlyAutosaveSyncResult> ForceSyncAsync(string characterSaveId, CancellationToken cancellationToken = default)
        {
            return SyncInternalAsync(characterSaveId, true, cancellationToken);
        }

        private async Task<PersistlyAutosaveSyncResult> SyncInternalAsync(string characterSaveId, bool force, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            if (_lastRemoteSyncByCharacter.TryGetValue(characterSaveId, out var lastSync))
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

            if (!_store.TryLoad(characterSaveId, out var draft))
            {
                return new PersistlyAutosaveSyncResult(false, null, PersistlyAutosaveSkippedReason.NoDraft);
            }

            var result = await _syncRemote(draft, force, cancellationToken);
            if (result.SyncedRemotely)
            {
                _lastRemoteSyncByCharacter[characterSaveId] = now;
                _store.Clear(characterSaveId);
            }

            return result;
        }
    }
}
