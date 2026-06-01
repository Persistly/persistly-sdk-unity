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
        Account,
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
            string slotId,
            PersistlySlotStatus status,
            PersistlyGameSaveConflict? conflict = null,
            bool historyRetained = false,
            IReadOnlyList<string>? warnings = null)
        {
            SlotId = slotId;
            Status = status;
            Conflict = conflict;
            HistoryRetained = historyRetained;
            Warnings = warnings ?? Array.Empty<string>();
        }

        public string SlotId { get; }

        public PersistlySlotStatus Status { get; }

        public PersistlyGameSaveConflict? Conflict { get; }

        public bool HistoryRetained { get; }

        public IReadOnlyList<string> Warnings { get; }
    }

    public sealed class PersistlySlotResult<TState> : PersistlySlotResult where TState : class
    {
        public PersistlySlotResult(string slotId, PersistlySlotStatus status, TState? state, bool found, PersistlySlotInspection? inspection = null)
            : base(slotId, status)
        {
            State = state;
            Found = found;
            SlotInfoJson = inspection?.SlotInfoJson;
            Dirty = inspection?.Dirty ?? false;
            Version = inspection?.Version;
            CloudStateJson = inspection?.CloudStateJson;
            CloudSlotInfoJson = inspection?.CloudSlotInfoJson;
            CloudVersion = inspection?.CloudVersion;
            Archived = inspection?.Archived ?? false;
            UpdatedAt = inspection?.UpdatedAt;
            LastRemoteSyncAt = inspection?.LastRemoteSyncAt;
        }

        public TState? State { get; }

        public bool Found { get; }

        public string? SlotInfoJson { get; }

        public bool Dirty { get; }

        public int? Version { get; }

        public string? CloudStateJson { get; }

        public string? CloudSlotInfoJson { get; }

        public int? CloudVersion { get; }

        public bool Archived { get; }

        public DateTimeOffset? UpdatedAt { get; }

        public DateTimeOffset? LastRemoteSyncAt { get; }
    }

    public sealed class PersistlyGameSaveConflict
    {
        public PersistlyGameSaveConflict(
            PersistlyGameSaveTarget target,
            string? slotId,
            string? localStateJson,
            string? localSlotInfoJson,
            int? localVersion,
            DateTimeOffset? localUpdatedAt,
            string? cloudStateJson,
            string? cloudSlotInfoJson,
            int? cloudVersion,
            DateTimeOffset? cloudUpdatedAt)
        {
            Target = target;
            SlotId = slotId;
            LocalStateJson = localStateJson;
            LocalSlotInfoJson = localSlotInfoJson;
            LocalVersion = localVersion;
            LocalUpdatedAt = localUpdatedAt;
            CloudStateJson = cloudStateJson;
            CloudSlotInfoJson = cloudSlotInfoJson;
            CloudVersion = cloudVersion;
            CloudUpdatedAt = cloudUpdatedAt;
        }

        public PersistlyGameSaveTarget Target { get; }

        public string? SlotId { get; }

        public string? LocalStateJson { get; }

        public string? LocalSlotInfoJson { get; }

        public int? LocalVersion { get; }

        public DateTimeOffset? LocalUpdatedAt { get; }

        public string? CloudStateJson { get; }

        public string? CloudSlotInfoJson { get; }

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

        public string? ExternalAccountRefJson { get; set; }

        public string? LocalAccountKey { get; set; }

        public string? AccountId { get; set; }

        public string? AccountSessionToken { get; set; }

        public IPersistlyTransport? Transport { get; set; }

        public IPersistlyGameSavesStore? Store { get; set; }

        public PersistlySyncPolicy? SyncPolicy { get; set; }

        public Action<PersistlySyncNotification>? OnSyncResult { get; set; }
    }

    public sealed class PersistlySaveSlotOptions
    {
        public string? SlotInfoJson { get; set; }
    }

    public sealed class PersistlySyncOptions
    {
        public bool BypassCooldown { get; set; }

        public bool IncludeSkipped { get; set; }
    }

    public sealed class PersistlyListSlotDataOptions
    {
        public bool IncludeArchived { get; set; }
    }

    public sealed class PersistlyAccountSessionInfo
    {
        public PersistlyAccountSessionInfo(string? accountId, string? accountSessionToken)
        {
            AccountId = accountId;
            AccountSessionToken = accountSessionToken;
        }

        public string? AccountId { get; }

        public string? AccountSessionToken { get; }
    }

    public sealed class PersistlyAccountInspection
    {
        public PersistlyAccountInspection(string accountDataJson, string slotInfoJson, bool dirty, int? version)
        {
            AccountDataJson = accountDataJson;
            SlotInfoJson = slotInfoJson;
            Dirty = dirty;
            Version = version;
        }

        public string AccountDataJson { get; }

        public string SlotInfoJson { get; }

        public bool Dirty { get; }

        public int? Version { get; }
    }

    public sealed class PersistlySlotInspection
    {
        public PersistlySlotInspection(
            string slotId,
            bool exists,
            string? stateJson,
            string? slotInfoJson,
            bool dirty,
            int? version,
            string? cloudStateJson,
            string? cloudSlotInfoJson,
            int? cloudVersion,
            bool archived,
            DateTimeOffset? updatedAt,
            DateTimeOffset? lastRemoteSyncAt)
        {
            SlotId = slotId;
            Exists = exists;
            StateJson = stateJson;
            SlotInfoJson = slotInfoJson;
            Dirty = dirty;
            Version = version;
            CloudStateJson = cloudStateJson;
            CloudSlotInfoJson = cloudSlotInfoJson;
            CloudVersion = cloudVersion;
            Archived = archived;
            UpdatedAt = updatedAt;
            LastRemoteSyncAt = lastRemoteSyncAt;
        }

        public string SlotId { get; }

        public bool Exists { get; }

        public string? StateJson { get; }

        public string? SlotInfoJson { get; }

        public bool Dirty { get; }

        public int? Version { get; }

        public string? CloudStateJson { get; }

        public string? CloudSlotInfoJson { get; }

        public int? CloudVersion { get; }

        public bool Archived { get; }

        public DateTimeOffset? UpdatedAt { get; }

        public DateTimeOffset? LastRemoteSyncAt { get; }
    }

    public sealed class PersistlySyncNotification
    {
        public PersistlySyncNotification(PersistlyGameSaveTarget target, PersistlyGameSaveStatus status, string? slotId = null, PersistlyGameSaveConflict? conflict = null)
        {
            Target = target;
            Status = status;
            SlotId = slotId;
            Conflict = conflict;
        }

        public PersistlyGameSaveTarget Target { get; }

        public PersistlyGameSaveStatus Status { get; }

        public string? SlotId { get; }

        public PersistlyGameSaveConflict? Conflict { get; }
    }

    public interface IPersistlyGameSavesStore
    {
        string? LoadAccountJson(string localAccountKey);

        void SaveAccountJson(string localAccountKey, string json);

        void DeleteAccountJson(string localAccountKey);

        string? LoadSlotJson(string localAccountKey, string slotId);

        void SaveSlotJson(string localAccountKey, string slotId, string json);

        void DeleteSlotJson(string localAccountKey, string slotId);

        IReadOnlyList<string> ListSlotIds(string localAccountKey);
    }

    public sealed class InMemoryPersistlyGameSavesStore : IPersistlyGameSavesStore
    {
        private readonly Dictionary<string, string> _accounts = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, string>> _slots = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        public string? LoadAccountJson(string localAccountKey)
        {
            return _accounts.TryGetValue(localAccountKey, out var value) ? value : null;
        }

        public void SaveAccountJson(string localAccountKey, string json)
        {
            _accounts[localAccountKey] = json;
        }

        public void DeleteAccountJson(string localAccountKey)
        {
            _accounts.Remove(localAccountKey);
        }

        public string? LoadSlotJson(string localAccountKey, string slotId)
        {
            return _slots.TryGetValue(localAccountKey, out var accountSlots) && accountSlots.TryGetValue(slotId, out var value) ? value : null;
        }

        public void SaveSlotJson(string localAccountKey, string slotId, string json)
        {
            if (!_slots.TryGetValue(localAccountKey, out var accountSlots))
            {
                accountSlots = new Dictionary<string, string>(StringComparer.Ordinal);
                _slots[localAccountKey] = accountSlots;
            }

            accountSlots[slotId] = json;
        }

        public void DeleteSlotJson(string localAccountKey, string slotId)
        {
            if (_slots.TryGetValue(localAccountKey, out var accountSlots))
            {
                accountSlots.Remove(slotId);
            }
        }

        public IReadOnlyList<string> ListSlotIds(string localAccountKey)
        {
            if (!_slots.TryGetValue(localAccountKey, out var accountSlots))
            {
                return Array.Empty<string>();
            }

            return new List<string>(accountSlots.Keys);
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

        public string? LoadAccountJson(string localAccountKey)
        {
            var path = AccountPath(localAccountKey);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public void SaveAccountJson(string localAccountKey, string json)
        {
            Directory.CreateDirectory(AccountDirectory(localAccountKey));
            File.WriteAllText(AccountPath(localAccountKey), json);
        }

        public void DeleteAccountJson(string localAccountKey)
        {
            var path = AccountPath(localAccountKey);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public string? LoadSlotJson(string localAccountKey, string slotId)
        {
            var path = SlotPath(localAccountKey, slotId);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public void SaveSlotJson(string localAccountKey, string slotId, string json)
        {
            Directory.CreateDirectory(SlotsDirectory(localAccountKey));
            File.WriteAllText(SlotPath(localAccountKey, slotId), json);
        }

        public void DeleteSlotJson(string localAccountKey, string slotId)
        {
            var path = SlotPath(localAccountKey, slotId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public IReadOnlyList<string> ListSlotIds(string localAccountKey)
        {
            var directory = SlotsDirectory(localAccountKey);
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

        private string AccountDirectory(string localAccountKey)
        {
            return Path.Combine(_rootDirectory, SafePath(localAccountKey));
        }

        private string SlotsDirectory(string localAccountKey)
        {
            return Path.Combine(AccountDirectory(localAccountKey), "slots");
        }

        private string AccountPath(string localAccountKey)
        {
            return Path.Combine(AccountDirectory(localAccountKey), "account.json");
        }

        private string SlotPath(string localAccountKey, string slotId)
        {
            return Path.Combine(SlotsDirectory(localAccountKey), SafePath(slotId) + ".json");
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
        public const string DefaultSlotId = "autosave";

        private const string AccountSchema = "persistly.local.account.v1";
        private const string SlotSchema = "persistly.local.slot.v1";
        private const string AnonymousNamespaceSchema = "persistly.local.anonymous.v1";
        private const string AnonymousNamespaceRecordKey = "__persistly_anonymous_namespace__";
        private static readonly PersistlySyncPolicy DefaultSyncPolicy = new PersistlySyncPolicy(60, 10, true, true, true, 25);
        private static PersistlyGameSaves? _shared;

        private readonly PersistlyClient _client;
        private readonly IPersistlyGameSavesStore _store;
        private readonly string _localAccountKey;
        private readonly Dictionary<string, LocalSlotRecord> _slots = new Dictionary<string, LocalSlotRecord>(StringComparer.Ordinal);
        private readonly object _gate = new object();
        private LocalAccountRecord _account;

        private PersistlyGameSaves(PersistlyGameSavesSettings settings, PersistlyClient client, IPersistlyGameSavesStore store, string localAccountKey, LocalAccountRecord account)
        {
            Settings = settings;
            _client = client;
            _store = store;
            _localAccountKey = localAccountKey;
            _account = account;
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
            var localAccountKey = ResolveLocalAccountKey(settings, store);
            var account = LoadAccount(store, localAccountKey, settings);
            var client = new PersistlyClient(new PersistlyClientOptions(settings.BaseUrl, settings.RuntimeKey.Trim())
            {
                Transport = settings.Transport,
                UserAgent = "Persistly Unity SDK/1.0.0"
            });

            _shared = new PersistlyGameSaves(settings, client, store, localAccountKey, account);
            _shared.SaveAccount();
            return Task.CompletedTask;
        }

        public PersistlyAccountSessionInfo GetAccountSession(bool includeToken = false)
        {
            lock (_gate)
            {
                return new PersistlyAccountSessionInfo(_account.AccountId, includeToken ? _account.AccountSessionToken : null);
            }
        }

        public PersistlyAccountInspection InspectAccount()
        {
            lock (_gate)
            {
                return new PersistlyAccountInspection(_account.AccountDataJson, _account.SlotInfoJson, _account.Dirty, _account.Version);
            }
        }

        public string GetAccountDataJson()
        {
            lock (_gate)
            {
                return _account.AccountDataJson;
            }
        }

        public async Task<PersistlyGameSaveResult> CreateAccountAsync(CancellationToken cancellationToken = default)
        {
            await AssertNoExistingLocalAccountStateAsync("create_account_local_state_exists: Call ClearLocalAccountAsync before creating a different account.");
            return await EnsureAccountAsync(cancellationToken);
        }

        public async Task<PersistlyGameSaveResult> AttachAccountAsync(string accountId, string accountSessionToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(accountSessionToken))
            {
                throw new PersistlyConfigurationError("attach_account_invalid_input: AttachAccountAsync requires non-empty accountId and accountSessionToken.");
            }

            await AssertNoExistingLocalAccountStateAsync("attach_account_local_state_exists: Call ClearLocalAccountAsync before attaching a different account.");
            var normalizedAccountId = accountId.Trim();
            var normalizedAccountSessionToken = accountSessionToken.Trim();
            var envelope = await _client.LoadAccountAsync(normalizedAccountId, normalizedAccountSessionToken, cancellationToken);
            lock (_gate)
            {
                _account.AccountId = envelope.AccountId;
                _account.AccountSessionToken = string.IsNullOrWhiteSpace(envelope.AccountSessionToken) ? normalizedAccountSessionToken : envelope.AccountSessionToken;
                if (envelope.SyncPolicy != null)
                {
                    _account.SyncPolicy = envelope.SyncPolicy;
                }
                ApplyAccountSave(envelope.Account, false);
                _account.LastRemoteSyncAt = DateTimeOffset.UtcNow;
                SaveAccount();
            }
            return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.Synced);
        }

        public Task<PersistlyCreateTransferCodeResponse> CreateTransferCodeAsync(
            string? deviceLabel = null,
            int? ttlSeconds = null,
            CancellationToken cancellationToken = default)
        {
            if (!HasAccountSession())
            {
                throw new PersistlyConfigurationError("create_transfer_code_missing_account_session: CreateTransferCodeAsync requires a stored accountId and accountSessionToken.");
            }

            return _client.CreateTransferCodeAsync(_account.AccountId!, _account.AccountSessionToken!, deviceLabel, ttlSeconds, cancellationToken);
        }

        public async Task<PersistlyGameSaveResult> AttachWithTransferCodeAsync(
            string transferCode,
            string? deviceLabel = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(transferCode))
            {
                throw new PersistlyConfigurationError("attach_transfer_code_invalid_input: AttachWithTransferCodeAsync requires a non-empty transferCode.");
            }

            await AssertNoExistingLocalAccountStateAsync("attach_transfer_code_local_state_exists: Call ClearLocalAccountAsync before attaching a different account.");
            var consumed = await _client.ConsumeTransferCodeAsync(transferCode.Trim(), deviceLabel, cancellationToken);
            ApplyAccountResponse(consumed, false);
            lock (_gate)
            {
                _account.LastRemoteSyncAt = DateTimeOffset.UtcNow;
                SaveAccount();
            }

            return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.Synced);
        }

        public async Task<PersistlyGameSaveResult> EnsureAccountAsync(CancellationToken cancellationToken = default)
        {
            if (HasAccountSession())
            {
                if (!_account.Version.HasValue)
                {
                    await RestoreAccountAsync(cancellationToken);
                    return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.Synced);
                }

                return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.LocalFound);
            }

            var created = await _client.CreateAccountAsync(new PersistlyCreateAccountRequest(
                _account.AccountDataJson,
                playerRef: Settings.PlayerRef,
                externalAccountRefJson: Settings.ExternalAccountRefJson), cancellationToken);
            ApplyAccountResponse(created, false);
            return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.Synced);
        }

        public Task<PersistlyGameSaveResult> SaveAccountDataAsync<TState>(TState accountData)
        {
            var json = PersistlyJson.CanonicalizeObjectJson(JsonUtility.ToJson(accountData), "accountData");
            lock (_gate)
            {
                _account.AccountDataJson = json;
                _account.PendingAccountDataPatchJson = null;
                _account.Dirty = true;
                _account.UpdatedAt = DateTimeOffset.UtcNow;
                SaveAccount();
            }

            return Task.FromResult(new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.LocalSaved));
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
                var account = PersistlyJson.ParseJsonValue(_account.AccountDataJson, "accountData") as Dictionary<string, object?> ?? new Dictionary<string, object?>(StringComparer.Ordinal);
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

                _account.AccountDataJson = PersistlyJson.Serialize(account);
                _account.PendingAccountDataPatchJson = PersistlyJson.Serialize(patch);
                _account.Dirty = true;
                _account.UpdatedAt = DateTimeOffset.UtcNow;
                SaveAccount();
            }

            return Task.FromResult(new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.LocalSaved));
        }

        public async Task<PersistlyGameSaveResult> ForceSyncAccountAsync(PersistlySyncOptions? options = null, CancellationToken cancellationToken = default)
        {
            options = options ?? new PersistlySyncOptions();
            if (!_account.Dirty)
            {
                return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.NoChanges);
            }

            if (!options.BypassCooldown && IsInCooldown(_account.LastForceSyncAt, _account.SyncPolicy.ForceSyncCooldownSeconds))
            {
                return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.Cooldown);
            }

            if (HasAccountSession() && !_account.Version.HasValue)
            {
                await RestoreAccountAsync(cancellationToken, preserveLocalDirty: true);
            }
            else
            {
                await EnsureAccountAsync(cancellationToken);
            }
            var request = new PersistlySyncAccountDataRequest(
                _account.Version ?? 1,
                accountDataJson: _account.PendingAccountDataPatchJson == null ? _account.AccountDataJson : null,
                accountDataPatchJson: _account.PendingAccountDataPatchJson);

            try
            {
                var response = await _client.SyncAccountDataAsync(_account.AccountId!, _account.AccountSessionToken!, request, cancellationToken);
                if (response.Status == PersistlySyncStatus.Conflict)
                {
                    var cloudAccountDataJson = ExtractAccountData(response.Save.StateJson);
                    var conflict = BuildAccountConflict(response.Save, cloudAccountDataJson);
                    _account.CloudAccountDataJson = cloudAccountDataJson;
                    _account.CloudVersion = response.Save.Version;
                    _account.LastForceSyncAt = DateTimeOffset.UtcNow;
                    SaveAccount();
                    Notify(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.Conflict, conflict: conflict);
                    return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.Conflict, conflict);
                }

                ApplyAccountSave(response.Save, false);
                _account.LastForceSyncAt = DateTimeOffset.UtcNow;
                _account.LastRemoteSyncAt = _account.LastForceSyncAt;
                SaveAccount();
                Notify(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.Synced);
                return new PersistlyGameSaveResult(
                    PersistlyGameSaveTarget.Account,
                    PersistlyGameSaveStatus.Synced,
                    historyRetained: response.HistoryRetained,
                    warnings: response.Warnings);
            }
            catch (PersistlyRateLimitedError)
            {
                return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.RateLimited);
            }
            catch (PersistlyTransportError)
            {
                return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.Offline);
            }
        }

        public Task<PersistlyGameSaveResult> SyncDueAccountAsync(PersistlySyncOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (!_account.Dirty)
            {
                return Task.FromResult(new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.NoChanges));
            }

            if (IsInCooldown(_account.LastRemoteSyncAt, _account.SyncPolicy.MinRemoteSyncIntervalSeconds))
            {
                return Task.FromResult(new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.Cooldown));
            }

            return ForceSyncAccountAsync(new PersistlySyncOptions { BypassCooldown = true }, cancellationToken);
        }

        public Task<PersistlySlotResult> SaveDataAsync<TState>(TState state, PersistlySaveSlotOptions? options = null)
        {
            return SaveSlotAsync(DefaultSlotId, state, options);
        }

        public Task<PersistlySlotResult<TState>> LoadDataAsync<TState>() where TState : class
        {
            return LoadSlotAsync<TState>(DefaultSlotId);
        }

        public PersistlySlotInspection InspectData()
        {
            return InspectSlot(DefaultSlotId);
        }

        public Task<PersistlySlotResult> RefreshDataAsync(CancellationToken cancellationToken = default)
        {
            return RefreshSlotAsync(DefaultSlotId, cancellationToken);
        }

        public Task<PersistlySlotResult> ForceSyncDataAsync(PersistlySyncOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ForceSyncAsync(DefaultSlotId, options, cancellationToken);
        }

        public Task<PersistlySlotResult> SaveSlotAsync<TState>(string slotId, TState state, PersistlySaveSlotOptions? options = null)
        {
            var normalizedSlotId = PersistlySlotId.Normalize(slotId);
            var json = PersistlyJson.CanonicalizeObjectJson(JsonUtility.ToJson(state), "state");
            var slotInfo = PersistlyJson.CanonicalizeObjectJson(options?.SlotInfoJson ?? "{}", "slotInfo");
            PersistlyJson.ValidatePayloadSizes(slotInfo, json);

            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotId, out var slot))
                {
                    slot = new LocalSlotRecord(normalizedSlotId);
                    _slots[normalizedSlotId] = slot;
                }

                if (slot.Archived)
                {
                    slot.Version = null;
                    slot.CloudStateJson = null;
                    slot.CloudSlotInfoJson = null;
                    slot.CloudVersion = null;
                    slot.RemoteSlotKnown = false;
                    slot.LastForceSyncAt = null;
                    slot.LastRemoteSyncAt = null;
                }

                slot.StateJson = json;
                slot.SlotInfoJson = slotInfo;
                slot.Dirty = true;
                slot.Archived = false;
                slot.UpdatedAt = DateTimeOffset.UtcNow;
                SaveSlot(slot);
            }

            return Task.FromResult(new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.LocalSaved));
        }

        public Task<PersistlySlotResult<TState>> LoadSlotAsync<TState>(string slotId) where TState : class
        {
            var normalizedSlotId = PersistlySlotId.Normalize(slotId);
            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotId, out var slot) || slot.Archived)
                {
                    return Task.FromResult(new PersistlySlotResult<TState>(normalizedSlotId, PersistlySlotStatus.NotFound, null, false));
                }

                var state = JsonUtility.FromJson<TState>(slot.StateJson);
                return Task.FromResult(new PersistlySlotResult<TState>(normalizedSlotId, PersistlySlotStatus.LocalFound, state, true, ToInspection(slot)));
            }
        }

        public IReadOnlyList<PersistlySlotInspection> ListSlotDataAsync(PersistlyListSlotDataOptions? options = null)
        {
            options = options ?? new PersistlyListSlotDataOptions();
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

        public PersistlySlotInspection InspectSlot(string slotId)
        {
            var normalizedSlotId = PersistlySlotId.Normalize(slotId);
            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotId, out var slot))
                {
                    return new PersistlySlotInspection(normalizedSlotId, false, null, null, false, null, null, null, null, false, null, null);
                }

                return ToInspection(slot);
            }
        }

        public async Task<PersistlySlotResult> RefreshSlotAsync(string slotId, CancellationToken cancellationToken = default)
        {
            var normalizedSlotId = PersistlySlotId.Normalize(slotId);
            if (!HasAccountSession())
            {
                throw new PersistlyConfigurationError("refresh_slot_missing_account_session: RefreshSlotAsync requires a stored accountId and accountSessionToken.");
            }

            try
            {
                await RestoreAccountAsync(cancellationToken, preserveLocalDirty: true);

                LocalSlotRecord slot;
                string expectedSlotId;
                lock (_gate)
                {
                    if (!_slots.TryGetValue(normalizedSlotId, out slot) || slot.Archived)
                    {
                        return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.NotFound);
                    }
                    expectedSlotId = slot.SlotId!;
                }

                var remoteSave = await _client.LoadAccountSlotAsync(_account.AccountId!, _account.AccountSessionToken!, expectedSlotId, cancellationToken);
                lock (_gate)
                {
                    if (!_slots.TryGetValue(normalizedSlotId, out slot) || slot.Archived || !string.Equals(slot.SlotId, expectedSlotId, StringComparison.Ordinal))
                    {
                        return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.NotFound);
                    }

                    if (slot.Dirty)
                    {
                        var conflict = BuildSlotConflict(slot, remoteSave);
                        slot.CloudStateJson = remoteSave.StateJson;
                        slot.CloudSlotInfoJson = remoteSave.SlotInfoJson;
                        slot.CloudVersion = remoteSave.Version;
                        slot.LastRemoteSyncAt = DateTimeOffset.UtcNow;
                        slot.UpdatedAt = slot.UpdatedAt ?? remoteSave.UpdatedAt;
                        SaveSlot(slot);
                        Notify(PersistlyGameSaveTarget.Slot, PersistlyGameSaveStatus.Conflict, normalizedSlotId, conflict);
                        return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.Conflict, conflict);
                    }

                    ApplySyncedSlot(slot, remoteSave);
                    SaveSlot(slot);
                    Notify(PersistlyGameSaveTarget.Slot, PersistlyGameSaveStatus.Synced, normalizedSlotId);
                    return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.Synced);
                }
            }
            catch (PersistlyRateLimitedError)
            {
                return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.RateLimited);
            }
            catch (PersistlyNotFoundError)
            {
                return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.NotFound);
            }
            catch (PersistlyTransportError)
            {
                return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.Offline);
            }
        }

        public async Task<PersistlySlotResult> ForceSyncAsync(string slotId, PersistlySyncOptions? options = null, CancellationToken cancellationToken = default)
        {
            options = options ?? new PersistlySyncOptions();
            var normalizedSlotId = PersistlySlotId.Normalize(slotId);
            LocalSlotRecord slot;
            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotId, out slot))
                {
                    return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.NotFound);
                }

                if (!slot.Dirty)
                {
                    return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.NoChanges);
                }

                if (!options.BypassCooldown && IsInCooldown(slot.LastForceSyncAt, _account.SyncPolicy.ForceSyncCooldownSeconds))
                {
                    return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.Cooldown);
                }
            }

            try
            {
                if (!HasAccountSession())
                {
                    var created = await _client.CreateAccountAsync(new PersistlyCreateAccountRequest(
                        _account.AccountDataJson,
                        playerRef: Settings.PlayerRef,
                        externalAccountRefJson: Settings.ExternalAccountRefJson,
                        slot: new PersistlyCreateAccountInitialSlotRequest(slot.SlotId, slot.SlotInfoJson, slot.StateJson)), cancellationToken);
                    ApplyAccountResponse(created, false);
                    ApplySyncedSlot(slot, created.Slot!);
                    SaveSlot(slot);
                    Notify(PersistlyGameSaveTarget.Slot, PersistlyGameSaveStatus.Synced, normalizedSlotId);
                    return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.Synced);
                }

                await EnsureAccountAsync(cancellationToken);
                if (!slot.Version.HasValue && slot.RemoteSlotKnown)
                {
                    try
                    {
                        await ReconcileExistingRemoteSlotAsync(normalizedSlotId, cancellationToken, restoreAccount: false);
                    }
                    catch (PersistlyNotFoundError)
                    {
                    }
                }

                if (!slot.Version.HasValue)
                {
                    try
                    {
                        var created = await _client.CreateAccountSlotAsync(
                            _account.AccountId!,
                            _account.AccountSessionToken!,
                            new PersistlyCreateAccountSlotRequest(slot.SlotId, slot.SlotInfoJson, slot.StateJson),
                            cancellationToken);
                        ApplyAccountResponse(created, false);
                        ApplySyncedSlot(slot, created.Slot!);
                        SaveSlot(slot);
                        Notify(PersistlyGameSaveTarget.Slot, PersistlyGameSaveStatus.Synced, normalizedSlotId);
                        return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.Synced);
                    }
                    catch (PersistlySlotAlreadyExistsError)
                    {
                        await ReconcileExistingRemoteSlotAsync(normalizedSlotId, cancellationToken);
                    }
                }

                if (!slot.Version.HasValue)
                {
                    await ReconcileExistingRemoteSlotAsync(normalizedSlotId, cancellationToken, restoreAccount: false);
                }

                var response = await _client.SyncAccountSlotAsync(
                    _account.AccountId!,
                    _account.AccountSessionToken!,
                    slot.SlotId,
                    new PersistlySyncSaveRequest(slot.StateJson, slot.Version, BuildRemoteSlotSlotInfoJson(slot)),
                    cancellationToken);
                slot.LastForceSyncAt = DateTimeOffset.UtcNow;
                if (response.Status == PersistlySyncStatus.Conflict)
                {
                    var conflict = BuildSlotConflict(slot, response.Save);
                    slot.CloudStateJson = response.Save.StateJson;
                    slot.CloudSlotInfoJson = response.Save.SlotInfoJson;
                    slot.CloudVersion = response.Save.Version;
                    slot.LastRemoteSyncAt = slot.LastForceSyncAt;
                    SaveSlot(slot);
                    Notify(PersistlyGameSaveTarget.Slot, PersistlyGameSaveStatus.Conflict, normalizedSlotId, conflict);
                    return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.Conflict, conflict);
                }

                ApplySyncedSlot(slot, response.Save);
                SaveSlot(slot);
                Notify(PersistlyGameSaveTarget.Slot, PersistlyGameSaveStatus.Synced, normalizedSlotId);
                return new PersistlySlotResult(
                    normalizedSlotId,
                    PersistlySlotStatus.Synced,
                    historyRetained: response.HistoryRetained,
                    warnings: response.Warnings);
            }
            catch (PersistlyRateLimitedError)
            {
                return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.RateLimited);
            }
            catch (PersistlyTransportError)
            {
                return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.Offline);
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

                if (IsInCooldown(inspect.LastRemoteSyncAt, _account.SyncPolicy.MinRemoteSyncIntervalSeconds))
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
            var account = await SyncDueAccountAsync(options, cancellationToken);
            if (account.Status != PersistlyGameSaveStatus.NoChanges || (options != null && options.IncludeSkipped))
            {
                results.Add(account);
            }

            foreach (var result in await SyncDueSlotsAsync(options, cancellationToken))
            {
                results.Add(result);
            }

            return results;
        }

        public async Task<PersistlySlotResult> ArchiveSlotAsync(string slotId, CancellationToken cancellationToken = default)
        {
            var normalizedSlotId = PersistlySlotId.Normalize(slotId);
            LocalSlotRecord slot;
            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotId, out slot))
                {
                    return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.NotFound);
                }
            }

            if (!slot.Version.HasValue)
            {
                throw new PersistlyConfigurationError("archive_slot_unsynced: archiveSlot requires a slot that has already synced to a remote slot.");
            }

            if (!HasAccountSession())
            {
                throw new PersistlyConfigurationError("archive_slot_missing_account_session: archiveSlot requires a stored accountId and accountSessionToken.");
            }

            var response = await _client.ArchiveSlotAsync(_account.AccountId!, _account.AccountSessionToken!, slot.SlotId, cancellationToken);
            ApplyAccountResponse(response, false);
            slot.Archived = true;
            slot.RemoteSlotKnown = false;
            slot.Dirty = false;
            slot.UpdatedAt = DateTimeOffset.UtcNow;
            SaveSlot(slot);
            return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.Synced);
        }

        public async Task<PersistlyGameSaveResult> DeleteAccountAsync(CancellationToken cancellationToken = default)
        {
            if (!HasAccountSession())
            {
                ResetLocalAccountState();
                return new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.LocalSaved);
            }

            var response = await _client.DeleteAccountAsync(_account.AccountId!, _account.AccountSessionToken!, cancellationToken);
            ResetLocalAccountState();
            return new PersistlyGameSaveResult(
                PersistlyGameSaveTarget.Account,
                PersistlyGameSaveStatus.Synced,
                warnings: response.CleanupQueued ? new[] { "delete_cleanup_queued" } : null);
        }

        public async Task<PersistlySlotResult> DeleteSlotAsync(string slotId, CancellationToken cancellationToken = default)
        {
            var normalizedSlotId = PersistlySlotId.Normalize(slotId);
            LocalSlotRecord? slot;
            lock (_gate)
            {
                _slots.TryGetValue(normalizedSlotId, out slot);
            }

            if (slot == null)
            {
                return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.NotFound);
            }

            if (!slot.Version.HasValue)
            {
                DeleteLocalSlot(normalizedSlotId);
                return new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.LocalSaved);
            }

            if (!HasAccountSession())
            {
                throw new PersistlyConfigurationError("delete_slot_missing_account_session: deleteSlot requires a stored accountId and accountSessionToken.");
            }

            var response = await _client.DeleteAccountSlotAsync(_account.AccountId!, _account.AccountSessionToken!, slot.SlotId!, cancellationToken);
            lock (_gate)
            {
                _slots.Remove(normalizedSlotId);
                _store.DeleteSlotJson(_localAccountKey, normalizedSlotId);
                if (response.Account != null)
                {
                    ApplyAccountSave(response.Account, false);
                    SaveAccount();
                }
            }

            return new PersistlySlotResult(
                normalizedSlotId,
                PersistlySlotStatus.Synced,
                warnings: response.CleanupQueued ? new[] { "delete_cleanup_queued" } : null);
        }

        public Task<PersistlyGameSaveResult> ClearLocalAccountAsync()
        {
            ResetLocalAccountState();
            return Task.FromResult(new PersistlyGameSaveResult(PersistlyGameSaveTarget.Account, PersistlyGameSaveStatus.LocalSaved));
        }

        public Task<PersistlySlotResult> ClearLocalSlotAsync(string slotId)
        {
            var normalizedSlotId = PersistlySlotId.Normalize(slotId);
            DeleteLocalSlot(normalizedSlotId);
            return Task.FromResult(new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.LocalSaved));
        }

        public Task<PersistlySlotResult> AcceptCloudDataAsync()
        {
            return AcceptCloudVersionAsync(DefaultSlotId);
        }

        public Task<PersistlySlotResult> KeepLocalDataForLaterAsync()
        {
            return KeepLocalForLaterAsync(DefaultSlotId);
        }

        public Task<PersistlySlotResult> OverwriteCloudDataAsync(PersistlySyncOptions? options = null, CancellationToken cancellationToken = default)
        {
            return OverwriteCloudVersionAsync(DefaultSlotId, options, cancellationToken);
        }

        public Task<PersistlySlotResult> AcceptCloudVersionAsync(string slotId)
        {
            var normalizedSlotId = PersistlySlotId.Normalize(slotId);
            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotId, out var slot) || slot.CloudStateJson == null)
                {
                    return Task.FromResult(new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.NotFound));
                }

                slot.StateJson = slot.CloudStateJson;
                slot.SlotInfoJson = slot.CloudSlotInfoJson == null ? slot.SlotInfoJson : NormalizeSlotInfoJson(slot.CloudSlotInfoJson);
                slot.Version = slot.CloudVersion;
                slot.Dirty = false;
                SaveSlot(slot);
                return Task.FromResult(new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.Synced));
            }
        }

        public Task<PersistlySlotResult> KeepLocalForLaterAsync(string slotId)
        {
            var normalizedSlotId = PersistlySlotId.Normalize(slotId);
            lock (_gate)
            {
                if (_slots.TryGetValue(normalizedSlotId, out var slot))
                {
                    slot.Dirty = true;
                    SaveSlot(slot);
                }
            }

            return Task.FromResult(new PersistlySlotResult(normalizedSlotId, PersistlySlotStatus.LocalSaved));
        }

        public Task<PersistlySlotResult> OverwriteCloudVersionAsync(string slotId, PersistlySyncOptions? options = null, CancellationToken cancellationToken = default)
        {
            var normalizedSlotId = PersistlySlotId.Normalize(slotId);
            lock (_gate)
            {
                if (_slots.TryGetValue(normalizedSlotId, out var slot) && slot.CloudVersion.HasValue)
                {
                    slot.Version = slot.CloudVersion;
                    slot.Dirty = true;
                    SaveSlot(slot);
                }
            }

            options = options ?? new PersistlySyncOptions { BypassCooldown = true };
            options.BypassCooldown = true;
            return ForceSyncAsync(normalizedSlotId, options, cancellationToken);
        }

