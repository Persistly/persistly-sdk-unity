using System.Collections.Generic;
using System.Threading.Tasks;
using Persistly.Unity;
using UnityEngine;

namespace Persistly.Templates.MultiSlot
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

        public IReadOnlyList<PersistlySlotInspection> ListSlots()
        {
            return PersistlyGameSaves.Shared.ListSlotDataAsync();
        }

        public Task<PersistlySlotResult<CampaignSaveState>> LoadSlotAsync(string slotId)
        {
            return PersistlyGameSaves.Shared.LoadSlotAsync<CampaignSaveState>(slotId);
        }

        public Task<PersistlySlotResult> SaveSlotAsync(string slotId, CampaignSaveState state, string label)
        {
            return PersistlyGameSaves.Shared.SaveSlotAsync(slotId, state, new PersistlySaveSlotOptions
            {
                SlotInfoJson = JsonUtility.ToJson(new SlotInfo { Label = label })
            });
        }

        public Task<PersistlySlotResult> SyncSlotAsync(string slotId)
        {
            return PersistlyGameSaves.Shared.ForceSyncAsync(slotId);
        }

    }

    [System.Serializable]
    public sealed class CampaignSaveState
    {
        public int Level;
        public int Coins;
        public string Quest = "harbor";
    }

    [System.Serializable]
    public sealed class SlotInfo
    {
        public string Label = string.Empty;
    }
}
