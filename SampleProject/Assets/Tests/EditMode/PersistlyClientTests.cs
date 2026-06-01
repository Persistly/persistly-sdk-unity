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
        public async Task CreateAccountSupportsAccountOnlyCreationAndParsesSlotSlots()
        {
            var transport = new RecordingTransport(
                201,
                "{\"accountId\":\"acc_account\",\"accountSessionToken\":\"pst_account_session\",\"account\":{\"accountId\":\"acc_account\",\"accountData\":{\"diamonds\":20},\"slots\":[],\"version\":1,\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":10,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}");
            var client = BuildClient(transport);

            var result = await client.CreateAccountAsync(new PersistlyCreateAccountRequest(
                "{\"diamonds\":20}",
                playerRef: "player-184",
                externalAccountRefJson: "{\"provider\":\"auth0\",\"subject\":\"auth0|123\"}"));

            Assert.That(result.AccountId, Is.EqualTo("acc_account"));
            Assert.That(result.AccountSessionToken, Is.EqualTo("pst_account_session"));
            Assert.That(result.Account.StateJson, Does.Contain("\"slots\":[]"));
            Assert.That(result.Slot, Is.Null);
            Assert.That(result.SyncPolicy.MinRemoteSyncIntervalSeconds, Is.EqualTo(60));
            Assert.That(transport.LastRequest.Body, Does.Not.Contain("\"slot\""));
            Assert.That(transport.LastRequest.Body, Does.Contain("\"externalAccountRef\""));
            Assert.That(client.TryGetLocal("acc_account", out var cachedAccount), Is.True);
            Assert.That(cachedAccount.StateJson, Does.Contain("\"diamonds\":20"));
        }

        [Test]
        public async Task CreateAccountWithInitialSlotNestsSlotSlotInfo()
        {
            var transport = new RecordingTransport(
                201,
                "{\"accountId\":\"acc_account\",\"accountSessionToken\":\"pst_account_session\",\"account\":{\"accountId\":\"acc_account\",\"accountData\":{},\"slots\":[{\"slotId\":\"autosave\",\"slotInfo\":{\"name\":\"Ayla\"},\"version\":1,\"updatedAt\":\"2026-04-10T00:00:00Z\"}],\"version\":1,\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"slot\":{\"slotId\":\"autosave\",\"slotInfo\":{\"name\":\"Ayla\"},\"data\":{\"level\":1,\"gold\":100},\"version\":1,\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":10,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}");
            var client = BuildClient(transport);

            var result = await client.CreateAccountAsync(new PersistlyCreateAccountRequest(
                "{}",
                slot: new PersistlyCreateAccountInitialSlotRequest(
                    "autosave",
                    "{\"name\":\"Ayla\"}",
                    "{\"level\":1,\"gold\":100}"),
                playerRef: "player-184"));

            Assert.That(result.Slot, Is.Not.Null);
            Assert.That(result.Slot.SaveId, Is.EqualTo("autosave"));
            Assert.That(transport.LastRequest.Body, Does.Contain("\"slot\""));
            Assert.That(transport.LastRequest.Body, Does.Contain("\"slotInfo\""));
            Assert.That(client.TryGetLocal("autosave", out var cachedSlot), Is.True);
            Assert.That(cachedSlot.StateJson, Does.Contain("\"level\":1"));
        }

        [Test]
        public async Task AccountSlotRequestsSendSessionHeader()
        {
            var transport = new RecordingTransport(
                200,
                "{\"slotId\":\"autosave\",\"slotInfo\":{\"slot\":1},\"data\":{\"level\":2},\"version\":2,\"updatedAt\":\"2026-04-10T00:01:00Z\"}");
            var client = BuildClient(transport);

            var loaded = await client.LoadAccountSlotAsync("acc_account", "pst_account_session", "autosave");

            Assert.That(loaded.SaveId, Is.EqualTo("autosave"));
            Assert.That(transport.LastRequest.Url, Does.EndWith("/api/v1/accounts/acc_account/slots/autosave"));
            Assert.That(transport.LastRequest.Headers["X-Persistly-Account-Session"], Is.EqualTo("pst_account_session"));
            Assert.That(transport.LastRequest.Headers["X-Persistly-SDK"], Is.EqualTo("unity"));
            Assert.That(transport.LastRequest.Headers["X-Persistly-SDK-Version"], Is.EqualTo("1.0.0"));
            Assert.That(transport.LastRequest.Headers["X-Persistly-Platform"], Is.EqualTo("unity"));
        }

        [Test]
        public async Task CreateTransferCodeUsesAccountRouteSessionHeaderAndBody()
        {
            var transport = new RecordingTransport(
                201,
                "{\"transferCode\":\"P7K2D-M9Q4R\",\"expiresAt\":\"2026-06-01T12:10:00Z\",\"expiresInSeconds\":600}");
            var client = BuildClient(transport);

            var result = await client.CreateTransferCodeAsync(
                "acc_account",
                "pst_account_session",
                deviceLabel: "Switch",
                ttlSeconds: 600);

            Assert.That(result.TransferCode, Is.EqualTo("P7K2D-M9Q4R"));
            Assert.That(result.ExpiresAt, Is.EqualTo("2026-06-01T12:10:00Z"));
            Assert.That(result.ExpiresInSeconds, Is.EqualTo(600));
            Assert.That(transport.LastRequest.Method, Is.EqualTo("POST"));
            Assert.That(transport.LastRequest.Url, Does.EndWith("/api/v1/accounts/acc_account/transfer-codes"));
            Assert.That(transport.LastRequest.Headers["X-Persistly-Account-Session"], Is.EqualTo("pst_account_session"));
            Assert.That(transport.LastRequest.Body, Does.Contain("\"deviceLabel\":\"Switch\""));
            Assert.That(transport.LastRequest.Body, Does.Contain("\"ttlSeconds\":600"));
            Assert.That(transport.LastRequest.Body, Does.Not.Contain("pst_account_session"));
        }

        [Test]
        public async Task ConsumeTransferCodeUsesTopLevelRouteAndCachesAccount()
        {
            var transport = new RecordingTransport(
                200,
                "{\"accountId\":\"acc_account\",\"accountSessionToken\":\"pst_new_session\",\"account\":{\"accountId\":\"acc_account\",\"accountData\":{\"diamonds\":20},\"slots\":[],\"version\":3,\"updatedAt\":\"2026-06-01T12:01:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":10,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}");
            var client = BuildClient(transport);

            var result = await client.ConsumeTransferCodeAsync("P7K2D-M9Q4R", deviceLabel: "Laptop");

            Assert.That(result.AccountId, Is.EqualTo("acc_account"));
            Assert.That(result.AccountSessionToken, Is.EqualTo("pst_new_session"));
            Assert.That(result.Account.StateJson, Does.Contain("\"diamonds\":20"));
            Assert.That(transport.LastRequest.Method, Is.EqualTo("POST"));
            Assert.That(transport.LastRequest.Url, Does.EndWith("/api/v1/account-transfer-codes/consume"));
            Assert.That(transport.LastRequest.Headers.ContainsKey("X-Persistly-Account-Session"), Is.False);
            Assert.That(transport.LastRequest.Body, Does.Contain("\"transferCode\":\"P7K2D-M9Q4R\""));
            Assert.That(transport.LastRequest.Body, Does.Contain("\"deviceLabel\":\"Laptop\""));
            Assert.That(transport.LastRequest.Body, Does.Not.Contain("pst_new_session"));
            Assert.That(client.TryGetLocal("acc_account", out var cached), Is.True);
            Assert.That(cached.Version, Is.EqualTo(3));
        }

        [Test]
        public async Task SyncAccountAccountDataUsesExplicitRouteAndCachesAccount()
        {
            var transport = new RecordingTransport(
                200,
                "{\"status\":\"accepted\",\"account\":{\"accountId\":\"acc_account\",\"accountData\":{\"diamonds\":30},\"slots\":[],\"version\":2,\"updatedAt\":\"2026-04-10T00:02:00Z\"}}");
            var client = BuildClient(transport);

            var result = await client.SyncAccountDataAsync(
                "acc_account",
                "pst_account_session",
                new PersistlySyncAccountDataRequest(1, accountDataPatchJson: "{\"diamonds\":30}"));

            Assert.That(result.Status, Is.EqualTo(PersistlySyncStatus.Accepted));
            Assert.That(result.Save.SaveId, Is.EqualTo("acc_account"));
            Assert.That(transport.LastRequest.Url, Does.EndWith("/api/v1/accounts/acc_account/data/sync"));
            Assert.That(transport.LastRequest.Body, Does.Contain("\"accountDataPatch\":{\"diamonds\":30}"));
            Assert.That(client.TryGetLocal("acc_account", out var cached), Is.True);
            Assert.That(cached.Version, Is.EqualTo(2));
        }

        [Test]
        public async Task LightweightAccountAccountDataPatchDeletesNullKeysInSynthesizedCache()
        {
            var cache = new InMemoryPersistlySaveCache();
            cache.Store(new PersistlySave(
                "acc_account",
                "player-184",
                "{}",
                "{\"schema\":\"persistly.account.v1\",\"accountData\":{\"diamonds\":20,\"oldKey\":\"remove-me\"},\"slots\":[]}",
                1,
                System.DateTimeOffset.Parse("2026-04-10T00:00:00Z"),
                System.DateTimeOffset.Parse("2026-04-10T00:00:00Z")));
            var transport = new RecordingTransport(
                200,
                "{\"status\":\"accepted\",\"version\":2,\"updatedAt\":\"2026-04-10T00:02:00Z\",\"historyRetained\":true}");
            var client = new PersistlyClient(new PersistlyClientOptions("ps_test_example")
            {
                Transport = transport,
                Cache = cache,
            });

            var result = await client.SyncAccountDataAsync(
                "acc_account",
                "pst_account_session",
                new PersistlySyncAccountDataRequest(1, accountDataPatchJson: "{\"diamonds\":30,\"oldKey\":null}"));

            Assert.That(result.Status, Is.EqualTo(PersistlySyncStatus.Accepted));
            Assert.That(result.Save.StateJson, Does.Contain("\"diamonds\":30"));
            Assert.That(result.Save.StateJson, Does.Not.Contain("oldKey"));
            Assert.That(client.TryGetLocal("acc_account", out var cached), Is.True);
            Assert.That(cached.StateJson, Does.Not.Contain("oldKey"));
        }

        [Test]
        public async Task ArchiveAccountSlotUsesArchiveRouteAndCachesReturnedAccount()
        {
            var transport = new RecordingTransport(
                200,
                "{\"accountId\":\"acc_account\",\"account\":{\"accountId\":\"acc_account\",\"accountData\":{},\"slots\":[{\"slotId\":\"autosave\",\"slotInfo\":{},\"archived\":true,\"archivedAt\":\"2026-04-10T00:03:00Z\"}],\"version\":3,\"updatedAt\":\"2026-04-10T00:03:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":10,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}");
            var client = BuildClient(transport);

            var result = await client.ArchiveSlotAsync("acc_account", "pst_account_session", "autosave");

            Assert.That(result.AccountId, Is.EqualTo("acc_account"));
            Assert.That(result.Account.StateJson, Does.Contain("\"archived\":true"));
            Assert.That(transport.LastRequest.Url, Does.EndWith("/api/v1/accounts/acc_account/slots/autosave/archive"));
            Assert.That(client.TryGetLocal("acc_account", out var cached), Is.True);
            Assert.That(cached.Version, Is.EqualTo(3));
        }

        [Test]
        public async Task DeleteAccountSlotUsesDeleteRouteClearsSlotCacheAndUpdatesAccountCache()
        {
            var cache = new InMemoryPersistlySaveCache();
            cache.Store(new PersistlySave(
                "autosave",
                "player-184",
                "{}",
                "{\"level\":1}",
                1,
                System.DateTimeOffset.Parse("2026-04-10T00:00:00Z"),
                System.DateTimeOffset.Parse("2026-04-10T00:00:00Z")));
            var transport = new RecordingTransport(
                200,
                "{\"accountId\":\"acc_account\",\"slotId\":\"autosave\",\"deletedAt\":\"2026-04-10T00:04:00Z\",\"alreadyDeleted\":false,\"cleanupQueued\":true,\"account\":{\"accountId\":\"acc_account\",\"accountData\":{},\"slots\":[],\"version\":4,\"updatedAt\":\"2026-04-10T00:04:00Z\"}}");
            var client = new PersistlyClient(new PersistlyClientOptions("ps_test_example")
            {
                Transport = transport,
                Cache = cache,
            });

            var result = await client.DeleteAccountSlotAsync("acc_account", "pst_account_session", "autosave");

            Assert.That(result.SlotId, Is.EqualTo("autosave"));
            Assert.That(result.CleanupQueued, Is.True);
            Assert.That(transport.LastRequest.Url, Does.EndWith("/api/v1/accounts/acc_account/slots/autosave"));
            Assert.That(client.TryGetLocal("autosave", out _), Is.False);
            Assert.That(client.TryGetLocal("acc_account", out var cachedAccount), Is.True);
            Assert.That(cachedAccount.Version, Is.EqualTo(4));
        }

        [Test]
        public async Task DeleteAccountUsesDeleteRouteAndClearsAccountCache()
        {
            var cache = new InMemoryPersistlySaveCache();
            cache.Store(new PersistlySave(
                "acc_account",
                "player-184",
                "{}",
                "{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[]}",
                2,
                System.DateTimeOffset.Parse("2026-04-10T00:00:00Z"),
                System.DateTimeOffset.Parse("2026-04-10T00:02:00Z")));
            var transport = new RecordingTransport(
                200,
                "{\"accountId\":\"acc_account\",\"deletedAt\":\"2026-04-10T00:05:00Z\",\"deletedSlotCount\":2,\"alreadyDeleted\":false,\"cleanupQueued\":true}");
            var client = new PersistlyClient(new PersistlyClientOptions("ps_test_example")
            {
                Transport = transport,
                Cache = cache,
            });

            var result = await client.DeleteAccountAsync("acc_account", "pst_account_session");

            Assert.That(result.DeletedSlotCount, Is.EqualTo(2));
            Assert.That(result.CleanupQueued, Is.True);
            Assert.That(transport.LastRequest.Url, Does.EndWith("/api/v1/accounts/acc_account"));
            Assert.That(transport.LastRequest.Method, Is.EqualTo("DELETE"));
            Assert.That(client.TryGetLocal("acc_account", out _), Is.False);
        }

        [Test]
        public async Task SyncAccountSlotReturnsConflictAndCachesCanonicalSave()
        {
            var transport = new StubTransport(
                409,
                "{\"status\":\"conflict\",\"slot\":{\"slotId\":\"autosave\",\"slotInfo\":{\"slot\":1},\"data\":{\"level\":3},\"version\":7,\"updatedAt\":\"2026-04-10T00:08:00Z\"},\"details\":{\"reason\":\"base_version_mismatch\"}}");
            var client = BuildClient(transport);

            var result = await client.SyncAccountSlotAsync("acc_account", "pst_account_session", "autosave", new PersistlySyncSaveRequest("{\"level\":2}", 6, "{\"slot\":1}"));

            Assert.That(result.Status, Is.EqualTo(PersistlySyncStatus.Conflict));
            Assert.That(result.Save.Version, Is.EqualTo(7));
            Assert.That(client.TryGetLocal("autosave", out var cached), Is.True);
            Assert.That(cached.StateJson, Does.Contain("\"level\":3"));
        }

        [Test]
        public void ContractErrorsUseTypedErrors()
        {
            Assert.That(
                PersistlyClient.ParseErrorForTests(409, "{\"error\":{\"code\":\"slot_already_exists\",\"message\":\"Duplicate slot.\",\"details\":{\"slotId\":\"autosave\"}}}"),
                Is.TypeOf<PersistlySlotAlreadyExistsError>());
            Assert.That(
                PersistlyClient.ParseErrorForTests(409, "{\"error\":{\"code\":\"slot_archived\",\"message\":\"Archived.\",\"details\":{\"slotId\":\"autosave\"}}}"),
                Is.TypeOf<PersistlySlotArchivedError>());
            Assert.That(
                PersistlyClient.ParseErrorForTests(410, "{\"error\":{\"code\":\"account_deleted\",\"message\":\"Account was deleted.\",\"details\":{\"accountId\":\"acc_account\"}}}"),
                Is.TypeOf<PersistlyAccountDeletedError>());
            Assert.That(
                PersistlyClient.ParseErrorForTests(410, "{\"error\":{\"code\":\"slot_deleted\",\"message\":\"Slot was deleted.\",\"details\":{\"slotId\":\"autosave\"}}}"),
                Is.TypeOf<PersistlySlotDeletedError>());
            Assert.That(
                PersistlyClient.ParseErrorForTests(402, "{\"error\":{\"code\":\"monthly_quota_exceeded\",\"message\":\"Monthly runtime request quota exceeded.\",\"details\":{\"planTier\":\"free\",\"used\":100000,\"limit\":100000}}}"),
                Is.TypeOf<PersistlyMonthlyQuotaExceededError>());
            Assert.That(
                PersistlyClient.ParseErrorForTests(400, "{\"error\":{\"code\":\"transfer_code_invalid\",\"message\":\"Transfer code is invalid.\"}}"),
                Is.TypeOf<PersistlyTransferCodeInvalidError>());
            Assert.That(
                PersistlyClient.ParseErrorForTests(410, "{\"error\":{\"code\":\"transfer_code_expired\",\"message\":\"Transfer code expired.\"}}"),
                Is.TypeOf<PersistlyTransferCodeExpiredError>());
            Assert.That(
                PersistlyClient.ParseErrorForTests(409, "{\"error\":{\"code\":\"transfer_code_consumed\",\"message\":\"Transfer code was already used.\"}}"),
                Is.TypeOf<PersistlyTransferCodeConsumedError>());
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

            await manager.RecordLocalChangeAsync("acc_account", "pst_account_session", "autosave", "{\"slot\":1}", "{\"level\":2}");
            Assert.That(store.TryLoad("autosave", out var draft), Is.True);
            Assert.That(draft.StateJson, Does.Contain("\"level\":2"));

            var first = await manager.ForceSyncAsync("autosave");
            var second = await manager.ForceSyncAsync("autosave");

            Assert.That(first.SyncedRemotely, Is.True);
            Assert.That(second.SkippedReason, Is.EqualTo(PersistlyAutosaveSkippedReason.ForceSyncCooldown));
            Assert.That(syncCount, Is.EqualTo(1));
        }

        [Test]
        public async Task CreateSaveStoresCanonicalPayloadInCache()
        {
            var transport = new StubTransport(201, "{\"save\":{\"saveId\":\"sv_01\",\"playerRef\":\"player-184\",\"slotInfo\":{\"slotName\":\"Ayla\"},\"state\":{\"Scrap\":12,\"Workers\":1},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"}}");
            var client = BuildClient(transport);

            var created = await client.CreateSaveAsync(new PersistlyCreateSaveRequest("{\"Scrap\":12,\"Workers\":1}", "{\"slotName\":\"Ayla\"}", "player-184"));

            Assert.That(created.SaveId, Is.EqualTo("sv_01"));
            Assert.That(created.Version, Is.EqualTo(1));
            Assert.That(created.SlotInfoJson, Does.Contain("Ayla"));
            Assert.That(client.TryGetLocal("sv_01", out var cached), Is.True);
            Assert.That(cached.StateJson, Does.Contain("\"Scrap\":12"));
        }

        [Test]
        public async Task ConflictSyncReturnsCanonicalRemoteSave()
        {
            var transport = new StubTransport(
                409,
                "{\"status\":\"conflict\",\"save\":{\"saveId\":\"sv_01\",\"playerRef\":\"player-184\",\"slotInfo\":{\"slotName\":\"Ayla\"},\"state\":{\"Scrap\":77,\"Workers\":3},\"version\":5,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"},\"details\":{\"reason\":\"base_version_mismatch\"}}");
            var client = BuildClient(transport);

            var result = await client.SyncSaveAsync("sv_01", new PersistlySyncSaveRequest("{\"Scrap\":14}", 4, "{\"slotName\":\"Ayla\"}"));

            Assert.That(result.Status, Is.EqualTo(PersistlySyncStatus.Conflict));
            Assert.That(result.Save.Version, Is.EqualTo(5));
            Assert.That(result.Save.StateJson, Does.Contain("\"Scrap\":77"));
            Assert.That(client.TryGetLocal("sv_01", out var cached), Is.True);
            Assert.That(cached.Version, Is.EqualTo(5));
        }

        private static PersistlyClient BuildClient(IPersistlyTransport transport)
        {
            return new PersistlyClient(new PersistlyClientOptions("ps_test_example")
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
