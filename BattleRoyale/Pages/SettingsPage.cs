using UnityEngine;
using bGUI.Components;
using System.Collections.Generic;
using bGUI;

namespace NPCBattleRoyale.BattleRoyale.Pages
{
    /// <summary>
    /// Settings page for tournament rules and options.
    /// </summary>
    public class SettingsPage : ConfigPageBase
    {
        public override string PageTitle => "Settings";
        
        private ToggleWrapper _shuffleToggle;
        private ToggleWrapper _koToggle;
        
        public SettingsPage(RectTransform parent, RoundSettings currentSettings, List<GroupDefinition> groups, List<RoundSettings> presets)
            : base(parent, currentSettings, groups, presets)
        {
        }
        
        protected override void SetupUI()
        {
            CreateSectionHeader("⚙️ Tournament Settings", -25);
            CreateDescription("Configure tournament rules and behavior", -55);
            
            CreateShuffleToggle();
            CreateKOToggle();
            CreateAdditionalInfo();
        }
        
        private void CreateShuffleToggle()
        {
            var shuffleRow = CreateSettingsRow(-110);
            
            UI.Text(shuffleRow.RectTransform)
                .SetContent("🎲 Shuffle Participants:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(20, 0)
                .SetWidth(200)
                .Build();
            
            _shuffleToggle = UI.Toggle(shuffleRow.RectTransform)
                .SetSize(40, 30)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(240, 0)
                .SetIsOn(CurrentSettings.ShuffleParticipants)
                .OnValueChanged(on => CurrentSettings.ShuffleParticipants = on)
                .Build();
            
            UI.Text(shuffleRow.RectTransform)
                .SetContent("Randomize participant order for fairness")
                .SetFontSize(14)
                .SetColor(new Color(0.8f, 0.8f, 0.8f, 1f))
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(300, 0)
                .SetWidth(480)
                .Build();
        }
        
        private void CreateKOToggle()
        {
            var koRow = CreateSettingsRow(-180);
            
            UI.Text(koRow.RectTransform)
                .SetContent("💥 KO as Elimination:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(20, 0)
                .SetWidth(200)
                .Build();
            
            _koToggle = UI.Toggle(koRow.RectTransform)
                .SetSize(40, 30)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(240, 0)
                .SetIsOn(CurrentSettings.KnockoutCountsAsElim)
                .OnValueChanged(on => CurrentSettings.KnockoutCountsAsElim = on)
                .Build();
            
            UI.Text(koRow.RectTransform)
                .SetContent("Knocked out participants are eliminated")
                .SetFontSize(14)
                .SetColor(new Color(0.8f, 0.8f, 0.8f, 1f))
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(300, 0)
                .SetWidth(480)
                .Build();
        }
        
        private void CreateAdditionalInfo()
        {
            UI.Text(PagePanel.RectTransform)
                .SetContent("More tournament options coming soon...")
                .SetFontSize(16)
                .SetColor(new Color(0.6f, 0.6f, 0.7f, 1f))
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(0, -250)
                .Build();
        }
        
        protected override void RefreshUI()
        {
            if (_shuffleToggle != null)
                _shuffleToggle.IsOn = CurrentSettings.ShuffleParticipants;
            
            if (_koToggle != null)
                _koToggle.IsOn = CurrentSettings.KnockoutCountsAsElim;
        }
    }
}
