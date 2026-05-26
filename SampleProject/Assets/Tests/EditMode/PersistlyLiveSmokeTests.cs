#nullable enable
using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Persistly.Unity;

namespace Persistly.Unity.LastBeacon.Tests
{
    public sealed class PersistlyLiveSmokeTests
    {
        [SetUp]
        public void ResetSharedFacade()
        {
            var field = typeof(PersistlyGameSaves).GetField("_shared", BindingFlags.NonPublic | BindingFlags.Static);
            field.SetValue(null, null);
        }

        [Test]
        public async Task LiveGameSavesFacadeCreatesLoadsAndSyncsProfileAndSlot()
        {
            var runtimeKey = Environment.GetEnvironmentVariable("PERSISTLY_RUNTIME_KEY");
            if (string.IsNullOrWhiteSpace(runtimeKey))
            {
                Assert.Ignore("PERSISTLY_RUNTIME_KEY must be set to run the live parity smoke.");
            }

            var baseUrl = Environment.GetEnvironmentVariable("PERSISTLY_API_BASE");
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = PersistlyClientOptions.DefaultBaseUrl;
            }

            var smokeId = "unity-live-smoke-" + Guid.NewGuid().ToString("N");
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings(runtimeKey)
            {
                BaseUrl = baseUrl,
                PlayerRef = smokeId,
                LocalProfileKey = smokeId,
                SyncPolicy = new PersistlySyncPolicy(
                    minRemoteSyncIntervalSeconds: 1,
                    forceSyncCooldownSeconds: 0,
                    syncOnBackground: true,
                    syncOnForeground: true,
                    syncOnReconnect: true,
                    maxQueuedLocalSnapshots: 25)
            });

            var config = await new PersistlyClient(new PersistlyClientOptions(baseUrl, runtimeKey)).GetRuntimeConfigAsync();
            Assert.That(config.SyncPolicy.MinRemoteSyncIntervalSeconds, Is.GreaterThanOrEqualTo(1));

            var account = await PersistlyGameSaves.Shared.SaveAccountDataAsync(new LiveAccountState
            {
                Diamonds = 7,
                UnlockedSlots = 1
            });
            Assert.That(account.Status, Is.EqualTo(PersistlyGameSaveStatus.LocalSaved));

            var saved = await PersistlyGameSaves.Shared.SaveDataAsync(new LiveSmokeState
            {
                Level = 5,
                Coins = 1200,
                Checkpoint = "unity-live-smoke"
            }, new PersistlySaveSlotOptions
            {
                MetadataJson = "{\"characterName\":\"Smoke\"}"
            });
            Assert.That(saved.Status, Is.EqualTo(PersistlySlotStatus.LocalSaved));

            var loaded = await PersistlyGameSaves.Shared.LoadDataAsync<LiveSmokeState>();
            Assert.That(loaded.Status, Is.EqualTo(PersistlySlotStatus.LocalFound));
            Assert.That(loaded.Found, Is.True);
            Assert.That(loaded.State, Is.Not.Null);
            Assert.That(loaded.State.Level, Is.EqualTo(5));

            var firstSync = await PersistlyGameSaves.Shared.ForceSyncDataAsync(new PersistlySyncOptions { BypassCooldown = true });
            Assert.That(firstSync.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(PersistlyGameSaves.Shared.InspectData().CharacterSaveId, Is.Not.Empty);

            await PersistlyGameSaves.Shared.SaveDataAsync(new LiveSmokeState
            {
                Level = 6,
                Coins = 1300,
                Checkpoint = "unity-live-smoke-updated"
            }, new PersistlySaveSlotOptions
            {
                MetadataJson = "{\"characterName\":\"Smoke\"}"
            });

            var dueResults = await PersistlyGameSaves.Shared.SyncDueSlotsAsync(new PersistlySyncOptions
            {
                BypassCooldown = true,
                IncludeSkipped = true
            });
            Assert.That(dueResults, Has.Some.Matches<PersistlySlotResult>(result =>
                result.SlotKey == "autosave" && result.Status == PersistlySlotStatus.Synced));

            await PersistlyGameSaves.Shared.PatchAccountDataAsync("{\"diamonds\":8}");
            var profileSync = await PersistlyGameSaves.Shared.ForceSyncProfileAsync(new PersistlySyncOptions { BypassCooldown = true });
            Assert.That(profileSync.Status, Is.EqualTo(PersistlyGameSaveStatus.Synced));

            var session = PersistlyGameSaves.Shared.GetProfileSession(includeToken: true);
            Assert.That(session.ProfileSaveId, Is.Not.Empty);
            Assert.That(session.ProfileSessionToken, Is.Not.Empty);
        }

        [Serializable]
        private sealed class LiveSmokeState
        {
            public int Level;
            public int Coins;
            public string Checkpoint = "";
        }

        [Serializable]
        private sealed class LiveAccountState
        {
            public int Diamonds;
            public int UnlockedSlots;
        }
    }
}
