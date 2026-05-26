using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Persistly.Unity;

namespace Persistly.Unity.LastBeacon.Tests
{
    public sealed class PersistlyClientTests
    {
        [Test]
        public async Task CreateProfileSupportsProfileOnlyCreationAndParsesCharacterSlots()
        {
            var transport = new RecordingTransport(
                201,
                "{\"profileSaveId\":\"sv_profile\",\"profileSessionToken\":\"pst_profile_session\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{\"displayName\":\"Ayla\"},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{\"diamonds\":20},\"characterSlots\":[]},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":10,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}");
            var client = BuildClient(transport);

            var result = await client.CreateProfileAsync(new PersistlyCreateProfileRequest(
                "{\"accountData\":{\"diamonds\":20}}",
                profileMetadataJson: "{\"displayName\":\"Ayla\"}",
                playerRef: "player-184",
                externalProfileRefJson: "{\"provider\":\"auth0\",\"subject\":\"auth0|123\"}"));

            Assert.That(result.ProfileSaveId, Is.EqualTo("sv_profile"));
            Assert.That(result.ProfileSessionToken, Is.EqualTo("pst_profile_session"));
            Assert.That(result.Profile.StateJson, Does.Contain("\"characterSlots\":[]"));
            Assert.That(result.Character, Is.Null);
            Assert.That(result.SyncPolicy.MinRemoteSyncIntervalSeconds, Is.EqualTo(60));
            Assert.That(transport.LastRequest.Body, Does.Not.Contain("\"character\""));
            Assert.That(transport.LastRequest.Body, Does.Contain("\"externalProfileRef\""));
            Assert.That(client.TryGetLocal("sv_profile", out var cachedProfile), Is.True);
            Assert.That(cachedProfile.StateJson, Does.Contain("\"diamonds\":20"));
        }

        [Test]
        public async Task CreateProfileWithInitialCharacterNestsSlotMetadata()
        {
            var transport = new RecordingTransport(
                201,
                "{\"profileSaveId\":\"sv_profile\",\"profileSessionToken\":\"pst_profile_session\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[{\"slotKey\":\"autosave\",\"characterSaveId\":\"sv_char\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"},\"name\":\"Ayla\"}}]},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"character\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"},\"name\":\"Ayla\"},\"state\":{\"level\":1,\"gold\":100},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":10,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}");
            var client = BuildClient(transport);

            var result = await client.CreateProfileAsync(new PersistlyCreateProfileRequest(
                "{\"accountData\":{}}",
                character: new PersistlyCreateProfileInitialCharacterRequest(
                    "autosave",
                    "{\"name\":\"Ayla\"}",
                    "{\"level\":1,\"gold\":100}"),
                playerRef: "player-184"));

            Assert.That(result.Character, Is.Not.Null);
            Assert.That(result.Character.SaveId, Is.EqualTo("sv_char"));
            Assert.That(transport.LastRequest.Body, Does.Contain("\"character\""));
            Assert.That(transport.LastRequest.Body, Does.Contain("\"_persistly\":{\"slotKey\":\"autosave\"}"));
            Assert.That(client.TryGetLocal("sv_char", out var cachedCharacter), Is.True);
            Assert.That(cachedCharacter.StateJson, Does.Contain("\"level\":1"));
        }

        [Test]
        public async Task ProfileCharacterRequestsSendSessionHeader()
        {
            var transport = new RecordingTransport(
                200,
                "{\"save\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"slot\":1},\"state\":{\"level\":2},\"version\":2,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:01:00Z\"}}");
            var client = BuildClient(transport);

            var loaded = await client.LoadProfileCharacterAsync("sv_profile", "pst_profile_session", "sv_char");

            Assert.That(loaded.SaveId, Is.EqualTo("sv_char"));
            Assert.That(transport.LastRequest.Url, Does.EndWith("/api/v1/profiles/sv_profile/characters/sv_char"));
            Assert.That(transport.LastRequest.Headers["X-Persistly-Profile-Session"], Is.EqualTo("pst_profile_session"));
            Assert.That(transport.LastRequest.Headers["X-Persistly-SDK"], Is.EqualTo("unity"));
            Assert.That(transport.LastRequest.Headers["X-Persistly-SDK-Version"], Is.EqualTo("1.0.0"));
            Assert.That(transport.LastRequest.Headers["X-Persistly-Platform"], Is.EqualTo("unity"));
        }

