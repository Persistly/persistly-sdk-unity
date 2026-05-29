using Persistly.Unity;
using UnityEngine;

namespace Persistly.Templates.MultiSlot
{
    public sealed class PersistlyUsageExample : MonoBehaviour
    {
        private readonly PersistlySaveService _saves = new PersistlySaveService();
        private const string SelectedSlotId = "campaign-1";

        private async void Start()
        {
            await _saves.ConfigureAsync("ps_test_replace_me");

            var loaded = await _saves.LoadSlotAsync(SelectedSlotId);
            if (loaded.Status == PersistlySlotStatus.NotFound)
            {
                await _saves.SaveSlotAsync(SelectedSlotId, new CampaignSaveState(), "Campaign 1");
            }
        }

        public async void SaveProgress(CampaignSaveState state)
        {
            await _saves.SaveSlotAsync(SelectedSlotId, state, "Campaign 1");
            await _saves.SyncSlotAsync(SelectedSlotId);
        }

        public int SavedSlotCount()
        {
            return _saves.ListSlots().Count;
        }
    }
}
