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
        public string CharacterName = "Ayla";
        public string SlotLabel = "Beacon-A";
    }

    [Serializable]
    public sealed class LastBeaconProfile
    {
        public LastBeaconConfig Config = new LastBeaconConfig();
        public string ProfileSaveId = string.Empty;
        public string ProfileSessionToken = string.Empty;
        public string CharacterSaveId = string.Empty;
        public int Version = 0;
        public LastBeaconSaveState State = new LastBeaconSaveState();
    }

    public sealed class LastBeaconProfileStore
    {
        private readonly string _absolutePath;

        public LastBeaconProfileStore(string fileName = "last_beacon_profile.json")
        {
            _absolutePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        public LastBeaconProfile Load()
        {
            if (!File.Exists(_absolutePath))
            {
                return new LastBeaconProfile();
            }

            var raw = File.ReadAllText(_absolutePath);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new LastBeaconProfile();
            }

            try
            {
                return JsonUtility.FromJson<LastBeaconProfile>(raw) ?? new LastBeaconProfile();
            }
            catch
            {
                return new LastBeaconProfile();
            }
        }

        public void Save(LastBeaconProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_absolutePath) ?? Application.persistentDataPath);
            File.WriteAllText(_absolutePath, JsonUtility.ToJson(profile, true));
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
