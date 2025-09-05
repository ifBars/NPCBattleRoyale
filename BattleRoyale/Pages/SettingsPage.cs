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
        private DropdownWrapper _modeDropdown;
        private DropdownWrapper _arenaRotationDropdown;
        private SliderWrapper _advanceSlider;
        private TextWrapper _advanceLabel;
        private SliderWrapper _maxFfaSlider;
        private TextWrapper _maxFfaLabel;
        private ToggleWrapper _healToggle;
        private SliderWrapper _delaySlider;
        private TextWrapper _delayLabel;
        
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
            CreateModeAndRotation();
            CreateAdvancementControls();
            CreateFlowControls();
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

        private void CreateModeAndRotation()
        {
            var modeRow = CreateSettingsRow(-250);
            UI.Text(modeRow.RectTransform)
                .SetContent("Format:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(20, 0)
                .SetWidth(120)
                .Build();

            _modeDropdown = UI.Dropdown(modeRow.RectTransform)
                .SetSize(260, 32)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(150, 0)
                .SetOptions(new []{ "Groups → Finals", "Single Elimination" })
                .SetValue((int)CurrentSettings.TournamentMode)
                .OnValueChanged(i => CurrentSettings.TournamentMode = (ETournamentMode)i)
                .Build();

            UI.Text(modeRow.RectTransform)
                .SetContent("Arena Rotation:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(440, 0)
                .SetWidth(140)
                .Build();

            _arenaRotationDropdown = UI.Dropdown(modeRow.RectTransform)
                .SetSize(220, 32)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(590, 0)
                .SetOptions(new []{ "Fixed", "Alternate", "Random" })
                .SetValue((int)CurrentSettings.ArenaRotation)
                .OnValueChanged(i => CurrentSettings.ArenaRotation = (EArenaRotation)i)
                .Build();
        }

        private void CreateAdvancementControls()
        {
            var advRow = CreateSettingsRow(-320);
            UI.Text(advRow.RectTransform)
                .SetContent("Advance Per Group:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(20, 0)
                .SetWidth(200)
                .Build();

            _advanceSlider = UI.Slider(advRow.RectTransform)
                .SetHorizontal(200, 30)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(240, 0)
                .SetRange(1, 8)
                .SetWholeNumbers(true)
                .SetValue(Mathf.Clamp(CurrentSettings.AdvancePerGroup, 1, 8))
                .OnValueChanged(v =>
                {
                    CurrentSettings.AdvancePerGroup = Mathf.RoundToInt(v);
                    if (_advanceLabel != null) _advanceLabel.Content = CurrentSettings.AdvancePerGroup.ToString();
                })
                .Build();

            _advanceLabel = UI.Text(advRow.RectTransform)
                .SetContent(CurrentSettings.AdvancePerGroup.ToString())
                .SetFontSize(18)
                .SetFontStyle(FontStyle.Bold)
                .SetColor(new Color(0.7f, 0.8f, 1f, 1f))
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(460, 0)
                .SetWidth(40)
                .Build();

            UI.Text(advRow.RectTransform)
                .SetContent("Max FFA size (0 = no cap):")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(520, 0)
                .SetWidth(230)
                .Build();

            _maxFfaSlider = UI.Slider(advRow.RectTransform)
                .SetHorizontal(160, 30)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(760, 0)
                .SetRange(0, 16)
                .SetWholeNumbers(true)
                .SetValue(Mathf.Clamp(CurrentSettings.MaxFFASize, 0, 16))
                .OnValueChanged(v =>
                {
                    CurrentSettings.MaxFFASize = Mathf.RoundToInt(v);
                    if (_maxFfaLabel != null) _maxFfaLabel.Content = CurrentSettings.MaxFFASize.ToString();
                })
                .Build();

            _maxFfaLabel = UI.Text(advRow.RectTransform)
                .SetContent(CurrentSettings.MaxFFASize.ToString())
                .SetFontSize(18)
                .SetFontStyle(FontStyle.Bold)
                .SetColor(new Color(0.7f, 0.8f, 1f, 1f))
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(930, 0)
                .SetWidth(40)
                .Build();
        }

        private void CreateFlowControls()
        {
            var flowRow = CreateSettingsRow(-390);
            _healToggle = UI.Toggle(flowRow.RectTransform)
                .SetSize(40, 30)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(20, 0)
                .SetIsOn(CurrentSettings.HealBetweenRounds)
                .OnValueChanged(on => CurrentSettings.HealBetweenRounds = on)
                .Build();

            UI.Text(flowRow.RectTransform)
                .SetContent("Heal winners between rounds")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(70, 0)
                .SetWidth(260)
                .Build();

            UI.Text(flowRow.RectTransform)
                .SetContent("Inter-round delay (sec):")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(360, 0)
                .SetWidth(200)
                .Build();

            _delaySlider = UI.Slider(flowRow.RectTransform)
                .SetHorizontal(200, 30)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(570, 0)
                .SetRange(0, 10)
                .SetWholeNumbers(true)
                .SetValue(Mathf.RoundToInt(CurrentSettings.InterRoundDelaySeconds))
                .OnValueChanged(v =>
                {
                    CurrentSettings.InterRoundDelaySeconds = Mathf.RoundToInt(v);
                    if (_delayLabel != null) _delayLabel.Content = CurrentSettings.InterRoundDelaySeconds.ToString("0");
                })
                .Build();

            _delayLabel = UI.Text(flowRow.RectTransform)
                .SetContent(CurrentSettings.InterRoundDelaySeconds.ToString("0"))
                .SetFontSize(18)
                .SetFontStyle(FontStyle.Bold)
                .SetColor(new Color(0.7f, 0.8f, 1f, 1f))
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(790, 0)
                .SetWidth(40)
                .Build();
        }
        
        protected override void RefreshUI()
        {
            if (_shuffleToggle != null)
                _shuffleToggle.IsOn = CurrentSettings.ShuffleParticipants;
            
            if (_koToggle != null)
                _koToggle.IsOn = CurrentSettings.KnockoutCountsAsElim;

            if (_modeDropdown != null)
                _modeDropdown.Value = (int)CurrentSettings.TournamentMode;

            if (_arenaRotationDropdown != null)
                _arenaRotationDropdown.Value = (int)CurrentSettings.ArenaRotation;

            if (_advanceSlider != null)
                _advanceSlider.Value = Mathf.Clamp(CurrentSettings.AdvancePerGroup, 1, 8);
            if (_advanceLabel != null)
                _advanceLabel.Content = CurrentSettings.AdvancePerGroup.ToString();

            if (_maxFfaSlider != null)
                _maxFfaSlider.Value = Mathf.Clamp(CurrentSettings.MaxFFASize, 0, 16);
            if (_maxFfaLabel != null)
                _maxFfaLabel.Content = CurrentSettings.MaxFFASize.ToString();

            if (_healToggle != null)
                _healToggle.IsOn = CurrentSettings.HealBetweenRounds;

            if (_delaySlider != null)
                _delaySlider.Value = Mathf.RoundToInt(CurrentSettings.InterRoundDelaySeconds);
            if (_delayLabel != null)
                _delayLabel.Content = CurrentSettings.InterRoundDelaySeconds.ToString("0");
        }
    }
}
