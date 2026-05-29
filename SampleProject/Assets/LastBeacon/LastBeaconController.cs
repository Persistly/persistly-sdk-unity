#nullable enable
using System;
using System.Threading.Tasks;
using Persistly.Unity;
using UnityEngine;

namespace Persistly.Unity.LastBeacon
{
    [Serializable]
    public sealed class LastBeaconSlotInfo
    {
        public string slotName = "Ayla";
        public string slotLabel = "Beacon-A";
        public string build = "unity-last-beacon";
    }

    public sealed class LastBeaconController : MonoBehaviour
    {
        private const float AutoSyncIntervalSeconds = 20f;
        private const float ReferenceWidth = 1440f;
        private const float ReferenceHeight = 900f;
        private const string SlotIdFallback = "Beacon-A";

        private readonly LastBeaconState _state = new LastBeaconState();
        private LastBeaconAccountStore _store = null!;
        private LastBeaconAccount _account = null!;
        private Task? _pendingTask;
        private float _syncCountdown = AutoSyncIntervalSeconds;
        private string _status = "Configure Persistly and connect to create or resume a save.";
        private bool _facadeConfigured;
        private bool _connected;
        private Vector2 _scrollPosition;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            _store = new LastBeaconAccountStore();
            _account = _store.Load();
            _state.LoadFrom(_account.State);
            _status = string.IsNullOrWhiteSpace(_account.SlotId)
                ? "No synced slot linked yet. Save locally, then sync when ready."
                : "Stored slot saveId found. Connect / Resume loads the local facade slot first.";
        }

        private void Update()
        {
            _state.Tick(Time.deltaTime);
            _account.State = _state.ToSaveState();

            if (_connected && _pendingTask == null)
            {
                _syncCountdown -= Time.deltaTime;
                if (_syncCountdown <= 0f)
                {
                    BeginTask(SyncCurrentSaveAsync, "Syncing beacon state through PersistlyGameSaves...");
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PersistAccount();
            }
        }

        private void OnApplicationQuit()
        {
            PersistAccount();
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
            _account.Config.RuntimeKey = DrawField("Runtime Key", _account.Config.RuntimeKey);
            _account.Config.PlayerRef = DrawField("Player reference", _account.Config.PlayerRef);
            _account.Config.SlotName = DrawField("Slot Name", _account.Config.SlotName);
            _account.Config.SlotLabel = DrawField("Slot Label", _account.Config.SlotLabel);
            GUILayout.Label("Persistent path: " + Application.persistentDataPath);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Config", GUILayout.Height(32f)))
            {
                PersistAccount();
                _facadeConfigured = false;
                _status = "Configuration saved locally.";
            }

            GUI.enabled = _pendingTask == null;
            if (GUILayout.Button(string.IsNullOrWhiteSpace(_account.SlotId) ? "Save & Sync Slot" : "Connect / Resume", GUILayout.Height(32f)))
            {
                if (string.IsNullOrWhiteSpace(_account.SlotId))
                {
                    BeginTask(CreateAccountAsync, "Saving local slot and syncing through PersistlyGameSaves...");
                }
                else
                {
                    BeginTask(LoadAccountSlotAsync, "Loading local PersistlyGameSaves slot...");
                }
            }

            GUI.enabled = !string.IsNullOrWhiteSpace(_account.SlotId) && _pendingTask == null;
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
            GUILayout.Label("Account Save ID: " + (string.IsNullOrWhiteSpace(_account.AccountId) ? "(none)" : _account.AccountId));
            GUILayout.Label("Slot Save ID: " + (string.IsNullOrWhiteSpace(_account.SlotId) ? "(none)" : _account.SlotId));
            GUILayout.Label("Version: " + _account.Version);
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
                PersistAccount();
            }

            if (GUILayout.Button("Hire Worker (" + _state.WorkerCost() + ")", GUILayout.Height(36f)))
            {
                if (_state.TryHireWorker())
                {
                    PersistAccount();
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
                    PersistAccount();
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
            if (GUILayout.Button("Reset Local Account", GUILayout.Height(32f)))
            {
                _store.Reset();
                _account = new LastBeaconAccount();
                _state.LoadFrom(_account.State);
                _connected = false;
                _facadeConfigured = false;
                _syncCountdown = AutoSyncIntervalSeconds;
                _status = "Local account reset. Remote save remains untouched.";
            }

            if (GUILayout.Button("Reset Countdown", GUILayout.Height(32f)))
            {
                _syncCountdown = AutoSyncIntervalSeconds;
                _status = "Auto-sync countdown reset.";
            }
            GUILayout.EndHorizontal();
        }

        private async Task CreateAccountAsync()
        {
            await SaveCurrentSlotLocalAsync();
            await SyncCurrentSaveAsync();
        }

        private async Task LoadAccountSlotAsync()
        {
            await EnsureFacadeAsync();
            var loaded = await PersistlyGameSaves.Shared.LoadSlotAsync<LastBeaconSaveState>(NormalizeSlotId(_account.Config.SlotLabel));
            if (!loaded.Found || loaded.State == null)
            {
                _status = "No local PersistlyGameSaves slot found. Use Save & Sync Slot to create one.";
                return;
            }

            _account.State = loaded.State;
            _account.Version = loaded.Version ?? _account.Version;
            _account.SlotId = loaded.SlotId ?? _account.SlotId;
            _state.LoadFrom(_account.State);
            _connected = true;
            _syncCountdown = AutoSyncIntervalSeconds;
            RefreshAccountSessionAndSlotInspection();
            _status = "Loaded local slot from PersistlyGameSaves. Remote state was not imported automatically.";
        }

        private async Task SyncCurrentSaveAsync()
        {
            await SaveCurrentSlotLocalAsync();
            var sync = await PersistlyGameSaves.Shared.ForceSyncAsync(NormalizeSlotId(_account.Config.SlotLabel));
            RefreshAccountSessionAndSlotInspection();
            _connected = sync.Status == PersistlySlotStatus.Synced || sync.Status == PersistlySlotStatus.NoChanges || sync.Status == PersistlySlotStatus.Conflict;
            _syncCountdown = AutoSyncIntervalSeconds;

            if (sync.Status == PersistlySlotStatus.Conflict)
            {
                _status = "Conflict received. Local beacon state was kept. Use your game UI to compare local/cloud payloads before accepting cloud.";
                return;
            }

            _status = "PersistlyGameSaves sync status: " + sync.Status + ". Version " + _account.Version + ".";
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
                PersistAccount();
            }
        }

        private async Task SaveCurrentSlotLocalAsync()
        {
            await EnsureFacadeAsync();
            _account.State = _state.ToSaveState();
            await PersistlyGameSaves.Shared.SaveSlotAsync(NormalizeSlotId(_account.Config.SlotLabel), _account.State, new PersistlySaveSlotOptions
            {
                SlotInfoJson = JsonUtility.ToJson(BuildSlotInfo())
            });
            RefreshAccountSessionAndSlotInspection();
        }

        private LastBeaconSlotInfo BuildSlotInfo()
        {
            return new LastBeaconSlotInfo
            {
                slotName = string.IsNullOrWhiteSpace(_account.Config.SlotName) ? "Ayla" : _account.Config.SlotName.Trim(),
                slotLabel = string.IsNullOrWhiteSpace(_account.Config.SlotLabel) ? "Beacon-A" : _account.Config.SlotLabel.Trim(),
            };
        }

        private async Task EnsureFacadeAsync()
        {
            if (_facadeConfigured)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_account.Config.RuntimeKey))
            {
                throw new PersistlyConfigurationError("Persistly Runtime Key must be configured first.");
            }

