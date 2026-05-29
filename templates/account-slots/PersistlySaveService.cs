using System;
using System.Threading.Tasks;
using Persistly.Unity;
using UnityEngine;

namespace Persistly.Templates.AccountSlots
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

        public async Task AttachAccountAsync(AccountRestorePayload payload)
        {
            await PersistlyGameSaves.Shared.AttachAccountAsync(payload.AccountId, payload.AccountSessionToken);
        }

        public async Task<AccountRestorePayload> ExportAccountForBackendAsync()
        {
            await PersistlyGameSaves.Shared.EnsureAccountAsync();
            var session = PersistlyGameSaves.Shared.GetAccountSession(includeToken: true);

            if (string.IsNullOrWhiteSpace(session.AccountId) || string.IsNullOrWhiteSpace(session.AccountSessionToken))
            {
                throw new InvalidOperationException("Persistly account session is not ready to export.");
            }

            return new AccountRestorePayload
            {
                AccountId = session.AccountId,
                AccountSessionToken = session.AccountSessionToken
            };
        }

        public Task<PersistlySlotResult> SaveSlotAsync(string slotId, AccountSlotSaveState state, string label)
        {
            return PersistlyGameSaves.Shared.SaveSlotAsync(slotId, state, new PersistlySaveSlotOptions
            {
                SlotInfoJson = JsonUtility.ToJson(new SlotInfo { Label = label })
            });
        }

        public Task<PersistlySlotResult<AccountSlotSaveState>> LoadSlotAsync(string slotId)
        {
            return PersistlyGameSaves.Shared.LoadSlotAsync<AccountSlotSaveState>(slotId);
        }

        public Task<PersistlySlotResult> SyncSlotAsync(string slotId)
        {
            return PersistlyGameSaves.Shared.ForceSyncAsync(slotId);
        }

    }

    public sealed class AccountRestorePayload
    {
        public string AccountId = string.Empty;
        public string AccountSessionToken = string.Empty;
    }

    [System.Serializable]
    public sealed class AccountSlotSaveState
    {
        public int Level;
        public int Coins;
    }

    [System.Serializable]
    public sealed class SlotInfo
    {
        public string Label = string.Empty;
    }
}
