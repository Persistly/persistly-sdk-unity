using System.Threading.Tasks;
using UnityEngine;

namespace Persistly.Unity.Examples
{
    public sealed class MinimalUsage : MonoBehaviour
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
                PlayerRef = "player-184",
                Store = new FilePersistlyGameSavesStore(Application.persistentDataPath)
            });

            await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new MinimalSaveState
            {
                Gold = 120,
                Level = 2
            }, new PersistlySaveSlotOptions
            {
                MetadataJson = "{\"characterName\":\"Ayla\"}"
            });

            var local = await PersistlyGameSaves.Shared.LoadSlotAsync<MinimalSaveState>("autosave");
            Debug.Log("Loaded local level " + local.State.Level);

            var sync = await PersistlyGameSaves.Shared.ForceSyncAsync("autosave");
            Debug.Log("Sync status: " + sync.Status.ToString());
        }

        [System.Serializable]
        private sealed class MinimalSaveState
        {
            public int Gold;

            public int Level;
        }
    }
}
