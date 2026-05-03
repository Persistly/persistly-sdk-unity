#nullable enable
using System;
using System.Threading.Tasks;
using Persistly.Unity;
using UnityEngine;

namespace Persistly.Unity.LastBeacon
{
    [Serializable]
    public sealed class LastBeaconMetadata
    {
        public string characterName = "Ayla";
        public string slotLabel = "Beacon-A";
        public string build = "unity-last-beacon";
    }

    public sealed class LastBeaconController : MonoBehaviour
    {
        private const float AutoSyncIntervalSeconds = 20f;
        private const float ReferenceWidth = 1440f;
        private const float ReferenceHeight = 900f;

        private readonly LastBeaconState _state = new LastBeaconState();
        private LastBeaconProfileStore _store = null!;
        private LastBeaconProfile _profile = null!;
        private PersistlyClient? _client;
        private Task? _pendingTask;
        private float _syncCountdown = AutoSyncIntervalSeconds;
        private string _status = "Configure Persistly and connect to create or resume a save.";
        private bool _connected;
        private Vector2 _scrollPosition;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            _store = new LastBeaconProfileStore();
            _profile = _store.Load();
            _state.LoadFrom(_profile.State);
            RebuildClientIfPossible();
            _status = string.IsNullOrWhiteSpace(_profile.SaveId)
                ? "No remote save linked yet. Create one when ready."
                : "Stored saveId found. Use Connect / Resume to load the canonical server state.";
        }

        private void Update()
        {
            _state.Tick(Time.deltaTime);
            _profile.State = _state.ToSaveState();

            if (_connected && _pendingTask == null)
            {
                _syncCountdown -= Time.deltaTime;
                if (_syncCountdown <= 0f)
                {
                    BeginTask(SyncCurrentSaveAsync, "Auto-syncing beacon state...");
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PersistProfile();
            }
        }

        private void OnApplicationQuit()
        {
            PersistProfile();
        }

        private void OnGUI()
        {
            var uiScale = CalculateUiScale();
            var virtualWidth = Screen.width / uiScale;
            var virtualHeight = Screen.height / uiScale;
            var margin = Mathf.Lerp(24f, 40f, Mathf.InverseLerp(0.9f, 1.8f, uiScale));
            var panelWidth = Mathf.Clamp(virtualWidth * 0.34f, 520f, 760f);
            var panelHeight = Mathf.Clamp(virtualHeight - (margin * 2f), 640f, virtualHeight - 12f);
            var panelRect = new Rect(margin, margin, panelWidth, panelHeight);

            GUI.depth = 0;
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(uiScale, uiScale, 1f));

            GUILayout.BeginArea(panelRect, GUI.skin.box);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true, GUILayout.Width(panelRect.width - 20f), GUILayout.Height(panelRect.height - 20f));

            GUILayout.Label("Last Beacon", HeaderStyle());
            GUILayout.Label("Unity sample for Persistly. Gather scrap, hire workers, upgrade the core, and sync the beacon to the runtime API.");
            GUILayout.Space(12f);

