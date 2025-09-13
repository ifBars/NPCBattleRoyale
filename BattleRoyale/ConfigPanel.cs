using bGUI;
using bGUI.Components;
using bGUI.Core.Extensions;
using bGUI.Core.Constants;
using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NPCBattleRoyale.BattleRoyale.Pages;
#if MONO
using ScheduleOne;
using NPC = ScheduleOne.NPCs.NPC;
using NPCManager = ScheduleOne.NPCs.NPCManager;
#else
using Il2CppScheduleOne;
using NPC = Il2CppScheduleOne.NPCs.NPC;
using NPCManager = Il2CppScheduleOne.NPCs.NPCManager;
#endif

namespace NPCBattleRoyale.BattleRoyale
{
    /// <summary>
    /// Singleton manager for the Battle Royale configuration UI.
    /// Follows the same pattern as DemoMenuManager.
    /// </summary>
    public class ConfigPanel : IDisposable
    {
        private static ConfigPanel _instance;
        public static ConfigPanel Instance => _instance ??= new ConfigPanel();

        // UI References - New Clean Design
        private CanvasWrapper _canvas;
        private PanelWrapper _mainPanel;
        private PanelWrapper _tabContentArea;
        private readonly List<ButtonWrapper> _tabButtons = new();
        private readonly string[] _tabNames = { "Setup", "Settings", "Presets" };
        
        // Current tab tracking
        private int _selectedTab = 0;
        
        // Page References
        private SetupPage _setupPage;
        private SettingsPage _settingsPage;
        private PresetsPage _presetsPage;

        private List<GroupDefinition> _groups;
        private List<RoundSettings> _presets;
        private RoundSettings _current = new();
        public RoundSettings CurrentSettings => _current;
        private bool _isVisible = false;

        // Input state snapshot to restore after closing
        private bool _prevTyping;
        private CursorLockMode _prevLock;
        private bool _prevCursorVisible;

        /// <summary>
        /// Shows or hides the config panel (toggle behavior).
        /// </summary>
        public static void Toggle()
        {
            Instance.ToggleVisibility();
        }

        /// <summary>
        /// Shows the config panel.
        /// </summary>
        public static void Show()
        {
            Instance.ShowPanel();
        }

        /// <summary>
        /// Hides the config panel.
        /// </summary>
        public static void Hide()
        {
            Instance.HidePanel();
        }

        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private ConfigPanel()
        {
            try
            {
                _groups = GroupConfig.LoadOrCreateDefaults();
                _presets = PresetConfig.LoadOrCreateDefaults();
                _current = _presets.Count > 0 ? _presets[0] : new RoundSettings();
                BuildUI();
                HidePanel(); // Start hidden
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[BR] Failed to initialize ConfigPanel: {ex}");
            }
        }

        /// <summary>
        /// Toggles panel visibility.
        /// </summary>
        private void ToggleVisibility()
        {
            if (_isVisible)
                HidePanel();
            else
                ShowPanel();
        }

        /// <summary>
        /// Shows the panel.
        /// </summary>
        private void ShowPanel()
        {
            if (_canvas == null) return;
            _canvas.GameObject.SetActive(true);
            _isVisible = true;
            EnableMouseAndTyping(true);
        }

        /// <summary>
        /// Hides the panel.
        /// </summary>
        private void HidePanel()
        {
            if (_canvas == null) return;
            _canvas.GameObject.SetActive(false);
            _isVisible = false;
            EnableMouseAndTyping(false);
        }

        private void EnableMouseAndTyping(bool enable)
        {
            try
            {
                if (enable)
                {
                    _prevTyping = GameInput.IsTyping;
                    _prevLock = Cursor.lockState;
                    _prevCursorVisible = Cursor.visible;
                    GameInput.IsTyping = true;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    GameInput.IsTyping = _prevTyping;
                    Cursor.lockState = _prevLock;
                    Cursor.visible = _prevCursorVisible;
                }
            }
            catch { }
        }

