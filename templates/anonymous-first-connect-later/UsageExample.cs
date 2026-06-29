using System.Threading.Tasks;
using Persistly.Templates.AnonymousFirstConnectLater;
using UnityEngine;

public sealed class UsageExample : MonoBehaviour
{
    private readonly PersistlySaveService _saves = new();

    private async Task Start()
    {
        await _saves.ConfigureAsync("ps_test_replace_me");
        await _saves.SaveAsync(new PersistlySaveService.PlayerSaveState
        {
            Level = 3,
            Coins = 250,
            Checkpoint = "meadow-gate"
        });
        await _saves.SyncAsync();

        // Get this from Firebase Auth in your game. Use the matching Supabase or Auth0
        // connect helper when those SDKs provide the token instead.
        var firebaseIdToken = "firebase_id_token_from_your_login_flow";
        var connected = await _saves.ConnectFirebaseAsync(firebaseIdToken);
        if (!connected)
        {
            // account_auth_conflict means local anonymous progress is still present.
            // Only switch after confirmation.
        }
    }
}
