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

        public Task<PersistlyAuthSessionResult> SignInWithGoogleAsync(string googleIdToken)
        {
            return PersistlyGameSaves.Shared.SignInWithGoogleIdTokenAsync(googleIdToken, new PersistlyAuthOptions
            {
                DeviceLabel = SystemInfo.deviceName
            });
        }

        public Task<PersistlyAuthSessionResult> SignInWithOidcAsync(string oidcJwt)
        {
            return PersistlyGameSaves.Shared.SignInWithProviderAsync(new PersistlyProviderSignInRequest(PersistlyAuthProvider.OidcJwt, oidcJwt)
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
