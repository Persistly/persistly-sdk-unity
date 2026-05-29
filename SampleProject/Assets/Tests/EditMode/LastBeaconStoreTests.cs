using NUnit.Framework;

namespace Persistly.Unity.LastBeacon.Tests
{
    public sealed class LastBeaconStoreTests
    {
        [Test]
        public void AccountRoundTripPreservesConfigAndSaveIdentity()
        {
            const string path = "last_beacon_store_test.json";
            var store = new LastBeaconAccountStore(path);
            store.Reset();

            var account = new LastBeaconAccount
            {
                AccountId = "acc_account",
                AccountSessionToken = "pst_account_session",
                SlotId = "autosave",
                Version = 7,
                Config = new LastBeaconConfig
                {
                    RuntimeKey = "ps_test_example",
                    PlayerRef = "player-184",
                    SlotName = "Ayla",
                    SlotLabel = "Beacon-A",
                },
                State = new LastBeaconSaveState
                {
                    Scrap = 88,
                    Workers = 4,
                    Level = 2,
                    ManualGatherAmount = 5,
                    PowerCells = 1,
                    CoreCharge = 12f,
                    TotalTicks = 9,
                },
            };

            store.Save(account);
            var reloaded = store.Load();

            Assert.That(reloaded.AccountId, Is.EqualTo("acc_account"));
            Assert.That(reloaded.AccountSessionToken, Is.EqualTo("pst_account_session"));
            Assert.That(reloaded.SlotId, Is.EqualTo("autosave"));
            Assert.That(reloaded.Version, Is.EqualTo(7));
            Assert.That(reloaded.Config.SlotName, Is.EqualTo("Ayla"));
            Assert.That(reloaded.State.Workers, Is.EqualTo(4));

            store.Reset();
        }
    }
}