        [Test]
        public async Task SyncProfileAccountDataUsesExplicitRouteAndCachesProfile()
        {
            var transport = new RecordingTransport(
                200,
                "{\"status\":\"accepted\",\"save\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{\"label\":\"Main\"},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{\"diamonds\":30},\"characterSlots\":[]},\"version\":2,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:02:00Z\"}}");
            var client = BuildClient(transport);

            var result = await client.SyncProfileAccountDataAsync(
                "sv_profile",
                "pst_profile_session",
                new PersistlySyncProfileAccountDataRequest(1, accountDataPatchJson: "{\"diamonds\":30}"));

            Assert.That(result.Status, Is.EqualTo(PersistlySyncStatus.Accepted));
            Assert.That(result.Save.SaveId, Is.EqualTo("sv_profile"));
            Assert.That(transport.LastRequest.Url, Does.EndWith("/api/v1/profiles/sv_profile/account-data/sync"));
            Assert.That(transport.LastRequest.Body, Does.Contain("\"accountDataPatch\":{\"diamonds\":30}"));
            Assert.That(client.TryGetLocal("sv_profile", out var cached), Is.True);
            Assert.That(cached.Version, Is.EqualTo(2));
        }

        [Test]
        public async Task LightweightProfileAccountDataPatchDeletesNullKeysInSynthesizedCache()
        {
            var cache = new InMemoryPersistlySaveCache();
            cache.Store(new PersistlySave(
                "sv_profile",
                "player-184",
                "{}",
                "{\"schema\":\"persistly.profile.v1\",\"accountData\":{\"diamonds\":20,\"oldKey\":\"remove-me\"},\"characterSlots\":[]}",
                1,
                System.DateTimeOffset.Parse("2026-04-10T00:00:00Z"),
                System.DateTimeOffset.Parse("2026-04-10T00:00:00Z")));
            var transport = new RecordingTransport(
                200,
                "{\"status\":\"accepted\",\"version\":2,\"updatedAt\":\"2026-04-10T00:02:00Z\",\"historyRetained\":true}");
            var client = new PersistlyClient(new PersistlyClientOptions("http://127.0.0.1:8080", "ps_test_example")
            {
                Transport = transport,
                Cache = cache,
            });

            var result = await client.SyncProfileAccountDataAsync(
                "sv_profile",
                "pst_profile_session",
                new PersistlySyncProfileAccountDataRequest(1, accountDataPatchJson: "{\"diamonds\":30,\"oldKey\":null}"));

            Assert.That(result.Status, Is.EqualTo(PersistlySyncStatus.Accepted));
            Assert.That(result.Save.StateJson, Does.Contain("\"diamonds\":30"));
            Assert.That(result.Save.StateJson, Does.Not.Contain("oldKey"));
            Assert.That(client.TryGetLocal("sv_profile", out var cached), Is.True);
            Assert.That(cached.StateJson, Does.Not.Contain("oldKey"));
        }

