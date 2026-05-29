using Persistly.Unity;
using UnityEngine;

namespace Persistly.Templates.OneSave
{
    public sealed class PersistlyUsageExample : MonoBehaviour
    {
        private readonly PersistlySaveService _saves = new PersistlySaveService();
        private PlayerSaveState _state = new PlayerSaveState();

        private async void Start()
        {
            await _saves.ConfigureAsync("ps_test_replace_me");

            var loaded = await _saves.LoadAsync();
            if (loaded.Status == PersistlySlotStatus.LocalFound && loaded.State != null)
            {
                _state = loaded.State;
            }
        }

        public async void AwardCoins(int coins)
        {
            _state.Coins += coins;
            await _saves.SaveAsync(_state);
        }

        public async void SyncAtCheckpoint()
        {
            await _saves.SyncAsync();
        }
    }
}
