using System.Threading.Tasks;
using Persistly.Unity;
using UnityEngine;

namespace Persistly.Templates.OneSave
{
    public sealed class PersistlySaveService
    {
        public async Task ConfigureAsync(string runtimeKey)
        {
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings(runtimeKey)
            {
                Store = new FilePersistlyGameSavesStore(Application.persistentDataPath)
            });
        }

        public Task<PersistlySlotResult<PlayerSaveState>> LoadAsync()
        {
            return PersistlyGameSaves.Shared.LoadDataAsync<PlayerSaveState>();
        }

        public Task<PersistlySlotResult> SaveAsync(PlayerSaveState state)
        {
            return PersistlyGameSaves.Shared.SaveDataAsync(state);
        }

        public Task<PersistlySlotResult> SyncAsync()
        {
            return PersistlyGameSaves.Shared.ForceSyncDataAsync();
        }
    }

    [System.Serializable]
    public sealed class PlayerSaveState
    {
        public int Level;
        public int Coins;
        public string Checkpoint = "start";
    }
}