        /// <summary>
        /// Creates the entire UI using bGUI's elegant abstractions.
        /// Inspired by SimpleMenu.cs for clean, readable code.
        /// </summary>
        private void BuildUI()
        {
            // Create main canvas with beautiful fade-in effect
            _canvas = UI.Canvas("BR_ConfigCanvas")
                .SetRenderMode(RenderMode.ScreenSpaceOverlay)
                .SetSortingOrder(10000)
                .SetScaleMode(CanvasScaler.ScaleMode.ScaleWithScreenSize)
                .SetReferenceResolution(1920, 1080)
                .SetDontDestroyOnLoad(true)
                .FadeIn()
                .Build();
            
            _canvas.GameObject.SetActive(false);

            // Create the main container - a beautiful card with dark theme
            _mainPanel = UI.Panel(_canvas.RectTransform)
                .SetSize(1100, 820)
                .SetAnchor(0.5f, 0.5f)
                .SetPivot(0.5f, 0.5f)
                .SetPosition(0, 0)
                .SetBackgroundColor(new Color(0.1f, 0.11f, 0.14f, 0.95f))
                .Build();

            // Create stunning header
            CreateHeader();
            
            // Create tab navigation - sleek and modern
            CreateTabNavigation();
            
            // Create content area container
            CreateContentArea();
            
            // Create individual pages
            CreatePages();
            
            // Create footer with actions
            CreateFooter();
            
            // Show the setup tab by default
            ShowTab(0);
        }

        /// <summary>
        /// Creates a beautiful header with title, subtitle, and close button.
        /// </summary>
        private void CreateHeader()
        {
            // Header background
            var headerBg = UI.Panel(_mainPanel.RectTransform)
                .SetSize(1100, 90)
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(0, 0)
                .SetBackgroundColor(new Color(0.08f, 0.09f, 0.12f, 0.9f))
                .Build();

            // Main title
            UI.Text(headerBg.RectTransform)
                .SetContent("⚔️ Battle Royale Configuration")
                .SetFontSize(28)
                .SetFontStyle(FontStyle.Bold)
                .SetColor(new Color(0.7f, 0.8f, 1f, 1f))
                .SetAnchor(0.5f, 0.5f)
                .SetPivot(0.5f, 0.5f)
                .SetPosition(0, 10)
                .Build();

            // Subtitle
            UI.Text(headerBg.RectTransform)
                .SetContent("Configure your tournament settings")
                .SetFontSize(16)
                .SetColor(new Color(0.6f, 0.6f, 0.7f, 1f))
                .SetAnchor(0.5f, 0.5f)
                .SetPivot(0.5f, 0.5f)
                .SetPosition(0, -15)
                .Build();

            // Close button
            var closeBtn = UI.Button(_mainPanel.RectTransform)
                .SetText("✕")
                .SetSize(35, 35)
                .SetAnchor(1f, 1f)
                .SetPivot(1f, 1f)
                .SetPosition(-10, -10)
                .SetColors(
                    new Color(0.7f, 0.2f, 0.2f, 1f),
                    new Color(0.8f, 0.3f, 0.3f, 1f),
                    new Color(0.6f, 0.1f, 0.1f, 1f),
                    Color.gray)
                .Build();
            closeBtn.OnClick += HidePanel;
        }

        /// <summary>
        /// Creates the tab navigation bar - demonstrates theme consistency.
        /// </summary>
        private void CreateTabNavigation()
        {
            // Create tab bar background
            var tabBar = UI.Panel(_mainPanel.RectTransform)
                .SetSize(1080, 50)
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(0, -100)
                .SetBackgroundColor(new Color(0.06f, 0.07f, 0.1f, 0.8f))
                .Build();

            // Create individual tab buttons with proper spacing
            float tabWidth = 260f;
            float spacing = 20f;
            float totalWidth = (_tabNames.Length * tabWidth) + ((_tabNames.Length - 1) * spacing);
            float startX = -(totalWidth / 2f) + (tabWidth / 2f);
            
            for (int i = 0; i < _tabNames.Length; i++)
            {
                int tabIndex = i;
                float posX = startX + (i * (tabWidth + spacing));
                
                var tabButton = UI.Button(tabBar.RectTransform)
                    .SetText(_tabNames[i])
                    .SetSize(tabWidth, 38)
                    .SetAnchor(0.5f, 0.5f)
                    .SetPivot(0.5f, 0.5f)
                    .SetPosition(posX, 0)
                    .SetColors(
                        new Color(0.25f, 0.25f, 0.3f, 1f),
                        new Color(0.3f, 0.3f, 0.35f, 1f),
                        new Color(0.2f, 0.2f, 0.25f, 1f),
                        new Color(0.15f, 0.15f, 0.15f, 0.5f))
                    .Build();
                tabButton.OnClick += () => ShowTab(tabIndex);
                _tabButtons.Add(tabButton);
            }
        }

