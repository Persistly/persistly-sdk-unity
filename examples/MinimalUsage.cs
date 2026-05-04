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
            var createRequest = new PersistlyCreateProfileRequest(
                "{\"diamonds\":20}",
                "{\"characterName\":\"Ayla\",\"slot\":2}",
                "{\"gold\":100,\"level\":1}",
                "{\"displayName\":\"Ayla\"}",
                "player-184");

            PersistlyCreateProfileResponse created = await client.CreateProfileAsync(createRequest);
            Debug.Log("Created profile " + created.ProfileSaveId + " with character " + created.Character.Save.SaveId);

            PersistlySave loaded = await client.LoadProfileCharacterAsync(
                created.ProfileSaveId,
                created.ProfileSessionToken,
                created.Character.Save.SaveId);
            Debug.Log("Loaded character " + loaded.SaveId + " at " + loaded.UpdatedAt.ToString("O"));

            var syncRequest = new PersistlySyncSaveRequest(
                "{\"gold\":120,\"level\":2}",
                loaded.Version,
                "{\"characterName\":\"Ayla\",\"slot\":2}");

            PersistlySyncResponse sync = await client.SyncProfileCharacterAsync(
                created.ProfileSaveId,
                created.ProfileSessionToken,
                created.Character.Save.SaveId,
                syncRequest);
            Debug.Log("Sync status: " + sync.Status.ToString());
        }
    }
}