        [Test]
        public async Task ArchiveProfileCharacterUsesArchiveRouteAndCachesReturnedProfile()
        {
            var transport = new RecordingTransport(
                200,
                "{\"profileSaveId\":\"sv_profile\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[{\"slotKey\":\"autosave\",\"characterSaveId\":\"sv_char\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"}},\"archived\":true,\"archivedAt\":\"2026-04-10T00:03:00Z\"}]},\"version\":3,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:03:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":10,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}");
            var client = BuildClient(transport);

            var result = await client.ArchiveProfileCharacterAsync("sv_profile", "pst_profile_session", "sv_char");

            Assert.That(result.ProfileSaveId, Is.EqualTo("sv_profile"));
            Assert.That(result.Profile.StateJson, Does.Contain("\"archived\":true"));
            Assert.That(transport.LastRequest.Url, Does.EndWith("/api/v1/profiles/sv_profile/characters/sv_char/archive"));
            Assert.That(client.TryGetLocal("sv_profile", out var cached), Is.True);
            Assert.That(cached.Version, Is.EqualTo(3));
        }

        [Test]
        public async Task DeleteProfileCharacterUsesDeleteRouteClearsCharacterCacheAndUpdatesProfileCache()
        {
            var cache = new InMemoryPersistlySaveCache();
            cache.Store(new PersistlySave(
                "sv_char",
                "player-184",
                "{\"_persistly\":{\"slotKey\":\"autosave\"}}",
                "{\"level\":1}",
                1,
                System.DateTimeOffset.Parse("2026-04-10T00:00:00Z"),
                System.DateTimeOffset.Parse("2026-04-10T00:00:00Z")));
            var transport = new RecordingTransport(
                200,
                "{\"profileSaveId\":\"sv_profile\",\"characterSaveId\":\"sv_char\",\"slotKey\":\"autosave\",\"deletedAt\":\"2026-04-10T00:04:00Z\",\"alreadyDeleted\":false,\"cleanupQueued\":true,\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[]},\"version\":4,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"}}");
            var client = new PersistlyClient(new PersistlyClientOptions("http://127.0.0.1:8080", "ps_test_example")
            {
                Transport = transport,
                Cache = cache,
            });

            var result = await client.DeleteProfileCharacterAsync("sv_profile", "pst_profile_session", "sv_char");

            Assert.That(result.CharacterSaveId, Is.EqualTo("sv_char"));
            Assert.That(result.CleanupQueued, Is.True);
            Assert.That(transport.LastRequest.Url, Does.EndWith("/api/v1/profiles/sv_profile/characters/sv_char"));
            Assert.That(client.TryGetLocal("sv_char", out _), Is.False);
            Assert.That(client.TryGetLocal("sv_profile", out var cachedProfile), Is.True);
            Assert.That(cachedProfile.Version, Is.EqualTo(4));
        }

        [Test]
        public async Task DeleteProfileUsesDeleteRouteAndClearsProfileCache()
        {
            var cache = new InMemoryPersistlySaveCache();
            cache.Store(new PersistlySave(
                "sv_profile",
                "player-184",
                "{}",
                "{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[]}",
                2,
                System.DateTimeOffset.Parse("2026-04-10T00:00:00Z"),
                System.DateTimeOffset.Parse("2026-04-10T00:02:00Z")));
            var transport = new RecordingTransport(
                200,
                "{\"profileSaveId\":\"sv_profile\",\"deletedAt\":\"2026-04-10T00:05:00Z\",\"deletedCharacterCount\":2,\"alreadyDeleted\":false,\"cleanupQueued\":true}");
            var client = new PersistlyClient(new PersistlyClientOptions("http://127.0.0.1:8080", "ps_test_example")
            {
                Transport = transport,
                Cache = cache,
            });

            var result = await client.DeleteProfileAsync("sv_profile", "pst_profile_session");

            Assert.That(result.DeletedCharacterCount, Is.EqualTo(2));
            Assert.That(result.CleanupQueued, Is.True);
            Assert.That(transport.LastRequest.Url, Does.EndWith("/api/v1/profiles/sv_profile"));
            Assert.That(transport.LastRequest.Method, Is.EqualTo("DELETE"));
            Assert.That(client.TryGetLocal("sv_profile", out _), Is.False);
        }

        [Test]
        public async Task SyncProfileCharacterReturnsConflictAndCachesCanonicalSave()
        {
            var transport = new StubTransport(
                409,
                "{\"status\":\"conflict\",\"save\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"slot\":1},\"state\":{\"level\":3},\"version\":7,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:08:00Z\"},\"details\":{\"reason\":\"base_version_mismatch\"}}");
            var client = BuildClient(transport);

            var result = await client.SyncProfileCharacterAsync("sv_profile", "pst_profile_session", "sv_char", new PersistlySyncSaveRequest("{\"level\":2}", 6, "{\"slot\":1}"));

            Assert.That(result.Status, Is.EqualTo(PersistlySyncStatus.Conflict));
            Assert.That(result.Save.Version, Is.EqualTo(7));
            Assert.That(client.TryGetLocal("sv_char", out var cached), Is.True);
            Assert.That(cached.StateJson, Does.Contain("\"level\":3"));
        }

        [Test]
        public void ContractErrorsUseTypedErrors()
        {
            Assert.That(
                PersistlyClient.ParseErrorForTests(409, "{\"error\":{\"code\":\"slot_already_exists\",\"message\":\"Duplicate slot.\",\"details\":{\"slotKey\":\"autosave\"}}}"),
                Is.TypeOf<PersistlySlotAlreadyExistsError>());
            Assert.That(
                PersistlyClient.ParseErrorForTests(409, "{\"error\":{\"code\":\"character_archived\",\"message\":\"Archived.\",\"details\":{\"characterSaveId\":\"sv_char\"}}}"),
                Is.TypeOf<PersistlyArchivedCharacterError>());
            Assert.That(
                PersistlyClient.ParseErrorForTests(410, "{\"error\":{\"code\":\"profile_deleted\",\"message\":\"Profile was deleted.\",\"details\":{\"profileSaveId\":\"sv_profile\"}}}"),
                Is.TypeOf<PersistlyProfileDeletedError>());
            Assert.That(
                PersistlyClient.ParseErrorForTests(410, "{\"error\":{\"code\":\"character_deleted\",\"message\":\"Character was deleted.\",\"details\":{\"characterSaveId\":\"sv_char\"}}}"),
                Is.TypeOf<PersistlyCharacterDeletedError>());
            Assert.That(
                PersistlyClient.ParseErrorForTests(402, "{\"error\":{\"code\":\"monthly_quota_exceeded\",\"message\":\"Monthly runtime request quota exceeded.\",\"details\":{\"planTier\":\"free\",\"used\":100000,\"limit\":100000}}}"),
                Is.TypeOf<PersistlyMonthlyQuotaExceededError>());
        }

        [Test]
        public async Task GetRuntimeConfigParsesSyncPolicy()
        {
            var transport = new RecordingTransport(
                200,
                "{\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":10,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25},\"gameConfig\":{\"enabled\":true,\"version\":3,\"sizeBytes\":37,\"data\":{\"season\":\"spring\",\"eventName\":\"launch\"}}}");
            var client = BuildClient(transport);

            var config = await client.GetRuntimeConfigAsync(gameConfigVersion: 2);

            Assert.That(transport.LastRequest.Url, Does.EndWith("/api/v1/runtime-config?gameConfigVersion=2"));
            Assert.That(config.SyncPolicy.MinRemoteSyncIntervalSeconds, Is.EqualTo(60));
            Assert.That(config.SyncPolicy.ForceSyncCooldownSeconds, Is.EqualTo(10));
            Assert.That(config.SyncPolicy.SyncOnBackground, Is.True);
            Assert.That(config.SyncPolicy.MaxQueuedLocalSnapshots, Is.EqualTo(25));
            Assert.That(config.GameConfig, Is.Not.Null);
            Assert.That(config.GameConfig!.Enabled, Is.True);
            Assert.That(config.GameConfig.Version, Is.EqualTo(3));
            Assert.That(config.GameConfig.HasData, Is.True);
            Assert.That(config.GameConfig.EventName, Is.EqualTo("launch"));
            Assert.That(config.GameConfig.ConfigJson, Does.Contain("\"season\":\"spring\""));
        }

        [Test]
        public async Task AutosaveManagerStoresDraftLocallyAndHonorsForceSyncCooldown()
        {
            var store = new InMemoryPersistlyAutosaveDraftStore();
            var syncCount = 0;
            var manager = new PersistlyAutosaveManager(
                store,
                new PersistlySyncPolicy(60, 10, true, true, true, 25),
                (snapshot, force, cancellationToken) =>
                {
                    syncCount += 1;
                    return Task.FromResult(new PersistlyAutosaveSyncResult(true, 2));
                });

            await manager.RecordLocalChangeAsync("sv_profile", "pst_profile_session", "sv_char", "{\"slot\":1}", "{\"level\":2}");
            Assert.That(store.TryLoad("sv_char", out var draft), Is.True);
            Assert.That(draft.StateJson, Does.Contain("\"level\":2"));

            var first = await manager.ForceSyncAsync("sv_char");
            var second = await manager.ForceSyncAsync("sv_char");

            Assert.That(first.SyncedRemotely, Is.True);
            Assert.That(second.SkippedReason, Is.EqualTo(PersistlyAutosaveSkippedReason.ForceSyncCooldown));
            Assert.That(syncCount, Is.EqualTo(1));
        }

        [Test]
        public async Task CreateSaveStoresCanonicalPayloadInCache()
        {
            var transport = new StubTransport(201, "{\"save\":{\"saveId\":\"sv_01\",\"playerRef\":\"player-184\",\"metadata\":{\"characterName\":\"Ayla\"},\"state\":{\"Scrap\":12,\"Workers\":1},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"}}");
            var client = BuildClient(transport);

            var created = await client.CreateSaveAsync(new PersistlyCreateSaveRequest("{\"Scrap\":12,\"Workers\":1}", "{\"characterName\":\"Ayla\"}", "player-184"));

            Assert.That(created.SaveId, Is.EqualTo("sv_01"));
            Assert.That(created.Version, Is.EqualTo(1));
            Assert.That(created.MetadataJson, Does.Contain("Ayla"));
            Assert.That(client.TryGetLocal("sv_01", out var cached), Is.True);
            Assert.That(cached.StateJson, Does.Contain("\"Scrap\":12"));
        }

        [Test]
        public async Task ConflictSyncReturnsCanonicalRemoteSave()
        {
            var transport = new StubTransport(
                409,
                "{\"status\":\"conflict\",\"save\":{\"saveId\":\"sv_01\",\"playerRef\":\"player-184\",\"metadata\":{\"characterName\":\"Ayla\"},\"state\":{\"Scrap\":77,\"Workers\":3},\"version\":5,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"},\"details\":{\"reason\":\"base_version_mismatch\"}}");
            var client = BuildClient(transport);

            var result = await client.SyncSaveAsync("sv_01", new PersistlySyncSaveRequest("{\"Scrap\":14}", 4, "{\"characterName\":\"Ayla\"}"));

            Assert.That(result.Status, Is.EqualTo(PersistlySyncStatus.Conflict));
            Assert.That(result.Save.Version, Is.EqualTo(5));
            Assert.That(result.Save.StateJson, Does.Contain("\"Scrap\":77"));
            Assert.That(client.TryGetLocal("sv_01", out var cached), Is.True);
            Assert.That(cached.Version, Is.EqualTo(5));
        }

        private static PersistlyClient BuildClient(IPersistlyTransport transport)
        {
            return new PersistlyClient(new PersistlyClientOptions("http://127.0.0.1:8080", "ps_test_example")
            {
                Transport = transport,
            });
        }

        private sealed class StubTransport : IPersistlyTransport
        {
            private readonly PersistlyTransportResponse _response;

            public StubTransport(int statusCode, string body)
            {
                _response = new PersistlyTransportResponse(statusCode, body);
            }

            public Task<PersistlyTransportResponse> SendAsync(PersistlyTransportRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_response);
            }
        }

        private sealed class RecordingTransport : IPersistlyTransport
        {
            private readonly PersistlyTransportResponse _response;

            public RecordingTransport(int statusCode, string body)
            {
                _response = new PersistlyTransportResponse(statusCode, body);
            }

            public PersistlyTransportRequest LastRequest { get; private set; }

            public Task<PersistlyTransportResponse> SendAsync(PersistlyTransportRequest request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(_response);
            }
        }
    }
}
