#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Persistly.Unity
{
    public enum PersistlyGameSaveStatus
    {
        LocalSaved,
        LocalFound,
        NotFound,
        NoChanges,
        Cooldown,
        Synced,
        Conflict,
        Offline,
        RateLimited
    }

    public enum PersistlyGameSaveTarget
    {
        Profile,
        Slot
    }

    public enum PersistlySlotStatus
    {
        LocalSaved,
        LocalFound,
        NotFound,
        NoChanges,
        Cooldown,
        Synced,
        Conflict,
        Offline,
        RateLimited
    }

    public sealed class PersistlyGameSaveResult
    {
        public PersistlyGameSaveResult(
            PersistlyGameSaveTarget target,
            PersistlyGameSaveStatus status,
            PersistlyGameSaveConflict? conflict = null,
            bool historyRetained = false,
            IReadOnlyList<string>? warnings = null)
        {
            Target = target;
            Status = status;
            Conflict = conflict;
            HistoryRetained = historyRetained;
            Warnings = warnings ?? Array.Empty<string>();
        }

        public PersistlyGameSaveTarget Target { get; }

        public PersistlyGameSaveStatus Status { get; }

        public PersistlyGameSaveConflict? Conflict { get; }

        public bool HistoryRetained { get; }

        public IReadOnlyList<string> Warnings { get; }
    }

    public class PersistlySlotResult
    {
        public PersistlySlotResult(
            string slotKey,
            PersistlySlotStatus status,
            PersistlyGameSaveConflict? conflict = null,
            bool historyRetained = false,
            IReadOnlyList<string>? warnings = null)
        {
            SlotKey = slotKey;
            Status = status;
            Conflict = conflict;
            HistoryRetained = historyRetained;
            Warnings = warnings ?? Array.Empty<string>();
        }

        public string SlotKey { get; }

        public PersistlySlotStatus Status { get; }

        public PersistlyGameSaveConflict? Conflict { get; }

        public bool HistoryRetained { get; }

        public IReadOnlyList<string> Warnings { get; }
    }

    public sealed class PersistlySlotResult<TState> : PersistlySlotResult where TState : class
    {
        public PersistlySlotResult(string slotKey, PersistlySlotStatus status, TState? state, bool found, PersistlySlotInspection? inspection = null)
            : base(slotKey, status)
        {
            State = state;
            Found = found;
            MetadataJson = inspection?.MetadataJson;
            Dirty = inspection?.Dirty ?? false;
            Version = inspection?.Version;
            CharacterSaveId = inspection?.CharacterSaveId;
            CloudStateJson = inspection?.CloudStateJson;
            CloudMetadataJson = inspection?.CloudMetadataJson;
            CloudVersion = inspection?.CloudVersion;
            Archived = inspection?.Archived ?? false;
            UpdatedAt = inspection?.UpdatedAt;
            LastRemoteSyncAt = inspection?.LastRemoteSyncAt;
        }

        public TState? State { get; }

        public bool Found { get; }

        public string? MetadataJson { get; }

        public bool Dirty { get; }

        public int? Version { get; }

        public string? CharacterSaveId { get; }

        public string? CloudStateJson { get; }

        public string? CloudMetadataJson { get; }

        public int? CloudVersion { get; }

        public bool Archived { get; }

        public DateTimeOffset? UpdatedAt { get; }

        public DateTimeOffset? LastRemoteSyncAt { get; }
    }

    public sealed class PersistlyGameSaveConflict
    {
        public PersistlyGameSaveConflict(
            PersistlyGameSaveTarget target,
            string? slotKey,
            string? localStateJson,
            string? localMetadataJson,
            int? localVersion,
            DateTimeOffset? localUpdatedAt,
            string? cloudStateJson,
            string? cloudMetadataJson,
            int? cloudVersion,
            DateTimeOffset? cloudUpdatedAt)
        {
            Target = target;
            SlotKey = slotKey;
            LocalStateJson = localStateJson;
            LocalMetadataJson = localMetadataJson;
            LocalVersion = localVersion;
            LocalUpdatedAt = localUpdatedAt;
            CloudStateJson = cloudStateJson;
            CloudMetadataJson = cloudMetadataJson;
            CloudVersion = cloudVersion;
            CloudUpdatedAt = cloudUpdatedAt;
        }

        public PersistlyGameSaveTarget Target { get; }

        public string? SlotKey { get; }

        public string? LocalStateJson { get; }

        public string? LocalMetadataJson { get; }

        public int? LocalVersion { get; }

        public DateTimeOffset? LocalUpdatedAt { get; }

        public string? CloudStateJson { get; }

        public string? CloudMetadataJson { get; }

        public int? CloudVersion { get; }

        public DateTimeOffset? CloudUpdatedAt { get; }
    }

    public sealed class PersistlyGameSavesSettings
    {
        public PersistlyGameSavesSettings(string runtimeKey)
        {
            RuntimeKey = runtimeKey;
        }

        public string RuntimeKey { get; }

        public string BaseUrl { get; set; } = PersistlyClientOptions.DefaultBaseUrl;

        public string? PlayerRef { get; set; }

        public string? ExternalProfileRefJson { get; set; }

        public string? LocalProfileKey { get; set; }

        public string? ProfileSaveId { get; set; }

        public string? ProfileSessionToken { get; set; }

        public IPersistlyTransport? Transport { get; set; }

        public IPersistlyGameSavesStore? Store { get; set; }

        public PersistlySyncPolicy? SyncPolicy { get; set; }

        public Action<PersistlySyncNotification>? OnSyncResult { get; set; }
    }

    public sealed class PersistlySaveSlotOptions
    {
        public string? MetadataJson { get; set; }
    }

    public sealed class PersistlySyncOptions
    {
        public bool BypassCooldown { get; set; }

        public bool IncludeSkipped { get; set; }
    }

    public sealed class PersistlyListSlotsOptions
    {
        public bool IncludeArchived { get; set; }
    }

    public sealed class PersistlyProfileSessionInfo
    {
        public PersistlyProfileSessionInfo(string? profileSaveId, string? profileSessionToken)
        {
            ProfileSaveId = profileSaveId;
            ProfileSessionToken = profileSessionToken;
        }

        public string? ProfileSaveId { get; }

        public string? ProfileSessionToken { get; }
    }

    public sealed class PersistlyProfileInspection
    {
        public PersistlyProfileInspection(string accountDataJson, string metadataJson, bool dirty, int? version)
        {
            AccountDataJson = accountDataJson;
            MetadataJson = metadataJson;
            Dirty = dirty;
            Version = version;
        }

        public string AccountDataJson { get; }

        public string MetadataJson { get; }

        public bool Dirty { get; }

        public int? Version { get; }
    }

    public sealed class PersistlySlotInspection
    {
        public PersistlySlotInspection(
            string slotKey,
            bool exists,
            string? stateJson,
            string? metadataJson,
            bool dirty,
            int? version,
            string? characterSaveId,
            string? cloudStateJson,
            string? cloudMetadataJson,
            int? cloudVersion,
            bool archived,
            DateTimeOffset? updatedAt,
            DateTimeOffset? lastRemoteSyncAt)
        {
            SlotKey = slotKey;
            Exists = exists;
            StateJson = stateJson;
            MetadataJson = metadataJson;
            Dirty = dirty;
            Version = version;
            CharacterSaveId = characterSaveId;
            CloudStateJson = cloudStateJson;
            CloudMetadataJson = cloudMetadataJson;
            CloudVersion = cloudVersion;
            Archived = archived;
            UpdatedAt = updatedAt;
            LastRemoteSyncAt = lastRemoteSyncAt;
        }

        public string SlotKey { get; }

        public bool Exists { get; }

        public string? StateJson { get; }

        public string? MetadataJson { get; }

        public bool Dirty { get; }

        public int? Version { get; }

        public string? CharacterSaveId { get; }

        public string? CloudStateJson { get; }

        public string? CloudMetadataJson { get; }

        public int? CloudVersion { get; }

        public bool Archived { get; }

        public DateTimeOffset? UpdatedAt { get; }

        public DateTimeOffset? LastRemoteSyncAt { get; }
    }

    public sealed class PersistlySyncNotification
    {
        public PersistlySyncNotification(PersistlyGameSaveTarget target, PersistlyGameSaveStatus status, string? slotKey = null, PersistlyGameSaveConflict? conflict = null)
        {
            Target = target;
            Status = status;
            SlotKey = slotKey;
            Conflict = conflict;
        }

        public PersistlyGameSaveTarget Target { get; }

        public PersistlyGameSaveStatus Status { get; }

        public string? SlotKey { get; }

        public PersistlyGameSaveConflict? Conflict { get; }
    }

    public interface IPersistlyGameSavesStore
    {
        string? LoadProfileJson(string localProfileKey);

        void SaveProfileJson(string localProfileKey, string json);

        string? LoadSlotJson(string localProfileKey, string slotKey);

        void SaveSlotJson(string localProfileKey, string slotKey, string json);

        void DeleteSlotJson(string localProfileKey, string slotKey);

        IReadOnlyList<string> ListSlotKeys(string localProfileKey);
    }

    public sealed class InMemoryPersistlyGameSavesStore : IPersistlyGameSavesStore
    {
        private readonly Dictionary<string, string> _profiles = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, string>> _slots = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        public string? LoadProfileJson(string localProfileKey)
        {
            return _profiles.TryGetValue(localProfileKey, out var value) ? value : null;
        }

        public void SaveProfileJson(string localProfileKey, string json)
        {
            _profiles[localProfileKey] = json;
        }

        public string? LoadSlotJson(string localProfileKey, string slotKey)
        {
            return _slots.TryGetValue(localProfileKey, out var profileSlots) && profileSlots.TryGetValue(slotKey, out var value) ? value : null;
        }

        public void SaveSlotJson(string localProfileKey, string slotKey, string json)
        {
            if (!_slots.TryGetValue(localProfileKey, out var profileSlots))
            {
                profileSlots = new Dictionary<string, string>(StringComparer.Ordinal);
                _slots[localProfileKey] = profileSlots;
            }

            profileSlots[slotKey] = json;
        }

        public void DeleteSlotJson(string localProfileKey, string slotKey)
        {
            if (_slots.TryGetValue(localProfileKey, out var profileSlots))
            {
                profileSlots.Remove(slotKey);
            }
        }

        public IReadOnlyList<string> ListSlotKeys(string localProfileKey)
        {
            if (!_slots.TryGetValue(localProfileKey, out var profileSlots))
            {
                return Array.Empty<string>();
            }

            return new List<string>(profileSlots.Keys);
        }
    }

    public sealed class FilePersistlyGameSavesStore : IPersistlyGameSavesStore
    {
        private readonly string _rootDirectory;

        public FilePersistlyGameSavesStore(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new PersistlyConfigurationError("FilePersistlyGameSavesStore rootDirectory must be set.");
            }

            _rootDirectory = rootDirectory;
            Directory.CreateDirectory(_rootDirectory);
        }

        public string? LoadProfileJson(string localProfileKey)
        {
            var path = ProfilePath(localProfileKey);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public void SaveProfileJson(string localProfileKey, string json)
        {
            Directory.CreateDirectory(ProfileDirectory(localProfileKey));
            File.WriteAllText(ProfilePath(localProfileKey), json);
        }

        public string? LoadSlotJson(string localProfileKey, string slotKey)
        {
            var path = SlotPath(localProfileKey, slotKey);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public void SaveSlotJson(string localProfileKey, string slotKey, string json)
        {
            Directory.CreateDirectory(SlotsDirectory(localProfileKey));
            File.WriteAllText(SlotPath(localProfileKey, slotKey), json);
        }

        public void DeleteSlotJson(string localProfileKey, string slotKey)
        {
            var path = SlotPath(localProfileKey, slotKey);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public IReadOnlyList<string> ListSlotKeys(string localProfileKey)
        {
            var directory = SlotsDirectory(localProfileKey);
            if (!Directory.Exists(directory))
            {
                return Array.Empty<string>();
            }

            var keys = new List<string>();
            foreach (var path in Directory.GetFiles(directory, "*.json"))
            {
                keys.Add(Path.GetFileNameWithoutExtension(path));
            }

            return keys;
        }

        private string ProfileDirectory(string localProfileKey)
        {
            return Path.Combine(_rootDirectory, SafePath(localProfileKey));
        }

        private string SlotsDirectory(string localProfileKey)
        {
            return Path.Combine(ProfileDirectory(localProfileKey), "slots");
        }

        private string ProfilePath(string localProfileKey)
        {
            return Path.Combine(ProfileDirectory(localProfileKey), "profile.json");
        }

        private string SlotPath(string localProfileKey, string slotKey)
        {
            return Path.Combine(SlotsDirectory(localProfileKey), SafePath(slotKey) + ".json");
        }

        private static string SafePath(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value;
        }
    }

    public sealed class PersistlyGameSaves
    {
        private const string ProfileSchema = "persistly.local.profile.v1";
        private const string SlotSchema = "persistly.local.slot.v1";
        private const string AnonymousNamespaceSchema = "persistly.local.anonymous.v1";
        private const string AnonymousNamespaceRecordKey = "__persistly_anonymous_namespace__";
        private static readonly PersistlySyncPolicy DefaultSyncPolicy = new PersistlySyncPolicy(60, 10, true, true, true, 25);
        private static PersistlyGameSaves? _shared;

        private readonly PersistlyClient _client;
        private readonly IPersistlyGameSavesStore _store;
        private readonly string _localProfileKey;
        private readonly Dictionary<string, LocalSlotRecord> _slots = new Dictionary<string, LocalSlotRecord>(StringComparer.Ordinal);
        private readonly object _gate = new object();
        private LocalProfileRecord _profile;

        private PersistlyGameSaves(PersistlyGameSavesSettings settings, PersistlyClient client, IPersistlyGameSavesStore store, string localProfileKey, LocalProfileRecord profile)
        {
            Settings = settings;
            _client = client;
            _store = store;
            _localProfileKey = localProfileKey;
            _profile = profile;
            LoadSlots();
        }

        public static PersistlyGameSaves Shared
        {
            get
            {
                if (_shared == null)
                {
                    throw new PersistlyConfigurationError("persistly_game_saves_not_configured: call PersistlyGameSaves.ConfigureAsync before using PersistlyGameSaves.Shared.");
                }

                return _shared;
            }
        }

        public PersistlyGameSavesSettings Settings { get; }

        public static Task ConfigureAsync(PersistlyGameSavesSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (string.IsNullOrWhiteSpace(settings.RuntimeKey))
            {
                throw new PersistlyConfigurationError("PersistlyGameSavesSettings.RuntimeKey must be set.");
            }

            var store = settings.Store ?? new InMemoryPersistlyGameSavesStore();
            var localProfileKey = ResolveLocalProfileKey(settings, store);
            var profile = LoadProfile(store, localProfileKey, settings);
            var client = new PersistlyClient(new PersistlyClientOptions(settings.BaseUrl, settings.RuntimeKey.Trim())
            {
                Transport = settings.Transport,
                UserAgent = "Persistly Unity SDK/1.0.0"
            });

            _shared = new PersistlyGameSaves(settings, client, store, localProfileKey, profile);
            _shared.SaveProfile();
            return Task.CompletedTask;
        }

        public PersistlyProfileSessionInfo GetProfileSession(bool includeToken = false)
        {
            lock (_gate)
            {
                return new PersistlyProfileSessionInfo(_profile.ProfileSaveId, includeToken ? _profile.ProfileSessionToken : null);
            }
        }

        public PersistlyProfileInspection InspectProfile()
        {
            lock (_gate)
            {
                return new PersistlyProfileInspection(_profile.AccountDataJson, _profile.MetadataJson, _profile.Dirty, _profile.Version);
            }
        }

        public async Task<PersistlyGameSaveResult> EnsureProfileAsync(CancellationToken cancellationToken = default)
        {
            if (HasProfileSession())
            {
                if (!_profile.Version.HasValue)
                {
                    await RestoreProfileAsync(cancellationToken);
                    return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.Synced);
                }

                return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.LocalFound);
            }

            var created = await _client.CreateProfileAsync(new PersistlyCreateProfileRequest(
                _profile.AccountDataJson,
                profileMetadataJson: _profile.MetadataJson,
                playerRef: Settings.PlayerRef,
                externalProfileRefJson: Settings.ExternalProfileRefJson), cancellationToken);
            ApplyProfileResponse(created, false);
            return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.Synced);
        }

        public Task<PersistlyGameSaveResult> SaveAccountDataAsync<TState>(TState accountData)
        {
            var json = PersistlyJson.CanonicalizeObjectJson(JsonUtility.ToJson(accountData), "accountData");
            lock (_gate)
            {
                _profile.AccountDataJson = json;
                _profile.PendingAccountDataPatchJson = null;
                _profile.Dirty = true;
                _profile.UpdatedAt = DateTimeOffset.UtcNow;
                SaveProfile();
            }

            return Task.FromResult(new PersistlyGameSaveResult(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.LocalSaved));
        }

        public Task<PersistlyGameSaveResult> PatchAccountDataAsync(string accountDataPatchJson)
        {
            var patch = PersistlyJson.ParseJsonValue(accountDataPatchJson, "accountDataPatch") as Dictionary<string, object?>;
            if (patch == null)
            {
                throw new PersistlyConfigurationError("accountDataPatch must be a JSON object.");
            }

            lock (_gate)
            {
                var account = PersistlyJson.ParseJsonValue(_profile.AccountDataJson, "accountData") as Dictionary<string, object?> ?? new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var entry in patch)
                {
                    if (entry.Value == null)
                    {
                        account.Remove(entry.Key);
                    }
                    else
                    {
                        account[entry.Key] = entry.Value;
                    }
                }

                _profile.AccountDataJson = PersistlyJson.Serialize(account);
                _profile.PendingAccountDataPatchJson = PersistlyJson.Serialize(patch);
                _profile.Dirty = true;
                _profile.UpdatedAt = DateTimeOffset.UtcNow;
                SaveProfile();
            }

            return Task.FromResult(new PersistlyGameSaveResult(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.LocalSaved));
        }

        public async Task<PersistlyGameSaveResult> ForceSyncProfileAsync(PersistlySyncOptions? options = null, CancellationToken cancellationToken = default)
        {
            options = options ?? new PersistlySyncOptions();
            if (!_profile.Dirty)
            {
                return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.NoChanges);
            }

            if (!options.BypassCooldown && IsInCooldown(_profile.LastForceSyncAt, _profile.SyncPolicy.ForceSyncCooldownSeconds))
            {
                return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.Cooldown);
            }

            if (HasProfileSession() && !_profile.Version.HasValue)
            {
                await RestoreProfileAsync(cancellationToken, preserveLocalDirty: true);
            }
            else
            {
                await EnsureProfileAsync(cancellationToken);
            }
            var request = new PersistlySyncProfileAccountDataRequest(
                _profile.Version ?? 1,
                accountDataJson: _profile.PendingAccountDataPatchJson == null ? _profile.AccountDataJson : null,
                accountDataPatchJson: _profile.PendingAccountDataPatchJson);

            try
            {
                var response = await _client.SyncProfileAccountDataAsync(_profile.ProfileSaveId!, _profile.ProfileSessionToken!, request, cancellationToken);
                if (response.Status == PersistlySyncStatus.Conflict)
                {
                    var cloudAccountDataJson = ExtractAccountData(response.Save.StateJson);
                    var conflict = BuildProfileConflict(response.Save, cloudAccountDataJson);
                    _profile.CloudAccountDataJson = cloudAccountDataJson;
                    _profile.CloudVersion = response.Save.Version;
                    _profile.LastForceSyncAt = DateTimeOffset.UtcNow;
                    SaveProfile();
                    Notify(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.Conflict, conflict: conflict);
                    return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.Conflict, conflict);
                }

                ApplyProfileSave(response.Save, false);
                _profile.LastForceSyncAt = DateTimeOffset.UtcNow;
                _profile.LastRemoteSyncAt = _profile.LastForceSyncAt;
                SaveProfile();
                Notify(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.Synced);
                return new PersistlyGameSaveResult(
                    PersistlyGameSaveTarget.Profile,
                    PersistlyGameSaveStatus.Synced,
                    historyRetained: response.HistoryRetained,
                    warnings: response.Warnings);
            }
            catch (PersistlyRateLimitedError)
            {
                return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.RateLimited);
            }
            catch (PersistlyTransportError)
            {
                return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.Offline);
            }
        }

        public Task<PersistlyGameSaveResult> SyncDueProfileAsync(PersistlySyncOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (!_profile.Dirty)
            {
                return Task.FromResult(new PersistlyGameSaveResult(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.NoChanges));
            }

            if (IsInCooldown(_profile.LastRemoteSyncAt, _profile.SyncPolicy.MinRemoteSyncIntervalSeconds))
            {
                return Task.FromResult(new PersistlyGameSaveResult(PersistlyGameSaveTarget.Profile, PersistlyGameSaveStatus.Cooldown));
            }

            return ForceSyncProfileAsync(new PersistlySyncOptions { BypassCooldown = true }, cancellationToken);
        }

        public Task<PersistlySlotResult> SaveSlotAsync<TState>(string slotKey, TState state, PersistlySaveSlotOptions? options = null)
        {
            var normalizedSlotKey = PersistlySlotKey.Normalize(slotKey);
            var json = PersistlyJson.CanonicalizeObjectJson(JsonUtility.ToJson(state), "state");
            var metadata = PersistlyJson.CanonicalizeObjectJson(options?.MetadataJson ?? "{}", "metadata");
            PersistlyJson.ValidatePayloadSizes(metadata, json);

            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotKey, out var slot))
                {
                    slot = new LocalSlotRecord(normalizedSlotKey);
                    _slots[normalizedSlotKey] = slot;
                }

                if (slot.Archived)
                {
                    slot.CharacterSaveId = null;
                    slot.Version = null;
                    slot.CloudStateJson = null;
                    slot.CloudMetadataJson = null;
                    slot.CloudVersion = null;
                    slot.LastForceSyncAt = null;
                    slot.LastRemoteSyncAt = null;
                }

                slot.StateJson = json;
                slot.MetadataJson = metadata;
                slot.Dirty = true;
                slot.Archived = false;
                slot.UpdatedAt = DateTimeOffset.UtcNow;
                SaveSlot(slot);
            }

            return Task.FromResult(new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.LocalSaved));
        }

        public Task<PersistlySlotResult<TState>> LoadSlotAsync<TState>(string slotKey) where TState : class
        {
            var normalizedSlotKey = PersistlySlotKey.Normalize(slotKey);
            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotKey, out var slot) || slot.Archived)
                {
                    return Task.FromResult(new PersistlySlotResult<TState>(normalizedSlotKey, PersistlySlotStatus.NotFound, null, false));
                }

                var state = JsonUtility.FromJson<TState>(slot.StateJson);
                return Task.FromResult(new PersistlySlotResult<TState>(normalizedSlotKey, PersistlySlotStatus.LocalFound, state, true, ToInspection(slot)));
            }
        }

        public IReadOnlyList<PersistlySlotInspection> ListSlots(PersistlyListSlotsOptions? options = null)
        {
            options = options ?? new PersistlyListSlotsOptions();
            lock (_gate)
            {
                var result = new List<PersistlySlotInspection>();
                foreach (var slot in _slots.Values)
                {
                    if (!options.IncludeArchived && slot.Archived)
                    {
                        continue;
                    }

                    result.Add(ToInspection(slot));
                }

                return result;
            }
        }

        public PersistlySlotInspection InspectSlot(string slotKey)
        {
            var normalizedSlotKey = PersistlySlotKey.Normalize(slotKey);
            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotKey, out var slot))
                {
                    return new PersistlySlotInspection(normalizedSlotKey, false, null, null, false, null, null, null, null, null, false, null, null);
                }

                return ToInspection(slot);
            }
        }

        public async Task<PersistlySlotResult> ForceSyncAsync(string slotKey, PersistlySyncOptions? options = null, CancellationToken cancellationToken = default)
        {
            options = options ?? new PersistlySyncOptions();
            var normalizedSlotKey = PersistlySlotKey.Normalize(slotKey);
            LocalSlotRecord slot;
            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotKey, out slot))
                {
                    return new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.NotFound);
                }

                if (!slot.Dirty)
                {
                    return new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.NoChanges);
                }

                if (!options.BypassCooldown && IsInCooldown(slot.LastForceSyncAt, _profile.SyncPolicy.ForceSyncCooldownSeconds))
                {
                    return new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.Cooldown);
                }
            }

            try
            {
                if (!HasProfileSession() && string.IsNullOrWhiteSpace(slot.CharacterSaveId))
                {
                    var created = await _client.CreateProfileAsync(new PersistlyCreateProfileRequest(
                        _profile.AccountDataJson,
                        profileMetadataJson: _profile.MetadataJson,
                        playerRef: Settings.PlayerRef,
                        externalProfileRefJson: Settings.ExternalProfileRefJson,
                        character: new PersistlyCreateProfileInitialCharacterRequest(slot.SlotKey, slot.MetadataJson, slot.StateJson)), cancellationToken);
                    ApplyProfileResponse(created, false);
                    ApplySyncedSlot(slot, created.Character!);
                    SaveSlot(slot);
                    Notify(PersistlyGameSaveTarget.Slot, PersistlyGameSaveStatus.Synced, normalizedSlotKey);
                    return new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.Synced);
                }

                await EnsureProfileAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(slot.CharacterSaveId))
                {
                    var created = await _client.CreateProfileCharacterAsync(
                        _profile.ProfileSaveId!,
                        _profile.ProfileSessionToken!,
                        new PersistlyCreateProfileCharacterRequest(slot.SlotKey, slot.MetadataJson, slot.StateJson),
                        cancellationToken);
                    ApplyProfileResponse(created, false);
                    ApplySyncedSlot(slot, created.Character!);
                    SaveSlot(slot);
                    Notify(PersistlyGameSaveTarget.Slot, PersistlyGameSaveStatus.Synced, normalizedSlotKey);
                    return new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.Synced);
                }

                var response = await _client.SyncProfileCharacterAsync(
                    _profile.ProfileSaveId!,
                    _profile.ProfileSessionToken!,
                    slot.CharacterSaveId,
                    new PersistlySyncSaveRequest(slot.StateJson, slot.Version, BuildRemoteSlotMetadataJson(slot)),
                    cancellationToken);
                slot.LastForceSyncAt = DateTimeOffset.UtcNow;
                if (response.Status == PersistlySyncStatus.Conflict)
                {
                    var conflict = BuildSlotConflict(slot, response.Save);
                    slot.CloudStateJson = response.Save.StateJson;
                    slot.CloudMetadataJson = response.Save.MetadataJson;
                    slot.CloudVersion = response.Save.Version;
                    slot.LastRemoteSyncAt = slot.LastForceSyncAt;
                    SaveSlot(slot);
                    Notify(PersistlyGameSaveTarget.Slot, PersistlyGameSaveStatus.Conflict, normalizedSlotKey, conflict);
                    return new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.Conflict, conflict);
                }

                ApplySyncedSlot(slot, response.Save);
                SaveSlot(slot);
                Notify(PersistlyGameSaveTarget.Slot, PersistlyGameSaveStatus.Synced, normalizedSlotKey);
                return new PersistlySlotResult(
                    normalizedSlotKey,
                    PersistlySlotStatus.Synced,
                    historyRetained: response.HistoryRetained,
                    warnings: response.Warnings);
            }
            catch (PersistlyRateLimitedError)
            {
                return new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.RateLimited);
            }
            catch (PersistlyTransportError)
            {
                return new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.Offline);
            }
        }

        public async Task<IReadOnlyList<PersistlySlotResult>> SyncDueSlotsAsync(PersistlySyncOptions? options = null, CancellationToken cancellationToken = default)
        {
            options = options ?? new PersistlySyncOptions();
            var results = new List<PersistlySlotResult>();
            List<string> keys;
            lock (_gate)
            {
                keys = new List<string>(_slots.Keys);
            }

            foreach (var key in keys)
            {
                var inspect = InspectSlot(key);
                if (!inspect.Dirty)
                {
                    if (options.IncludeSkipped)
                    {
                        results.Add(new PersistlySlotResult(key, PersistlySlotStatus.NoChanges));
                    }

                    continue;
                }

                if (IsInCooldown(inspect.LastRemoteSyncAt, _profile.SyncPolicy.MinRemoteSyncIntervalSeconds))
                {
                    if (options.IncludeSkipped)
                    {
                        results.Add(new PersistlySlotResult(key, PersistlySlotStatus.Cooldown));
                    }

                    continue;
                }

                results.Add(await ForceSyncAsync(key, new PersistlySyncOptions { BypassCooldown = true }, cancellationToken));
            }

            return results;
        }

        public async Task<IReadOnlyList<object>> SyncDueAsync(PersistlySyncOptions? options = null, CancellationToken cancellationToken = default)
        {
            var results = new List<object>();
            var profile = await SyncDueProfileAsync(options, cancellationToken);
            if (profile.Status != PersistlyGameSaveStatus.NoChanges || (options != null && options.IncludeSkipped))
            {
                results.Add(profile);
            }

            foreach (var result in await SyncDueSlotsAsync(options, cancellationToken))
            {
                results.Add(result);
            }

            return results;
        }

        public async Task<PersistlySlotResult> ArchiveSlotAsync(string slotKey, CancellationToken cancellationToken = default)
        {
            var normalizedSlotKey = PersistlySlotKey.Normalize(slotKey);
            LocalSlotRecord slot;
            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotKey, out slot))
                {
                    return new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.NotFound);
                }
            }

            if (string.IsNullOrWhiteSpace(slot.CharacterSaveId))
            {
                throw new PersistlyConfigurationError("archive_slot_unsynced: archiveSlot requires a slot that has already synced to a remote character.");
            }

            if (!HasProfileSession())
            {
                throw new PersistlyConfigurationError("archive_slot_missing_profile_session: archiveSlot requires a stored profileSaveId and profileSessionToken.");
            }

            var response = await _client.ArchiveProfileCharacterAsync(_profile.ProfileSaveId!, _profile.ProfileSessionToken!, slot.CharacterSaveId, cancellationToken);
            ApplyProfileResponse(response, false);
            slot.Archived = true;
            slot.Dirty = false;
            slot.UpdatedAt = DateTimeOffset.UtcNow;
            SaveSlot(slot);
            return new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.Synced);
        }

        public Task<PersistlySlotResult> ClearLocalSlotAsync(string slotKey)
        {
            var normalizedSlotKey = PersistlySlotKey.Normalize(slotKey);
            lock (_gate)
            {
                _slots.Remove(normalizedSlotKey);
                _store.DeleteSlotJson(_localProfileKey, normalizedSlotKey);
            }

            return Task.FromResult(new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.LocalSaved));
        }

        public Task<PersistlySlotResult> AcceptCloudVersionAsync(string slotKey)
        {
            var normalizedSlotKey = PersistlySlotKey.Normalize(slotKey);
            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotKey, out var slot) || slot.CloudStateJson == null)
                {
                    return Task.FromResult(new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.NotFound));
                }

                slot.StateJson = slot.CloudStateJson;
                slot.MetadataJson = slot.CloudMetadataJson == null ? slot.MetadataJson : StripPersistlyMetadata(slot.CloudMetadataJson);
                slot.Version = slot.CloudVersion;
                slot.Dirty = false;
                SaveSlot(slot);
                return Task.FromResult(new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.LocalSaved));
            }
        }

        public Task<PersistlySlotResult> KeepLocalForLaterAsync(string slotKey)
        {
            var normalizedSlotKey = PersistlySlotKey.Normalize(slotKey);
            return Task.FromResult(new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.LocalSaved));
        }

        public Task<PersistlySlotResult> OverwriteCloudVersionAsync(string slotKey, PersistlySyncOptions? options = null, CancellationToken cancellationToken = default)
        {
            var normalizedSlotKey = PersistlySlotKey.Normalize(slotKey);
            lock (_gate)
            {
                if (_slots.TryGetValue(normalizedSlotKey, out var slot) && slot.CloudVersion.HasValue)
                {
                    slot.Version = slot.CloudVersion;
                    slot.Dirty = true;
                    SaveSlot(slot);
                }
            }

            options = options ?? new PersistlySyncOptions { BypassCooldown = true };
            options.BypassCooldown = true;
            return ForceSyncAsync(normalizedSlotKey, options, cancellationToken);
        }

