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
        public async Task LiveGameSavesFacadeCreatesLoadsAndSyncsAccountAndSlot()
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
                LocalAccountKey = smokeId,
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
                SlotInfoJson = "{\"slotName\":\"Smoke\"}"
            });
            Assert.That(saved.Status, Is.EqualTo(PersistlySlotStatus.LocalSaved));

            var loaded = await PersistlyGameSaves.Shared.LoadDataAsync<LiveSmokeState>();
            Assert.That(loaded.Status, Is.EqualTo(PersistlySlotStatus.LocalFound));
            Assert.That(loaded.Found, Is.True);
            Assert.That(loaded.State, Is.Not.Null);
            Assert.That(loaded.State.Level, Is.EqualTo(5));

            var firstSync = await PersistlyGameSaves.Shared.ForceSyncDataAsync(new PersistlySyncOptions { BypassCooldown = true });
            Assert.That(firstSync.Status, Is.EqualTo(PersistlySlotStatus.Synced));
            Assert.That(PersistlyGameSaves.Shared.InspectData().SlotId, Is.Not.Empty);

            await PersistlyGameSaves.Shared.SaveDataAsync(new LiveSmokeState
            {
                Level = 6,
                Coins = 1300,
                Checkpoint = "unity-live-smoke-updated"
            }, new PersistlySaveSlotOptions
            {
                SlotInfoJson = "{\"slotName\":\"Smoke\"}"
            });

            var dueResults = await PersistlyGameSaves.Shared.SyncDueSlotsAsync(new PersistlySyncOptions
            {
                BypassCooldown = true,
                IncludeSkipped = true
            });
            Assert.That(dueResults, Has.Some.Matches<PersistlySlotResult>(result =>
                result.SlotId == "autosave" && result.Status == PersistlySlotStatus.Synced));

            await PersistlyGameSaves.Shared.PatchAccountDataAsync("{\"diamonds\":8}");
            var accountSync = await PersistlyGameSaves.Shared.ForceSyncAccountAsync(new PersistlySyncOptions { BypassCooldown = true });
            Assert.That(accountSync.Status, Is.EqualTo(PersistlyGameSaveStatus.Synced));

            var session = PersistlyGameSaves.Shared.GetAccountSession(includeToken: true);
            Assert.That(session.AccountId, Is.Not.Empty);
            Assert.That(session.AccountSessionToken, Is.Not.Empty);
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