            await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings(_account.Config.RuntimeKey.Trim())
            {
                PlayerRef = NormalizeOptional(_account.Config.PlayerRef),
                AccountId = NormalizeOptional(_account.AccountId),
                AccountSessionToken = NormalizeOptional(_account.AccountSessionToken),
                Store = new FilePersistlyGameSavesStore(Application.persistentDataPath),
                OnSyncResult = OnPersistlySyncResult
            });
            _facadeConfigured = true;
            RefreshAccountSessionAndSlotInspection();
        }

        private void OnPersistlySyncResult(PersistlySyncNotification notification)
        {
            if (notification.Status == PersistlyGameSaveStatus.Conflict && notification.Conflict != null)
            {
                _status = "Conflict callback for " + notification.Target + ". Local payload is still active; cloud payload is available for UI comparison.";
            }
        }

        private void RefreshAccountSessionAndSlotInspection()
        {
            var session = PersistlyGameSaves.Shared.GetAccountSession(includeToken: true);
            _account.AccountId = session.AccountId ?? string.Empty;
            _account.AccountSessionToken = session.AccountSessionToken ?? string.Empty;

            var inspect = PersistlyGameSaves.Shared.InspectSlot(NormalizeSlotId(_account.Config.SlotLabel));
            _account.SlotId = inspect.SlotId ?? _account.SlotId;
            _account.Version = inspect.Version ?? _account.Version;
        }

        private void PersistAccount()
        {
            _account.State = _state.ToSaveState();
            _store.Save(_account);
        }

        private static string? NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizeSlotId(string value)
        {
            return PersistlySlotId.Normalize(string.IsNullOrWhiteSpace(value) ? SlotIdFallback : value.Trim());
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
