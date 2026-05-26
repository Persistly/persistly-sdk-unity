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
            Assert.That(Enum.IsDefined(typeof(PersistlyGameSaveTarget), PersistlyGameSaveTarget.Profile), Is.True);
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
            Assert.That(saved.SlotKey, Is.EqualTo("slot-1"));
            Assert.That(loaded.Status, Is.EqualTo(PersistlySlotStatus.LocalFound));
            Assert.That(loaded.SlotKey, Is.EqualTo("slot-1"));
            Assert.That(loaded.State.Level, Is.EqualTo(7));
            Assert.That(loaded.State.Gold, Is.EqualTo(120));
            Assert.That(loaded.Dirty, Is.True);
            Assert.That(loaded.MetadataJson, Is.EqualTo("{}"));
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
                MetadataJson = "{\"name\":\"Ayla\"}"
            });
            var loaded = await PersistlyGameSaves.Shared.LoadDataAsync<TestSaveState>();
            var inspect = PersistlyGameSaves.Shared.InspectData();

            Assert.That(PersistlyGameSaves.DefaultSlotKey, Is.EqualTo("autosave"));
            Assert.That(saved.Status, Is.EqualTo(PersistlySlotStatus.LocalSaved));
            Assert.That(saved.SlotKey, Is.EqualTo("autosave"));
            Assert.That(loaded.Status, Is.EqualTo(PersistlySlotStatus.LocalFound));
            Assert.That(loaded.SlotKey, Is.EqualTo("autosave"));
            Assert.That(loaded.State.Level, Is.EqualTo(4));
            Assert.That(loaded.State.Gold, Is.EqualTo(70));
            Assert.That(inspect.Exists, Is.True);
            Assert.That(inspect.SlotKey, Is.EqualTo("autosave"));
            Assert.That(inspect.MetadataJson, Does.Contain("\"name\":\"Ayla\""));
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
        public async Task ProfileSessionTokenIsExplicitlyExportedOnlyWhenRequested()
        {
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                ProfileSaveId = "sv_profile",
                ProfileSessionToken = "pst_profile_session"
            });

            var hidden = PersistlyGameSaves.Shared.GetProfileSession();
            var exported = PersistlyGameSaves.Shared.GetProfileSession(includeToken: true);

            Assert.That(hidden.ProfileSaveId, Is.EqualTo("sv_profile"));
            Assert.That(hidden.ProfileSessionToken, Is.Null);
            Assert.That(exported.ProfileSessionToken, Is.EqualTo("pst_profile_session"));
        }

        [Test]
        public async Task ExistingProfileSessionLoadsRemoteProfileBeforeUse()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"profileSaveId\":\"sv_profile\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{\"label\":\"Cloud\"},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{\"diamonds\":99},\"characterSlots\":[]},\"version\":7,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":45,\"forceSyncCooldownSeconds\":8,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                ProfileSaveId = "sv_profile",
                ProfileSessionToken = "pst_profile_session",
                Transport = transport
            });

            var ensured = await PersistlyGameSaves.Shared.EnsureProfileAsync();
            var profile = PersistlyGameSaves.Shared.InspectProfile();

            Assert.That(ensured.Status, Is.EqualTo(PersistlyGameSaveStatus.Synced));
            Assert.That(transport.Requests[0].Url, Does.EndWith("/api/v1/profiles/sv_profile"));
            Assert.That(profile.AccountDataJson, Does.Contain("\"diamonds\":99"));
            Assert.That(profile.MetadataJson, Does.Contain("\"label\":\"Cloud\""));
            Assert.That(profile.Version, Is.EqualTo(7));
        }

        [Test]
        public async Task FacadeCreateProfileRequiresEmptyLocalState()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(201, "{\"profileSaveId\":\"sv_profile\",\"profileSessionToken\":\"pst_profile_session\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[]},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                Transport = transport
            });

            var created = await PersistlyGameSaves.Shared.CreateProfileAsync();
            Assert.That(created.Status, Is.EqualTo(PersistlyGameSaveStatus.Synced));

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 });
            var error = Assert.ThrowsAsync<PersistlyConfigurationError>(() => PersistlyGameSaves.Shared.CreateProfileAsync());
            Assert.That(error.Message, Does.Contain("ClearLocalProfileAsync"));
        }

        [Test]
        public async Task AttachProfileRequiresEmptyLocalStateAndStoresRemoteProfileLocally()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"profileSaveId\":\"sv_profile\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{\"label\":\"Cloud\"},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{\"diamonds\":99},\"characterSlots\":[]},\"version\":7,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":45,\"forceSyncCooldownSeconds\":8,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                Transport = transport
            });

            var attached = await PersistlyGameSaves.Shared.AttachProfileAsync("sv_profile", "pst_profile_session");
            var exported = PersistlyGameSaves.Shared.GetProfileSession(includeToken: true);
            var profile = PersistlyGameSaves.Shared.InspectProfile();

            Assert.That(attached.Status, Is.EqualTo(PersistlyGameSaveStatus.Synced));
            Assert.That(exported.ProfileSaveId, Is.EqualTo("sv_profile"));
            Assert.That(exported.ProfileSessionToken, Is.EqualTo("pst_profile_session"));
            Assert.That(profile.Version, Is.EqualTo(7));

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 });
            var error = Assert.ThrowsAsync<PersistlyConfigurationError>(() => PersistlyGameSaves.Shared.AttachProfileAsync("sv_other", "pst_other"));
            Assert.That(error.Message, Does.Contain("ClearLocalProfileAsync"));
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
            var profile = PersistlyGameSaves.Shared.InspectProfile();

            Assert.That(saved.Status, Is.EqualTo(PersistlyGameSaveStatus.LocalSaved));
            Assert.That(patched.Status, Is.EqualTo(PersistlyGameSaveStatus.LocalSaved));
            Assert.That(profile.AccountDataJson, Does.Contain("\"diamonds\":25"));
            Assert.That(PersistlyGameSaves.Shared.GetAccountDataJson(), Does.Contain("\"diamonds\":25"));
            Assert.That(profile.AccountDataJson, Does.Not.Contain("unlockedSlots"));
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
                MetadataJson = "{\"name\":\"Ayla\"}"
            });

            var slots = PersistlyGameSaves.Shared.ListSlots();
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
                new PersistlyTransportResponse(200, "{\"profileSaveId\":\"sv_profile\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[{\"slotKey\":\"autosave\",\"characterSaveId\":\"sv_char\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"},\"name\":\"Cloud\"}}]},\"version\":4,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(200, "{\"save\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"},\"name\":\"Cloud\"},\"state\":{\"Level\":9,\"Gold\":999},\"version\":5,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                ProfileSaveId = "sv_profile",
                ProfileSessionToken = "pst_profile_session",
                Transport = transport
            });

            var result = await PersistlyGameSaves.Shared.RefreshSlotAsync("autosave");
            var loaded = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("autosave");

            Assert.That(result.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(loaded.Found, Is.True);
            Assert.That(loaded.State.Level, Is.EqualTo(9));
            Assert.That(loaded.State.Gold, Is.EqualTo(999));
            Assert.That(loaded.Dirty, Is.False);
            Assert.That(loaded.MetadataJson, Does.Contain("\"name\":\"Cloud\""));
            Assert.That(transport.Requests[1].Url, Does.EndWith("/api/v1/profiles/sv_profile/characters/sv_char"));
        }

        [Test]
        public async Task RefreshSlotKeepsDirtyLocalStateAndStoresCloudConflict()
        {
            PersistlySyncNotification callback = null;
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"profileSaveId\":\"sv_profile\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[{\"slotKey\":\"autosave\",\"characterSaveId\":\"sv_char\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"},\"name\":\"Cloud\"}}]},\"version\":4,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(200, "{\"save\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"},\"name\":\"Cloud\"},\"state\":{\"Level\":9,\"Gold\":999},\"version\":5,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                ProfileSaveId = "sv_profile",
                ProfileSessionToken = "pst_profile_session",
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
        public async Task ExistingSlotSyncAddsReservedSlotMetadataWithoutPersistingItLocally()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"profileSaveId\":\"sv_profile\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[{\"slotKey\":\"autosave\",\"characterSaveId\":\"sv_char\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"}}}]},\"version\":4,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(200, "{\"status\":\"accepted\",\"save\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"},\"name\":\"Ayla\"},\"state\":{\"Level\":2,\"Gold\":20},\"version\":5,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                ProfileSaveId = "sv_profile",
                ProfileSessionToken = "pst_profile_session",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 2, Gold = 20 }, new PersistlySaveSlotOptions { MetadataJson = "{\"name\":\"Ayla\"}" });
            PersistlyGameSaves.Shared.AttachCharacterForTests("autosave", "sv_char", 4);

            var result = await PersistlyGameSaves.Shared.ForceSyncAsync("autosave", new PersistlySyncOptions { BypassCooldown = true });
            var loaded = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("autosave");

            Assert.That(result.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(transport.Requests[1].Body, Does.Contain("\"_persistly\":{\"slotKey\":\"autosave\"}"));
            Assert.That(loaded.MetadataJson, Does.Contain("\"name\":\"Ayla\""));
            Assert.That(loaded.MetadataJson, Does.Not.Contain("_persistly"));
        }

        [Test]
        public async Task ForceSyncCreatesProfileWithInitialCharacterAndHonorsCooldown()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(201, "{\"profileSaveId\":\"sv_profile\",\"profileSessionToken\":\"pst_profile_session\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[{\"slotKey\":\"autosave\",\"characterSaveId\":\"sv_char\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"},\"name\":\"Ayla\"}}]},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"character\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"},\"name\":\"Ayla\"},\"state\":{\"Level\":1,\"Gold\":10},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":30,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 30, true, true, true, 25)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 }, new PersistlySaveSlotOptions { MetadataJson = "{\"name\":\"Ayla\"}" });
            var first = await PersistlyGameSaves.Shared.ForceSyncAsync("autosave");
            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 2, Gold = 20 });
            var second = await PersistlyGameSaves.Shared.ForceSyncAsync("autosave");

            Assert.That(first.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(second.Status, Is.EqualTo(PersistlySlotStatus.Cooldown));
            Assert.That(transport.Requests[0].Body, Does.Contain("\"character\""));
            Assert.That(transport.Requests[0].Body, Does.Contain("\"_persistly\":{\"slotKey\":\"autosave\"}"));
        }

        [Test]
        public async Task ForceSyncReconcilesExistingRemoteSlotWhenLocalCharacterIdIsMissing()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"profileSaveId\":\"sv_profile\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[{\"slotKey\":\"autosave\",\"characterSaveId\":\"sv_char\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"},\"name\":\"Cloud\"}}]},\"version\":2,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:02:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(200, "{\"save\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"},\"name\":\"Cloud\"},\"state\":{\"Level\":1,\"Gold\":10},\"version\":2,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:02:00Z\"}}"),
                new PersistlyTransportResponse(200, "{\"status\":\"accepted\",\"save\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"},\"name\":\"Local\"},\"state\":{\"Level\":3,\"Gold\":30},\"version\":3,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:03:00Z\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                ProfileSaveId = "sv_profile",
                ProfileSessionToken = "pst_profile_session",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 3, Gold = 30 }, new PersistlySaveSlotOptions { MetadataJson = "{\"name\":\"Local\"}" });

            var result = await PersistlyGameSaves.Shared.ForceSyncAsync("autosave", new PersistlySyncOptions { BypassCooldown = true });
            var inspect = PersistlyGameSaves.Shared.InspectSlot("autosave");

            Assert.That(result.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(inspect.CharacterSaveId, Is.EqualTo("sv_char"));
            Assert.That(inspect.Version, Is.EqualTo(3));
            Assert.That(transport.Requests.Count, Is.EqualTo(3));
            Assert.That(transport.Requests[2].Url, Does.EndWith("/api/v1/profiles/sv_profile/characters/sv_char/sync"));
            Assert.That(transport.Requests[2].Body, Does.Contain("\"Level\":3"));
        }

        [Test]
        public async Task ConflictKeepsLocalStateDirtyAndStoresCloudStateSeparately()
        {
            PersistlySyncNotification callback = null;
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"profileSaveId\":\"sv_profile\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[{\"slotKey\":\"autosave\",\"characterSaveId\":\"sv_char\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"}}}]},\"version\":4,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(409, "{\"status\":\"conflict\",\"save\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"},\"name\":\"Cloud\"},\"state\":{\"Level\":9,\"Gold\":999},\"version\":5,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"},\"details\":{\"reason\":\"base_version_mismatch\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                ProfileSaveId = "sv_profile",
                ProfileSessionToken = "pst_profile_session",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25),
                OnSyncResult = notification => callback = notification
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 2, Gold = 20 });
            PersistlyGameSaves.Shared.AttachCharacterForTests("autosave", "sv_char", 4);

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
            Assert.That(callback.Conflict.CloudMetadataJson, Does.Contain("\"name\":\"Cloud\""));

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
        public async Task ProfileAccountConflictResultAndCallbackIncludeLocalAndCloudPayloads()
        {
            PersistlySyncNotification callback = null;
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"profileSaveId\":\"sv_profile\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{\"label\":\"Cloud\"},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{\"diamonds\":99},\"characterSlots\":[]},\"version\":6,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(409, "{\"status\":\"conflict\",\"save\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{\"label\":\"Cloud\"},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{\"diamonds\":99},\"characterSlots\":[]},\"version\":7,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"},\"details\":{\"reason\":\"base_version_mismatch\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                ProfileSaveId = "sv_profile",
                ProfileSessionToken = "pst_profile_session",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25),
                OnSyncResult = notification => callback = notification
            });

            await PersistlyGameSaves.Shared.SaveAccountDataAsync(new AccountState { Diamonds = 25, UnlockedSlots = 2 });
            var result = await PersistlyGameSaves.Shared.ForceSyncProfileAsync(new PersistlySyncOptions { BypassCooldown = true });

            Assert.That(result.Status, Is.EqualTo(PersistlyGameSaveStatus.Conflict));
            Assert.That(result.Conflict.Target, Is.EqualTo(PersistlyGameSaveTarget.Profile));
            Assert.That(result.Conflict.LocalStateJson, Does.Contain("\"Diamonds\":25"));
            Assert.That(result.Conflict.CloudStateJson, Does.Contain("\"diamonds\":99"));
            Assert.That(result.Conflict.CloudMetadataJson, Does.Contain("\"label\":\"Cloud\""));
            Assert.That(callback.Conflict.CloudVersion, Is.EqualTo(7));
        }

        [Test]
        public async Task ArchiveSlotRejectsUnsyncedLocalSlotWithoutCreatingProfile()
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
                new PersistlyTransportResponse(200, "{\"profileSaveId\":\"sv_profile\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[{\"slotKey\":\"autosave\",\"characterSaveId\":\"sv_char\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"}}}]},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(200, "{\"save\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"}},\"state\":{\"Level\":1,\"Gold\":10},\"version\":1,\"createdAt\":\"2026-04-10T00:01:00Z\",\"updatedAt\":\"2026-04-10T00:01:00Z\"}}"),
                new PersistlyTransportResponse(200, "{\"status\":\"accepted\",\"save\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"}},\"state\":{\"Level\":1,\"Gold\":10},\"version\":2,\"createdAt\":\"2026-04-10T00:01:00Z\",\"updatedAt\":\"2026-04-10T00:02:00Z\"}}"),
                new PersistlyTransportResponse(200, "{\"profileSaveId\":\"sv_profile\",\"characterSaveId\":\"sv_char\",\"slotKey\":\"autosave\",\"deletedAt\":\"2026-04-10T00:02:00Z\",\"alreadyDeleted\":false,\"cleanupQueued\":true,\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[]},\"version\":3,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:02:00Z\"}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                ProfileSaveId = "sv_profile",
                ProfileSessionToken = "pst_profile_session",
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
            Assert.That(transport.Requests[3].Url, Does.EndWith("/api/v1/profiles/sv_profile/characters/sv_char"));
            Assert.That(transport.Requests[3].Method, Is.EqualTo("DELETE"));
            Assert.That(localDeleted.Status, Is.EqualTo(PersistlySlotStatus.LocalSaved));
            Assert.That(localLoad.Found, Is.False);
        }

        [Test]
        public async Task DeleteProfileDeletesRemotelyWhenSyncedAndFallsBackToLocalClearWhenUnsynced()
        {
            var store = new InMemoryPersistlyGameSavesStore();
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"profileSaveId\":\"sv_profile\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[{\"slotKey\":\"autosave\",\"characterSaveId\":\"sv_char\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"}}}]},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(200, "{\"save\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"}},\"state\":{\"Level\":1,\"Gold\":10},\"version\":1,\"createdAt\":\"2026-04-10T00:01:00Z\",\"updatedAt\":\"2026-04-10T00:01:00Z\"}}"),
                new PersistlyTransportResponse(200, "{\"status\":\"accepted\",\"save\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"}},\"state\":{\"Level\":1,\"Gold\":10},\"version\":2,\"createdAt\":\"2026-04-10T00:01:00Z\",\"updatedAt\":\"2026-04-10T00:02:00Z\"}}"),
                new PersistlyTransportResponse(200, "{\"profileSaveId\":\"sv_profile\",\"deletedAt\":\"2026-04-10T00:02:00Z\",\"deletedCharacterCount\":1,\"alreadyDeleted\":false,\"cleanupQueued\":true}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                ProfileSaveId = "sv_profile",
                ProfileSessionToken = "pst_profile_session",
                Transport = transport,
                Store = store,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 });
            await PersistlyGameSaves.Shared.ForceSyncAsync("autosave", new PersistlySyncOptions { BypassCooldown = true });
            var remoteDeleted = await PersistlyGameSaves.Shared.DeleteProfileAsync();
            var hiddenSession = PersistlyGameSaves.Shared.GetProfileSession(includeToken: true);
            var missing = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("autosave");

            Assert.That(remoteDeleted.Status, Is.EqualTo(PersistlyGameSaveStatus.Synced));
            Assert.That(remoteDeleted.Warnings, Does.Contain("delete_cleanup_queued"));
            Assert.That(hiddenSession.ProfileSaveId, Is.Null.Or.Empty);
            Assert.That(hiddenSession.ProfileSessionToken, Is.Null.Or.Empty);
            Assert.That(missing.Found, Is.False);
            Assert.That(store.LoadProfileJson("player-184"), Is.Null);
            Assert.That(transport.Requests[3].Url, Does.EndWith("/api/v1/profiles/sv_profile"));
            Assert.That(transport.Requests[3].Method, Is.EqualTo("DELETE"));

            ResetSharedFacade();
            var localStore = new InMemoryPersistlyGameSavesStore();
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-local",
                Store = localStore
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("local", new TestSaveState { Level = 5, Gold = 50 });
            var localDeleted = await PersistlyGameSaves.Shared.DeleteProfileAsync();
            var localMissing = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("local");

            Assert.That(localDeleted.Status, Is.EqualTo(PersistlyGameSaveStatus.LocalSaved));
            Assert.That(localMissing.Found, Is.False);
            Assert.That(localStore.LoadProfileJson("player-local"), Is.Null);
        }

        [Test]
        public async Task ClearLocalProfileRemovesLocalSessionAndSlotsAndAllowsFreshBootstrap()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(201, "{\"profileSaveId\":\"sv_profile_new\",\"profileSessionToken\":\"pst_profile_session_new\",\"profile\":{\"saveId\":\"sv_profile_new\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[{\"slotKey\":\"fresh\",\"characterSaveId\":\"sv_char_new\",\"metadata\":{\"_persistly\":{\"slotKey\":\"fresh\"}}}]},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"character\":{\"saveId\":\"sv_char_new\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"fresh\"}},\"state\":{\"Level\":2,\"Gold\":20},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                ProfileSaveId = "sv_old_profile",
                ProfileSessionToken = "pst_old_session",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 });
            var cleared = await PersistlyGameSaves.Shared.ClearLocalProfileAsync();
            var session = PersistlyGameSaves.Shared.GetProfileSession(includeToken: true);
            var missing = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("autosave");

            await PersistlyGameSaves.Shared.SaveSlotAsync("fresh", new TestSaveState { Level = 2, Gold = 20 });
            var synced = await PersistlyGameSaves.Shared.ForceSyncAsync("fresh", new PersistlySyncOptions { BypassCooldown = true });

            Assert.That(cleared.Status, Is.EqualTo(PersistlyGameSaveStatus.LocalSaved));
            Assert.That(cleared.Target, Is.EqualTo(PersistlyGameSaveTarget.Profile));
            Assert.That(session.ProfileSaveId, Is.Null.Or.Empty);
            Assert.That(session.ProfileSessionToken, Is.Null.Or.Empty);
            Assert.That(missing.Found, Is.False);
            Assert.That(synced.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(transport.Requests[0].Url, Does.EndWith("/api/v1/profiles"));
        }

        [Test]
        public async Task SavingSameSlotAfterArchiveCreatesReplacementCharacter()
        {
            var transport = new QueueTransport(
                new PersistlyTransportResponse(200, "{\"profileSaveId\":\"sv_profile\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[{\"slotKey\":\"autosave\",\"characterSaveId\":\"sv_old\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"}},\"archived\":true,\"archivedAt\":\"2026-04-10T00:03:00Z\"}]},\"version\":2,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:03:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"),
                new PersistlyTransportResponse(201, "{\"profileSaveId\":\"sv_profile\",\"profile\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{},\"state\":{\"schema\":\"persistly.profile.v1\",\"accountData\":{},\"characterSlots\":[{\"slotKey\":\"autosave\",\"characterSaveId\":\"sv_old\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"}},\"archived\":true,\"archivedAt\":\"2026-04-10T00:03:00Z\"},{\"slotKey\":\"autosave\",\"characterSaveId\":\"sv_new\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"}}}]},\"version\":3,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"character\":{\"saveId\":\"sv_new\",\"playerRef\":\"player-184\",\"metadata\":{\"_persistly\":{\"slotKey\":\"autosave\"}},\"state\":{\"Level\":2,\"Gold\":20},\"version\":1,\"createdAt\":\"2026-04-10T00:04:00Z\",\"updatedAt\":\"2026-04-10T00:04:00Z\"},\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":0,\"syncOnAppBackground\":true,\"syncOnAppForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}"));
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example")
            {
                PlayerRef = "player-184",
                ProfileSaveId = "sv_profile",
                ProfileSessionToken = "pst_profile_session",
                Transport = transport,
                SyncPolicy = new PersistlySyncPolicy(60, 0, true, true, true, 25)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 1, Gold = 10 });
            PersistlyGameSaves.Shared.AttachCharacterForTests("autosave", "sv_old", 1);
            var archived = await PersistlyGameSaves.Shared.ArchiveSlotAsync("autosave");
            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new TestSaveState { Level = 2, Gold = 20 });
            var replacement = await PersistlyGameSaves.Shared.ForceSyncAsync("autosave", new PersistlySyncOptions { BypassCooldown = true });
            var inspect = PersistlyGameSaves.Shared.InspectSlot("autosave");

            Assert.That(archived.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(replacement.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(transport.Requests[1].Url, Does.EndWith("/api/v1/profiles/sv_profile/characters"));
            Assert.That(transport.Requests[1].Url, Does.Not.Contain("/sv_old/sync"));
            Assert.That(inspect.CharacterSaveId, Is.EqualTo("sv_new"));
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

            Assert.That(store.LoadProfileJson("anonymous-device"), Is.Null);
            Assert.That(loaded.Found, Is.True);
            Assert.That(loaded.State.Level, Is.EqualTo(1));
        }

        [Test]
        public void UnknownLocalSchemaThrowsConfigurationError()
        {
            var store = new InMemoryPersistlyGameSavesStore();
            store.SaveProfileJson("player-184", "{\"schema\":\"persistly.local.profile.v999\"}");

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