#if UNITY_INCLUDE_TESTS
        public void AttachCharacterForTests(string slotKey, string characterSaveId, int version)
        {
            var normalizedSlotKey = PersistlySlotKey.Normalize(slotKey);
            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotKey, out var slot))
                {
                    throw new PersistlyConfigurationError("slot_not_found: no local save exists for slotKey.");
                }

                slot.CharacterSaveId = characterSaveId;
                slot.Version = version;
                SaveSlot(slot);
            }
        }
#endif

        private void Notify(PersistlyGameSaveTarget target, PersistlyGameSaveStatus status, string? slotKey = null, PersistlyGameSaveConflict? conflict = null)
        {
            Settings.OnSyncResult?.Invoke(new PersistlySyncNotification(target, status, slotKey, conflict));
        }

        private void ApplyProfileResponse(PersistlyCreateProfileResponse response, bool dirty)
        {
            lock (_gate)
            {
                _profile.ProfileSaveId = response.ProfileSaveId;
                if (!string.IsNullOrWhiteSpace(response.ProfileSessionToken))
                {
                    _profile.ProfileSessionToken = response.ProfileSessionToken;
                }

                _profile.SyncPolicy = response.SyncPolicy;
                ApplyProfileSave(response.Profile, dirty);
                SaveProfile();
            }
        }

        private async Task RestoreProfileAsync(CancellationToken cancellationToken, bool preserveLocalDirty = false)
        {
            var localAccountDataJson = _profile.AccountDataJson;
            var localMetadataJson = _profile.MetadataJson;
            var localPendingAccountDataPatchJson = _profile.PendingAccountDataPatchJson;
            var localDirty = _profile.Dirty;
            var envelope = await _client.LoadProfileAsync(_profile.ProfileSaveId!, _profile.ProfileSessionToken!, cancellationToken);
            lock (_gate)
            {
                _profile.ProfileSaveId = envelope.ProfileSaveId;
                if (!string.IsNullOrWhiteSpace(envelope.ProfileSessionToken))
                {
                    _profile.ProfileSessionToken = envelope.ProfileSessionToken;
                }

                if (envelope.SyncPolicy != null)
                {
                    _profile.SyncPolicy = envelope.SyncPolicy;
                }

                ApplyProfileSave(envelope.Profile, false);
                if (preserveLocalDirty && localDirty)
                {
                    _profile.AccountDataJson = localAccountDataJson;
                    _profile.MetadataJson = localMetadataJson;
                    _profile.PendingAccountDataPatchJson = localPendingAccountDataPatchJson;
                    _profile.Dirty = true;
                }
                _profile.LastRemoteSyncAt = DateTimeOffset.UtcNow;
                SaveProfile();
            }
        }

        private void ApplyProfileSave(PersistlySave save, bool dirty)
        {
            _profile.ProfileSaveId = save.SaveId;
            _profile.Version = save.Version;
            _profile.AccountDataJson = ExtractAccountData(save.StateJson);
            _profile.MetadataJson = save.MetadataJson;
            _profile.PendingAccountDataPatchJson = null;
            _profile.Dirty = dirty;
            _profile.UpdatedAt = save.UpdatedAt;
        }

        private static string ExtractAccountData(string profileStateJson)
        {
            return PersistlyProfileState.Parse(profileStateJson).AccountDataJson;
        }

        private void ApplySyncedSlot(LocalSlotRecord slot, PersistlySave save)
        {
            slot.CharacterSaveId = save.SaveId;
            slot.StateJson = save.StateJson;
            slot.MetadataJson = StripPersistlyMetadata(save.MetadataJson);
            slot.Version = save.Version;
            slot.CloudStateJson = save.StateJson;
            slot.CloudMetadataJson = save.MetadataJson;
            slot.CloudVersion = save.Version;
            slot.Dirty = false;
            slot.LastForceSyncAt = DateTimeOffset.UtcNow;
            slot.LastRemoteSyncAt = slot.LastForceSyncAt;
            slot.UpdatedAt = save.UpdatedAt;
        }

        private static string StripPersistlyMetadata(string metadataJson)
        {
            var metadata = PersistlyJson.ParseJsonValue(metadataJson, "metadata") as Dictionary<string, object?>;
            if (metadata == null)
            {
                return "{}";
            }

            metadata.Remove("_persistly");
            return PersistlyJson.Serialize(metadata);
        }

        private static string BuildRemoteSlotMetadataJson(LocalSlotRecord slot)
        {
            return PersistlySlotKey.BuildMetadataJson(slot.SlotKey, slot.MetadataJson);
        }

        private PersistlyGameSaveConflict BuildSlotConflict(LocalSlotRecord slot, PersistlySave cloudSave)
        {
            return new PersistlyGameSaveConflict(
                PersistlyGameSaveTarget.Slot,
                slot.SlotKey,
                slot.StateJson,
                slot.MetadataJson,
                slot.Version,
                slot.UpdatedAt,
                cloudSave.StateJson,
                cloudSave.MetadataJson,
                cloudSave.Version,
                cloudSave.UpdatedAt);
        }

        private PersistlyGameSaveConflict BuildProfileConflict(PersistlySave cloudSave, string cloudAccountDataJson)
        {
            return new PersistlyGameSaveConflict(
                PersistlyGameSaveTarget.Profile,
                null,
                _profile.AccountDataJson,
                _profile.MetadataJson,
                _profile.Version,
                _profile.UpdatedAt,
                cloudAccountDataJson,
                cloudSave.MetadataJson,
                cloudSave.Version,
                cloudSave.UpdatedAt);
        }

        private bool HasProfileSession()
        {
            return !string.IsNullOrWhiteSpace(_profile.ProfileSaveId) && !string.IsNullOrWhiteSpace(_profile.ProfileSessionToken);
        }

        private static bool IsInCooldown(DateTimeOffset? lastSync, int cooldownSeconds)
        {
            return lastSync.HasValue && cooldownSeconds > 0 && (DateTimeOffset.UtcNow - lastSync.Value).TotalSeconds < cooldownSeconds;
        }

        private PersistlySlotInspection ToInspection(LocalSlotRecord slot)
        {
            return new PersistlySlotInspection(
                slot.SlotKey,
                true,
                slot.StateJson,
                slot.MetadataJson,
                slot.Dirty,
                slot.Version,
                slot.CharacterSaveId,
                slot.CloudStateJson,
                slot.CloudMetadataJson,
                slot.CloudVersion,
                slot.Archived,
                slot.UpdatedAt,
                slot.LastRemoteSyncAt);
        }

        private void LoadSlots()
        {
            foreach (var slotKey in _store.ListSlotKeys(_localProfileKey))
            {
                var json = _store.LoadSlotJson(_localProfileKey, slotKey);
                if (string.IsNullOrWhiteSpace(json))
                {
                    continue;
                }

                var slot = LocalSlotRecord.FromJson(json!);
                _slots[slot.SlotKey] = slot;
            }
        }

        private void SaveProfile()
        {
            _store.SaveProfileJson(_localProfileKey, _profile.ToJson());
        }

        private void SaveSlot(LocalSlotRecord slot)
        {
            _store.SaveSlotJson(_localProfileKey, slot.SlotKey, slot.ToJson());
        }

        private static LocalProfileRecord LoadProfile(IPersistlyGameSavesStore store, string localProfileKey, PersistlyGameSavesSettings settings)
        {
            var json = store.LoadProfileJson(localProfileKey);
            var profile = string.IsNullOrWhiteSpace(json) ? new LocalProfileRecord() : LocalProfileRecord.FromJson(json!);
            if (!string.IsNullOrWhiteSpace(settings.ProfileSaveId))
            {
                profile.ProfileSaveId = settings.ProfileSaveId!.Trim();
            }

            if (!string.IsNullOrWhiteSpace(settings.ProfileSessionToken))
            {
                profile.ProfileSessionToken = settings.ProfileSessionToken!.Trim();
            }

            profile.PlayerRef = string.IsNullOrWhiteSpace(settings.PlayerRef) ? profile.PlayerRef : settings.PlayerRef!.Trim();
            profile.ExternalProfileRefJson = string.IsNullOrWhiteSpace(settings.ExternalProfileRefJson) ? profile.ExternalProfileRefJson : PersistlyJson.CanonicalizeObjectJson(settings.ExternalProfileRefJson!, "externalProfileRef");
            profile.SyncPolicy = settings.SyncPolicy ?? profile.SyncPolicy ?? DefaultSyncPolicy;
            return profile;
        }

        private static string ResolveLocalProfileKey(PersistlyGameSavesSettings settings, IPersistlyGameSavesStore store)
        {
            if (!string.IsNullOrWhiteSpace(settings.LocalProfileKey))
            {
                return settings.LocalProfileKey!.Trim();
            }

            if (!string.IsNullOrWhiteSpace(settings.ExternalProfileRefJson))
            {
                var external = PersistlyJson.ParseJsonValue(settings.ExternalProfileRefJson!, "externalProfileRef") as Dictionary<string, object?>;
                if (external != null && external.TryGetValue("provider", out var providerRaw) && providerRaw is string provider && external.TryGetValue("subject", out var subjectRaw) && subjectRaw is string subject)
                {
                    return provider + ":" + subject;
                }
            }

            if (!string.IsNullOrWhiteSpace(settings.PlayerRef))
            {
                return settings.PlayerRef!.Trim();
            }

            return ResolveAnonymousLocalProfileKey(store);
        }

        private static string ResolveAnonymousLocalProfileKey(IPersistlyGameSavesStore store)
        {
            var json = store.LoadProfileJson(AnonymousNamespaceRecordKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var root = PersistlyJson.ParseJsonValue(json!, "anonymous namespace") as Dictionary<string, object?>;
                if (root == null)
                {
                    throw new PersistlyConfigurationError("anonymous namespace must be a JSON object.");
                }

                var schema = ReadString(root, "schema");
                if (!string.Equals(schema, AnonymousNamespaceSchema, StringComparison.Ordinal))
                {
                    throw new PersistlyConfigurationError("Unknown Persistly anonymous namespace schema: " + schema + ".");
                }

                var localProfileKey = ReadString(root, "localProfileKey");
                if (string.IsNullOrWhiteSpace(localProfileKey))
                {
                    throw new PersistlyConfigurationError("anonymous namespace is missing localProfileKey.");
                }

                return localProfileKey!;
            }

            var generated = "anonymous-" + Guid.NewGuid().ToString("N");
            store.SaveProfileJson(AnonymousNamespaceRecordKey, PersistlyJson.Serialize(new Dictionary<string, object?>
            {
                { "schema", AnonymousNamespaceSchema },
                { "localProfileKey", generated },
                { "createdAt", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture) }
            }));
            return generated;
        }

        private sealed class LocalProfileRecord
        {
            public string AccountDataJson = "{}";
            public string MetadataJson = "{}";
            public string? PendingAccountDataPatchJson;
            public string? ProfileSaveId;
            public string? ProfileSessionToken;
            public string? PlayerRef;
            public string? ExternalProfileRefJson;
            public PersistlySyncPolicy SyncPolicy = DefaultSyncPolicy;
            public bool Dirty;
            public int? Version;
            public string? CloudAccountDataJson;
            public int? CloudVersion;
            public DateTimeOffset? UpdatedAt;
            public DateTimeOffset? LastForceSyncAt;
            public DateTimeOffset? LastRemoteSyncAt;

            public string ToJson()
            {
                var payload = new Dictionary<string, object?>
                {
                    { "schema", ProfileSchema },
                    { "accountData", PersistlyJson.ParseJsonValue(AccountDataJson, "accountData") },
                    { "metadata", PersistlyJson.ParseJsonValue(MetadataJson, "metadata") },
                    { "pendingAccountDataPatch", PendingAccountDataPatchJson == null ? null : PersistlyJson.ParseJsonValue(PendingAccountDataPatchJson, "accountDataPatch") },
                    { "profileSaveId", ProfileSaveId },
                    { "profileSessionToken", ProfileSessionToken },
                    { "playerRef", PlayerRef },
                    { "externalProfileRef", ExternalProfileRefJson == null ? null : PersistlyJson.ParseJsonValue(ExternalProfileRefJson, "externalProfileRef") },
                    { "dirty", Dirty },
                    { "version", Version },
                    { "cloudAccountData", CloudAccountDataJson == null ? null : PersistlyJson.ParseJsonValue(CloudAccountDataJson, "cloudAccountData") },
                    { "cloudVersion", CloudVersion },
                    { "updatedAt", FormatDate(UpdatedAt) },
                    { "lastForceSyncAt", FormatDate(LastForceSyncAt) },
                    { "lastRemoteSyncAt", FormatDate(LastRemoteSyncAt) },
                    { "syncPolicy", SyncPolicyToDictionary(SyncPolicy) }
                };
                return PersistlyJson.Serialize(payload);
            }

            public static LocalProfileRecord FromJson(string json)
            {
                var root = PersistlyJson.ParseJsonValue(json, "local profile") as Dictionary<string, object?>;
                if (root == null)
                {
                    throw new PersistlyConfigurationError("local profile must be a JSON object.");
                }

                var schema = ReadString(root, "schema");
                if (!string.Equals(schema, ProfileSchema, StringComparison.Ordinal))
                {
                    throw new PersistlyConfigurationError("Unknown Persistly local profile schema: " + schema + ".");
                }

                var record = new LocalProfileRecord
                {
                    AccountDataJson = SerializeObject(root, "accountData", "{}"),
                    MetadataJson = SerializeObject(root, "metadata", "{}"),
                    PendingAccountDataPatchJson = SerializeNullableObject(root, "pendingAccountDataPatch"),
                    ProfileSaveId = ReadString(root, "profileSaveId"),
                    ProfileSessionToken = ReadString(root, "profileSessionToken"),
                    PlayerRef = ReadString(root, "playerRef"),
                    ExternalProfileRefJson = SerializeNullableObject(root, "externalProfileRef"),
                    Dirty = ReadBool(root, "dirty"),
                    Version = ReadInt(root, "version"),
                    CloudAccountDataJson = SerializeNullableObject(root, "cloudAccountData"),
                    CloudVersion = ReadInt(root, "cloudVersion"),
                    UpdatedAt = ReadDate(root, "updatedAt"),
                    LastForceSyncAt = ReadDate(root, "lastForceSyncAt"),
                    LastRemoteSyncAt = ReadDate(root, "lastRemoteSyncAt")
                };

                if (root.TryGetValue("syncPolicy", out var rawPolicy) && rawPolicy is Dictionary<string, object?> policy)
                {
                    record.SyncPolicy = new PersistlySyncPolicy(
                        ReadRequiredInt(policy, "minRemoteSyncIntervalSeconds"),
                        ReadRequiredInt(policy, "forceSyncCooldownSeconds"),
                        ReadBool(policy, "syncOnAppBackground"),
                        ReadBool(policy, "syncOnAppForeground"),
                        ReadBool(policy, "syncOnReconnect"),
                        ReadRequiredInt(policy, "maxQueuedLocalSnapshots"));
                }

                return record;
            }
        }

        private sealed class LocalSlotRecord
        {
            public LocalSlotRecord(string slotKey)
            {
                SlotKey = slotKey;
            }

            public string SlotKey;
            public string StateJson = "{}";
            public string MetadataJson = "{}";
            public string? CharacterSaveId;
            public bool Dirty;
            public bool Archived;
            public int? Version;
            public string? CloudStateJson;
            public string? CloudMetadataJson;
            public int? CloudVersion;
            public DateTimeOffset? UpdatedAt;
            public DateTimeOffset? LastForceSyncAt;
            public DateTimeOffset? LastRemoteSyncAt;

            public string ToJson()
            {
                var payload = new Dictionary<string, object?>
                {
                    { "schema", SlotSchema },
                    { "slotKey", SlotKey },
                    { "state", PersistlyJson.ParseJsonValue(StateJson, "state") },
                    { "metadata", PersistlyJson.ParseJsonValue(MetadataJson, "metadata") },
                    { "characterSaveId", CharacterSaveId },
                    { "dirty", Dirty },
                    { "archived", Archived },
                    { "version", Version },
                    { "cloudState", CloudStateJson == null ? null : PersistlyJson.ParseJsonValue(CloudStateJson, "cloudState") },
                    { "cloudMetadata", CloudMetadataJson == null ? null : PersistlyJson.ParseJsonValue(CloudMetadataJson, "cloudMetadata") },
                    { "cloudVersion", CloudVersion },
                    { "updatedAt", FormatDate(UpdatedAt) },
                    { "lastForceSyncAt", FormatDate(LastForceSyncAt) },
                    { "lastRemoteSyncAt", FormatDate(LastRemoteSyncAt) }
                };
                return PersistlyJson.Serialize(payload);
            }

            public static LocalSlotRecord FromJson(string json)
            {
                var root = PersistlyJson.ParseJsonValue(json, "local slot") as Dictionary<string, object?>;
                if (root == null)
                {
                    throw new PersistlyConfigurationError("local slot must be a JSON object.");
                }

                var schema = ReadString(root, "schema");
                if (!string.Equals(schema, SlotSchema, StringComparison.Ordinal))
                {
                    throw new PersistlyConfigurationError("Unknown Persistly local slot schema: " + schema + ".");
                }

                return new LocalSlotRecord(PersistlySlotKey.Normalize(ReadString(root, "slotKey") ?? ""))
                {
                    StateJson = SerializeObject(root, "state", "{}"),
                    MetadataJson = SerializeObject(root, "metadata", "{}"),
                    CharacterSaveId = ReadString(root, "characterSaveId"),
                    Dirty = ReadBool(root, "dirty"),
                    Archived = ReadBool(root, "archived"),
                    Version = ReadInt(root, "version"),
                    CloudStateJson = SerializeNullableObject(root, "cloudState"),
                    CloudMetadataJson = SerializeNullableObject(root, "cloudMetadata"),
                    CloudVersion = ReadInt(root, "cloudVersion"),
                    UpdatedAt = ReadDate(root, "updatedAt"),
                    LastForceSyncAt = ReadDate(root, "lastForceSyncAt"),
                    LastRemoteSyncAt = ReadDate(root, "lastRemoteSyncAt")
                };
            }
        }

        private static Dictionary<string, object?> SyncPolicyToDictionary(PersistlySyncPolicy policy)
        {
            return new Dictionary<string, object?>
            {
                { "minRemoteSyncIntervalSeconds", policy.MinRemoteSyncIntervalSeconds },
                { "forceSyncCooldownSeconds", policy.ForceSyncCooldownSeconds },
                { "syncOnAppBackground", policy.SyncOnBackground },
                { "syncOnAppForeground", policy.SyncOnForeground },
                { "syncOnReconnect", policy.SyncOnReconnect },
                { "maxQueuedLocalSnapshots", policy.MaxQueuedLocalSnapshots }
            };
        }

        private static string? FormatDate(DateTimeOffset? value)
        {
            return value.HasValue ? value.Value.ToString("O", CultureInfo.InvariantCulture) : null;
        }

        private static string? ReadString(Dictionary<string, object?> root, string key)
        {
            return root.TryGetValue(key, out var raw) && raw is string value && !string.IsNullOrWhiteSpace(value) ? value : null;
        }

        private static bool ReadBool(Dictionary<string, object?> root, string key)
        {
            return root.TryGetValue(key, out var raw) && raw is bool value && value;
        }

        private static int? ReadInt(Dictionary<string, object?> root, string key)
        {
            if (!root.TryGetValue(key, out var raw) || raw == null)
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

        private static int ReadRequiredInt(Dictionary<string, object?> root, string key)
        {
            var value = ReadInt(root, key);
            if (!value.HasValue)
            {
                throw new PersistlyConfigurationError(key + " must be an integer.");
            }

            return value.Value;
        }

        private static DateTimeOffset? ReadDate(Dictionary<string, object?> root, string key)
        {
            var value = ReadString(root, key);
            if (value == null)
            {
                return null;
            }

            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        private static string SerializeObject(Dictionary<string, object?> root, string key, string fallback)
        {
            if (!root.TryGetValue(key, out var raw) || raw == null)
            {
                return fallback;
            }

            if (!(raw is Dictionary<string, object?>))
            {
                throw new PersistlyConfigurationError(key + " must be a JSON object.");
            }

            return PersistlyJson.Serialize(raw);
        }

        private static string? SerializeNullableObject(Dictionary<string, object?> root, string key)
        {
            if (!root.TryGetValue(key, out var raw) || raw == null)
            {
                return null;
            }

            if (!(raw is Dictionary<string, object?>))
            {
                throw new PersistlyConfigurationError(key + " must be a JSON object or null.");
            }

            return PersistlyJson.Serialize(raw);
        }
    }
}
