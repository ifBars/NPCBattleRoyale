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
        
        private DropdownWrapper _presetDropdown;
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
            var loadRow = CreateSettingsRow(-110);
            
            UI.Text(loadRow.RectTransform)
                .SetContent("Load Preset:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(20, 0)
                .SetWidth(120)
                .Build();
            
            _presetDropdown = UI.Dropdown(loadRow.RectTransform)
                .SetSize(250, 35)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(150, 0)
                .SetOptions(GetPresetNames())
                .OnValueChanged(index => _onPresetChanged?.Invoke(index))
                .Build();
            
            var loadBtn = UI.Button(loadRow.RectTransform)
                .SetText("Load")
                .SetSize(80, 35)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(420, 0)
                .SetColors(
                    new Color(0.3f, 0.6f, 0.9f, 1f),
                    new Color(0.4f, 0.7f, 1f, 1f),
                    new Color(0.2f, 0.5f, 0.8f, 1f),
                    Color.gray)
                .Build();
            loadBtn.OnClick += () => _onPresetChanged?.Invoke(_presetDropdown.Value);
        }
        
        private void CreateSaveSection()
        {
            var saveRow = CreateSettingsRow(-180);
            
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
                .SetPosition(0, -250)
                .SetWidth(780)
                .Build();
        }
        
        public void RefreshPresetDropdown()
        {
            if (_presetDropdown != null)
            {
                _presetDropdown.SetOptions(GetPresetNames());
            }
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
