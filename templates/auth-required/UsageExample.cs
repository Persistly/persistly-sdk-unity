using System.Threading.Tasks;
using UnityEngine;

namespace Persistly.Templates.AuthRequired
{
    public sealed class UsageExample : MonoBehaviour
    {
        private readonly PersistlySaveService _saves = new PersistlySaveService();

        private async void Start()
        {
            await _saves.ConfigureAsync("ps_test_replace_me");
        }

        public async Task SaveBeforeSignInAsync()
        {
            var local = await _saves.SaveLocalAsync(new AuthRequiredSaveState
            {
                Level = 1,
                Coins = 50
            });

            if (local.Status == Persistly.Unity.PersistlySlotStatus.AuthRequired)
            {
                ShowSignInPrompt();
            }
        }

        public async Task SignInAndSyncAsync(string firebaseIdToken)
        {
            await _saves.SignInWithFirebaseAsync(firebaseIdToken);
            await _saves.SyncAsync();
        }

        public Task SignOutAsync()
        {
            return _saves.SignOutAsync();
        }

        private void ShowSignInPrompt()
        {
        }
    }
}
