using System.Threading.Tasks;
using Persistly.Unity;
using UnityEngine;

namespace Persistly.Templates.AuthRequired
{
    public sealed class PersistlySaveService
    {
        public async Task ConfigureAsync(string runtimeKey)
        {
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings(runtimeKey)
            {
                AccountMode = PersistlyAccountMode.AuthRequired,
                Store = new FilePersistlyGameSavesStore(Application.persistentDataPath)
            });
        }

        public Task<PersistlyAuthSessionResult> SignInWithFirebaseAsync(string firebaseIdToken)
        {
            return PersistlyGameSaves.Shared.SignInWithFirebaseTokenAsync(firebaseIdToken, new PersistlyAuthOptions
            {
                DeviceLabel = SystemInfo.deviceName
            });
        }

        public Task<PersistlyAuthSessionResult> SignInWithSupabaseAsync(string supabaseAccessToken)
        {
            return PersistlyGameSaves.Shared.SignInWithSupabaseTokenAsync(supabaseAccessToken, new PersistlyAuthOptions
            {
                DeviceLabel = SystemInfo.deviceName
            });
        }

        public Task<PersistlyAuthSessionResult> SignInWithAuth0Async(string auth0Token)
        {
            return PersistlyGameSaves.Shared.SignInWithAuth0TokenAsync(auth0Token, new PersistlyAuthOptions
            {
                DeviceLabel = SystemInfo.deviceName
            });
        }

        public Task<PersistlySlotResult> SaveLocalAsync(AuthRequiredSaveState state)
        {
            return PersistlyGameSaves.Shared.SaveDataAsync(state);
        }

        public Task<PersistlySlotResult<AuthRequiredSaveState>> LoadLocalAsync()
        {
            return PersistlyGameSaves.Shared.LoadDataAsync<AuthRequiredSaveState>();
        }

        public Task<PersistlySlotResult> SyncAsync()
        {
            return PersistlyGameSaves.Shared.ForceSyncDataAsync();
        }

        public Task SignOutAsync()
        {
            return PersistlyGameSaves.Shared.SignOutAsync();
        }
    }

    [System.Serializable]
    public sealed class AuthRequiredSaveState
    {
        public int Level;
        public int Coins;
    }
}
