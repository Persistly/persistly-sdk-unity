using System.Threading.Tasks;
using Persistly.Unity;
using UnityEngine;

namespace Persistly.Templates.AnonymousFirstConnectLater
{
    public sealed class PersistlySaveService
    {
        public async Task ConfigureAsync(string runtimeKey)
        {
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings(runtimeKey)
            {
                LocalAccountKey = "current-player",
                Store = new FilePersistlyGameSavesStore(Application.persistentDataPath)
            });
        }

        public Task<PersistlySlotResult> SaveAsync(PlayerSaveState state)
        {
            return PersistlyGameSaves.Shared.SaveDataAsync(state, new PersistlySaveSlotOptions
            {
                SlotInfoJson = JsonUtility.ToJson(new SlotPreview
                {
                    Level = state.Level,
                    Checkpoint = state.Checkpoint
                })
            });
        }

        public Task<PersistlySlotResult> SyncAsync()
        {
            return PersistlyGameSaves.Shared.ForceSyncDataAsync(new PersistlySyncOptions { BypassCooldown = true });
        }

        public async Task<bool> ConnectFirebaseAsync(string firebaseIdToken)
        {
            try
            {
                // The token comes from Firebase Auth in your game. Normal save/load/sync calls
                // do not receive or store this token.
                await PersistlyGameSaves.Shared.ConnectWithFirebaseTokenAsync(firebaseIdToken, new PersistlyAuthOptions
                {
                    DeviceLabel = SystemInfo.deviceName
                });
                return true;
            }
            catch (PersistlyAccountAuthConflictError)
            {
                return false;
            }
        }

        public async Task SwitchToProviderAccountAsync(string firebaseIdToken)
        {
            // Only call this after the player confirms replacing this device's local progress.
            await PersistlyGameSaves.Shared.ClearLocalAccountAsync();
            await PersistlyGameSaves.Shared.SignInWithFirebaseTokenAsync(firebaseIdToken, new PersistlyAuthOptions
            {
                DeviceLabel = SystemInfo.deviceName
            });
        }

        [System.Serializable]
        public sealed class PlayerSaveState
        {
            public int Level;
            public int Coins;
            public string Checkpoint = "";
        }

        [System.Serializable]
        private sealed class SlotPreview
        {
            public int Level;
            public string Checkpoint = "";
        }
    }
}
