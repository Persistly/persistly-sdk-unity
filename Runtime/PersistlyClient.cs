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

            PersistlyJson.ValidatePayloadSizes(request.MetadataJson, request.StateJson);

            var response = await SendJsonAsync(
                "POST",
                "/api/v1/saves",
                BuildCreateBody(request),
                cancellationToken);

            var save = ParseSaveEnvelope(response.Body);
            _cache.Store(save);
            return save;
        }

        public async Task<PersistlyCreateProfileResponse> CreateProfileAsync(PersistlyCreateProfileRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            PersistlyJson.ValidatePayloadSizes(request.ProfileMetadataJson, request.AccountDataJson);
            if (request.Character != null)
            {
                PersistlyJson.ValidatePayloadSizes(request.Character.MetadataJson, request.Character.StateJson);
            }

            var response = await SendJsonAsync(
                "POST",
                "/api/v1/profiles",
                BuildCreateProfileBody(request),
                cancellationToken);

            var created = ParseCreateProfileResponse(response.Body);
            _cache.Store(created.Profile);
            if (created.Character != null)
            {
                _cache.Store(created.Character);
            }

            return created;
        }

        public async Task<PersistlyProfileEnvelope> LoadProfileAsync(string profileSaveId, string profileSessionToken, CancellationToken cancellationToken = default)
        {
            EnsureSaveId(profileSaveId);
            EnsureSessionToken(profileSessionToken);

            var response = await SendJsonAsync(
                "GET",
                "/api/v1/profiles/" + Uri.EscapeDataString(profileSaveId),
                null,
                cancellationToken,
                profileSessionToken: profileSessionToken);

            var profile = ParseProfileEnvelope(response.Body);
            _cache.Store(profile.Save);
            return profile;
        }

        public async Task<PersistlyCreateProfileResponse> CreateProfileCharacterAsync(
            string profileSaveId,
            string profileSessionToken,
            PersistlyCreateProfileCharacterRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureSaveId(profileSaveId);
            EnsureSessionToken(profileSessionToken);
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            PersistlyJson.ValidatePayloadSizes(request.CharacterMetadataJson, request.CharacterStateJson);

            var response = await SendJsonAsync(
                "POST",
                "/api/v1/profiles/" + Uri.EscapeDataString(profileSaveId) + "/characters",
                BuildCreateProfileCharacterBody(request),
                cancellationToken,
                profileSessionToken: profileSessionToken);

            var created = ParseCreateProfileResponse(response.Body);
            _cache.Store(created.Profile);
            if (created.Character != null)
            {
                _cache.Store(created.Character);
            }

            return created;
        }

        public async Task<PersistlyCreateProfileResponse> ArchiveProfileCharacterAsync(
            string profileSaveId,
            string profileSessionToken,
            string characterSaveId,
            CancellationToken cancellationToken = default)
        {
            EnsureSaveId(profileSaveId);
            EnsureSaveId(characterSaveId);
            EnsureSessionToken(profileSessionToken);

            var response = await SendJsonAsync(
                "POST",
                "/api/v1/profiles/" + Uri.EscapeDataString(profileSaveId) + "/characters/" + Uri.EscapeDataString(characterSaveId) + "/archive",
                null,
                cancellationToken,
                profileSessionToken: profileSessionToken);

            var archived = ParseCreateProfileResponse(response.Body);
            _cache.Store(archived.Profile);
            if (archived.Character != null)
            {
                _cache.Store(archived.Character);
            }

            return archived;
        }

        public async Task<PersistlySave> LoadProfileCharacterAsync(string profileSaveId, string profileSessionToken, string characterSaveId, CancellationToken cancellationToken = default)
        {
            EnsureSaveId(profileSaveId);
            EnsureSaveId(characterSaveId);
            EnsureSessionToken(profileSessionToken);

            var response = await SendJsonAsync(
                "GET",
                "/api/v1/profiles/" + Uri.EscapeDataString(profileSaveId) + "/characters/" + Uri.EscapeDataString(characterSaveId),
                null,
                cancellationToken,
                profileSessionToken: profileSessionToken);

            var save = ParseSaveEnvelope(response.Body);
            _cache.Store(save);
            return save;
        }

        public async Task<PersistlySyncResponse> SyncProfileCharacterAsync(
            string profileSaveId,
            string profileSessionToken,
            string characterSaveId,
            PersistlySyncSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureSaveId(profileSaveId);
            EnsureSaveId(characterSaveId);
            EnsureSessionToken(profileSessionToken);

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            PersistlyJson.ValidatePayloadSizes(request.MetadataJson, request.StateJson);

            var baseVersion = request.BaseVersion;
            PersistlySave cachedSave;
            if (!baseVersion.HasValue && _cache.TryGet(characterSaveId, out cachedSave))
            {
                baseVersion = cachedSave.Version;
            }

            if (!baseVersion.HasValue)
            {
                throw new PersistlyConfigurationError("SyncProfileCharacterAsync requires baseVersion unless the character save is already cached.");
            }

            var response = await SendJsonAsync(
                "POST",
                "/api/v1/profiles/" + Uri.EscapeDataString(profileSaveId) + "/characters/" + Uri.EscapeDataString(characterSaveId) + "/sync",
                BuildSyncBody(request, baseVersion.Value),
                cancellationToken,
                acceptConflictStatus: true,
                profileSessionToken: profileSessionToken);

            if (response.StatusCode == 200)
            {
                PersistlySave cachedSave;
                _cache.TryGet(characterSaveId, out cachedSave);
                var acceptedPayload = ParseAcceptedSyncResponse(response.Body);
                var accepted = BuildAcceptedSyncResponse(
                    acceptedPayload,
                    acceptedPayload.Save ?? SynthesizeSave(characterSaveId, cachedSave, request.MetadataJson, request.StateJson));
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

        public async Task<PersistlyRuntimeConfig> GetRuntimeConfigAsync(CancellationToken cancellationToken = default)
        {
            var response = await SendJsonAsync(
                "GET",
                "/api/v1/runtime-config",
                null,
                cancellationToken);

            return ParseRuntimeConfig(response.Body);
        }

        public async Task<PersistlySyncResponse> SyncProfileAccountDataAsync(
            string profileSaveId,
            string profileSessionToken,
            PersistlySyncProfileAccountDataRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureSaveId(profileSaveId);
            EnsureSessionToken(profileSessionToken);
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            PersistlyJson.ValidatePayloadSizes(request.MetadataJson, request.AccountDataJson ?? request.AccountDataPatchJson ?? "{}");

            var response = await SendJsonAsync(
                "POST",
                "/api/v1/profiles/" + Uri.EscapeDataString(profileSaveId) + "/account-data/sync",
                BuildSyncProfileAccountDataBody(request),
                cancellationToken,
                acceptConflictStatus: true,
                profileSessionToken: profileSessionToken);

            if (response.StatusCode == 200)
            {
                PersistlySave cachedSave;
                _cache.TryGet(profileSaveId, out cachedSave);
                var acceptedPayload = ParseAcceptedSyncResponse(response.Body);
                var accepted = BuildAcceptedSyncResponse(
                    acceptedPayload,
                    acceptedPayload.Save ?? SynthesizeProfileSave(profileSaveId, cachedSave, request));
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

            PersistlyJson.ValidatePayloadSizes(request.MetadataJson, request.StateJson);

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
                PersistlySave cachedSave;
                _cache.TryGet(saveId, out cachedSave);
                var acceptedPayload = ParseAcceptedSyncResponse(response.Body);
                var accepted = BuildAcceptedSyncResponse(
                    acceptedPayload,
                    acceptedPayload.Save ?? SynthesizeSave(saveId, cachedSave, request.MetadataJson, request.StateJson));
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
            string? profileSessionToken = null)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Authorization", "Bearer " + _runtimeKey },
                { "Content-Type", "application/json" },
                { "User-Agent", _userAgent }
            };
            if (!string.IsNullOrWhiteSpace(profileSessionToken))
            {
                headers["X-Persistly-Profile-Session"] = profileSessionToken.Trim();
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

            if (request.MetadataJson != null)
            {
                body += "\"metadata\":" + request.MetadataJson + ",";
            }

            body += "\"state\":" + request.StateJson;
            body += "}";
            return body;
        }

        private static string BuildSyncBody(PersistlySyncSaveRequest request, int baseVersion)
        {
            var body = "{";
            body += "\"baseVersion\":" + baseVersion.ToString(CultureInfo.InvariantCulture) + ",";
            if (request.MetadataJson != null)
            {
                body += "\"metadata\":" + request.MetadataJson + ",";
            }

            body += "\"state\":" + request.StateJson;
            body += "}";
            return body;
        }

        private static string BuildCreateProfileBody(PersistlyCreateProfileRequest request)
        {
            var body = "{";
            if (request.PlayerRef != null)
            {
                body += "\"playerRef\":" + PersistlyJson.EscapeJsonString(request.PlayerRef) + ",";
            }

            if (request.ExternalProfileRefJson != null)
            {
                body += "\"externalProfileRef\":" + request.ExternalProfileRefJson + ",";
            }

            if (request.ProfileMetadataJson != null)
            {
                body += "\"profileMetadata\":" + request.ProfileMetadataJson + ",";
            }

            body += "\"accountData\":" + request.AccountDataJson + ",";
            if (request.Character != null)
            {
                body += "\"character\":{\"metadata\":" + request.Character.MetadataJson + ",\"state\":" + request.Character.StateJson + "}";
            }
            else
            {
                body = body.TrimEnd(',');
            }

            body += "}";
            return body;
        }

        private static string BuildCreateProfileCharacterBody(PersistlyCreateProfileCharacterRequest request)
        {
            return "{" +
                "\"metadata\":" + request.CharacterMetadataJson + "," +
                "\"state\":" + request.CharacterStateJson +
                "}";
        }

        private static string BuildSyncProfileAccountDataBody(PersistlySyncProfileAccountDataRequest request)
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

            if (request.ClearMetadata)
            {
                body += ",\"metadata\":null";
            }
            else if (request.MetadataJson != null)
            {
                body += ",\"metadata\":" + request.MetadataJson;
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

        private static PersistlyCreateProfileResponse ParseCreateProfileResponse(string body)
        {
            var root = AsObject(PersistlyJson.ParseJsonValue(body, "create profile response"), "create profile response");
            var profileSaveId = GetRequiredString(root, "profileSaveId", "profile envelope");
            var profileSessionToken = GetOptionalString(root, "profileSessionToken");
            var profileRoot = GetRequiredObject(root, "profile", "profile envelope");
            PersistlySave? character = null;
            if (root.ContainsKey("character") && root["character"] != null)
            {
                character = ParseSave(GetRequiredObject(root, "character", "profile envelope"));
            }

            var policy = root.ContainsKey("syncPolicy")
                ? ParseSyncPolicy(GetRequiredObject(root, "syncPolicy", "profile envelope"))
                : new PersistlySyncPolicy(60, 10, true, true, true, 25);
            return new PersistlyCreateProfileResponse(profileSaveId, profileSessionToken, ParseSave(profileRoot), character, policy);
        }

        private static PersistlyProfileEnvelope ParseProfileEnvelope(string body)
        {
            var root = AsObject(PersistlyJson.ParseJsonValue(body, "profile envelope"), "profile envelope");
            return ParseProfileEnvelope(root);
        }

        private static PersistlyProfileEnvelope ParseProfileEnvelope(Dictionary<string, object?> profileRoot)
        {
            var profileSaveId = GetRequiredString(profileRoot, "profileSaveId", "profile envelope");
            var profileSessionToken = GetOptionalString(profileRoot, "profileSessionToken");
            var save = profileRoot.ContainsKey("save")
                ? GetRequiredObject(profileRoot, "save", "profile envelope")
                : GetRequiredObject(profileRoot, "profile", "profile envelope");
            var policy = profileRoot.ContainsKey("syncPolicy")
                ? ParseSyncPolicy(GetRequiredObject(profileRoot, "syncPolicy", "profile envelope"))
                : null;
            return new PersistlyProfileEnvelope(profileSaveId, profileSessionToken, ParseSave(save), policy);
        }

        private static PersistlyCharacterEnvelope ParseCharacterEnvelope(string body)
        {
            var root = AsObject(PersistlyJson.ParseJsonValue(body, "character envelope"), "character envelope");
            var characterRoot = root.ContainsKey("character") ? GetRequiredObject(root, "character", "character envelope") : root;
            return ParseCharacterEnvelope(characterRoot);
        }

        private static PersistlyCharacterEnvelope ParseCharacterEnvelope(Dictionary<string, object?> characterRoot)
        {
            var save = GetRequiredObject(characterRoot, "save", "character envelope");
            return new PersistlyCharacterEnvelope(ParseSave(save));
        }

        private static PersistlyRuntimeConfig ParseRuntimeConfig(string body)
        {
            var root = AsObject(PersistlyJson.ParseJsonValue(body, "runtime config"), "runtime config");
            var policy = GetRequiredObject(root, "syncPolicy", "runtime config");
            return new PersistlyRuntimeConfig(ParseSyncPolicy(policy));
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
                    save.MetadataJson,
                    save.StateJson,
                    payload.Version,
                    save.CreatedAt,
                    payload.UpdatedAt),
                historyRetained: payload.HistoryRetained,
                warnings: payload.Warnings);
        }

        private static PersistlySave SynthesizeSave(string saveId, PersistlySave? cachedSave, string? metadataJson, string stateJson)
        {
            return new PersistlySave(
                saveId,
                cachedSave?.PlayerRef,
                metadataJson ?? cachedSave?.MetadataJson ?? "{}",
                stateJson,
                1,
                cachedSave?.CreatedAt ?? DateTimeOffset.FromUnixTimeSeconds(0),
                DateTimeOffset.FromUnixTimeSeconds(0));
        }

        private static PersistlySave SynthesizeProfileSave(
            string profileSaveId,
            PersistlySave? cachedSave,
            PersistlySyncProfileAccountDataRequest request)
        {
            var cachedState = cachedSave == null
                ? new Dictionary<string, object?>()
                : AsObject(PersistlyJson.ParseJsonValue(cachedSave.StateJson, "cached profile state"), "cached profile state");
            var accountData = request.AccountDataJson != null
                ? AsObject(PersistlyJson.ParseJsonValue(request.AccountDataJson, "accountData"), "accountData")
                : MergeObjects(
                    cachedState.ContainsKey("accountData") && cachedState["accountData"] is Dictionary<string, object?> existingAccountData
                        ? existingAccountData
                        : new Dictionary<string, object?>(),
                    request.AccountDataPatchJson == null
                        ? new Dictionary<string, object?>()
                        : AsObject(PersistlyJson.ParseJsonValue(request.AccountDataPatchJson, "accountDataPatch"), "accountDataPatch"));
            var characterSlots = cachedState.ContainsKey("characterSlots") && cachedState["characterSlots"] is List<object?> existingSlots
                ? existingSlots
                : new List<object?>();
            var stateJson = PersistlyJson.Serialize(new Dictionary<string, object?>
            {
                ["schema"] = "persistly.profile.v1",
                ["accountData"] = accountData,
                ["characterSlots"] = characterSlots,
            });

            return new PersistlySave(
                profileSaveId,
                cachedSave?.PlayerRef,
                request.ClearMetadata ? "{}" : request.MetadataJson ?? cachedSave?.MetadataJson ?? "{}",
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

            var save = GetRequiredObject(root, "save", "sync response");
            var details = GetRequiredObject(root, "details", "sync response");
            var reason = GetRequiredString(details, "reason", "sync response details");
            if (!string.Equals(reason, "base_version_mismatch", StringComparison.Ordinal))
            {
                throw new PersistlyConfigurationError("Conflict sync response had an unexpected reason.");
            }

            return new PersistlySyncResponse(
                PersistlySyncStatus.Conflict,
                ParseSave(save),
                new PersistlySyncConflictDetails(PersistlySyncConflictReason.BaseVersionMismatch));
        }

        private static PersistlySave ParseSave(Dictionary<string, object?> saveObject)
        {
            var saveId = GetRequiredString(saveObject, "saveId", "save");
            var playerRef = GetOptionalString(saveObject, "playerRef");
            var metadata = GetRequiredObject(saveObject, "metadata", "save");
            var state = GetRequiredObject(saveObject, "state", "save");
            var version = GetRequiredInt(saveObject, "version", "save");
            var createdAt = GetRequiredDateTimeOffset(saveObject, "createdAt", "save");
            var updatedAt = GetRequiredDateTimeOffset(saveObject, "updatedAt", "save");

            return new PersistlySave(
                saveId,
                playerRef,
                PersistlyJson.Serialize(metadata),
                PersistlyJson.Serialize(state),
                version,
                createdAt,
                updatedAt);
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
                        string? slotKey = null;
                        Dictionary<string, object?>? details;
                        if (TryGetObject(error, "details", out details) && details != null)
                        {
                            slotKey = GetOptionalString(details, "slotKey");
                        }

                        return new PersistlySlotAlreadyExistsError(statusCode, message, slotKey, detailsJson);
                    }

                    if (code == PersistlyErrorCode.CharacterArchived)
                    {
                        string? characterSaveId = null;
                        Dictionary<string, object?>? details;
                        if (TryGetObject(error, "details", out details) && details != null)
                        {
                            characterSaveId = GetOptionalString(details, "characterSaveId");
                        }

                        return new PersistlyArchivedCharacterError(statusCode, message, characterSaveId, detailsJson);
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
                case PersistlyErrorCode.CharacterArchived:
                    return new PersistlyArchivedCharacterError(statusCode, message, null, detailsJson);
                case PersistlyErrorCode.RateLimited:
                    return new PersistlyRateLimitedError(statusCode, message, detailsJson);
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
                case "character_archived":
                    return PersistlyErrorCode.CharacterArchived;
                case "rate_limited":
                    return PersistlyErrorCode.RateLimited;
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

        private static void EnsureSessionToken(string profileSessionToken)
        {
            if (string.IsNullOrWhiteSpace(profileSessionToken))
            {
                throw new PersistlyConfigurationError("profileSessionToken must be set.");
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
    }
}
