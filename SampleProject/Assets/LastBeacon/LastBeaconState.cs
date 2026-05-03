using System;

namespace Persistly.Unity.LastBeacon
{
    [Serializable]
    public sealed class LastBeaconSaveState
    {
        public int Scrap = 12;
        public int Workers = 1;
        public int Level = 1;
        public int ManualGatherAmount = 3;
        public int PowerCells = 0;
        public float CoreCharge = 0f;
        public int TotalTicks = 0;
    }

    public sealed class LastBeaconState
    {
        private readonly LastBeaconSaveState _snapshot = new LastBeaconSaveState();

        public int Scrap => _snapshot.Scrap;
        public int Workers => _snapshot.Workers;
        public int Level => _snapshot.Level;
        public int ManualGatherAmount => _snapshot.ManualGatherAmount;
        public int PowerCells => _snapshot.PowerCells;
        public float CoreCharge => _snapshot.CoreCharge;
        public int TotalTicks => _snapshot.TotalTicks;

        public void Tick(float deltaSeconds)
        {
            var safeDelta = Math.Max(0f, deltaSeconds);
            _snapshot.CoreCharge = Math.Min(_snapshot.CoreCharge + (safeDelta * ChargeRatePerSecond()), 100f);
            _snapshot.Scrap += (int)Math.Floor(PassiveScrapPerSecond() * safeDelta);
            _snapshot.TotalTicks += (int)Math.Floor(safeDelta);
            if (_snapshot.CoreCharge >= 100f)
            {
                _snapshot.CoreCharge = 0f;
                _snapshot.PowerCells += 1;
                _snapshot.Scrap += _snapshot.Level * 4;
            }
        }

        public void Gather()
        {
            _snapshot.Scrap += _snapshot.ManualGatherAmount;
        }

        public bool TryHireWorker()
        {
            var cost = WorkerCost();
            if (_snapshot.Scrap < cost)
            {
                return false;
            }

            _snapshot.Scrap -= cost;
            _snapshot.Workers += 1;
            return true;
        }

        public bool TryUpgradeCore()
        {
            var cost = CoreUpgradeCost();
            if (_snapshot.Scrap < cost)
            {
                return false;
            }

            _snapshot.Scrap -= cost;
            _snapshot.Level += 1;
            _snapshot.ManualGatherAmount += 2;
            return true;
        }

        public int WorkerCost()
        {
            return 10 + ((_snapshot.Workers - 1) * 6);
        }

        public int CoreUpgradeCost()
        {
            return 18 + ((_snapshot.Level - 1) * 12);
        }

        public float PassiveScrapPerSecond()
        {
            return _snapshot.Workers * (1f + ((_snapshot.Level - 1) * 0.35f));
        }

        public float ChargeRatePerSecond()
        {
            return 4f + (_snapshot.Workers * 0.35f) + ((_snapshot.Level - 1) * 0.55f);
        }

        public LastBeaconSaveState ToSaveState()
        {
            return new LastBeaconSaveState
            {
                Scrap = _snapshot.Scrap,
                Workers = _snapshot.Workers,
                Level = _snapshot.Level,
                ManualGatherAmount = _snapshot.ManualGatherAmount,
                PowerCells = _snapshot.PowerCells,
                CoreCharge = _snapshot.CoreCharge,
                TotalTicks = _snapshot.TotalTicks,
            };
        }

        public bool LoadFrom(LastBeaconSaveState saveState)
        {
            if (saveState == null)
            {
                return false;
            }

            _snapshot.Scrap = Math.Max(saveState.Scrap, 0);
            _snapshot.Workers = Math.Max(saveState.Workers, 1);
            _snapshot.Level = Math.Max(saveState.Level, 1);
            _snapshot.ManualGatherAmount = Math.Max(saveState.ManualGatherAmount, 1);
            _snapshot.PowerCells = Math.Max(saveState.PowerCells, 0);
            _snapshot.CoreCharge = Math.Clamp(saveState.CoreCharge, 0f, 100f);
            _snapshot.TotalTicks = Math.Max(saveState.TotalTicks, 0);
            return true;
        }
    }
}
