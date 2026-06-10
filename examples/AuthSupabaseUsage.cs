using System.Threading.Tasks;
using UnityEngine;

namespace Persistly.Unity.Examples
{
    public sealed class AuthSupabaseUsage : MonoBehaviour
    {
        [SerializeField] private string runtimeKey = "";

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(runtimeKey))
            {
                throw new UnityException("Set a Persistly runtime key in the inspector before running this example.");
            }
        }

        private async void Start()
        {
            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings(runtimeKey)
            {
                AccountMode = PersistlyAccountMode.AuthRequired,
                Store = new FilePersistlyGameSavesStore(Application.persistentDataPath)
            });
        }

        public async Task SignInAndSyncAsync(string supabaseAccessToken)
        {
            await PersistlyGameSaves.Shared.SignInWithSupabaseTokenAsync(supabaseAccessToken, new PersistlyAuthOptions
            {
                DeviceLabel = SystemInfo.deviceName
            });

            await PersistlyGameSaves.Shared.SaveDataAsync(new AuthSaveState
            {
                Level = 3,
                Coins = 250
            });

            var sync = await PersistlyGameSaves.Shared.ForceSyncDataAsync();
            Debug.Log("Persistly sync status: " + sync.Status.ToString());
        }

        public Task SignOutAsync()
        {
            return PersistlyGameSaves.Shared.SignOutAsync();
        }

        [System.Serializable]
        private sealed class AuthSaveState
        {
            public int Level;
            public int Coins;
        }
    }
}
