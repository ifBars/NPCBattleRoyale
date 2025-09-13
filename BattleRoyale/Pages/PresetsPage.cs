using UnityEngine;
using bGUI.Components;
using System;
using System.Collections.Generic;
using bGUI;

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
            saveBtn.OnClick += () => _onSavePreset?.Invoke();
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
            var list = new List<string>();
            for (int i = 0; i < Presets.Count; i++) 
                list.Add(Presets[i].PresetName);
            return list;
        }
        
        protected override void RefreshUI()
        {
            RefreshPresetDropdown();
        }
    }
}