            DrawConnectionSection();
            GUILayout.Space(12f);
            DrawStatusSection();
            GUILayout.Space(12f);
            DrawGameSection();
            GUILayout.Space(12f);
            DrawActionsSection();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
        }

        private void DrawConnectionSection()
        {
            GUILayout.Label("Persistly Connection", SectionStyle());
            _profile.Config.BaseUrl = DrawField("API Base URL", _profile.Config.BaseUrl);
            _profile.Config.RuntimeKey = DrawField("Runtime Key", _profile.Config.RuntimeKey);
            _profile.Config.ExternalUserId = DrawField("External User ID", _profile.Config.ExternalUserId);
            _profile.Config.CharacterName = DrawField("Character Name", _profile.Config.CharacterName);
            _profile.Config.SlotLabel = DrawField("Slot Label", _profile.Config.SlotLabel);
            GUILayout.Label("Persistent path: " + Application.persistentDataPath);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Config", GUILayout.Height(32f)))
            {
                PersistProfile();
                RebuildClientIfPossible();
                _status = "Configuration saved locally.";
            }

            GUI.enabled = _pendingTask == null;
            if (GUILayout.Button(string.IsNullOrWhiteSpace(_profile.SaveId) ? "Create Remote Save" : "Connect / Resume", GUILayout.Height(32f)))
            {
                if (string.IsNullOrWhiteSpace(_profile.SaveId))
                {
                    BeginTask(CreateRemoteSaveAsync, "Creating Persistly save...");
                }
                else
                {
                    BeginTask(LoadRemoteSaveAsync, "Loading canonical Persistly save...");
                }
            }

            GUI.enabled = !string.IsNullOrWhiteSpace(_profile.SaveId) && _pendingTask == null;
            if (GUILayout.Button("Sync Now", GUILayout.Height(32f)))
            {
                BeginTask(SyncCurrentSaveAsync, "Syncing beacon state...");
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void DrawStatusSection()
        {
            GUILayout.Label("Connection Status", SectionStyle());
            GUILayout.Label("Save ID: " + (string.IsNullOrWhiteSpace(_profile.SaveId) ? "(none)" : _profile.SaveId));
            GUILayout.Label("Version: " + _profile.Version);
            GUILayout.Label("Connected: " + (_connected ? "yes" : "no"));
            GUILayout.Label("Auto-sync in: " + Mathf.Max(_syncCountdown, 0f).ToString("0.0") + "s");
            GUILayout.TextArea(_status, GUILayout.MinHeight(84f));
        }

        private void DrawGameSection()
        {
            GUILayout.Label("Beacon Loop", SectionStyle());
            GUILayout.Label("Scrap: " + _state.Scrap);
            GUILayout.Label("Workers: " + _state.Workers);
            GUILayout.Label("Core Level: " + _state.Level);
            GUILayout.Label("Manual Gather: +" + _state.ManualGatherAmount);
            GUILayout.Label("Power Cells: " + _state.PowerCells);
            GUILayout.Label("Core Charge: " + _state.CoreCharge.ToString("0.0") + "%");
            GUILayout.Label("Passive Scrap / sec: " + _state.PassiveScrapPerSecond().ToString("0.0"));
            GUILayout.Label("Charge / sec: " + _state.ChargeRatePerSecond().ToString("0.0"));
            GUILayout.Label("Ticks: " + _state.TotalTicks);
        }

        private void DrawActionsSection()
        {
            GUILayout.Label("Actions", SectionStyle());
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Gather Scrap", GUILayout.Height(36f)))
            {
                _state.Gather();
                PersistProfile();
            }

            if (GUILayout.Button("Hire Worker (" + _state.WorkerCost() + ")", GUILayout.Height(36f)))
            {
                if (_state.TryHireWorker())
                {
                    PersistProfile();
                    _status = "Worker hired. Save locally updated.";
                }
                else
                {
                    _status = "Not enough scrap to hire another worker.";
                }
            }

            if (GUILayout.Button("Upgrade Core (" + _state.CoreUpgradeCost() + ")", GUILayout.Height(36f)))
            {
                if (_state.TryUpgradeCore())
                {
                    PersistProfile();
                    _status = "Core upgraded. Sync when ready.";
                }
                else
                {
                    _status = "Not enough scrap to upgrade the core.";
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Local Profile", GUILayout.Height(32f)))
            {
                _store.Reset();
                _profile = new LastBeaconProfile();
                _state.LoadFrom(_profile.State);
                _connected = false;
                _syncCountdown = AutoSyncIntervalSeconds;
                RebuildClientIfPossible();
                _status = "Local profile reset. Remote save remains untouched.";
            }

            if (GUILayout.Button("Reset Countdown", GUILayout.Height(32f)))
            {
                _syncCountdown = AutoSyncIntervalSeconds;
                _status = "Auto-sync countdown reset.";
            }
            GUILayout.EndHorizontal();
        }

        private async Task CreateRemoteSaveAsync()
        {
            var client = EnsureClient();
            var created = await client.CreateSaveAsync(new PersistlyCreateSaveRequest(
                JsonUtility.ToJson(_state.ToSaveState()),
                JsonUtility.ToJson(BuildMetadata()),
                NormalizeOptional(_profile.Config.ExternalUserId)));

            ApplyCanonicalSave(created, "Created new Persistly save.");
            _connected = true;
        }

        private async Task LoadRemoteSaveAsync()
        {
            var client = EnsureClient();
            var loaded = await client.LoadSaveAsync(_profile.SaveId);
            ApplyCanonicalSave(loaded, "Loaded canonical state from Persistly.");
            _connected = true;
        }

        private async Task SyncCurrentSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(_profile.SaveId))
            {
                throw new PersistlyConfigurationError("No saveId is stored yet. Create a remote save first.");
            }

            var client = EnsureClient();
            var sync = await client.SyncSaveAsync(_profile.SaveId, new PersistlySyncSaveRequest(
                JsonUtility.ToJson(_state.ToSaveState()),
                _profile.Version,
                JsonUtility.ToJson(BuildMetadata())));

            ApplyCanonicalSave(sync.Save, sync.Status == PersistlySyncStatus.Conflict
                ? "Conflict received. Local state replaced with canonical remote save."
                : "Sync accepted. Canonical state refreshed.");
            _connected = true;
        }

        private void BeginTask(Func<Task> taskFactory, string status)
        {
            if (_pendingTask != null)
            {
                return;
            }

            _status = status;
            _pendingTask = RunTaskAsync(taskFactory);
        }

        private async Task RunTaskAsync(Func<Task> taskFactory)
        {
            try
            {
                await taskFactory();
            }
            catch (Exception exception)
            {
                _connected = false;
                _status = "Persistly error: " + exception.Message;
            }
            finally
            {
                _pendingTask = null;
                PersistProfile();
            }
        }

        private void ApplyCanonicalSave(PersistlySave save, string status)
        {
            _profile.SaveId = save.SaveId;
            _profile.Version = save.Version;
            _profile.State = JsonUtility.FromJson<LastBeaconSaveState>(save.StateJson) ?? new LastBeaconSaveState();
            _state.LoadFrom(_profile.State);
            _syncCountdown = AutoSyncIntervalSeconds;
            _status = status + " Version " + save.Version + ".";
        }

        private LastBeaconMetadata BuildMetadata()
        {
            return new LastBeaconMetadata
            {
                characterName = string.IsNullOrWhiteSpace(_profile.Config.CharacterName) ? "Ayla" : _profile.Config.CharacterName.Trim(),
                slotLabel = string.IsNullOrWhiteSpace(_profile.Config.SlotLabel) ? "Beacon-A" : _profile.Config.SlotLabel.Trim(),
            };
        }

        private PersistlyClient EnsureClient()
        {
            RebuildClientIfPossible();
            if (_client == null)
            {
                throw new PersistlyConfigurationError("Persistly Base URL and Runtime Key must be configured first.");
            }

            return _client;
        }

        private void RebuildClientIfPossible()
        {
            if (string.IsNullOrWhiteSpace(_profile.Config.BaseUrl) || string.IsNullOrWhiteSpace(_profile.Config.RuntimeKey))
            {
                _client = null;
                return;
            }

            _client = new PersistlyClient(new PersistlyClientOptions(_profile.Config.BaseUrl, _profile.Config.RuntimeKey));
        }

        private void PersistProfile()
        {
            _profile.State = _state.ToSaveState();
            _store.Save(_profile);
        }

        private static string? NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static float CalculateUiScale()
        {
            var widthScale = Screen.width / ReferenceWidth;
            var heightScale = Screen.height / ReferenceHeight;
            return Mathf.Clamp(Mathf.Min(widthScale, heightScale), 1f, 2.1f);
        }

        private static GUIStyle HeaderStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
            };
        }

        private static GUIStyle SectionStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
            };
        }

        private static string DrawField(string label, string value)
        {
            GUILayout.Label(label);
            return GUILayout.TextField(value ?? string.Empty);
        }
    }
}
