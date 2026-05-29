using System;
using System.IO;
using UnityEngine;

namespace Persistly.Unity.LastBeacon
{
    [Serializable]
    public sealed class LastBeaconConfig
    {
        // Legacy persisted field retained so older local config JSON still loads.
        // Last Beacon uses the SDK default API origin instead of exposing this in UI.
        public string BaseUrl = global::Persistly.Unity.PersistlyClientOptions.DefaultBaseUrl;
        public string RuntimeKey = string.Empty;
        public string PlayerRef = string.Empty;
        public string SlotName = "Ayla";
        public string SlotLabel = "Beacon-A";
    }

    [Serializable]
    public sealed class LastBeaconAccount
    {
        public LastBeaconConfig Config = new LastBeaconConfig();
        public string AccountId = string.Empty;
        public string AccountSessionToken = string.Empty;
        public string SlotId = string.Empty;
        public int Version = 0;
        public LastBeaconSaveState State = new LastBeaconSaveState();
    }

    public sealed class LastBeaconAccountStore
    {
        private readonly string _absolutePath;

        public LastBeaconAccountStore(string fileName = "last_beacon_account.json")
        {
            _absolutePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        public LastBeaconAccount Load()
        {
            if (!File.Exists(_absolutePath))
            {
                return new LastBeaconAccount();
            }

            var raw = File.ReadAllText(_absolutePath);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new LastBeaconAccount();
            }

            try
            {
                return JsonUtility.FromJson<LastBeaconAccount>(raw) ?? new LastBeaconAccount();
            }
            catch
            {
                return new LastBeaconAccount();
            }
        }

        public void Save(LastBeaconAccount account)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_absolutePath) ?? Application.persistentDataPath);
            File.WriteAllText(_absolutePath, JsonUtility.ToJson(account, true));
        }

        public void Reset()
        {
            if (File.Exists(_absolutePath))
            {
                File.Delete(_absolutePath);
            }
        }
    }
}
