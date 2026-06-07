using System.Threading.Tasks;
using UnityEngine;

namespace Persistly.Unity.Examples
{
    public sealed class AuthOidcUsage : MonoBehaviour
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

        public async Task SignInWithOidcJwtAsync(string oidcJwt)
        {
            await PersistlyGameSaves.Shared.SignInWithProviderAsync(new PersistlyProviderSignInRequest(PersistlyAuthProvider.OidcJwt, oidcJwt)
            {
                DeviceLabel = SystemInfo.deviceName
            });
        }

        public async Task LinkGoogleAsync(string googleIdToken)
        {
            await PersistlyGameSaves.Shared.LinkProviderAsync(new PersistlyProviderSignInRequest(PersistlyAuthProvider.Google, googleIdToken)
            {
                DeviceLabel = SystemInfo.deviceName
            });

            var providers = await PersistlyGameSaves.Shared.ListLinkedProvidersAsync();
            Debug.Log("Linked Persistly providers: " + providers.Count);
        }
    }
}