#if UNITY_INCLUDE_TESTS
        public void AttachSlotForTests(string slotId, int version)
        {
            var normalizedSlotId = PersistlySlotId.Normalize(slotId);
            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotId, out var slot))
                {
                    throw new PersistlyConfigurationError("slot_not_found: no local save exists for slotId.");
                }

                slot.Version = version;
                SaveSlot(slot);
            }
        }
#endif

        private void Notify(PersistlyGameSaveTarget target, PersistlyGameSaveStatus status, string? slotId = null, PersistlyGameSaveConflict? conflict = null)
        {
            Settings.OnSyncResult?.Invoke(new PersistlySyncNotification(target, status, slotId, conflict));
        }

        private void ApplyAccountResponse(PersistlyCreateAccountResponse response, bool dirty)
        {
            lock (_gate)
            {
                _account.AccountId = response.AccountId;
                if (!string.IsNullOrWhiteSpace(response.AccountSessionToken))
                {
                    _account.AccountSessionToken = response.AccountSessionToken;
                }

                _account.SyncPolicy = response.SyncPolicy;
                ApplyAccountSave(response.Account, dirty);
                SaveAccount();
            }
        }

        private async Task RestoreAccountAsync(CancellationToken cancellationToken, bool preserveLocalDirty = false)
        {
            var localAccountDataJson = _account.AccountDataJson;
            var localSlotInfoJson = _account.SlotInfoJson;
            var localPendingAccountDataPatchJson = _account.PendingAccountDataPatchJson;
            var localDirty = _account.Dirty;
            var envelope = await _client.LoadAccountAsync(_account.AccountId!, _account.AccountSessionToken!, cancellationToken);
            lock (_gate)
            {
                _account.AccountId = envelope.AccountId;
                if (!string.IsNullOrWhiteSpace(envelope.AccountSessionToken))
                {
                    _account.AccountSessionToken = envelope.AccountSessionToken;
                }

                if (envelope.SyncPolicy != null)
                {
                    _account.SyncPolicy = envelope.SyncPolicy;
                }

                ApplyAccountSave(envelope.Account, false);
                if (preserveLocalDirty && localDirty)
                {
                    _account.AccountDataJson = localAccountDataJson;
                    _account.SlotInfoJson = localSlotInfoJson;
                    _account.PendingAccountDataPatchJson = localPendingAccountDataPatchJson;
                    _account.Dirty = true;
                }
                _account.LastRemoteSyncAt = DateTimeOffset.UtcNow;
                SaveAccount();
            }
        }

        private void ApplyAccountSave(PersistlySave save, bool dirty)
        {
            var accountState = PersistlyAccountState.Parse(save.StateJson);
            _account.AccountId = save.SaveId;
            _account.Version = save.Version;
            _account.AccountDataJson = accountState.AccountDataJson;
            _account.SlotInfoJson = save.SlotInfoJson;
            _account.PendingAccountDataPatchJson = null;
            _account.Dirty = dirty;
            _account.UpdatedAt = save.UpdatedAt;
            ApplySlotRefs(accountState.Slots);
        }

        private static string ExtractAccountData(string accountStateJson)
        {
            return PersistlyAccountState.Parse(accountStateJson).AccountDataJson;
        }

        private async Task ReconcileExistingRemoteSlotAsync(string slotId, CancellationToken cancellationToken, bool restoreAccount = true)
        {
            if (restoreAccount)
            {
                await RestoreAccountAsync(cancellationToken, preserveLocalDirty: true);
            }

            LocalSlotRecord slot;
            lock (_gate)
            {
                if (!_slots.TryGetValue(slotId, out slot))
                {
                    throw new PersistlyConfigurationError("slot_reconcile_failed: Persistly could not find remote slot " + slotId + " after duplicate slot response.");
                }
            }

            var remoteSave = await _client.LoadAccountSlotAsync(_account.AccountId!, _account.AccountSessionToken!, slot.SlotId!, cancellationToken);
            lock (_gate)
            {
                ApplyRemoteSlotSnapshot(slot, remoteSave);
                SaveSlot(slot);
            }
        }

        private async Task AssertNoExistingLocalAccountStateAsync(string message)
        {
            bool hasLocalAccountState;
            lock (_gate)
            {
                hasLocalAccountState = !IsBlankLocalAccountState(_account) || _slots.Count > 0;
            }

            if (hasLocalAccountState)
            {
                throw new PersistlyConfigurationError(message);
            }

            await Task.CompletedTask;
        }

        private static bool IsBlankLocalAccountState(LocalAccountRecord account)
        {
            return string.IsNullOrWhiteSpace(account.AccountId)
                && string.IsNullOrWhiteSpace(account.AccountSessionToken)
                && string.Equals(account.AccountDataJson, "{}", StringComparison.Ordinal)
                && string.Equals(account.SlotInfoJson, "{}", StringComparison.Ordinal)
                && !account.Dirty
                && !account.Version.HasValue
                && account.CloudAccountDataJson == null
                && !account.CloudVersion.HasValue
                && !account.UpdatedAt.HasValue
                && !account.LastForceSyncAt.HasValue
                && !account.LastRemoteSyncAt.HasValue;
        }

        private void ResetLocalAccountState()
        {
            lock (_gate)
            {
                foreach (var slotId in new List<string>(_slots.Keys))
                {
                    _store.DeleteSlotJson(_localAccountKey, slotId);
                }

                _slots.Clear();
                _store.DeleteAccountJson(_localAccountKey);
                _account = CreateClearedAccount(Settings);
            }
        }

        private void DeleteLocalSlot(string normalizedSlotId)
        {
            lock (_gate)
            {
                _slots.Remove(normalizedSlotId);
                _store.DeleteSlotJson(_localAccountKey, normalizedSlotId);
            }
        }

        private void ApplySlotRefs(IReadOnlyList<PersistlySlotRef> slotRefs)
        {
            foreach (var slotRef in slotRefs)
            {
                var slotId = PersistlySlotId.Normalize(slotRef.SlotId);
                if (!_slots.TryGetValue(slotId, out var slot))
                {
                    slot = new LocalSlotRecord(slotId);
                    _slots[slotId] = slot;
                }

                slot.SlotId = slotRef.SlotId;
                if (string.Equals(slot.SlotInfoJson, "{}", StringComparison.Ordinal))
                {
                    slot.SlotInfoJson = NormalizeSlotInfoJson(slotRef.SlotInfoJson);
                }

                slot.Archived = slotRef.Archived;
                slot.RemoteSlotKnown = !slotRef.Archived;
                SaveSlot(slot);
            }
        }

        private static void ApplyRemoteSlotSnapshot(LocalSlotRecord slot, PersistlySave save)
        {
            slot.SlotId = save.SaveId;
            slot.Version = save.Version;
            slot.CloudStateJson = save.StateJson;
            slot.CloudSlotInfoJson = save.SlotInfoJson;
            slot.CloudVersion = save.Version;
            slot.RemoteSlotKnown = true;
            slot.LastRemoteSyncAt = DateTimeOffset.UtcNow;
            slot.UpdatedAt = save.UpdatedAt;
            if (string.Equals(slot.SlotInfoJson, "{}", StringComparison.Ordinal))
            {
                slot.SlotInfoJson = NormalizeSlotInfoJson(save.SlotInfoJson);
            }
        }

        private void ApplySyncedSlot(LocalSlotRecord slot, PersistlySave save)
        {
            slot.SlotId = save.SaveId;
            slot.StateJson = save.StateJson;
            slot.SlotInfoJson = NormalizeSlotInfoJson(save.SlotInfoJson);
            slot.Version = save.Version;
            slot.CloudStateJson = save.StateJson;
            slot.CloudSlotInfoJson = save.SlotInfoJson;
            slot.CloudVersion = save.Version;
            slot.RemoteSlotKnown = true;
            slot.Dirty = false;
            slot.LastForceSyncAt = DateTimeOffset.UtcNow;
            slot.LastRemoteSyncAt = slot.LastForceSyncAt;
            slot.UpdatedAt = save.UpdatedAt;
        }

        private static string NormalizeSlotInfoJson(string slotInfoJson)
        {
            var slotInfo = PersistlyJson.ParseJsonValue(slotInfoJson, "slotInfo") as Dictionary<string, object?>;
            if (slotInfo == null)
            {
                return "{}";
            }

            slotInfo.Remove("_persistly");
            return PersistlyJson.Serialize(slotInfo);
        }

        private static string BuildRemoteSlotSlotInfoJson(LocalSlotRecord slot)
        {
            return PersistlySlotId.BuildSlotInfoJson(slot.SlotId, slot.SlotInfoJson);
        }

        private PersistlyGameSaveConflict BuildSlotConflict(LocalSlotRecord slot, PersistlySave cloudSave)
        {
            return new PersistlyGameSaveConflict(
                PersistlyGameSaveTarget.Slot,
                slot.SlotId,
                slot.StateJson,
                slot.SlotInfoJson,
                slot.Version,
                slot.UpdatedAt,
                cloudSave.StateJson,
                cloudSave.SlotInfoJson,
                cloudSave.Version,
                cloudSave.UpdatedAt);
        }

        private PersistlyGameSaveConflict BuildAccountConflict(PersistlySave cloudSave, string cloudAccountDataJson)
        {
            return new PersistlyGameSaveConflict(
                PersistlyGameSaveTarget.Account,
                null,
                _account.AccountDataJson,
                _account.SlotInfoJson,
                _account.Version,
                _account.UpdatedAt,
                cloudAccountDataJson,
                cloudSave.SlotInfoJson,
                cloudSave.Version,
                cloudSave.UpdatedAt);
        }

        private bool HasAccountSession()
        {
            return !string.IsNullOrWhiteSpace(_account.AccountId) && !string.IsNullOrWhiteSpace(_account.AccountSessionToken);
        }

        private static bool IsInCooldown(DateTimeOffset? lastSync, int cooldownSeconds)
        {
            return lastSync.HasValue && cooldownSeconds > 0 && (DateTimeOffset.UtcNow - lastSync.Value).TotalSeconds < cooldownSeconds;
        }

        private PersistlySlotInspection ToInspection(LocalSlotRecord slot)
        {
            return new PersistlySlotInspection(
                slot.SlotId,
                true,
                slot.StateJson,
                slot.SlotInfoJson,
                slot.Dirty,
                slot.Version,
                slot.CloudStateJson,
                slot.CloudSlotInfoJson,
                slot.CloudVersion,
                slot.Archived,
                slot.UpdatedAt,
                slot.LastRemoteSyncAt);
        }

        private void LoadSlots()
        {
            foreach (var slotId in _store.ListSlotIds(_localAccountKey))
            {
                var json = _store.LoadSlotJson(_localAccountKey, slotId);
                if (string.IsNullOrWhiteSpace(json))
                {
                    continue;
                }

                var slot = LocalSlotRecord.FromJson(json!);
                _slots[slot.SlotId] = slot;
            }
        }

        private void SaveAccount()
        {
            _store.SaveAccountJson(_localAccountKey, _account.ToJson());
        }

        private void SaveSlot(LocalSlotRecord slot)
        {
            _store.SaveSlotJson(_localAccountKey, slot.SlotId, slot.ToJson());
        }

        private static LocalAccountRecord LoadAccount(IPersistlyGameSavesStore store, string localAccountKey, PersistlyGameSavesSettings settings)
        {
            var json = store.LoadAccountJson(localAccountKey);
            var account = string.IsNullOrWhiteSpace(json) ? CreateBlankAccount(settings) : LocalAccountRecord.FromJson(json!);
            ApplySettingsToAccount(account, settings);
            return account;
        }

        private static LocalAccountRecord CreateBlankAccount(PersistlyGameSavesSettings settings)
        {
            var account = new LocalAccountRecord();
            ApplySettingsToAccount(account, settings);
            return account;
        }

        private static LocalAccountRecord CreateClearedAccount(PersistlyGameSavesSettings settings)
        {
            var account = new LocalAccountRecord
            {
                PlayerRef = string.IsNullOrWhiteSpace(settings.PlayerRef) ? null : settings.PlayerRef!.Trim(),
                ExternalAccountRefJson = string.IsNullOrWhiteSpace(settings.ExternalAccountRefJson) ? null : PersistlyJson.CanonicalizeObjectJson(settings.ExternalAccountRefJson!, "externalAccountRef"),
                SyncPolicy = settings.SyncPolicy ?? DefaultSyncPolicy
            };
            return account;
        }

        private static void ApplySettingsToAccount(LocalAccountRecord account, PersistlyGameSavesSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.AccountId))
            {
                account.AccountId = settings.AccountId!.Trim();
            }

            if (!string.IsNullOrWhiteSpace(settings.AccountSessionToken))
            {
                account.AccountSessionToken = settings.AccountSessionToken!.Trim();
            }

            account.PlayerRef = string.IsNullOrWhiteSpace(settings.PlayerRef) ? account.PlayerRef : settings.PlayerRef!.Trim();
            account.ExternalAccountRefJson = string.IsNullOrWhiteSpace(settings.ExternalAccountRefJson) ? account.ExternalAccountRefJson : PersistlyJson.CanonicalizeObjectJson(settings.ExternalAccountRefJson!, "externalAccountRef");
            account.SyncPolicy = settings.SyncPolicy ?? account.SyncPolicy ?? DefaultSyncPolicy;
        }

        private static string ResolveLocalAccountKey(PersistlyGameSavesSettings settings, IPersistlyGameSavesStore store)
        {
            if (!string.IsNullOrWhiteSpace(settings.LocalAccountKey))
            {
                return settings.LocalAccountKey!.Trim();
            }

            if (!string.IsNullOrWhiteSpace(settings.ExternalAccountRefJson))
            {
                var external = PersistlyJson.ParseJsonValue(settings.ExternalAccountRefJson!, "externalAccountRef") as Dictionary<string, object?>;
                if (external != null && external.TryGetValue("provider", out var providerRaw) && providerRaw is string provider && external.TryGetValue("subject", out var subjectRaw) && subjectRaw is string subject)
                {
                    return provider + ":" + subject;
                }
            }

            if (!string.IsNullOrWhiteSpace(settings.PlayerRef))
            {
                return settings.PlayerRef!.Trim();
            }

            return ResolveAnonymousLocalAccountKey(store);
        }

        private static string ResolveAnonymousLocalAccountKey(IPersistlyGameSavesStore store)
        {
            var json = store.LoadAccountJson(AnonymousNamespaceRecordKey);
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

                var localAccountKey = ReadString(root, "localAccountKey");
                if (string.IsNullOrWhiteSpace(localAccountKey))
                {
                    throw new PersistlyConfigurationError("anonymous namespace is missing localAccountKey.");
                }

                return localAccountKey!;
            }

            var generated = "anonymous-" + Guid.NewGuid().ToString("N");
            store.SaveAccountJson(AnonymousNamespaceRecordKey, PersistlyJson.Serialize(new Dictionary<string, object?>
            {
                { "schema", AnonymousNamespaceSchema },
                { "localAccountKey", generated },
                { "createdAt", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture) }
            }));
            return generated;
        }

        private sealed class LocalAccountRecord
        {
            public string AccountDataJson = "{}";
            public string SlotInfoJson = "{}";
            public string? PendingAccountDataPatchJson;
            public string? AccountId;
            public string? AccountSessionToken;
            public string? PlayerRef;
            public string? ExternalAccountRefJson;
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
                    { "schema", AccountSchema },
                    { "accountData", PersistlyJson.ParseJsonValue(AccountDataJson, "accountData") },
                    { "slotInfo", PersistlyJson.ParseJsonValue(SlotInfoJson, "slotInfo") },
                    { "pendingAccountDataPatch", PendingAccountDataPatchJson == null ? null : PersistlyJson.ParseJsonValue(PendingAccountDataPatchJson, "accountDataPatch") },
                    { "accountId", AccountId },
                    { "accountSessionToken", AccountSessionToken },
                    { "playerRef", PlayerRef },
                    { "externalAccountRef", ExternalAccountRefJson == null ? null : PersistlyJson.ParseJsonValue(ExternalAccountRefJson, "externalAccountRef") },
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

            public static LocalAccountRecord FromJson(string json)
            {
                var root = PersistlyJson.ParseJsonValue(json, "local account") as Dictionary<string, object?>;
                if (root == null)
                {
                    throw new PersistlyConfigurationError("local account must be a JSON object.");
                }

                var schema = ReadString(root, "schema");
                if (!string.Equals(schema, AccountSchema, StringComparison.Ordinal))
                {
                    throw new PersistlyConfigurationError("Unknown Persistly local account schema: " + schema + ".");
                }

                var record = new LocalAccountRecord
                {
                    AccountDataJson = SerializeObject(root, "accountData", "{}"),
                    SlotInfoJson = SerializeObject(root, "slotInfo", "{}"),
                    PendingAccountDataPatchJson = SerializeNullableObject(root, "pendingAccountDataPatch"),
                    AccountId = ReadString(root, "accountId"),
                    AccountSessionToken = ReadString(root, "accountSessionToken"),
                    PlayerRef = ReadString(root, "playerRef"),
                    ExternalAccountRefJson = SerializeNullableObject(root, "externalAccountRef"),
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
            public LocalSlotRecord(string slotId)
            {
                SlotId = slotId;
            }

            public string SlotId;
            public string StateJson = "{}";
            public string SlotInfoJson = "{}";
            public bool Dirty;
            public bool Archived;
            public bool RemoteSlotKnown;
            public int? Version;
            public string? CloudStateJson;
            public string? CloudSlotInfoJson;
            public int? CloudVersion;
            public DateTimeOffset? UpdatedAt;
            public DateTimeOffset? LastForceSyncAt;
            public DateTimeOffset? LastRemoteSyncAt;

            public string ToJson()
            {
                var payload = new Dictionary<string, object?>
                {
                    { "schema", SlotSchema },
                    { "slotId", SlotId },
                    { "state", PersistlyJson.ParseJsonValue(StateJson, "state") },
                    { "slotInfo", PersistlyJson.ParseJsonValue(SlotInfoJson, "slotInfo") },
                    { "dirty", Dirty },
                    { "archived", Archived },
                    { "remoteSlotKnown", RemoteSlotKnown },
                    { "version", Version },
                    { "cloudState", CloudStateJson == null ? null : PersistlyJson.ParseJsonValue(CloudStateJson, "cloudState") },
                    { "cloudSlotInfo", CloudSlotInfoJson == null ? null : PersistlyJson.ParseJsonValue(CloudSlotInfoJson, "cloudSlotInfo") },
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

                return new LocalSlotRecord(PersistlySlotId.Normalize(ReadString(root, "slotId") ?? ""))
                {
                    StateJson = SerializeObject(root, "state", "{}"),
                    SlotInfoJson = SerializeObject(root, "slotInfo", "{}"),
                    Dirty = ReadBool(root, "dirty"),
                    Archived = ReadBool(root, "archived"),
                    RemoteSlotKnown = ReadBool(root, "remoteSlotKnown"),
                    Version = ReadInt(root, "version"),
                    CloudStateJson = SerializeNullableObject(root, "cloudState"),
                    CloudSlotInfoJson = SerializeNullableObject(root, "cloudSlotInfo"),
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