        /// <summary>
        /// Creates the main content area container.
        /// </summary>
        private void CreateContentArea()
        {
            _tabContentArea = UI.Panel(_mainPanel.RectTransform)
                .SetSize(1060, 650)
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(0, -170)
                .SetBackgroundColor(Color.clear) // Transparent container, pages handle their own backgrounds
                .Build();
        }

        /// <summary>
        /// Creates all configuration pages.
        /// </summary>
        private void CreatePages()
        {
            _setupPage = new SetupPage(_tabContentArea.RectTransform, _current, _groups, _presets);
            _settingsPage = new SettingsPage(_tabContentArea.RectTransform, _current, _groups, _presets);
            _presetsPage = new PresetsPage(_tabContentArea.RectTransform, _current, _groups, _presets);
            
            // Set up preset page callbacks
            _presetsPage.SetCallbacks(OnPresetChanged, SaveCurrentAsPreset);
        }

        /// <summary>
        /// Creates the footer with action buttons.
        /// </summary>
        private void CreateFooter()
        {
            // Footer background
            var footerBg = UI.Panel(_mainPanel.RectTransform)
                .SetSize(1100, 70)
                .SetAnchor(0.5f, 0f)
                .SetPivot(0.5f, 0f)
                .SetPosition(0, 0)
                .SetBackgroundColor(new Color(0.08f, 0.09f, 0.12f, 0.9f))
                .Build();

            // Start Tournament button (large, prominent center)
            var startBtn = UI.Button(footerBg.RectTransform)
                .SetText("🚀 Start")
                .SetSize(280, 50)
                .SetAnchor(0.5f, 0.5f)
                .SetPivot(0.5f, 0.5f)
                .SetPosition(0, 0)
                .SetColors(
                    new Color(0.2f, 0.7f, 0.3f, 1f),
                    new Color(0.3f, 0.8f, 0.4f, 1f),
                    new Color(0.1f, 0.6f, 0.2f, 1f),
                    Color.gray)
                .Build();
            startBtn.OnClick += StartFromSettings;

            // Close button on the right
            var footerCloseBtn = UI.Button(footerBg.RectTransform)
                .SetText("Close")
                .SetSize(100, 40)
                .SetAnchor(1f, 0.5f)
                .SetPivot(1f, 0.5f)
                .SetPosition(-20, 0)
                .SetColors(
                    new Color(0.4f, 0.4f, 0.45f, 1f),
                    new Color(0.5f, 0.5f, 0.55f, 1f),
                    new Color(0.3f, 0.3f, 0.35f, 1f),
                    Color.gray)
                .Build();
            footerCloseBtn.OnClick += HidePanel;

            // Teleport helpers on the left
            var toArenaBtn = UI.Button(footerBg.RectTransform)
                .SetText("Teleport: Arena")
                .SetSize(150, 40)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(20, 0)
                .SetColors(
                    new Color(0.35f, 0.55f, 0.95f, 1f),
                    new Color(0.45f, 0.65f, 1f, 1f),
                    new Color(0.25f, 0.45f, 0.85f, 1f),
                    Color.gray)
                .Build();
            toArenaBtn.OnClick += () => BattleRoyaleManager.Instance?.TeleportPlayerToArena();

            var toPanelBtn = UI.Button(footerBg.RectTransform)
                .SetText("Teleport: Panel")
                .SetSize(150, 40)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(180, 0)
                .SetColors(
                    new Color(0.35f, 0.55f, 0.95f, 1f),
                    new Color(0.45f, 0.65f, 1f, 1f),
                    new Color(0.25f, 0.45f, 0.85f, 1f),
                    Color.gray)
                .Build();
            toPanelBtn.OnClick += () => BattleRoyaleManager.Instance?.TeleportPlayerToControlPanel();
        }

