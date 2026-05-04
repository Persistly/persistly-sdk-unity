using NUnit.Framework;

namespace Persistly.Unity.LastBeacon.Tests
{
    public sealed class LastBeaconStoreTests
    {
        [Test]
        public void ProfileRoundTripPreservesConfigAndSaveIdentity()
        {
            const string path = "last_beacon_store_test.json";
            var store = new LastBeaconProfileStore(path);
            store.Reset();

            var profile = new LastBeaconProfile
            {
                ProfileSaveId = "sv_profile",
                ProfileSessionToken = "pst_profile_session",
                CharacterSaveId = "sv_01HXYZ",
                Version = 7,
                Config = new LastBeaconConfig
                {
                    BaseUrl = "http://127.0.0.1:8080",
                    RuntimeKey = "ps_test_example",
                    PlayerRef = "player-184",
                    CharacterName = "Ayla",
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

            store.Save(profile);
            var reloaded = store.Load();

            Assert.That(reloaded.ProfileSaveId, Is.EqualTo("sv_profile"));
            Assert.That(reloaded.ProfileSessionToken, Is.EqualTo("pst_profile_session"));
            Assert.That(reloaded.CharacterSaveId, Is.EqualTo("sv_01HXYZ"));
            Assert.That(reloaded.Version, Is.EqualTo(7));
            Assert.That(reloaded.Config.CharacterName, Is.EqualTo("Ayla"));
            Assert.That(reloaded.State.Workers, Is.EqualTo(4));

            store.Reset();
        }
    }
}
