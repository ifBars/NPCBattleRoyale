using UnityEngine;
using bGUI.Components;
using bGUI;
using MelonLoader;
using MelonLoader.Utils;
#if MONO
using Newtonsoft.Json;
#else
using Il2CppNewtonsoft.Json;
#endif

namespace NPCBattleRoyale.BattleRoyale.Pages
{
    /// <summary>
    /// Presets page for loading and saving tournament configurations.
    /// </summary>
    public class PresetsPage : ConfigPageBase
    {
        public override string PageTitle => "Presets";
        
        private ToggleGroupWrapper _presetToggleGroup;
        private readonly List<ToggleWrapper> _presetToggles = new List<ToggleWrapper>();
        private Action<int> _onPresetChanged;
        private Action _onSavePreset;
        
        public PresetsPage(RectTransform parent, RoundSettings currentSettings, List<GroupDefinition> groups, List<RoundSettings> presets)
            : base(parent, currentSettings, groups, presets)
        {
        }
        
        /// <summary>
        /// Sets the callbacks for preset operations.
        /// </summary>
        public void SetCallbacks(Action<int> onPresetChanged, Action onSavePreset)
        {
            _onPresetChanged = onPresetChanged;
            _onSavePreset = onSavePreset;
        }
        
        protected override void SetupUI()
        {
            CreateSectionHeader("💾 Tournament Presets", -25);
            CreateDescription("Save and load your favorite tournament configurations", -55);
            
            CreateLoadSection();
            CreateSaveSection();
            CreateInfoSection();
        }
        
