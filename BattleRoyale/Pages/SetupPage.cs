using UnityEngine;
using bGUI.Components;
using System;
using System.Collections.Generic;
using bGUI;
using MelonLoader;

namespace NPCBattleRoyale.BattleRoyale.Pages
{
    /// <summary>
    /// Setup page for tournament configuration - Arena selection, groups, and participants.
    /// </summary>
    public class SetupPage : ConfigPageBase
    {
        public override string PageTitle => "Setup";
        
        private DropdownWrapper _arenaDropdown;
        private PanelWrapper _groupsArea;
        private ToggleGroupWrapper _groupsLeft;
        private ToggleGroupWrapper _groupsRight;
        private SliderWrapper _participantsSlider;
        private TextWrapper _participantsLabel;
        
        public SetupPage(RectTransform parent, RoundSettings currentSettings, List<GroupDefinition> groups, List<RoundSettings> presets)
            : base(parent, currentSettings, groups, presets)
        {
        }
        
        protected override void SetupUI()
        {
            CreateSectionHeader("🏟️ Tournament Setup", -25);
            
            CreateArenaSelection();
            CreateParticipantsSlider();
            CreateGroupSelection();
        }
        
        private void CreateArenaSelection()
        {
            var arenaRow = CreateSettingsRow(-80, 60); // Match participants row height
            
            UI.Text(arenaRow.RectTransform)
                .SetContent("Arena:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(20, 0)
                .SetWidth(100)
                .Build();
            
            _arenaDropdown = UI.Dropdown(arenaRow.RectTransform)
                .SetSize(250, 35)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(140, 0)
                .SetOptions(GetArenaNames())
                .SetValue(CurrentSettings.ArenaIndex)
                .OnValueChanged(i => CurrentSettings.ArenaIndex = i)
                .Build();
        }
        
        private void CreateParticipantsSlider()
        {
            var participantsRow = CreateSettingsRow(-150, 60); // Make row taller for better slider
            
            UI.Text(participantsRow.RectTransform)
                .SetContent("Participants per Group:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(20, 0)
                .SetWidth(200)
                .Build();
            
            // Create slider with proper sizing and color scheme like in ModSettingsMenu
            _participantsSlider = UI.Slider(participantsRow.RectTransform)
                .SetHorizontal(200, 30) // Much larger and more visible
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(240, 0)
                .SetRange(2, 24)
                .SetWholeNumbers(true)
                .SetValue(Mathf.Clamp(CurrentSettings.ParticipantsPerGroup, 2, 24))
                .SetColorScheme(
                    new Color(0.3f, 0.7f, 0.9f, 1f), // Fill color (blue)
                    new Color(0.6f, 0.6f, 0.6f, 1f), // Background color (bright gray)
                    new Color(1f, 1f, 1f, 1f)        // Handle color (white)
                )
                .OnValueChanged(v =>
                {
                    CurrentSettings.ParticipantsPerGroup = Mathf.RoundToInt(v);
                    UpdateParticipantsDisplay(Mathf.RoundToInt(v));
                })
                .Build();
            
            _participantsLabel = UI.Text(participantsRow.RectTransform)
                .SetContent(CurrentSettings.ParticipantsPerGroup.ToString())
                .SetFontSize(18)
                .SetFontStyle(FontStyle.Bold)
                .SetColor(new Color(0.7f, 0.8f, 1f, 1f))
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(460, 0) // Moved further right to account for larger slider
                .SetWidth(50)
                .Build();
        }
        
        private void CreateGroupSelection()
        {
            // Groups section header (adjusted position for taller rows)
            UI.Text(PagePanel.RectTransform)
                .SetContent("Select Groups")
                .SetFontSize(20)
                .SetFontStyle(FontStyle.Bold)
                .SetColor(new Color(0.6f, 0.7f, 0.9f, 1f))
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(0, -240) // Moved down to account for taller rows
                .Build();
            
            // Two-column groups area to avoid overflowing behind the footer
            _groupsArea = UI.Panel(PagePanel.RectTransform)
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(0, -290)
                .SetSize(820, 260)
                .SetBackgroundColor(new Color(0.18f, 0.2f, 0.24f, 0.15f))
                .Build();

            // Left column
            _groupsLeft = UI.ToggleGroup(_groupsArea.RectTransform)
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(-200, 0)
                .SetSize(380, 240)
                .Build();

            // Right column
            _groupsRight = UI.ToggleGroup(_groupsArea.RectTransform)
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(200, 0)
                .SetSize(380, 240)
                .Build();
            
            RefreshGroupToggles();
        }
        
        private void RefreshGroupToggles()
        {
            // Clear existing toggles
            if (_groupsLeft != null)
            {
                for (int i = _groupsLeft.RectTransform.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(_groupsLeft.RectTransform.GetChild(i).gameObject);
            }
            if (_groupsRight != null)
            {
                for (int i = _groupsRight.RectTransform.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(_groupsRight.RectTransform.GetChild(i).gameObject);
            }

            // Split groups into two balanced columns
            int leftCount = Mathf.CeilToInt(Groups.Count / 2f);
            for (int i = 0; i < Groups.Count; i++)
            {
                var name = Groups[i].Name;
                var container = (i < leftCount) ? _groupsLeft : _groupsRight;
                var toggle = container.AddToggle(name);
                toggle.IsOn = CurrentSettings.SelectedGroups.Exists(s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase));
                toggle.OnValueChanged += on => OnGroupToggled(name, on);
            }
        }
        
        private void OnGroupToggled(string group, bool isOn)
        {
            if (isOn)
            {
                if (!CurrentSettings.SelectedGroups.Exists(s => string.Equals(s, group, StringComparison.OrdinalIgnoreCase)))
                    CurrentSettings.SelectedGroups.Add(group);
            }
            else
            {
                CurrentSettings.SelectedGroups.RemoveAll(s => string.Equals(s, group, StringComparison.OrdinalIgnoreCase));
            }
        }
        
        private void UpdateParticipantsDisplay(int value)
        {
            if (_participantsLabel != null)
            {
                _participantsLabel.Content = value.ToString();
            }
        }
        
        private List<string> GetArenaNames()
        {
            var mgr = BattleRoyaleManager.Instance;
            var names = new List<string>();
            if (mgr == null || mgr.Arenas == null) return names;
            for (int i = 0; i < mgr.Arenas.Length; i++) names.Add(mgr.Arenas[i].Name);
            return names;
        }
        
        protected override void RefreshUI()
        {
            if (_arenaDropdown != null)
                _arenaDropdown.Value = Mathf.Clamp(CurrentSettings.ArenaIndex, 0, GetArenaNames().Count - 1);
            
            if (_participantsSlider != null)
            {
                _participantsSlider.Value = Mathf.Clamp(CurrentSettings.ParticipantsPerGroup, 2, 24);
                UpdateParticipantsDisplay(CurrentSettings.ParticipantsPerGroup);
            }
            
            RefreshGroupToggles();
        }
    }
}
