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
        public void SlotStatusContainsGameFriendlyStates()
        {
            Assert.That(Enum.IsDefined(typeof(PersistlySlotStatus), PersistlySlotStatus.LocalSaved), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlySlotStatus), PersistlySlotStatus.Synced), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlySlotStatus), PersistlySlotStatus.Conflict), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlySlotStatus), PersistlySlotStatus.Offline), Is.True);
            Assert.That(Enum.IsDefined(typeof(PersistlySlotStatus), PersistlySlotStatus.RateLimited), Is.True);
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
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example", "player-184"));

            var saved = await PersistlyGameSaves.Shared.SaveSlotAsync("slot-1", new TestSaveState
            {
                Level = 7,
                Gold = 120
            });
            var loaded = await PersistlyGameSaves.Shared.LoadSlotAsync<TestSaveState>("slot-1");

            Assert.That(saved.Status, Is.EqualTo(PersistlySlotStatus.LocalSaved));
            Assert.That(saved.SlotKey, Is.EqualTo("slot-1"));
            Assert.That(loaded.Status, Is.EqualTo(PersistlySlotStatus.LocalSaved));
            Assert.That(loaded.SlotKey, Is.EqualTo("slot-1"));
            Assert.That(loaded.State.Level, Is.EqualTo(7));
            Assert.That(loaded.State.Gold, Is.EqualTo(120));
        }

        [Test]
        public async Task ForceSyncReturnsSlotResultShell()
        {
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_example", "player-184"));

            var result = await PersistlyGameSaves.Shared.ForceSyncAsync("slot-1");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.SlotKey, Is.EqualTo("slot-1"));
            Assert.That(Enum.IsDefined(typeof(PersistlySlotStatus), result.Status), Is.True);
        }

        [Serializable]
        private sealed class TestSaveState
        {
            public int Level;

            public int Gold;
        }
    }
}
