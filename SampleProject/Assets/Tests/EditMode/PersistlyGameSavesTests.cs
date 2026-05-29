using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Persistly.Unity;

namespace Persistly.Unity.LastBeacon.Tests
{
    public sealed class PersistlyGameSavesTests
    {
        [SetUp]
        public void ResetSharedFacade()
        {
            var field = typeof(PersistlyGameSaves).GetField("_shared", BindingFlags.NonPublic | BindingFlags.Static);
            field.SetValue(null, null);
        }

        [Test]
        public void GameSaveEnumsContainReleaseContractStatesAndTargets()
        {
            Assert.That(Enum.IsDefined(typeof(PersistlyGameSaveStatus), PersistlyGameSaveStatus.LocalSaved), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlyGameSaveStatus), PersistlyGameSaveStatus.LocalFound), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlyGameSaveStatus), PersistlyGameSaveStatus.NotFound), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlyGameSaveStatus), PersistlyGameSaveStatus.NoChanges), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlyGameSaveStatus), PersistlyGameSaveStatus.Cooldown), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlyGameSaveStatus), PersistlyGameSaveStatus.Synced), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlyGameSaveStatus), PersistlyGameSaveStatus.Conflict), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlyGameSaveStatus), PersistlyGameSaveStatus.Offline), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlyGameSaveStatus), PersistlyGameSaveStatus.RateLimited), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlyGameSaveTarget), PersistlyGameSaveTarget.Account), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlyGameSaveTarget), PersistlyGameSaveTarget.Slot), Is.True);
        }

        [Test]
        public void SharedBeforeConfigureThrowsNotConfigured()
        {
            var error = Assert.Throws<PersistlyConfigurationError>(() =>
            {
                _ = PersistlyGameSaves.Shared;
            });

            Assert.That(error.Message, Does.Contain("not_configured"));
        }

        [Test]
        public async Task ConfiguredFacadeSavesAndLoadsLocalState()
        {
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184"
            });

            var saved = await PersistlyGameSaves.Shared.SaveSlotAsync("slot-1", new TestSaveState
            {
                Level = 7,
                Gold = 120
            });
            var loaded = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("slot-1");

            Assert.That(saved.Status, Is.EqualTo(PersistlySlotStatus.LocalSaved));
            Assert.That(saved.SlotId, Is.EqualTo("slot-1"));
            Assert.That(loaded.Status, Is.EqualTo(PersistlySlotStatus.LocalFound));
            Assert.That(loaded.SlotId, Is.EqualTo("slot-1"));
            Assert.That(loaded.State.Level, Is.EqualTo(7));
            Assert.That(loaded.State.Gold, Is.EqualTo(120));
            Assert.That(loaded.Dirty, Is.True);
            Assert.That(loaded.SlotInfoJson, Is.EqualTo("{}"));
        }

        [Test]
        public async Task DataConvenienceMethodsUseDefaultAutosaveSlot()
        {
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184"
            });

            var saved = await PersistlyGameSaves.Shared.SaveDataAsync(new TestSaveState
            {
                Level = 4,
                Gold = 70
            }, new PersistlySaveSlotOptions
            {
                SlotInfoJson = "{\"name\":\"Ayla\"}"
            });
            var loaded = await PersistlyGameSaves.Shared.LoadDataAsync<TestSaveState>();
            var inspect = PersistlyGameSaves.Shared.InspectData();

            Assert.That(PersistlyGameSaves.DefaultSlotId, Is.EqualTo("autosave"));
            Assert.That(saved.Status, Is.EqualTo(PersistlySlotStatus.LocalSaved));
            Assert.That(saved.SlotId, Is.EqualTo("autosave"));
            Assert.That(loaded.Status, Is.EqualTo(PersistlySlotStatus.LocalFound));
            Assert.That(loaded.SlotId, Is.EqualTo("autosave"));
            Assert.That(loaded.State.Level, Is.EqualTo(4));
            Assert.That(loaded.State.Gold, Is.EqualTo(70));
            Assert.That(inspect.Exists, Is.True);
            Assert.That(inspect.SlotId, Is.EqualTo("autosave"));
            Assert.That(inspect.SlotInfoJson, Does.Contain("\"name\":\"Ayla\""));
        }

        [Test]
        public async Task LoadSlotReturnsTypedNotFoundInsteadOfThrowing()
        {
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184"
            });

            var result = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("missing");

            Assert.That(result.Status, Is.EqualTo(PersistlySlotStatus.NotFound));
            Assert.That(result.Found, Is.False);
            Assert.That(result.State, Is.Null);
        }

        [Test]
        public async Task AccountSessionTokenIsExplicitlyExportedOnlyWhenRequested()
        {
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                AccountId = "acc_account",
                AccountSessionToken = "pst_account_session"
            });

            var hidden = PersistlyGameSaves.Shared.GetAccountSession();
            var exported = PersistlyGameSaves.Shared.GetAccountSession(includeToken: true);

            Assert.That(hidden.AccountId, Is.EqualTo("acc_account"));
            Assert.That(hidden.AccountSessionToken, Is.Null);
            Assert.That(exported.AccountSessionToken, Is.EqualTo("pst_account_session"));
        }

        [Test]
        public async Task ExistingAccountSessionLoadsRemoteAccountBeforeUse()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"accountId\":\"acc_account\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{\"label\":\"Cloud\"},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{\"diamonds\":99},\"slots\":[]},\"version\":7,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":45,\"forceSyncCooldownSeconds\":8,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                AccountId = "acc_account",
                AccountSessionToken = "pst_account_session",
                Transport = transport
            });

            var ensured = await PersistlyGameSaves.Shared.EnsureAccountAsync();
            var account = PersistlyGameSaves.Shared.InspectAccount();

            Assert.That(ensured.Status, Is.EqualTo(PersistlyGameSaveStatus.Synced));
            Assert.That(transport.Requests[0].Url, Does.EndWith("/api/v1/accounts/acc_account"));
            Assert.That(account.AccountDataJson, Does.Contain("\"diamonds\":99"));
            Assert.That(account.SlotInfoJson, Does.Contain("\"label\":\"Cloud\""));
            Assert.That(account.Version, Is.EqualTo(7));
        }

        [Test]
        public async Task FacadeCreateAccountRequiresEmptyLocalState()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(201, "{\"accountId\":\"acc_account\",\"accountSessionToken\":\"pst_account_session\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[]},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                Transport = transport
            });

            var created = await PersistlyGameSaves.Shared.CreateAccountAsync();
            Assert.That(created.Status, Is.EqualTo(PersistlyGameSaveStatus.Synced));

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 });
            var error = Assert.ThrowsAsync<PersistlyConfigurationError>(() => PersistlyGameSaves.Shared.CreateAccountAsync());
            Assert.That(error.Message, Does.Contain("ClearLocalAccountAsync"));
        }

        [Test]
        public async Task AttachAccountRequiresEmptyLocalStateAndStoresRemoteAccountLocally()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"accountId\":\"acc_account\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{\"label\":\"Cloud\"},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{\"diamonds\":99},\"slots\":[]},\"version\":7,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":45,\"forceSyncCooldownSeconds\":8,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                Transport = transport
            });

            var attached = await PersistlyGameSaves.Shared.AttachAccountAsync("acc_account", "pst_account_session");
            var exported = PersistlyGameSaves.Shared.GetAccountSession(includeToken: true);
            var account = PersistlyGameSaves.Shared.InspectAccount();

            Assert.That(attached.Status, Is.EqualTo(PersistlyGameSaveStatus.Synced));
            Assert.That(exported.AccountId, Is.EqualTo("acc_account"));
            Assert.That(exported.AccountSessionToken, Is.EqualTo("pst_account_session"));
            Assert.That(account.Version, Is.EqualTo(7));

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 });
            var error = Assert.ThrowsAsync<PersistlyConfigurationError>(() => PersistlyGameSaves.Shared.AttachAccountAsync("acc_other", "pst_other"));
            Assert.That(error.Message, Does.Contain("ClearLocalAccountAsync"));
        }

        [Test]
        public async Task AccountDataHelpersAreLocalFirstAndPatchTopLevelKeys()
        {
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184"
            });

            var saved = await PersistlyGameSaves.Shared.SaveAccountDataAsync(new AccountState
            {
                Diamonds = 10,
                UnlockedSlots = 2
            });
            var patched = await PersistlyGameSaves.Shared.PatchAccountDataAsync("{\"diamonds\":25,\"unlockedSlots\":null}");
            var account = PersistlyGameSaves.Shared.InspectAccount();

            Assert.That(saved.Status, Is.EqualTo(PersistlyGameSaveStatus.LocalSaved));
            Assert.That(patched.Status, Is.EqualTo(PersistlyGameSaveStatus.LocalSaved));
            Assert.That(account.AccountDataJson, Does.Contain("\"diamonds\":25"));
            Assert.That(PersistlyGameSaves.Shared.GetAccountDataJson(), Does.Contain("\"diamonds\":25"));
            Assert.That(account.AccountDataJson, Does.Not.Contain("unlockedSlots"));
        }

        [Test]
        public async Task ListAndInspectSlotsExposeLocalCloudAndDirtyStateSeparately()
        {
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184"
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("slot-1", new TestSaveState
            {
                Level = 3,
                Gold = 40
            }, new PersistlySaveSlotOptions
            {
                SlotInfoJson = "{\"name\":\"Ayla\"}"
            });

            var slots = PersistlyGameSaves.Shared.ListSlotDataAsync();
            var inspect = PersistlyGameSaves.Shared.InspectSlot("slot-1");

            Assert.That(slots.Count, Is.EqualTo(1));
            Assert.That(inspect.Exists, Is.True);
            Assert.That(inspect.Dirty, Is.True);
            Assert.That(inspect.StateJson, Does.Contain("\"Level\":3"));
            Assert.That(inspect.CloudStateJson, Is.Null);
        }

        [Test]
        public async Task RefreshSlotPullsRemoteStateAfterAttach()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"accountId\":\"acc_account\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[{\"slotId\":\"autosave\",\"slotInfo\":{\"name\":\"Cloud\"}}]},\"version\":4,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(200, "{\"save\":{\"saveId\":\"autosave\",\"playerRef\":\"player-184\",\"slotInfo\":{\"name\":\"Cloud\"},\"state\":{\"Level\":9,\"Gold\":999},\"version\":5,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                AccountId = "acc_account",
                AccountSessionToken = "pst_account_session",
                Transport = transport
            });

            var result = await PersistlyGameSaves.Shared.RefreshSlotAsync("autosave");
            var loaded = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("autosave");

            Assert.That(result.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(loaded.Found, Is.True);
            Assert.That(loaded.State.Level, Is.EqualTo(9));
            Assert.That(loaded.State.Gold, Is.EqualTo(999));
            Assert.That(loaded.Dirty, Is.False);
            Assert.That(loaded.SlotInfoJson, Does.Contain("\"name\":\"Cloud\""));
            Assert.That(transport.Requests[1].Url, Does.EndWith("/api/v1/accounts/acc_account/slots/autosave"));
        }

        [Test]
        public async Task RefreshSlotKeepsDirtyLocalStateAndStoresCloudConflict()
        {
            PersistlySyncNotification callback = null;
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"accountId\":\"acc_account\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[{\"slotId\":\"autosave\",\"slotInfo\":{\"name\":\"Cloud\"}}]},\"version\":4,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(200, "{\"save\":{\"saveId\":\"autosave\",\"playerRef\":\"player-184\",\"slotInfo\":{\"name\":\"Cloud\"},\"state\":{\"Level\":9,\"Gold\":999},\"version\":5,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                AccountId = "acc_account",
                AccountSessionToken = "pst_account_session",
                Transport = transport,
                OnSyncResult = notification => callback = notification
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 2, Gold = 20 });

            var result = await PersistlyGameSaves.Shared.RefreshSlotAsync("autosave");
            var inspect = PersistlyGameSaves.Shared.InspectSlot("autosave");

            Assert.That(result.Status, Is.EqualTo(PersistlySlotStatus.Conflict));
            Assert.That(inspect.Dirty, Is.True);
            Assert.That(inspect.StateJson, Does.Contain("\"Level\":2"));
            Assert.That(inspect.CloudStateJson, Does.Contain("\"Level\":9"));
            Assert.That(result.Conflict.LocalStateJson, Does.Contain("\"Level\":2"));
            Assert.That(result.Conflict.CloudStateJson, Does.Contain("\"Level\":9"));
            Assert.That(callback.Status, Is.EqualTo(PersistlyGameSaveStatus.Conflict));
        }

        [Test]
        public async Task ExistingSlotSyncAddsReservedSlotSlotInfoWithoutPersistingItLocally()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"accountId\":\"acc_account\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[{\"slotId\":\"autosave\",\"slotInfo\":{}}]},\"version\":4,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(200, "{\"status\":\"accepted\",\"save\":{\"saveId\":\"autosave\",\"playerRef\":\"player-184\",\"slotInfo\":{\"name\":\"Ayla\"},\"state\":{\"Level\":2,\"Gold\":20},\"version\":5,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                AccountId = "acc_account",
                AccountSessionToken = "pst_account_session",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 2, Gold = 20 }, new PersistlySaveSlotOptions { SlotInfoJson = "{\"name\":\"Ayla\"}" });
            PersistlyGameSaves.Shared.AttachSlotForTests("autosave", 4);

            var result = await PersistlyGameSaves.Shared.ForceSyncAsync("autosave", new PersistlySyncOptions { BypassCooldown = true });
            var loaded = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("autosave");

            Assert.That(result.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(transport.Requests[1].Body, Does.Contain("\"slotInfo\""));
            Assert.That(loaded.SlotInfoJson, Does.Contain("\"name\":\"Ayla\""));
            Assert.That(loaded.SlotInfoJson, Does.Not.Contain("__unused_reserved_marker__"));
        }

        [Test]
        public async Task ForceSyncCreatesAccountWithInitialSlotAndHonorsCooldown()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(201, "{\"accountId\":\"acc_account\",\"accountSessionToken\":\"pst_account_session\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[{\"slotId\":\"autosave\",\"slotInfo\":{\"name\":\"Ayla\"}}]},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"slot\":{\"saveId\":\"autosave\",\"playerRef\":\"player-184\",\"slotInfo\":{\"name\":\"Ayla\"},\"state\":{\"Level\":1,\"Gold\":10},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":30,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 30, true, true, true, 25)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 }, new PersistlySaveSlotOptions { SlotInfoJson = "{\"name\":\"Ayla\"}" });
            var first = await PersistlyGameSaves.Shared.ForceSyncAsync("autosave");
            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 2, Gold = 20 });
            var second = await PersistlyGameSaves.Shared.ForceSyncAsync("autosave");

            Assert.That(first.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(second.Status, Is.EqualTo(PersistlySlotStatus.Cooldown));
            Assert.That(transport.Requests[0].Body, Does.Contain("\"slot\""));
            Assert.That(transport.Requests[0].Body, Does.Contain("\"slotInfo\""));
        }

        [Test]
        public async Task ForceSyncReconcilesExistingRemoteSlotWhenLocalSlotIdIsMissing()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"accountId\":\"acc_account\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[{\"slotId\":\"autosave\",\"slotInfo\":{\"name\":\"Cloud\"}}]},\"version\":2,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:02:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(200, "{\"save\":{\"saveId\":\"autosave\",\"playerRef\":\"player-184\",\"slotInfo\":{\"name\":\"Cloud\"},\"state\":{\"Level\":1,\"Gold\":10},\"version\":2,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:02:00Z\"}}"),
                new PersistlyTransportResponse(200, "{\"status\":\"accepted\",\"save\":{\"saveId\":\"autosave\",\"playerRef\":\"player-184\",\"slotInfo\":{\"name\":\"Local\"},\"state\":{\"Level\":3,\"Gold\":30},\"version\":3,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:03:00Z\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                AccountId = "acc_account",
                AccountSessionToken = "pst_account_session",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 3, Gold = 30 }, new PersistlySaveSlotOptions { SlotInfoJson = "{\"name\":\"Local\"}" });

            var result = await PersistlyGameSaves.Shared.ForceSyncAsync("autosave", new PersistlySyncOptions { BypassCooldown = true });
            var inspect = PersistlyGameSaves.Shared.InspectSlot("autosave");

            Assert.That(result.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(inspect.SlotId, Is.EqualTo("autosave"));
            Assert.That(inspect.Version, Is.EqualTo(3));
            Assert.That(transport.Requests.Count, Is.EqualTo(3));
            Assert.That(transport.Requests[2].Url, Does.EndWith("/api/v1/accounts/acc_account/slots/autosave/sync"));
            Assert.That(transport.Requests[2].Body, Does.Contain("\"Level\":3"));
        }

        [Test]
        public async Task ConflictKeepsLocalStateDirtyAndStoresCloudStateSeparately()
        {
            PersistlySyncNotification callback = null;
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"accountId\":\"acc_account\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[{\"slotId\":\"autosave\",\"slotInfo\":{}}]},\"version\":4,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(409, "{\"status\":\"conflict\",\"save\":{\"saveId\":\"autosave\",\"playerRef\":\"player-184\",\"slotInfo\":{\"name\":\"Cloud\"},\"state\":{\"Level\":9,\"Gold\":999},\"version\":5,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"},\"details\":{\"reason\":\"base_version_mismatch\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                AccountId = "acc_account",
                AccountSessionToken = "pst_account_session",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25),
                OnSyncResult = notification => callback = notification
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 2, Gold = 20 });
            PersistlyGameSaves.Shared.AttachSlotForTests("autosave", 4);

            var result = await PersistlyGameSaves.Shared.ForceSyncAsync("autosave", new PersistlySyncOptions { BypassCooldown = true });
            var inspect = PersistlyGameSaves.Shared.InspectSlot("autosave");

            Assert.That(result.Status, Is.EqualTo(PersistlySlotStatus.Conflict));
            Assert.That(inspect.Dirty, Is.True);
            Assert.That(inspect.StateJson, Does.Contain("\"Level\":2"));
            Assert.That(inspect.CloudStateJson, Does.Contain("\"Level\":9"));
            Assert.That(result.Conflict.LocalStateJson, Does.Contain("\"Level\":2"));
            Assert.That(result.Conflict.CloudStateJson, Does.Contain("\"Level\":9"));
            Assert.That(result.Conflict.LocalVersion, Is.EqualTo(4));
            Assert.That(result.Conflict.CloudVersion, Is.EqualTo(5));
            Assert.That(callback.Conflict.CloudSlotInfoJson, Does.Contain("\"name\":\"Cloud\""));

            var kept = await PersistlyGameSaves.Shared.KeepLocalDataForLaterAsync();
            var afterKeep = PersistlyGameSaves.Shared.InspectData();
            var accepted = await PersistlyGameSaves.Shared.AcceptCloudDataAsync();
            var loaded = await PersistlyGameSaves.Shared.LoadDataAsync<TestSaveState>();

            Assert.That(kept.Status, Is.EqualTo(PersistlySlotStatus.LocalSaved));
            Assert.That(afterKeep.Dirty, Is.True);
            Assert.That(accepted.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(loaded.State.Level, Is.EqualTo(9));
            Assert.That(loaded.Dirty, Is.False);
        }

        [Test]
        public async Task AccountAccountConflictResultAndCallbackIncludeLocalAndCloudPayloads()
        {
            PersistlySyncNotification callback = null;
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"accountId\":\"acc_account\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{\"label\":\"Cloud\"},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{\"diamonds\":99},\"slots\":[]},\"version\":6,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(409, "{\"status\":\"conflict\",\"save\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{\"label\":\"Cloud\"},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{\"diamonds\":99},\"slots\":[]},\"version\":7,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"},\"details\":{\"reason\":\"base_version_mismatch\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                AccountId = "acc_account",
                AccountSessionToken = "pst_account_session",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25),
                OnSyncResult = notification => callback = notification
            });

            await PersistlyGameSaves.Shared.SaveAccountDataAsync(new AccountState { Diamonds = 25, UnlockedSlots = 2 });
            var result = await PersistlyGameSaves.Shared.ForceSyncAccountAsync(new PersistlySyncOptions { BypassCooldown = true });

            Assert.That(result.Status, Is.EqualTo(PersistlyGameSaveStatus.Conflict));
            Assert.That(result.Conflict.Target, Is.EqualTo(PersistlyGameSaveTarget.Account));
            Assert.That(result.Conflict.LocalStateJson, Does.Contain("\"Diamonds\":25"));
            Assert.That(result.Conflict.CloudStateJson, Does.Contain("\"diamonds\":99"));
            Assert.That(result.Conflict.CloudSlotInfoJson, Does.Contain("\"label\":\"Cloud\""));
            Assert.That(callback.Conflict.CloudVersion, Is.EqualTo(7));
        }

        [Test]
        public async Task ArchiveSlotRejectsUnsyncedLocalSlotWithoutCreatingAccount()
        {
            var transport = new QueueTransport();
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                Transport = transport
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 });

            var error = Assert.ThrowsAsync<PersistlyConfigurationError>(() => PersistlyGameSaves.Shared.ArchiveSlotAsync("autosave"));

            Assert.That(error.Message, Does.Contain("archive_slot_unsynced"));
            Assert.That(transport.Requests.Count, Is.EqualTo(0));
            Assert.That(PersistlyGameSaves.Shared.InspectSlot("autosave").Archived, Is.False);
        }

        [Test]
        public async Task DeleteSlotDeletesRemotelyForSyncedSlotsAndLocallyForUnsyncedSlots()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"accountId\":\"acc_account\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[{\"slotId\":\"autosave\",\"slotInfo\":{}}]},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(200, "{\"save\":{\"saveId\":\"autosave\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"Level\":1,\"Gold\":10},\"version\":1,\"createdAt\":\"2026-04-10T00:01:00Z\",\"updatedAt\":\"2026-04-10T00:01:00Z\"}}"),
                new PersistlyTransportResponse(200, "{\"status\":\"accepted\",\"save\":{\"saveId\":\"autosave\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"Level\":1,\"Gold\":10},\"version\":2,\"createdAt\":\"2026-04-10T00:01:00Z\",\"updatedAt\":\"2026-04-10T00:02:00Z\"}}"),
                new PersistlyTransportResponse(200, "{\"accountId\":\"acc_account\",\"slotId\":\"autosave\",\"deletedAt\":\"2026-04-10T00:02:00Z\",\"alreadyDeleted\":false,\"cleanupQueued\":true,\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[]},\"version\":3,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:02:00Z\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                AccountId = "acc_account",
                AccountSessionToken = "pst_account_session",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 });
            await PersistlyGameSaves.Shared.ForceSyncAsync("autosave", new PersistlySyncOptions { BypassCooldown = true });

            var deleted = await PersistlyGameSaves.Shared.DeleteSlotAsync("autosave");
            var deletedLoad = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("autosave");

            await PersistlyGameSaves.Shared.SaveSlotAsync("manual", new TestSaveState { Level = 2, Gold = 20 });
            var localDeleted = await PersistlyGameSaves.Shared.DeleteSlotAsync("manual");
            var localLoad = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("manual");

            Assert.That(deleted.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(deleted.Warnings, Does.Contain("delete_cleanup_queued"));
            Assert.That(deletedLoad.Found, Is.False);
            Assert.That(transport.Requests[3].Url, Does.EndWith("/api/v1/accounts/acc_account/slots/autosave"));
            Assert.That(transport.Requests[3].Method, Is.EqualTo("DELETE"));
            Assert.That(localDeleted.Status, Is.EqualTo(PersistlySlotStatus.LocalSaved));
            Assert.That(localLoad.Found, Is.False);
        }

        [Test]
        public async Task DeleteAccountDeletesRemotelyWhenSyncedAndFallsBackToLocalClearWhenUnsynced()
        {
            var store = new InMemoryPersistlyGameSavesStore();
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"accountId\":\"acc_account\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[{\"slotId\":\"autosave\",\"slotInfo\":{}}]},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(200, "{\"save\":{\"saveId\":\"autosave\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"Level\":1,\"Gold\":10},\"version\":1,\"createdAt\":\"2026-04-10T00:01:00Z\",\"updatedAt\":\"2026-04-10T00:01:00Z\"}}"),
                new PersistlyTransportResponse(200, "{\"status\":\"accepted\",\"save\":{\"saveId\":\"autosave\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"Level\":1,\"Gold\":10},\"version\":2,\"createdAt\":\"2026-04-10T00:01:00Z\",\"updatedAt\":\"2026-04-10T00:02:00Z\"}}"),
                new PersistlyTransportResponse(200, "{\"accountId\":\"acc_account\",\"deletedAt\":\"2026-04-10T00:02:00Z\",\"deletedSlotCount\":1,\"alreadyDeleted\":false,\"cleanupQueued\":true}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                AccountId = "acc_account",
                AccountSessionToken = "pst_account_session",
                Transport = transport,
                Store = store,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 });
            await PersistlyGameSaves.Shared.ForceSyncAsync("autosave", new PersistlySyncOptions { BypassCooldown = true });
            var remoteDeleted = await PersistlyGameSaves.Shared.DeleteAccountAsync();
            var hiddenSession = PersistlyGameSaves.Shared.GetAccountSession(includeToken: true);
            var missing = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("autosave");

            Assert.That(remoteDeleted.Status, Is.EqualTo(PersistlyGameSaveStatus.Synced));
            Assert.That(remoteDeleted.Warnings, Does.Contain("delete_cleanup_queued"));
            Assert.That(hiddenSession.AccountId, Is.Null.Or.Empty);
            Assert.That(hiddenSession.AccountSessionToken, Is.Null.Or.Empty);
            Assert.That(missing.Found, Is.False);
            Assert.That(store.LoadAccountJson("player-184"), Is.Null);
            Assert.That(transport.Requests[3].Url, Does.EndWith("/api/v1/accounts/acc_account"));
            Assert.That(transport.Requests[3].Method, Is.EqualTo("DELETE"));

            ResetSharedFacade();
            var localStore = new InMemoryPersistlyGameSavesStore();
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-local",
                Store = localStore
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("local", new TestSaveState { Level = 5, Gold = 50 });
            var localDeleted = await PersistlyGameSaves.Shared.DeleteAccountAsync();
            var localMissing = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("local");

            Assert.That(localDeleted.Status, Is.EqualTo(PersistlyGameSaveStatus.LocalSaved));
            Assert.That(localMissing.Found, Is.False);
            Assert.That(localStore.LoadAccountJson("player-local"), Is.Null);
        }

        [Test]
        public async Task ClearLocalAccountRemovesLocalSessionAndSlotsAndAllowsFreshBootstrap()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(201, "{\"accountId\":\"acc_account_new\",\"accountSessionToken\":\"pst_account_session_new\",\"account\":{\"saveId\":\"acc_account_new\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[{\"slotId\":\"fresh\",\"slotInfo\":{}}]},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"slot\":{\"saveId\":\"fresh\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"Level\":2,\"Gold\":20},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                AccountId = "autosave_account",
                AccountSessionToken = "pst_old_session",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 });
            var cleared = await PersistlyGameSaves.Shared.ClearLocalAccountAsync();
            var session = PersistlyGameSaves.Shared.GetAccountSession(includeToken: true);
            var missing = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("autosave");

            await PersistlyGameSaves.Shared.SaveSlotAsync("fresh", new TestSaveState { Level = 2, Gold = 20 });
            var synced = await PersistlyGameSaves.Shared.ForceSyncAsync("fresh", new PersistlySyncOptions { BypassCooldown = true });

            Assert.That(cleared.Status, Is.EqualTo(PersistlyGameSaveStatus.LocalSaved));
            Assert.That(cleared.Target, Is.EqualTo(PersistlyGameSaveTarget.Account));
            Assert.That(session.AccountId, Is.Null.Or.Empty);
            Assert.That(session.AccountSessionToken, Is.Null.Or.Empty);
            Assert.That(missing.Found, Is.False);
            Assert.That(synced.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(transport.Requests[0].Url, Does.EndWith("/api/v1/accounts"));
        }

        [Test]
        public async Task SavingSameSlotAfterArchiveCreatesReplacementSlot()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"accountId\":\"acc_account\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[{\"slotId\":\"autosave\",\"slotInfo\":{},\"archived\":true,\"archivedAt\":\"2026-04-10T00:03:00Z\"}]},\"version\":2,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:03:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(201, "{\"accountId\":\"acc_account\",\"account\":{\"saveId\":\"acc_account\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"schema\":\"persistly.account.v1\",\"accountData\":{},\"slots\":[{\"slotId\":\"autosave\",\"slotInfo\":{},\"archived\":true,\"archivedAt\":\"2026-04-10T00:03:00Z\"},{\"slotId\":\"autosave\",\"slotInfo\":{}}]},\"version\":3,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"slot\":{\"saveId\":\"autosave\",\"playerRef\":\"player-184\",\"slotInfo\":{},\"state\":{\"Level\":2,\"Gold\":20},\"version\":1,\"createdAt\":\"2026-04-10T00:04:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                AccountId = "acc_account",
                AccountSessionToken = "pst_account_session",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 });
            PersistlyGameSaves.Shared.AttachSlotForTests("autosave", 1);
            var archived = await PersistlyGameSaves.Shared.ArchiveSlotAsync("autosave");
            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 2, Gold = 20 });
            var replacement = await PersistlyGameSaves.Shared.ForceSyncAsync("autosave", new PersistlySyncOptions { BypassCooldown = true });
            var inspect = PersistlyGameSaves.Shared.InspectSlot("autosave");

            Assert.That(archived.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(replacement.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(transport.Requests[1].Url, Does.EndWith("/api/v1/accounts/acc_account/slots"));
            Assert.That(transport.Requests[1].Url, Does.Not.Contain("/autosave/sync"));
            Assert.That(inspect.SlotId, Is.EqualTo("autosave"));
        }

        [Test]
        public async Task AnonymousLocalNamespaceIsGeneratedAndPersisted()
        {
            var store = new InMemoryPersistlyGameSavesStore();
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                Store = store
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 });
            ResetSharedFacade();
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                Store = store
            });

            var loaded = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("autosave");

            Assert.That(store.LoadAccountJson("anonymous-device"), Is.Null);
            Assert.That(loaded.Found, Is.True);
            Assert.That(loaded.State.Level, Is.EqualTo(1));
        }

        [Test]
        public void UnknownLocalSchemaThrowsConfigurationError()
        {
            var store = new InMemoryPersistlyGameSavesStore();
            store.SaveAccountJson("player-184", "{\"schema\":\"persistly.local.account.v999\"}");

            Assert.ThrowsAsync<PersistlyConfigurationError>(() => PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                Store = store
            }));
        }

        [Serializable]
        private sealed class TestSaveState
        {
            public int Level;

            public int Gold;
        }

        [Serializable]
        private sealed class AccountState
        {
            public int Diamonds;

            public int UnlockedSlots;
        }

        private sealed class QueueTransport : IPersistlyTransport
        {
            private readonly System.Collections.Generic.Queue<PersistlyTransportResponse> _responses;

            public QueueTransport(params PersistlyTransportResponse[] responses)
            {
                _responses = new System.Collections.Generic.Queue<PersistlyTransportResponse>(responses);
                Requests = new System.Collections.Generic.List<PersistlyTransportRequest>();
            }

            public System.Collections.Generic.List<PersistlyTransportRequest> Requests { get; }

            public Task<PersistlyTransportResponse> SendAsync(PersistlyTransportRequest request, System.Threading.CancellationToken cancellationToken)
            {
                Requests.Add(request);
                return Task.FromResult(_responses.Dequeue());
            }
        }
    }
}
