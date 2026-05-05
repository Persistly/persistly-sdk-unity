#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Persistly.Unity
{
    public enum PersistlySlotStatus
    {
        LocalSaved,
        Synced,
        Conflict,
        Offline,
        RateLimited
    }

    public class PersistlySlotResult
    {
        public PersistlySlotResult(string slotKey, PersistlySlotStatus status)
        {
            SlotKey = slotKey;
            Status = status;
        }

        public string SlotKey { get; }

        public PersistlySlotStatus Status { get; }
    }

    public sealed class PersistlySlotResult<TState> : PersistlySlotResult
    {
        public PersistlySlotResult(string slotKey, PersistlySlotStatus status, TState state)
            : base(slotKey, status)
        {
            State = state;
        }

        public TState State { get; }
    }

    public sealed class PersistlyGameSavesSettings
    {
        public PersistlyGameSavesSettings(string runtimeKey, string playerRef, int syncIntervalSeconds = 60)
        {
            RuntimeKey = runtimeKey;
            PlayerRef = playerRef;
            SyncIntervalSeconds = syncIntervalSeconds;
        }

        public string RuntimeKey { get; }

        public string PlayerRef { get; }

        public int SyncIntervalSeconds { get; }
    }

    public sealed class PersistlyGameSaves
    {
        private static PersistlyGameSaves? _shared;

        private readonly Dictionary<string, SlotSnapshot> _slots = new Dictionary<string, SlotSnapshot>(StringComparer.Ordinal);
        private readonly object _gate = new object();

        private PersistlyGameSaves(PersistlyGameSavesSettings settings)
        {
            Settings = settings;
        }

        public static PersistlyGameSaves Shared
        {
            get
            {
                if (_shared == null)
                {
                    throw new PersistlyConfigurationError("persistly_game_saves_not_configured: call PersistlyGameSaves.ConfigureAsync before using PersistlyGameSaves.Shared.");
                }

                return _shared;
            }
        }

        public PersistlyGameSavesSettings Settings { get; }

        public static Task ConfigureAsync(PersistlyGameSavesSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (string.IsNullOrWhiteSpace(settings.RuntimeKey))
            {
                throw new PersistlyConfigurationError("PersistlyGameSavesSettings.RuntimeKey must be set.");
            }

            if (string.IsNullOrWhiteSpace(settings.PlayerRef))
            {
                throw new PersistlyConfigurationError("PersistlyGameSavesSettings.PlayerRef must be set.");
            }

            if (settings.SyncIntervalSeconds < 0)
            {
                throw new PersistlyConfigurationError("PersistlyGameSavesSettings.SyncIntervalSeconds must be zero or greater.");
            }

            _shared = new PersistlyGameSaves(new PersistlyGameSavesSettings(
                settings.RuntimeKey.Trim(),
                settings.PlayerRef.Trim(),
                settings.SyncIntervalSeconds));
            return Task.CompletedTask;
        }

        public Task<PersistlySlotResult> SaveSlotAsync<TState>(string slotKey, TState state)
        {
            var normalizedSlotKey = NormalizeSlotKey(slotKey);
            var json = JsonUtility.ToJson(state);

            lock (_gate)
            {
                _slots[normalizedSlotKey] = new SlotSnapshot(json);
            }

            return Task.FromResult(new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.LocalSaved));
        }

        public Task<PersistlySlotResult<TState>> LoadSlotAsync<TState>(string slotKey)
        {
            var normalizedSlotKey = NormalizeSlotKey(slotKey);
            SlotSnapshot snapshot;

            lock (_gate)
            {
                if (!_slots.TryGetValue(normalizedSlotKey, out snapshot))
                {
                    throw new PersistlyConfigurationError("slot_not_found: no local save exists for slotKey.");
                }
            }

            var state = JsonUtility.FromJson<TState>(snapshot.StateJson);
            return Task.FromResult(new PersistlySlotResult<TState>(normalizedSlotKey, PersistlySlotStatus.LocalSaved, state));
        }

        public Task<PersistlySlotResult> ForceSyncAsync(string slotKey)
        {
            var normalizedSlotKey = NormalizeSlotKey(slotKey);
            return Task.FromResult(new PersistlySlotResult(normalizedSlotKey, PersistlySlotStatus.LocalSaved));
        }

        private static string NormalizeSlotKey(string slotKey)
        {
            if (string.IsNullOrWhiteSpace(slotKey))
            {
                throw new PersistlyConfigurationError("slotKey must be set.");
            }

            return slotKey.Trim();
        }

        private sealed class SlotSnapshot
        {
            public SlotSnapshot(string stateJson)
            {
                StateJson = stateJson;
            }

            public string StateJson { get; }
        }
    }
}