        private void CreateLoadSection()
        {
            // Taller row to comfortably display preset toggles
            var loadRow = CreateSettingsRow(-110, 200f);
            
            UI.Text(loadRow.RectTransform)
                .SetContent("Load Preset:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(20, 0)
                .SetWidth(120)
                .Build();
            
            _presetToggleGroup = UI.ToggleGroup(loadRow.RectTransform)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(150, 0)
                .SetSize(700, 180) // Wider area on larger page
                .Build();

            BuildPresetToggles();
            
            var loadBtn = UI.Button(loadRow.RectTransform)
                .SetText("Load")
                .SetSize(80, 35)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(880, 0)
                .SetColors(
                    new Color(0.3f, 0.6f, 0.9f, 1f),
                    new Color(0.4f, 0.7f, 1f, 1f),
                    new Color(0.2f, 0.5f, 0.8f, 1f),
                    Color.gray)
                .Build();
            loadBtn.OnClick += () =>
            {
                int idx = GetSelectedPresetIndex();
                if (idx >= 0) _onPresetChanged?.Invoke(idx);
            };
        }
        
        private void CreateSaveSection()
        {
            // Place below the taller load row (200px) with extra spacing
            var saveRow = CreateSettingsRow(-340);
            
            UI.Text(saveRow.RectTransform)
                .SetContent("Save Current as Preset:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(20, 0)
                .SetWidth(200)
                .Build();
            
            var saveBtn = UI.Button(saveRow.RectTransform)
                .SetText("💾 Save Preset")
                .SetSize(140, 35)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(230, 0)
                .SetColors(
                    new Color(0.2f, 0.7f, 0.3f, 1f),
                    new Color(0.3f, 0.8f, 0.4f, 1f),
                    new Color(0.1f, 0.6f, 0.2f, 1f),
                    Color.gray)
                .Build();
            saveBtn.OnClick += () =>
            {
                try
                {
                    SaveCurrentPresetToDisk();
                    _onSavePreset?.Invoke();
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[BR] Failed to save preset: {ex}");
                }
            };
        }
        
        private void CreateInfoSection()
        {
            UI.Text(PagePanel.RectTransform)
                .SetContent("✨ Presets save all your tournament settings including arena, groups, participants, and rules!")
                .SetFontSize(16)
                .SetColor(new Color(0.6f, 0.8f, 1f, 1f))
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(0, -500)
                .SetWidth(980)
                .Build();
        }
        
        public void RefreshPresetDropdown()
        {
            BuildPresetToggles();
        }

        private void BuildPresetToggles()
        {
            if (_presetToggleGroup == null) return;

            // Clear existing children
            for (int i = _presetToggleGroup.RectTransform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(_presetToggleGroup.RectTransform.GetChild(i).gameObject);
            _presetToggles.Clear();

            var names = GetPresetNames();
            for (int i = 0; i < names.Count; i++)
            {
                int idx = i;
                var t = _presetToggleGroup.AddToggle(names[i]);
                t.IsOn = false;
                t.OnValueChanged += on =>
                {
                    if (!on) return;
                    // Radio behavior
                    for (int j = 0; j < _presetToggles.Count; j++)
                        if (j != idx && _presetToggles[j].IsOn) _presetToggles[j].IsOn = false;
                    // Load preset from disk as source of truth
                    TryLoadPresetFromDisk(names[idx]);
                    _onPresetChanged?.Invoke(idx);
                };
                _presetToggles.Add(t);
            }
        }

        private int GetSelectedPresetIndex()
        {
            for (int i = 0; i < _presetToggles.Count; i++)
            {
                if (_presetToggles[i].IsOn) return i;
            }
            return -1;
        }
        
        private List<string> GetPresetNames()
        {
            try
            {
                EnsurePresetDir();
                var files = Directory.GetFiles(PresetDir, "*.json", SearchOption.TopDirectoryOnly);
                var list = new List<string>();
                for (int i = 0; i < files.Length; i++)
                {
                    list.Add(Path.GetFileNameWithoutExtension(files[i]));
                }
                if (list.Count == 0)
                {
                    // fall back to in-memory presets if none saved yet
                    for (int i = 0; i < Presets.Count; i++) list.Add(Presets[i].PresetName);
                }
                return list;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BR] Failed to get preset names: {ex}");
                var list = new List<string>();
                for (int i = 0; i < Presets.Count; i++) list.Add(Presets[i].PresetName);
                return list;
            }
        }
        
        protected override void RefreshUI()
        {
            RefreshPresetDropdown();
        }

        private static string PresetDir => Path.Combine(MelonEnvironment.UserDataDirectory, "NPCBattleRoyale", "Presets");

        private static void EnsurePresetDir()
        {
            if (!Directory.Exists(PresetDir)) Directory.CreateDirectory(PresetDir);
        }

        private void SaveCurrentPresetToDisk()
        {
            EnsurePresetDir();
            var name = string.IsNullOrWhiteSpace(CurrentSettings.PresetName) ? $"Preset_{DateTime.Now:yyyyMMdd_HHmmss}" : CurrentSettings.PresetName;
            var path = Path.Combine(PresetDir, Sanitize(name) + ".json");
            var json = NPCBattleRoyale.Utils.JsonUtils.Serialize(CurrentSettings, true);
            File.WriteAllText(path, json);
        }

        private void TryLoadPresetFromDisk(string presetName)
        {
            try
            {
                EnsurePresetDir();
                var path = Path.Combine(PresetDir, Sanitize(presetName) + ".json");
                if (!File.Exists(path)) return;
                var json = File.ReadAllText(path);
                var loaded = NPCBattleRoyale.Utils.JsonUtils.Deserialize<RoundSettings>(json);
                if (loaded == null) return;
                // Copy fields into CurrentSettings
                CopySettings(loaded, CurrentSettings);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BR] Failed to load preset '{presetName}': {ex}");
            }
        }

        private static void CopySettings(RoundSettings from, RoundSettings to)
        {
            to.PresetName = from.PresetName;
            to.ArenaIndex = from.ArenaIndex;
            to.SelectedGroups = new List<string>(from.SelectedGroups ?? new List<string>());
            to.ParticipantsPerGroup = from.ParticipantsPerGroup;
            to.ShuffleParticipants = from.ShuffleParticipants;
            to.KnockoutCountsAsElim = from.KnockoutCountsAsElim;
            to.MatchTimeoutSeconds = from.MatchTimeoutSeconds;
            to.FinalsParticipants = from.FinalsParticipants;
            to.PlayMode = from.PlayMode;
            to.TournamentMode = from.TournamentMode;
            to.ArenaRotation = from.ArenaRotation;
            to.AdvancePerGroup = from.AdvancePerGroup;
            to.MaxFFASize = from.MaxFFASize;
            to.HealBetweenRounds = from.HealBetweenRounds;
            to.InterRoundDelaySeconds = from.InterRoundDelaySeconds;
            to.IncludePolice = from.IncludePolice;
        }

        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Trim();
        }
    }
}
