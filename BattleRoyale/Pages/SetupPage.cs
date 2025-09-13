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
        
        private ToggleGroupWrapper _arenaToggleGroup;
        private readonly List<ToggleWrapper> _arenaToggleItems = new List<ToggleWrapper>();
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
            // Increased height to comfortably fit toggle group
            var arenaRow = CreateSettingsRow(-80, 100);
            
            UI.Text(arenaRow.RectTransform)
                .SetContent("Arena:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(20, 0)
                .SetWidth(100)
                .Build();
            
            _arenaToggleGroup = UI.ToggleGroup(arenaRow.RectTransform)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(140, 0)
                .SetSize(860, 90)
                .Build();

            BuildArenaToggles();
        }

        private void BuildArenaToggles()
        {
            _arenaToggleItems.Clear();
            var names = GetArenaNames();
            for (int i = 0; i < names.Count; i++)
            {
                int index = i;
                var t = _arenaToggleGroup.AddToggle(names[i]);
                t.IsOn = index == Mathf.Clamp(CurrentSettings.ArenaIndex, 0, names.Count - 1);
                t.OnValueChanged += on =>
                {
                    if (!on) return;
                    CurrentSettings.ArenaIndex = index;
                    var mgr = BattleRoyaleManager.Instance;
                    if (mgr != null) mgr.SetArena(index);
                    // Radio behavior: turn off other toggles
                    for (int j = 0; j < _arenaToggleItems.Count; j++)
                    {
                        if (j != index && _arenaToggleItems[j].IsOn)
                            _arenaToggleItems[j].IsOn = false;
                    }
                };
                _arenaToggleItems.Add(t);
            }
        }
        
        private void CreateParticipantsSlider()
        {
            var participantsRow = CreateSettingsRow(-190, 60); // Shifted down to make space for arena toggles
            
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
                .SetPosition(460, 0)
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
                .SetPosition(0, -260) // Adjusted for revised row spacing
                .Build();
            
            // Two-column groups area to avoid overflowing behind the footer
            _groupsArea = UI.Panel(PagePanel.RectTransform)
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(0, -300)
                .SetSize(1000, 250)
                .SetBackgroundColor(new Color(0.18f, 0.2f, 0.24f, 0.15f))
                .Build();

            // Left column
            _groupsLeft = UI.ToggleGroup(_groupsArea.RectTransform)
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(-250, 0)
                .SetSize(460, 220)
                .Build();

            // Right column
            _groupsRight = UI.ToggleGroup(_groupsArea.RectTransform)
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(250, 0)
                .SetSize(460, 220)
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
            
            if (mgr == null) 
            {
                MelonLoader.MelonLogger.Warning("[SetupPage] BattleRoyaleManager.Instance is null, using fallback arena names");
                // Fallback to default arena names if manager isn't initialized yet
                return new List<string> { "Police Station", "Chemical Station" };
            }
            
            if (mgr.Arenas == null) 
            {
                MelonLoader.MelonLogger.Warning("[SetupPage] BattleRoyaleManager.Arenas is null");
                return names;
            }
            
            for (int i = 0; i < mgr.Arenas.Length; i++) 
            {
                names.Add(mgr.Arenas[i].Name);
            }
            
            MelonLoader.MelonLogger.Msg($"[SetupPage] Found {names.Count} arenas: {string.Join(", ", names)}");
            return names;
        }
        
        protected override void RefreshUI()
        {
            if (_arenaToggleGroup != null && _arenaToggleItems.Count > 0)
            {
                var names = GetArenaNames();
                int clamped = Mathf.Clamp(CurrentSettings.ArenaIndex, 0, names.Count - 1);
                for (int i = 0; i < _arenaToggleItems.Count; i++)
                    _arenaToggleItems[i].IsOn = (i == clamped);
            }
            
            if (_participantsSlider != null)
            {
                _participantsSlider.Value = Mathf.Clamp(CurrentSettings.ParticipantsPerGroup, 2, 24);
                UpdateParticipantsDisplay(CurrentSettings.ParticipantsPerGroup);
            }
            
            RefreshGroupToggles();
        }
    }
}
