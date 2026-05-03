using System.Threading.Tasks;
using UnityEngine;

namespace Persistly.Unity.Examples
{
    public sealed class MinimalUsage : MonoBehaviour
    {
        [SerializeField] private string runtimeKey = "";

        private PersistlyClient client;

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(runtimeKey))
            {
                throw new UnityException("Set a Persistly runtime key in the inspector before running this example.");
            }

            client = new PersistlyClient(new PersistlyClientOptions(runtimeKey));
        }

        private async void Start()
        {
            // Replace the JSON strings with your own state and metadata payloads.
            var createRequest = new PersistlyCreateSaveRequest(
                "{\"gold\":100,\"level\":1}",
                "{\"characterName\":\"Ayla\",\"slot\":2}",
                "player-184");

            PersistlySave save = await client.CreateSaveAsync(createRequest);
            Debug.Log("Created save " + save.SaveId + " version " + save.Version);

            PersistlySave loaded = await client.LoadSaveAsync(save.SaveId);
            Debug.Log("Loaded save " + loaded.SaveId + " at " + loaded.UpdatedAt.ToString("O"));

            var syncRequest = new PersistlySyncSaveRequest(
                "{\"gold\":120,\"level\":2}",
                loaded.Version,
                "{\"characterName\":\"Ayla\",\"slot\":2}");

            PersistlySyncResponse sync = await client.SyncSaveAsync(save.SaveId, syncRequest);
            Debug.Log("Sync status: " + sync.Status.ToString());
        }
    }
}
