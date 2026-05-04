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
        public async Task CreateProfileStoresProfileCharacterAndSessionToken()
        {
            var transport = new StubTransport(
                201,
                "{\"profile\":{\"profileSaveId\":\"sv_profile\",\"profileSessionToken\":\"pst_profile_session\",\"save\":{\"saveId\":\"sv_profile\",\"playerRef\":\"player-184\",\"metadata\":{\"displayName\":\"Ayla\"},\"state\":{\"accountData\":{\"diamonds\":20},\"characters\":[{\"saveId\":\"sv_char\",\"metadata\":{\"slot\":1,\"name\":\"Ayla\"}}]},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"}},\"character\":{\"save\":{\"saveId\":\"sv_char\",\"playerRef\":\"player-184\",\"metadata\":{\"slot\":1,\"name\":\"Ayla\"},\"state\":{\"level\":1,\"gold\":100},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"}}}");
            var client = BuildClient(transport);

            var result = await client.CreateProfileAsync(new PersistlyCreateProfileRequest(
                "{\"accountData\":{\"diamonds\":20}}",
                "{\"slot\":1,\"name\":\"Ayla\"}",
                "{\"level\":1,\"gold\":100}",
                "{\"displayName\":\"Ayla\"}",
                "player-184"));

            Assert.That(result.ProfileSaveId, Is.EqualTo("sv_profile"));
            Assert.That(result.ProfileSessionToken, Is.EqualTo("pst_profile_session"));
            Assert.That(result.Profile.Save.StateJson, Does.Contain("\"characters\""));
            Assert.That(result.Character.Save.SaveId, Is.EqualTo("sv_char"));
            Assert.That(client.TryGetLocal("sv_profile", out var cachedProfile), Is.True);
            Assert.That(cachedProfile.StateJson, Does.Contain("\"diamonds\":20"));
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
        public async Task GetRuntimeConfigParsesSyncPolicy()
        {
            var transport = new StubTransport(
                200,
                "{\"syncPolicy\":{\"minRemoteSyncIntervalSeconds\":60,\"forceSyncCooldownSeconds\":10,\"syncOnBackground\":true,\"syncOnForeground\":true,\"syncOnReconnect\":true,\"maxQueuedLocalSnapshots\":25}}");
            var client = BuildClient(transport);

            var config = await client.GetRuntimeConfigAsync();

            Assert.That(config.SyncPolicy.MinRemoteSyncIntervalSeconds, Is.EqualTo(60));
            Assert.That(config.SyncPolicy.ForceSyncCooldownSeconds, Is.EqualTo(10));
            Assert.That(config.SyncPolicy.SyncOnBackground, Is.True);
            Assert.That(config.SyncPolicy.MaxQueuedLocalSnapshots, Is.EqualTo(25));
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
