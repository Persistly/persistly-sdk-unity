#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Persistly.Unity
{
    public sealed class PersistlyClient
    {
        private readonly Uri _baseUri;
        private readonly string _runtimeKey;
        private readonly IPersistlyTransport _transport;
        private readonly IPersistlySaveCache _cache;
        private readonly int _timeoutSeconds;
        private readonly string _userAgent;
        private readonly string _sdkName;
        private readonly string _sdkVersion;
        private readonly string _platform;
        private readonly string? _engineVersion;
        private readonly string? _clientVersion;

        public PersistlyClient(PersistlyClientOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                throw new PersistlyConfigurationError("PersistlyClientOptions.BaseUrl must be set.");
            }

            if (!Uri.TryCreate(options.BaseUrl.Trim(), UriKind.Absolute, out _baseUri))
            {
                throw new PersistlyConfigurationError("PersistlyClientOptions.BaseUrl must be an absolute URL.");
            }

            if (string.IsNullOrWhiteSpace(options.RuntimeKey))
            {
                throw new PersistlyConfigurationError("PersistlyClientOptions.RuntimeKey must be set.");
            }

            _runtimeKey = options.RuntimeKey.Trim();
            _transport = options.Transport ?? new UnityWebRequestTransport();
            _cache = options.Cache ?? new InMemoryPersistlySaveCache();
            _timeoutSeconds = options.TimeoutSeconds;
            _userAgent = options.UserAgent;
            _sdkName = NormalizeDiagnosticsHeader(options.SdkName, "unity");
            _sdkVersion = NormalizeDiagnosticsHeader(options.SdkVersion, "1.0.0");
            _platform = NormalizeDiagnosticsHeader(options.Platform, "unity");
            _engineVersion = NormalizeOptionalDiagnosticsHeader(options.EngineVersion);
            _clientVersion = NormalizeOptionalDiagnosticsHeader(options.ClientVersion);
        }

        public Task UpdateLocalAsync(PersistlySave save)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            _cache.Store(save);
            return Task.CompletedTask;
        }

        public bool TryGetLocal(string saveId, out PersistlySave save)
        {
            if (string.IsNullOrWhiteSpace(saveId))
            {
                throw new PersistlyConfigurationError("saveId must be set.");
            }

            return _cache.TryGet(saveId, out save);
        }

        public Task ClearLocalAsync(string saveId)
        {
            if (string.IsNullOrWhiteSpace(saveId))
            {
                throw new PersistlyConfigurationError("saveId must be set.");
            }

            _cache.Clear(saveId);
            return Task.CompletedTask;
        }

        public async Task<PersistlySave> CreateSaveAsync(PersistlyCreateSaveRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            PersistlyJson.ValidatePayloadSizes(request.SlotInfoJson, request.StateJson);

            var response = await SendJsonAsync(
                "POST",
                "/api/v1/saves",
                BuildCreateBody(request),
                cancellationToken);

            var save = ParseSaveEnvelope(response.Body);
            _cache.Store(save);
            return save;
        }

        public async Task<PersistlyCreateAccountResponse> CreateAccountAsync(PersistlyCreateAccountRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            PersistlyJson.ValidatePayloadSizes(null, request.AccountDataJson);
            if (request.Slot != null)
            {
                PersistlyJson.ValidatePayloadSizes(request.Slot.SlotInfoJson, request.Slot.DataJson);
            }

            var response = await SendJsonAsync(
                "POST",
                "/api/v1/accounts",
                BuildCreateAccountBody(request),
                cancellationToken);

            var created = ParseCreateAccountResponse(response.Body);
            _cache.Store(created.Account);
            if (created.Slot != null)
            {
                _cache.Store(created.Slot);
            }

            return created;
        }

        public async Task<PersistlyAccountEnvelope> LoadAccountAsync(string accountId, string accountSessionToken, CancellationToken cancellationToken = default)
        {
            EnsureSaveId(accountId);
            EnsureSessionToken(accountSessionToken);

            var response = await SendJsonAsync(
                "GET",
                "/api/v1/accounts/" + Uri.EscapeDataString(accountId),
                null,
                cancellationToken,
                accountSessionToken: accountSessionToken);

            var account = ParseAccountEnvelope(response.Body);
            _cache.Store(account.Save);
            return account;
        }

        public async Task<PersistlyDeleteAccountResponse> DeleteAccountAsync(string accountId, string accountSessionToken, CancellationToken cancellationToken = default)
        {
            EnsureSaveId(accountId);
            EnsureSessionToken(accountSessionToken);

            var response = await SendJsonAsync(
                "DELETE",
                "/api/v1/accounts/" + Uri.EscapeDataString(accountId),
                null,
                cancellationToken,
                accountSessionToken: accountSessionToken);

            var deleted = ParseDeleteAccountResponse(response.Body);
            _cache.Clear(accountId);
            return deleted;
        }

        public async Task<PersistlyCreateAccountResponse> CreateAccountSlotAsync(
            string accountId,
            string accountSessionToken,
            PersistlyCreateAccountSlotRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureSaveId(accountId);
            EnsureSessionToken(accountSessionToken);
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            PersistlyJson.ValidatePayloadSizes(request.SlotInfoJson, request.SlotDataJson);

            var response = await SendJsonAsync(
                "POST",
                "/api/v1/accounts/" + Uri.EscapeDataString(accountId) + "/slots",
                BuildCreateAccountSlotBody(request),
                cancellationToken,
                accountSessionToken: accountSessionToken);

            var created = ParseCreateAccountResponse(response.Body);
            _cache.Store(created.Account);
            if (created.Slot != null)
            {
                _cache.Store(created.Slot);
            }

            return created;
        }

        public async Task<PersistlyCreateAccountResponse> ArchiveSlotAsync(
            string accountId,
            string accountSessionToken,
            string slotId,
            CancellationToken cancellationToken = default)
        {
            EnsureSaveId(accountId);
            EnsureSaveId(slotId);
            EnsureSessionToken(accountSessionToken);

            var response = await SendJsonAsync(
                "POST",
                "/api/v1/accounts/" + Uri.EscapeDataString(accountId) + "/slots/" + Uri.EscapeDataString(slotId) + "/archive",
                null,
                cancellationToken,
                accountSessionToken: accountSessionToken);

            var archived = ParseCreateAccountResponse(response.Body);
            _cache.Store(archived.Account);
            if (archived.Slot != null)
            {
                _cache.Store(archived.Slot);
            }

            return archived;
        }

        public async Task<PersistlySave> LoadAccountSlotAsync(string accountId, string accountSessionToken, string slotId, CancellationToken cancellationToken = default)
        {
            EnsureSaveId(accountId);
            EnsureSaveId(slotId);
            EnsureSessionToken(accountSessionToken);

            var response = await SendJsonAsync(
                "GET",
                "/api/v1/accounts/" + Uri.EscapeDataString(accountId) + "/slots/" + Uri.EscapeDataString(slotId),
                null,
                cancellationToken,
                accountSessionToken: accountSessionToken);

            var save = ParseSlotEnvelope(response.Body).Save;
            _cache.Store(save);
            return save;
        }

        public async Task<PersistlyDeleteSlotResponse> DeleteAccountSlotAsync(
            string accountId,
            string accountSessionToken,
            string slotId,
            CancellationToken cancellationToken = default)
        {
            EnsureSaveId(accountId);
            EnsureSaveId(slotId);
            EnsureSessionToken(accountSessionToken);

            var response = await SendJsonAsync(
                "DELETE",
                "/api/v1/accounts/" + Uri.EscapeDataString(accountId) + "/slots/" + Uri.EscapeDataString(slotId),
                null,
                cancellationToken,
                accountSessionToken: accountSessionToken);

            var deleted = ParseDeleteSlotResponse(response.Body);
            _cache.Clear(slotId);
            if (deleted.Account != null)
            {
                _cache.Store(deleted.Account);
            }

            return deleted;
        }

        public async Task<PersistlySyncResponse> SyncAccountSlotAsync(
            string accountId,
            string accountSessionToken,
            string slotId,
            PersistlySyncSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureSaveId(accountId);
            EnsureSaveId(slotId);
            EnsureSessionToken(accountSessionToken);

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            PersistlyJson.ValidatePayloadSizes(request.SlotInfoJson, request.StateJson);

            var baseVersion = request.BaseVersion;
            PersistlySave cachedSave;
            if (!baseVersion.HasValue && _cache.TryGet(slotId, out cachedSave))
            {
                baseVersion = cachedSave.Version;
            }

            if (!baseVersion.HasValue)
            {
                throw new PersistlyConfigurationError("SyncAccountSlotAsync requires baseVersion unless the slot save is already cached.");
            }

            var response = await SendJsonAsync(
                "POST",
                "/api/v1/accounts/" + Uri.EscapeDataString(accountId) + "/slots/" + Uri.EscapeDataString(slotId) + "/sync",
                BuildSyncSlotBody(request, baseVersion.Value),
                cancellationToken,
                acceptConflictStatus: true,
                accountSessionToken: accountSessionToken);

            if (response.StatusCode == 200)
            {
                PersistlySave acceptedCachedSave;
                _cache.TryGet(slotId, out acceptedCachedSave);
                var acceptedPayload = ParseAcceptedSyncResponse(response.Body);
                var accepted = BuildAcceptedSyncResponse(
                    acceptedPayload,
                    acceptedPayload.Save ?? SynthesizeSave(slotId, acceptedCachedSave, request.SlotInfoJson, request.StateJson));
                _cache.Store(accepted.Save);
                return accepted;
            }

            if (response.StatusCode == 409)
            {
                if (IsErrorResponse(response.Body))
                {
                    throw ParseApiError(response.StatusCode, response.Body, response.Error);
                }

                var conflict = ParseConflictSyncResponse(response.Body);
                _cache.Store(conflict.Save);
                return conflict;
            }

            throw ParseApiError(response.StatusCode, response.Body, response.Error);
        }

        public async Task<PersistlyRuntimeConfig> GetRuntimeConfigAsync(int? gameConfigVersion = null, CancellationToken cancellationToken = default)
        {
            if (gameConfigVersion.HasValue && gameConfigVersion.Value < 0)
            {
                throw new PersistlyConfigurationError("gameConfigVersion must be a non-negative integer.");
            }

            var path = gameConfigVersion.HasValue
                ? "/api/v1/runtime-config?gameConfigVersion=" + gameConfigVersion.Value.ToString(CultureInfo.InvariantCulture)
                : "/api/v1/runtime-config";
            var response = await SendJsonAsync(
                "GET",
                path,
                null,
                cancellationToken);

            return ParseRuntimeConfig(response.Body);
        }

        public async Task<PersistlySyncResponse> SyncAccountDataAsync(
            string accountId,
            string accountSessionToken,
            PersistlySyncAccountDataRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureSaveId(accountId);
            EnsureSessionToken(accountSessionToken);
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            PersistlyJson.ValidatePayloadSizes(null, request.AccountDataJson ?? request.AccountDataPatchJson ?? "{}");

            var response = await SendJsonAsync(
                "POST",
                "/api/v1/accounts/" + Uri.EscapeDataString(accountId) + "/data/sync",
                BuildSyncAccountDataBody(request),
                cancellationToken,
                acceptConflictStatus: true,
                accountSessionToken: accountSessionToken);

            if (response.StatusCode == 200)
            {
                PersistlySave cachedSave;
                _cache.TryGet(accountId, out cachedSave);
                var acceptedPayload = ParseAcceptedSyncResponse(response.Body);
                var accepted = BuildAcceptedSyncResponse(
                    acceptedPayload,
                    acceptedPayload.Save ?? SynthesizeAccountSave(accountId, cachedSave, request));
                _cache.Store(accepted.Save);
                return accepted;
            }

            if (response.StatusCode == 409)
            {
                var conflict = ParseConflictSyncResponse(response.Body);
                _cache.Store(conflict.Save);
                return conflict;
            }

            throw ParseApiError(response.StatusCode, response.Body, response.Error);
        }

        public async Task<PersistlySave> LoadSaveAsync(string saveId, CancellationToken cancellationToken = default)
        {
            EnsureSaveId(saveId);

            var response = await SendJsonAsync(
                "GET",
                "/api/v1/saves/" + Uri.EscapeDataString(saveId),
                null,
                cancellationToken);

            var save = ParseSaveEnvelope(response.Body);
            _cache.Store(save);
            return save;
        }

        public async Task<PersistlySyncResponse> SyncSaveAsync(string saveId, PersistlySyncSaveRequest request, CancellationToken cancellationToken = default)
        {
            EnsureSaveId(saveId);

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            PersistlyJson.ValidatePayloadSizes(request.SlotInfoJson, request.StateJson);

            var baseVersion = request.BaseVersion;
            PersistlySave cachedSave;
            if (!baseVersion.HasValue && _cache.TryGet(saveId, out cachedSave))
            {
                baseVersion = cachedSave.Version;
            }

            if (!baseVersion.HasValue)
            {
                throw new PersistlyConfigurationError("SyncSaveAsync requires baseVersion unless the save is already cached.");
            }

            var response = await SendJsonAsync(
                "POST",
                "/api/v1/saves/" + Uri.EscapeDataString(saveId) + "/sync",
                BuildSyncBody(request, baseVersion.Value),
                cancellationToken,
                acceptConflictStatus: true);

            if (response.StatusCode == 200)
            {
                PersistlySave acceptedCachedSave;
                _cache.TryGet(saveId, out acceptedCachedSave);
                var acceptedPayload = ParseAcceptedSyncResponse(response.Body);
                var accepted = BuildAcceptedSyncResponse(
                    acceptedPayload,
                    acceptedPayload.Save ?? SynthesizeSave(saveId, acceptedCachedSave, request.SlotInfoJson, request.StateJson));
                _cache.Store(accepted.Save);
                return accepted;
            }

            if (response.StatusCode == 409)
            {
                var conflict = ParseConflictSyncResponse(response.Body);
                _cache.Store(conflict.Save);
                return conflict;
            }

            throw ParseApiError(response.StatusCode, response.Body, response.Error);
        }

        private async Task<PersistlyTransportResponse> SendJsonAsync(
            string method,
            string relativePath,
            string? body,
            CancellationToken cancellationToken,
            bool acceptConflictStatus = false,
            string? accountSessionToken = null)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Authorization", "Bearer " + _runtimeKey },
                { "Content-Type", "application/json" },
                { "User-Agent", _userAgent },
                { "X-Persistly-SDK", _sdkName },
                { "X-Persistly-SDK-Version", _sdkVersion },
                { "X-Persistly-Platform", _platform }
            };
            if (!string.IsNullOrWhiteSpace(_engineVersion))
            {
                headers["X-Persistly-Engine-Version"] = _engineVersion!;
            }
            if (!string.IsNullOrWhiteSpace(_clientVersion))
            {
                headers["X-Persistly-Client-Version"] = _clientVersion!;
            }
            if (!string.IsNullOrWhiteSpace(accountSessionToken))
            {
                headers["X-Persistly-Account-Session"] = accountSessionToken.Trim();
            }

            var request = new PersistlyTransportRequest(method, new Uri(_baseUri, relativePath).ToString(), body, _timeoutSeconds, headers);
            PersistlyTransportResponse response;
            try
            {
                response = await _transport.SendAsync(request, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new PersistlyTransportError("Persistly request failed before the runtime API responded.", exception);
            }

            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                return response;
            }

            if (acceptConflictStatus && response.StatusCode == 409)
            {
                return response;
            }

            throw ParseApiError(response.StatusCode, response.Body, response.Error);
        }

        private static string BuildCreateBody(PersistlyCreateSaveRequest request)
        {
            var body = "{";
            if (request.PlayerRef != null)
            {
                body += "\"playerRef\":" + PersistlyJson.EscapeJsonString(request.PlayerRef) + ",";
            }

            if (request.SlotInfoJson != null)
            {
                body += "\"slotInfo\":" + request.SlotInfoJson + ",";
            }

            body += "\"state\":" + request.StateJson;
            body += "}";
            return body;
        }

        private static string BuildSyncBody(PersistlySyncSaveRequest request, int baseVersion)
        {
            var body = "{";
            body += "\"baseVersion\":" + baseVersion.ToString(CultureInfo.InvariantCulture) + ",";
            if (request.SlotInfoJson != null)
            {
                body += "\"slotInfo\":" + request.SlotInfoJson + ",";
            }

            body += "\"state\":" + request.StateJson;
            body += "}";
            return body;
        }

        private static string BuildSyncSlotBody(PersistlySyncSaveRequest request, int baseVersion)
        {
            var body = "{";
            body += "\"baseVersion\":" + baseVersion.ToString(CultureInfo.InvariantCulture) + ",";
            if (request.SlotInfoJson != null)
            {
                body += "\"slotInfo\":" + request.SlotInfoJson + ",";
            }

            body += "\"data\":" + request.StateJson;
            body += "}";
            return body;
        }

        private static string BuildCreateAccountBody(PersistlyCreateAccountRequest request)
        {
            var body = "{";
            if (request.PlayerRef != null)
            {
                body += "\"playerRef\":" + PersistlyJson.EscapeJsonString(request.PlayerRef) + ",";
            }

            if (request.ExternalAccountRefJson != null)
            {
                body += "\"externalAccountRef\":" + request.ExternalAccountRefJson + ",";
            }

            body += "\"accountData\":" + request.AccountDataJson + ",";
            if (request.Slot != null)
            {
                body += "\"slot\":{\"slotId\":" + PersistlyJson.EscapeJsonString(request.Slot.SlotId) + ",\"slotInfo\":" + request.Slot.SlotInfoJson + ",\"data\":" + request.Slot.DataJson + "}";
            }
            else
            {
                body = body.TrimEnd(',');
            }

            body += "}";
            return body;
        }

        private static string BuildCreateAccountSlotBody(PersistlyCreateAccountSlotRequest request)
        {
            return "{" +
                "\"slotId\":" + PersistlyJson.EscapeJsonString(request.SlotId) + "," +
                "\"slotInfo\":" + request.SlotInfoJson + "," +
                "\"data\":" + request.SlotDataJson +
                "}";
        }

        private static string BuildSyncAccountDataBody(PersistlySyncAccountDataRequest request)
        {
            var body = "{";
            body += "\"baseVersion\":" + request.BaseVersion.ToString(CultureInfo.InvariantCulture);
            if (request.AccountDataJson != null)
            {
                body += ",\"accountData\":" + request.AccountDataJson;
            }

            if (request.AccountDataPatchJson != null)
            {
                body += ",\"accountDataPatch\":" + request.AccountDataPatchJson;
            }

            body += "}";
            return body;
        }

        private static PersistlySave ParseSaveEnvelope(string body)
        {
            var root = AsObject(PersistlyJson.ParseJsonValue(body, "save envelope"), "save envelope");
            var save = GetRequiredObject(root, "save", "save envelope");
            return ParseSave(save);
        }

        private static PersistlyCreateAccountResponse ParseCreateAccountResponse(string body)
        {
            var root = AsObject(PersistlyJson.ParseJsonValue(body, "create account response"), "create account response");
            var accountId = GetRequiredString(root, "accountId", "account envelope");
            var accountSessionToken = GetOptionalString(root, "accountSessionToken");
            var accountRoot = GetRequiredObject(root, "account", "account envelope");
            PersistlySave? slot = null;
            if (root.ContainsKey("slot") && root["slot"] != null)
            {
                slot = ParseSlotAsSave(GetRequiredObject(root, "slot", "account envelope"));
            }

            var policy = root.ContainsKey("syncPolicy")
                ? ParseSyncPolicy(GetRequiredObject(root, "syncPolicy", "account envelope"))
                : new PersistlySyncPolicy(60, 10, true, true, true, 25);
            return new PersistlyCreateAccountResponse(accountId, accountSessionToken, ParseAccountAsSave(accountRoot), slot, policy);
        }

        private static PersistlyAccountEnvelope ParseAccountEnvelope(string body)
        {
            var root = AsObject(PersistlyJson.ParseJsonValue(body, "account envelope"), "account envelope");
            return ParseAccountEnvelope(root);
        }

        private static PersistlyAccountEnvelope ParseAccountEnvelope(Dictionary<string, object?> accountRoot)
        {
            var accountId = GetRequiredString(accountRoot, "accountId", "account envelope");
            var accountSessionToken = GetOptionalString(accountRoot, "accountSessionToken");
            var save = accountRoot.ContainsKey("save")
                ? ParseSave(GetRequiredObject(accountRoot, "save", "account envelope"))
                : ParseAccountAsSave(accountRoot.ContainsKey("account") ? GetRequiredObject(accountRoot, "account", "account envelope") : accountRoot);
            var policy = accountRoot.ContainsKey("syncPolicy")
                ? ParseSyncPolicy(GetRequiredObject(accountRoot, "syncPolicy", "account envelope"))
                : null;
            return new PersistlyAccountEnvelope(accountId, accountSessionToken, save, policy);
        }

        private static PersistlyDeleteAccountResponse ParseDeleteAccountResponse(string body)
        {
            var root = AsObject(PersistlyJson.ParseJsonValue(body, "delete account response"), "delete account response");
            var accountId = GetRequiredString(root, "accountId", "delete account response");
            var deletedAt = GetRequiredDateTimeOffset(root, "deletedAt", "delete account response");
            var deletedSlotCount = root.ContainsKey("deletedSlotCount")
                ? GetRequiredInt(root, "deletedSlotCount", "delete account response")
                : 0;
            if (deletedSlotCount < 0)
            {
                throw new PersistlyConfigurationError("delete account response deletedSlotCount must be zero or greater.");
            }

            return new PersistlyDeleteAccountResponse(
                accountId,
                deletedAt,
                deletedSlotCount,
                GetRequiredBool(root, "alreadyDeleted", "delete account response"),
                GetRequiredBool(root, "cleanupQueued", "delete account response"));
        }

        private static PersistlyDeleteSlotResponse ParseDeleteSlotResponse(string body)
        {
            var root = AsObject(PersistlyJson.ParseJsonValue(body, "delete account slot response"), "delete account slot response");
            PersistlySave? account = null;
            Dictionary<string, object?>? accountRoot;
            if (TryGetObject(root, "account", out accountRoot))
            {
                account = ParseAccountAsSave(accountRoot!);
            }

            return new PersistlyDeleteSlotResponse(
                GetRequiredString(root, "accountId", "delete account slot response"),
                GetRequiredString(root, "slotId", "delete account slot response"),
                GetRequiredDateTimeOffset(root, "deletedAt", "delete account slot response"),
                GetRequiredBool(root, "alreadyDeleted", "delete account slot response"),
                GetRequiredBool(root, "cleanupQueued", "delete account slot response"),
                account);
        }

        private static PersistlySlotEnvelope ParseSlotEnvelope(string body)
        {
            var root = AsObject(PersistlyJson.ParseJsonValue(body, "slot envelope"), "slot envelope");
            var slotRoot = root.ContainsKey("slot") ? GetRequiredObject(root, "slot", "slot envelope") : root;
            return ParseSlotEnvelope(slotRoot);
        }

        private static PersistlySlotEnvelope ParseSlotEnvelope(Dictionary<string, object?> slotRoot)
        {
            if (slotRoot.ContainsKey("save"))
            {
                return new PersistlySlotEnvelope(ParseSave(GetRequiredObject(slotRoot, "save", "slot envelope")));
            }

            return new PersistlySlotEnvelope(ParseSlotAsSave(slotRoot));
        }

        private static PersistlyRuntimeConfig ParseRuntimeConfig(string body)
        {
            var root = AsObject(PersistlyJson.ParseJsonValue(body, "runtime config"), "runtime config");
            var policy = GetRequiredObject(root, "syncPolicy", "runtime config");
            PersistlyRuntimeGameConfig? gameConfig = null;
            Dictionary<string, object?>? gameConfigRoot;
            if (TryGetObject(root, "gameConfig", out gameConfigRoot))
            {
                gameConfig = ParseRuntimeGameConfig(gameConfigRoot!);
            }

            return new PersistlyRuntimeConfig(ParseSyncPolicy(policy), gameConfig);
        }

        private static PersistlyRuntimeGameConfig ParseRuntimeGameConfig(Dictionary<string, object?> gameConfig)
        {
            var configJson = "{}";
            Dictionary<string, object?>? config;
            if (TryGetObject(gameConfig, "data", out config) || TryGetObject(gameConfig, "config", out config))
            {
                configJson = PersistlyJson.Serialize(config);
            }

            var hasData = GetOptionalBool(gameConfig, "hasData") ?? (config != null && config.Count > 0);
            var eventName = GetOptionalString(gameConfig, "eventName");
            if (eventName == null && config != null)
            {
                eventName = GetOptionalString(config, "eventName");
            }

            return new PersistlyRuntimeGameConfig(
                GetRequiredBool(gameConfig, "enabled", "gameConfig"),
                GetOptionalInt(gameConfig, "version"),
                GetOptionalBool(gameConfig, "unchanged") ?? false,
                GetOptionalInt(gameConfig, "sizeBytes"),
                hasData,
                eventName,
                configJson);
        }

        private static PersistlySyncPolicy ParseSyncPolicy(Dictionary<string, object?> policy)
        {
            return new PersistlySyncPolicy(
                GetRequiredInt(policy, "minRemoteSyncIntervalSeconds", "syncPolicy"),
                GetRequiredInt(policy, "forceSyncCooldownSeconds", "syncPolicy"),
                GetOptionalBool(policy, "syncOnAppBackground") ?? GetOptionalBool(policy, "syncOnBackground") ?? false,
                GetOptionalBool(policy, "syncOnAppForeground") ?? GetOptionalBool(policy, "syncOnForeground") ?? false,
                GetRequiredBool(policy, "syncOnReconnect", "syncPolicy"),
                GetRequiredInt(policy, "maxQueuedLocalSnapshots", "syncPolicy"));
        }

        private sealed class AcceptedSyncPayload
        {
            public AcceptedSyncPayload(PersistlySave? save, int version, DateTimeOffset updatedAt, bool historyRetained, IReadOnlyList<string> warnings)
            {
                Save = save;
                Version = version;
                UpdatedAt = updatedAt;
                HistoryRetained = historyRetained;
                Warnings = warnings;
            }

            public PersistlySave? Save { get; }

            public int Version { get; }

            public DateTimeOffset UpdatedAt { get; }

            public bool HistoryRetained { get; }

            public IReadOnlyList<string> Warnings { get; }
        }

        private static AcceptedSyncPayload ParseAcceptedSyncResponse(string body)
        {
            var root = AsObject(PersistlyJson.ParseJsonValue(body, "sync response"), "sync response");
            var status = GetRequiredString(root, "status", "sync response");
            if (!string.Equals(status, "accepted", StringComparison.Ordinal))
            {
                throw new PersistlyConfigurationError("Accepted sync response had an unexpected status.");
            }

            PersistlySave? save = null;
            if (TryGetObject(root, "save", out var saveObject) && saveObject != null)
            {
                save = ParseSave(saveObject);
            }
            else if (TryGetObject(root, "slot", out var slotObject) && slotObject != null)
            {
                save = ParseSlotAsSave(slotObject);
            }
            else if (TryGetObject(root, "account", out var accountObject) && accountObject != null)
            {
                save = ParseAccountAsSave(accountObject);
            }

            var version = root.ContainsKey("version") ? GetRequiredInt(root, "version", "sync response") : save?.Version ?? 0;
            if (version < 1)
            {
                throw new PersistlyConfigurationError("Accepted sync response version must be greater than zero.");
            }

            var updatedAt = root.ContainsKey("updatedAt")
                ? GetRequiredDateTimeOffset(root, "updatedAt", "sync response")
                : save?.UpdatedAt ?? DateTimeOffset.MinValue;
            if (updatedAt == DateTimeOffset.MinValue)
            {
                throw new PersistlyConfigurationError("Accepted sync response updatedAt must be set.");
            }

            var historyRetained = root.ContainsKey("historyRetained") ? GetRequiredBool(root, "historyRetained", "sync response") : false;
            return new AcceptedSyncPayload(save, version, updatedAt, historyRetained, ParseWarnings(root));
        }

        private static PersistlySyncResponse BuildAcceptedSyncResponse(AcceptedSyncPayload payload, PersistlySave save)
        {
            return new PersistlySyncResponse(
                PersistlySyncStatus.Accepted,
                new PersistlySave(
                    save.SaveId,
                    save.PlayerRef,
                    save.SlotInfoJson,
                    save.StateJson,
                    payload.Version,
                    save.CreatedAt,
                    payload.UpdatedAt),
                historyRetained: payload.HistoryRetained,
                warnings: payload.Warnings);
        }

        private static PersistlySave SynthesizeSave(string saveId, PersistlySave? cachedSave, string? slotInfoJson, string stateJson)
        {
            return new PersistlySave(
                saveId,
                cachedSave?.PlayerRef,
                slotInfoJson ?? cachedSave?.SlotInfoJson ?? "{}",
                stateJson,
                1,
                cachedSave?.CreatedAt ?? DateTimeOffset.FromUnixTimeSeconds(0),
                DateTimeOffset.FromUnixTimeSeconds(0));
        }

        private static PersistlySave SynthesizeAccountSave(
            string accountId,
            PersistlySave? cachedSave,
            PersistlySyncAccountDataRequest request)
        {
            var cachedState = cachedSave == null
                ? new Dictionary<string, object?>()
                : AsObject(PersistlyJson.ParseJsonValue(cachedSave.StateJson, "cached account state"), "cached account state");
            var accountData = request.AccountDataJson != null
                ? AsObject(PersistlyJson.ParseJsonValue(request.AccountDataJson, "accountData"), "accountData")
                : MergeObjects(
                    cachedState.ContainsKey("accountData") && cachedState["accountData"] is Dictionary<string, object?> existingAccountData
                        ? existingAccountData
                        : new Dictionary<string, object?>(),
                    request.AccountDataPatchJson == null
                        ? new Dictionary<string, object?>()
                        : AsObject(PersistlyJson.ParseJsonValue(request.AccountDataPatchJson, "accountDataPatch"), "accountDataPatch"));
            var slots = cachedState.ContainsKey("slots") && cachedState["slots"] is List<object?> existingSlots
                ? existingSlots
                : new List<object?>();
            var stateJson = PersistlyJson.Serialize(new Dictionary<string, object?>
            {
                ["schema"] = "persistly.account.v1",
                ["accountData"] = accountData,
                ["slots"] = slots,
            });

            return new PersistlySave(
                accountId,
                cachedSave?.PlayerRef,
                cachedSave?.SlotInfoJson ?? "{}",
                stateJson,
                1,
                cachedSave?.CreatedAt ?? DateTimeOffset.FromUnixTimeSeconds(0),
                DateTimeOffset.FromUnixTimeSeconds(0));
        }

        private static Dictionary<string, object?> MergeObjects(
            Dictionary<string, object?> existing,
            Dictionary<string, object?> patch)
        {
            var merged = new Dictionary<string, object?>(existing);
            foreach (var pair in patch)
            {
                if (pair.Value == null)
                {
                    merged.Remove(pair.Key);
                    continue;
                }

                merged[pair.Key] = pair.Value;
            }

            return merged;
        }

        private static IReadOnlyList<string> ParseWarnings(Dictionary<string, object?> root)
        {
            if (!root.ContainsKey("warnings") || root["warnings"] == null)
            {
                return Array.Empty<string>();
            }

            if (!(root["warnings"] is List<object?> rawWarnings))
            {
                throw new PersistlyConfigurationError("sync response.warnings must be an array.");
            }

            var warnings = new List<string>(rawWarnings.Count);
            foreach (var warning in rawWarnings)
            {
                if (!(warning is string value))
                {
                    throw new PersistlyConfigurationError("sync response.warnings must contain only strings.");
                }

                warnings.Add(value);
            }

            return warnings;
        }

        private static PersistlySyncResponse ParseConflictSyncResponse(string body)
        {
            var root = AsObject(PersistlyJson.ParseJsonValue(body, "sync response"), "sync response");
            var status = GetRequiredString(root, "status", "sync response");
            if (!string.Equals(status, "conflict", StringComparison.Ordinal))
            {
                throw new PersistlyConfigurationError("Conflict sync response had an unexpected status.");
            }

            var save = root.ContainsKey("slot")
                ? ParseSlotAsSave(GetRequiredObject(root, "slot", "sync response"))
                : root.ContainsKey("account")
                    ? ParseAccountAsSave(GetRequiredObject(root, "account", "sync response"))
                    : ParseSave(GetRequiredObject(root, "save", "sync response"));
            var details = GetRequiredObject(root, "details", "sync response");
            var reason = GetRequiredString(details, "reason", "sync response details");
            if (!string.Equals(reason, "base_version_mismatch", StringComparison.Ordinal))
            {
                throw new PersistlyConfigurationError("Conflict sync response had an unexpected reason.");
            }

            return new PersistlySyncResponse(
                PersistlySyncStatus.Conflict,
                save,
                new PersistlySyncConflictDetails(PersistlySyncConflictReason.BaseVersionMismatch));
        }

        private static PersistlySave ParseSave(Dictionary<string, object?> saveObject)
        {
            var saveId = GetRequiredString(saveObject, "saveId", "save");
            var playerRef = GetOptionalString(saveObject, "playerRef");
            var slotInfo = saveObject.ContainsKey("slotInfo")
                ? GetRequiredObject(saveObject, "slotInfo", "save")
                : GetRequiredObject(saveObject, "metadata", "save");
            var state = saveObject.ContainsKey("data")
                ? GetRequiredObject(saveObject, "data", "save")
                : GetRequiredObject(saveObject, "state", "save");
            var version = GetRequiredInt(saveObject, "version", "save");
            var createdAt = GetRequiredDateTimeOffset(saveObject, "createdAt", "save");
            var updatedAt = GetRequiredDateTimeOffset(saveObject, "updatedAt", "save");

            return new PersistlySave(
                saveId,
                playerRef,
                PersistlyJson.Serialize(slotInfo),
                PersistlyJson.Serialize(state),
                version,
                createdAt,
                updatedAt);
        }

        private static PersistlySave ParseAccountAsSave(Dictionary<string, object?> accountObject)
        {
            var accountId = GetRequiredString(accountObject, "accountId", "account");
            var accountData = GetRequiredObject(accountObject, "accountData", "account");
            var slots = accountObject.ContainsKey("slots") && accountObject["slots"] is List<object?> rawSlots
                ? rawSlots
                : new List<object?>();
            var state = new Dictionary<string, object?>
            {
                { "schema", PersistlyAccountState.Schema },
                { "accountData", accountData },
                { "slots", slots }
            };
            var version = accountObject.ContainsKey("version") ? GetRequiredInt(accountObject, "version", "account") : 1;
            var updatedAt = accountObject.ContainsKey("updatedAt")
                ? GetRequiredDateTimeOffset(accountObject, "updatedAt", "account")
                : DateTimeOffset.FromUnixTimeSeconds(0);

            return new PersistlySave(accountId, GetOptionalString(accountObject, "playerRef"), "{}", PersistlyJson.Serialize(state), version, updatedAt, updatedAt);
        }

        private static PersistlySave ParseSlotAsSave(Dictionary<string, object?> slotObject)
        {
            var slotId = GetRequiredString(slotObject, "slotId", "slot");
            var slotInfo = GetRequiredObject(slotObject, "slotInfo", "slot");
            var data = GetRequiredObject(slotObject, "data", "slot");
            var version = slotObject.ContainsKey("version") ? GetRequiredInt(slotObject, "version", "slot") : 1;
            var updatedAt = slotObject.ContainsKey("updatedAt")
                ? GetRequiredDateTimeOffset(slotObject, "updatedAt", "slot")
                : DateTimeOffset.FromUnixTimeSeconds(0);

            return new PersistlySave(slotId, null, PersistlyJson.Serialize(slotInfo), PersistlyJson.Serialize(data), version, updatedAt, updatedAt);
        }

        private static PersistlyApiError ParseApiError(int statusCode, string body, string? transportError)
        {
            string? detailsJson = null;
            string message = transportError ?? DefaultMessageForStatus(statusCode);
            PersistlyErrorCode code = MapStatusCodeToErrorCode(statusCode);

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    var root = AsObject(PersistlyJson.ParseJsonValue(body, "error response"), "error response");
                    var error = GetRequiredObject(root, "error", "error response");
                    var wireCode = GetRequiredString(error, "code", "error response");
                    message = GetRequiredString(error, "message", "error response");
                    if (error.ContainsKey("details"))
                    {
                        detailsJson = PersistlyJson.Serialize(error["details"]);
                    }

                    code = ParseWireErrorCode(wireCode);

                    if (code == PersistlyErrorCode.SlotAlreadyExists)
                    {
                        string? slotId = null;
                        Dictionary<string, object?>? details;
                        if (TryGetObject(error, "details", out details) && details != null)
                        {
                            slotId = GetOptionalString(details, "slotId");
                        }

                        return new PersistlySlotAlreadyExistsError(statusCode, message, slotId, detailsJson);
                    }

                    if (code == PersistlyErrorCode.SlotArchived)
                    {
                        string? slotId = null;
                        Dictionary<string, object?>? details;
                        if (TryGetObject(error, "details", out details) && details != null)
                        {
                            slotId = GetOptionalString(details, "slotId");
                        }

                        return new PersistlySlotArchivedError(statusCode, message, slotId, detailsJson);
                    }

                    if (code == PersistlyErrorCode.AccountDeleted)
                    {
                        string? accountId = null;
                        Dictionary<string, object?>? details;
                        if (TryGetObject(error, "details", out details) && details != null)
                        {
                            accountId = GetOptionalString(details, "accountId");
                        }

                        return new PersistlyAccountDeletedError(statusCode, message, accountId, detailsJson);
                    }

                    if (code == PersistlyErrorCode.SlotDeleted)
                    {
                        string? slotId = null;
                        Dictionary<string, object?>? details;
                        if (TryGetObject(error, "details", out details) && details != null)
                        {
                            slotId = GetOptionalString(details, "slotId");
                        }

                        return new PersistlySlotDeletedError(statusCode, message, slotId, detailsJson);
                    }

                    if (code == PersistlyErrorCode.MonthlyQuotaExceeded)
                    {
                        string? planTier = null;
                        long? used = null;
                        long? limit = null;
                        Dictionary<string, object?>? details;
                        if (TryGetObject(error, "details", out details) && details != null)
                        {
                            planTier = GetOptionalString(details, "planTier");
                            used = GetOptionalLong(details, "used");
                            limit = GetOptionalLong(details, "limit");
                        }

                        return new PersistlyMonthlyQuotaExceededError(statusCode, message, planTier, used, limit, detailsJson);
                    }

                    if (code == PersistlyErrorCode.PayloadTooLarge)
                    {
                        string? field = null;
                        int? maxBytes = null;
                        Dictionary<string, object?>? details;
                        if (TryGetObject(error, "details", out details) && details != null)
                        {
                            field = GetOptionalString(details, "field");
                            if (details.ContainsKey("maxBytes"))
                            {
                                maxBytes = GetOptionalInt(details, "maxBytes");
                            }
                        }

                        return new PersistlyPayloadTooLargeError(statusCode, message, field, maxBytes, detailsJson);
                    }
                }
                catch (PersistlyConfigurationError)
                {
                    code = MapStatusCodeToErrorCode(statusCode);
                }
            }

            switch (code)
            {
                case PersistlyErrorCode.InvalidRequest:
                    return new PersistlyInvalidRequestError(statusCode, message, detailsJson);
                case PersistlyErrorCode.Unauthorized:
                    return new PersistlyUnauthorizedError(statusCode, message, detailsJson);
                case PersistlyErrorCode.Forbidden:
                    return new PersistlyForbiddenError(statusCode, message, detailsJson);
                case PersistlyErrorCode.NotFound:
                    return new PersistlyNotFoundError(statusCode, message, detailsJson);
                case PersistlyErrorCode.Conflict:
                    return new PersistlyConflictError(statusCode, message, detailsJson);
                case PersistlyErrorCode.SlotAlreadyExists:
                    return new PersistlySlotAlreadyExistsError(statusCode, message, null, detailsJson);
                case PersistlyErrorCode.SlotArchived:
                    return new PersistlySlotArchivedError(statusCode, message, null, detailsJson);
                case PersistlyErrorCode.AccountDeleted:
                    return new PersistlyAccountDeletedError(statusCode, message, null, detailsJson);
                case PersistlyErrorCode.SlotDeleted:
                    return new PersistlySlotDeletedError(statusCode, message, null, detailsJson);
                case PersistlyErrorCode.RateLimited:
                    return new PersistlyRateLimitedError(statusCode, message, detailsJson);
                case PersistlyErrorCode.MonthlyQuotaExceeded:
                    return new PersistlyMonthlyQuotaExceededError(statusCode, message, null, null, null, detailsJson);
                case PersistlyErrorCode.PayloadTooLarge:
                    return new PersistlyPayloadTooLargeError(statusCode, message, null, null, detailsJson);
                case PersistlyErrorCode.ServerError:
                default:
                    return new PersistlyServerError(statusCode, message, detailsJson);
            }
        }

        private static PersistlyErrorCode ParseWireErrorCode(string wireCode)
        {
            switch (wireCode)
            {
                case "invalid_request":
                    return PersistlyErrorCode.InvalidRequest;
                case "unauthorized":
                    return PersistlyErrorCode.Unauthorized;
                case "forbidden":
                    return PersistlyErrorCode.Forbidden;
                case "not_found":
                    return PersistlyErrorCode.NotFound;
                case "conflict":
                    return PersistlyErrorCode.Conflict;
                case "slot_already_exists":
                    return PersistlyErrorCode.SlotAlreadyExists;
                case "slot_archived":
                    return PersistlyErrorCode.SlotArchived;
                case "account_deleted":
                    return PersistlyErrorCode.AccountDeleted;
                case "slot_deleted":
                    return PersistlyErrorCode.SlotDeleted;
                case "rate_limited":
                    return PersistlyErrorCode.RateLimited;
                case "monthly_quota_exceeded":
                    return PersistlyErrorCode.MonthlyQuotaExceeded;
                case "payload_too_large":
                    return PersistlyErrorCode.PayloadTooLarge;
                case "server_error":
                    return PersistlyErrorCode.ServerError;
                default:
                    throw new PersistlyConfigurationError("Persistly error code was unexpected: " + wireCode);
            }
        }

        private static PersistlyErrorCode MapStatusCodeToErrorCode(int statusCode)
        {
            if (statusCode == 400 || statusCode == 422)
            {
                return PersistlyErrorCode.InvalidRequest;
            }

            if (statusCode == 401)
            {
                return PersistlyErrorCode.Unauthorized;
            }

            if (statusCode == 403)
            {
                return PersistlyErrorCode.Forbidden;
            }

            if (statusCode == 404)
            {
                return PersistlyErrorCode.NotFound;
            }

            if (statusCode == 409)
            {
                return PersistlyErrorCode.Conflict;
            }

            if (statusCode == 413)
            {
                return PersistlyErrorCode.PayloadTooLarge;
            }

            if (statusCode == 402)
            {
                return PersistlyErrorCode.MonthlyQuotaExceeded;
            }

            if (statusCode == 429)
            {
                return PersistlyErrorCode.RateLimited;
            }

            return PersistlyErrorCode.ServerError;
        }

        private static string DefaultMessageForStatus(int statusCode)
        {
            return "Persistly request failed with HTTP " + statusCode.ToString(CultureInfo.InvariantCulture) + ".";
        }

        public static PersistlyApiError ParseErrorForTests(int statusCode, string body)
        {
            return ParseApiError(statusCode, body, null);
        }

        private static bool IsErrorResponse(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return false;
            }

            try
            {
                var root = PersistlyJson.ParseJsonValue(body, "response") as Dictionary<string, object?>;
                return root != null && root.ContainsKey("error");
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureSaveId(string saveId)
        {
            if (string.IsNullOrWhiteSpace(saveId))
            {
                throw new PersistlyConfigurationError("saveId must be set.");
            }
        }

        private static void EnsureSessionToken(string accountSessionToken)
        {
            if (string.IsNullOrWhiteSpace(accountSessionToken))
            {
                throw new PersistlyConfigurationError("accountSessionToken must be set.");
            }
        }

        private static Dictionary<string, object?> AsObject(object? value, string label)
        {
            var dictionary = value as Dictionary<string, object?>;
            if (dictionary == null)
            {
                throw new PersistlyConfigurationError(label + " must be a JSON object.");
            }

            return dictionary;
        }

        private static Dictionary<string, object?> GetRequiredObject(Dictionary<string, object?> element, string propertyName, string label)
        {
            Dictionary<string, object?>? property;
            if (!TryGetObject(element, propertyName, out property))
            {
                throw new PersistlyConfigurationError(label + " is missing required property " + propertyName + ".");
            }

            return property!;
        }

        private static bool TryGetObject(Dictionary<string, object?> element, string propertyName, out Dictionary<string, object?>? property)
        {
            object? raw;
            if (element.TryGetValue(propertyName, out raw))
            {
                var parsed = raw as Dictionary<string, object?>;
                property = parsed;
                return parsed != null;
            }

            property = null;
            return false;
        }

        private static string GetRequiredString(Dictionary<string, object?> element, string propertyName, string label)
        {
            object? property;
            if (!element.TryGetValue(propertyName, out property) || !(property is string value) || string.IsNullOrWhiteSpace(value))
            {
                throw new PersistlyConfigurationError(label + "." + propertyName + " must be a non-empty string.");
            }

            return value;
        }

        private static string? GetOptionalString(Dictionary<string, object?> element, string propertyName)
        {
            object? property;
            if (!element.TryGetValue(propertyName, out property) || property == null)
            {
                return null;
            }

            var value = property as string;
            if (value == null)
            {
                throw new PersistlyConfigurationError(propertyName + " must be a string or null.");
            }

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static int GetRequiredInt(Dictionary<string, object?> element, string propertyName, string label)
        {
            object? property;
            if (!element.TryGetValue(propertyName, out property))
            {
                throw new PersistlyConfigurationError(label + "." + propertyName + " must be an integer.");
            }

            return ConvertToInt(property!, label + "." + propertyName);
        }

        private static bool GetRequiredBool(Dictionary<string, object?> element, string propertyName, string label)
        {
            object? property;
            if (!element.TryGetValue(propertyName, out property) || !(property is bool value))
            {
                throw new PersistlyConfigurationError(label + "." + propertyName + " must be a boolean.");
            }

            return value;
        }

        private static bool? GetOptionalBool(Dictionary<string, object?> element, string propertyName)
        {
            object? property;
            if (!element.TryGetValue(propertyName, out property) || property == null)
            {
                return null;
            }

            if (property is bool value)
            {
                return value;
            }

            throw new PersistlyConfigurationError(propertyName + " must be a boolean.");
        }

        private static int? GetOptionalInt(Dictionary<string, object?> element, string propertyName)
        {
            object? property;
            if (!element.TryGetValue(propertyName, out property) || property == null)
            {
                return null;
            }

            return ConvertToInt(property, propertyName);
        }

        private static long? GetOptionalLong(Dictionary<string, object?> element, string propertyName)
        {
            object? property;
            if (!element.TryGetValue(propertyName, out property) || property == null)
            {
                return null;
            }

            return ConvertToLong(property, propertyName);
        }

        private static int ConvertToInt(object value, string label)
        {
            if (value is long longValue)
            {
                return checked((int)longValue);
            }

            if (value is int intValue)
            {
                return intValue;
            }

            if (value is double doubleValue)
            {
                var rounded = Math.Round(doubleValue);
                if (Math.Abs(doubleValue - rounded) > 0.00001d)
                {
                    throw new PersistlyConfigurationError(label + " must be an integer.");
                }

                return checked((int)rounded);
            }

            throw new PersistlyConfigurationError(label + " must be an integer.");
        }

        private static long ConvertToLong(object value, string label)
        {
            if (value is long longValue)
            {
                return longValue;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            if (value is double doubleValue)
            {
                var rounded = Math.Round(doubleValue);
                if (Math.Abs(doubleValue - rounded) > 0.00001d)
                {
                    throw new PersistlyConfigurationError(label + " must be an integer.");
                }

                return checked((long)rounded);
            }

            throw new PersistlyConfigurationError(label + " must be an integer.");
        }

        private static DateTimeOffset GetRequiredDateTimeOffset(Dictionary<string, object?> element, string propertyName, string label)
        {
            var raw = GetRequiredString(element, propertyName, label);
            DateTimeOffset value;
            if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value))
            {
                throw new PersistlyConfigurationError(label + "." + propertyName + " must be an RFC 3339 date-time string.");
            }

            return value;
        }

        private static string NormalizeDiagnosticsHeader(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string? NormalizeOptionalDiagnosticsHeader(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
