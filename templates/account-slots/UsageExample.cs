using System.Threading.Tasks;
using UnityEngine;

namespace Persistly.Templates.AccountSlots
{
    public sealed class PersistlyUsageExample : MonoBehaviour
    {
        private readonly PersistlySaveService _saves = new PersistlySaveService();

        private async void Start()
        {
            await _saves.ConfigureAsync("ps_test_replace_me");
        }

        public async Task FirstDeviceSaveAsync()
        {
            var restorePayload = await _saves.ExportAccountForBackendAsync();
            await SendPersistlySessionToBackendAsync(restorePayload);

            await _saves.SaveSlotAsync("campaign-1", new AccountSlotSaveState
            {
                Level = 7,
                Coins = 1200
            }, "Campaign 1");
            await _saves.SyncSlotAsync("campaign-1");
        }

        public async Task SecondDeviceRestoreAsync()
        {
            var restorePayload = await FetchPersistlySessionFromBackendAsync();
            await _saves.AttachAccountAsync(restorePayload);
        }

        private Task SendPersistlySessionToBackendAsync(AccountRestorePayload payload)
        {
            // Replace with your authenticated backend request. Do not log the token.
            _ = payload.AccountId;
            return Task.CompletedTask;
        }

        private Task<AccountRestorePayload> FetchPersistlySessionFromBackendAsync()
        {
            // Replace with your authenticated backend request.
            return Task.FromResult(new AccountRestorePayload
            {
                AccountId = "acc_replace_me",
                AccountSessionToken = "pst_replace_me"
            });
        }
    }
}
