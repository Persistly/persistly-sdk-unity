using NUnit.Framework;

namespace Persistly.Unity.LastBeacon.Tests
{
    public sealed class LastBeaconStateTests
    {
        [Test]
        public void NewStateStartsWithExpectedDefaults()
        {
            var state = new LastBeaconState();

            Assert.That(state.Scrap, Is.EqualTo(12));
            Assert.That(state.Workers, Is.EqualTo(1));
            Assert.That(state.Level, Is.EqualTo(1));
            Assert.That(state.ManualGatherAmount, Is.EqualTo(3));
        }

        [Test]
        public void TickAndActionsAdvanceTheIdleLoop()
        {
            var state = new LastBeaconState();

            state.Tick(5f);
            state.Gather();

            Assert.That(state.Scrap, Is.EqualTo(20));

            Assert.That(state.TryHireWorker(), Is.True);
            state.Tick(6f);
            state.Gather();
            state.Gather();

            Assert.That(state.TryUpgradeCore(), Is.True);
            Assert.That(state.Level, Is.EqualTo(2));
            Assert.That(state.ManualGatherAmount, Is.GreaterThan(3));
        }

        [Test]
        public void SaveStateRoundTripPreservesValues()
        {
            var state = new LastBeaconState();
            state.Tick(9f);
            state.Gather();
            state.TryHireWorker();

            var snapshot = state.ToSaveState();

            var rehydrated = new LastBeaconState();
            Assert.That(rehydrated.LoadFrom(snapshot), Is.True);
            Assert.That(rehydrated.Scrap, Is.EqualTo(state.Scrap));
            Assert.That(rehydrated.Workers, Is.EqualTo(state.Workers));
            Assert.That(rehydrated.Level, Is.EqualTo(state.Level));
        }
    }
}