        /// <summary>
        /// Shows the specified tab page.
        /// </summary>
        private void ShowTab(int tabIndex)
        {
            _selectedTab = tabIndex;
            
            // Update tab button appearances
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                var button = _tabButtons[i];
                if (i == tabIndex)
                {
                    // Selected tab colors (blue tone)
                    button.SetColors(
                        new Color(0.25f, 0.45f, 0.9f, 1f),
                        new Color(0.30f, 0.50f, 0.95f, 1f),
                        new Color(0.20f, 0.40f, 0.80f, 1f),
                        new Color(0.30f, 0.30f, 0.30f, 0.5f));
                }
                else
                {
                    // Unselected tab colors (gray tone)
                    button.SetColors(
                        new Color(0.25f, 0.25f, 0.3f, 1f),
                        new Color(0.3f, 0.3f, 0.35f, 1f),
                        new Color(0.2f, 0.2f, 0.25f, 1f),
                        new Color(0.15f, 0.15f, 0.15f, 0.5f));
                }
            }

            // Hide all pages first
            _setupPage?.Hide();
            _settingsPage?.Hide();
            _presetsPage?.Hide();

            // Show the selected page
            switch (tabIndex)
            {
                case 0: _setupPage?.Show(); break;
                case 1: _settingsPage?.Show(); break;
                case 2: _presetsPage?.Show(); break;
            }
        }

        private void UpdateAllPages()
        {
            _setupPage?.UpdateSettings(_current);
            _settingsPage?.UpdateSettings(_current);
            _presetsPage?.UpdateSettings(_current);
        }

        private void OnPresetChanged(int index)
        {
            if (index < 0 || index >= _presets.Count) return;
            _current = _presets[index];
            UpdateAllPages();
        }

        private void SaveCurrentAsPreset()
        {
            if (string.IsNullOrWhiteSpace(_current.PresetName)) _current.PresetName = $"Preset_{DateTime.Now:HHmmss}";
            var idx = _presets.FindIndex(p => string.Equals(p.PresetName, _current.PresetName, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) _presets[idx] = _current; else _presets.Add(_current);
            PresetConfig.SavePresets(_presets);
            _presetsPage?.RefreshPresetDropdown();
        }

        private void StartFromSettings()
        {
            var mgr = BattleRoyaleManager.Instance;
            if (mgr == null)
            {
                MelonLogger.Warning("[BR] Manager not ready");
                return;
            }

            // Validate settings
            if (_current.SelectedGroups.Count == 0)
            {
                MelonLogger.Warning("[BR] No groups selected for tournament");
                return;
            }

            // Hide panel before starting
            HidePanel();

            // Apply arena selection to spawn correct environment
            mgr.SetArena(Mathf.Clamp(_current.ArenaIndex, 0, mgr.Arenas.Length - 1));

            if (_current.PlayMode == EPlayMode.Tournament)
            {
                var tm = new TournamentManager(mgr, _groups, _current);
                MelonLogger.Msg($"[BR] Starting tournament with {_current.SelectedGroups.Count} groups, {_current.ParticipantsPerGroup} participants per group");
                tm.RunTournament();
            }
            else
            {
                // Build a combined participant list from selected groups and run one FFA with ALL matched NPCs (ignore ParticipantsPerGroup)
                var tm = new TournamentManager(mgr, _groups, _current);
                var everyone = new List<NPC>();
                for (int i = 0; i < NPCManager.NPCRegistry.Count; i++)
                {
                    var n = NPCManager.NPCRegistry[i];
                    if (n == null || n.Health == null) continue;
                    if (n.Health.IsDead || n.Health.IsKnockedOut) continue;
                    if (mgr.IsIgnored(n.ID)) continue;
                    for (int gi = 0; gi < _current.SelectedGroups.Count; gi++)
                    {
                        var def = _groups.Find(g => string.Equals(g.Name, _current.SelectedGroups[gi], StringComparison.OrdinalIgnoreCase));
                        if (def != null && GroupConfig.IsMember(def, n))
                        {
                            everyone.Add(n);
                            break;
                        }
                    }
                }
                if (everyone.Count < 2)
                {
                    MelonLogger.Warning("[BR] Need at least 2 participants to start an FFA");
                    return;
                }
                MelonLogger.Msg($"[BR] Starting Free-For-All with {everyone.Count} participants");
                tm.RunSingleFFARoutine(everyone);
            }
        }


        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Dispose of pages first
                _setupPage?.Dispose();
                _settingsPage?.Dispose();
                _presetsPage?.Dispose();
                
                if (_canvas != null)
                {
                    UnityEngine.Object.Destroy(_canvas.GameObject);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BR] Error disposing ConfigPanel: {ex}");
            }
            _instance = null;
        }
    }
}


