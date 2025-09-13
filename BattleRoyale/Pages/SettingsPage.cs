using UnityEngine;
using bGUI.Components;
using System.Collections.Generic;
using bGUI;
using bGUI.Core.Components;

namespace NPCBattleRoyale.BattleRoyale.Pages
{
    /// <summary>
    /// Settings page for tournament rules and options.
    /// </summary>
    public class SettingsPage : ConfigPageBase
    {
        public override string PageTitle => "Settings";
		
		private const float LeftPadding = 24f;
		private const float Column1LabelX = 24f;
		private const float Column1ControlX = 260f;
		private const float Column1DescX = 320f;
		private const float Column2LabelX = 530f;
		private const float Column2ControlX = 700f;
        
        private ToggleWrapper _shuffleToggle;
        private ToggleWrapper _koToggle;
        private ToggleGroupWrapper _playModeToggleGroup;
        private readonly System.Collections.Generic.List<ToggleWrapper> _playModeToggles = new System.Collections.Generic.List<ToggleWrapper>();
        private ToggleGroupWrapper _modeToggleGroup;
        private readonly System.Collections.Generic.List<ToggleWrapper> _modeToggles = new System.Collections.Generic.List<ToggleWrapper>();
        private ToggleGroupWrapper _arenaRotationToggleGroup;
        private readonly System.Collections.Generic.List<ToggleWrapper> _arenaRotationToggles = new System.Collections.Generic.List<ToggleWrapper>();
        private SliderWrapper _advanceSlider;
        private TextWrapper _advanceLabel;
        private SliderWrapper _maxFfaSlider;
        private TextWrapper _maxFfaLabel;
        private ToggleWrapper _healToggle;
        private SliderWrapper _delaySlider;
        private TextWrapper _delayLabel;
        
        private IScrollView _scrollView;
        
        public SettingsPage(RectTransform parent, RoundSettings currentSettings, List<GroupDefinition> groups, List<RoundSettings> presets)
            : base(parent, currentSettings, groups, presets)
        {
        }
        
        protected override void SetupUI()
        {
            // Create a scrollable content area that fills the page panel
            _scrollView = UI.ScrollView(PagePanel.RectTransform)
                .WithHorizontalScrolling(false)
                .WithVerticalScrolling(true)
                .Build();
            _scrollView.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _scrollView.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _scrollView.RectTransform.pivot = new Vector2(0.5f, 0.5f);
            _scrollView.RectTransform.anchoredPosition = Vector2.zero;
            _scrollView.RectTransform.sizeDelta = new Vector2(1000f, 610f);

            // Route all subsequent controls into the scroll content
            SetContentParent(_scrollView.Content);

            CreateSectionHeader("⚙️ Tournament Settings", -25);
            CreateDescription("Configure tournament rules and behavior", -55);
            
            CreateShuffleToggle();
            CreateKOToggle();
            CreatePlayModeRow();
            CreateModeAndRotation();
            CreateAdvancementControls();
            CreateFlowControls();

            // Ensure content is taller than the viewport so scrolling is active
            var size = _scrollView.Content.sizeDelta;
            _scrollView.Content.sizeDelta = new Vector2(size.x, 760f);
            _scrollView.VerticalNormalizedPosition = 1f; // Start scrolled to top
        }
        
        private void CreateShuffleToggle()
        {
            var shuffleRow = CreateSettingsRow(-100);
            
            UI.Text(shuffleRow.RectTransform)
                .SetContent("🎲 Shuffle Participants:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1LabelX, 0)
                .SetWidth(200)
                .Build();
            
            _shuffleToggle = UI.Toggle(shuffleRow.RectTransform)
                .SetSize(40, 30)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1ControlX, 0)
                .SetIsOn(CurrentSettings.ShuffleParticipants)
                .OnValueChanged(on => CurrentSettings.ShuffleParticipants = on)
                .Build();
            
            UI.Text(shuffleRow.RectTransform)
                .SetContent("Randomize participant order for fairness")
                .SetFontSize(14)
                .SetColor(new Color(0.8f, 0.8f, 0.8f, 1f))
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1DescX, 0)
                .SetWidth(480)
                .Build();
        }
        
        private void CreateKOToggle()
        {
            var koRow = CreateSettingsRow(-170);
            
            UI.Text(koRow.RectTransform)
                .SetContent("💥 KO as Elimination:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1LabelX, 0)
                .SetWidth(200)
                .Build();
            
            _koToggle = UI.Toggle(koRow.RectTransform)
                .SetSize(40, 30)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1ControlX, 0)
                .SetIsOn(CurrentSettings.KnockoutCountsAsElim)
                .OnValueChanged(on => CurrentSettings.KnockoutCountsAsElim = on)
                .Build();
            
            UI.Text(koRow.RectTransform)
                .SetContent("Knocked out participants are eliminated")
                .SetFontSize(14)
                .SetColor(new Color(0.8f, 0.8f, 0.8f, 1f))
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1DescX, 0)
                .SetWidth(480)
                .Build();
        }

        private void CreatePlayModeRow()
        {
            var row = CreateSettingsRow(-240, 60f);
            UI.Text(row.RectTransform)
                .SetContent("Play Mode:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1LabelX, 0)
                .SetWidth(140)
                .Build();

            _playModeToggleGroup = UI.ToggleGroup(row.RectTransform)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1ControlX, 0)
                .SetSize(420, 50)
                .Build();

            BuildPlayModeToggles();
        }

        private void BuildPlayModeToggles()
        {
            _playModeToggles.Clear();
            var labels = new []{ "Tournament", "Free for All" };
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                var t = _playModeToggleGroup.AddToggle(labels[i]);
                t.IsOn = idx == (int)CurrentSettings.PlayMode;
                t.OnValueChanged += on =>
                {
                    if (!on) return;
                    CurrentSettings.PlayMode = (EPlayMode)idx;
                    for (int j = 0; j < _playModeToggles.Count; j++)
                        if (j != idx && _playModeToggles[j].IsOn) _playModeToggles[j].IsOn = false;
                };
                _playModeToggles.Add(t);
            }
        }

        private void CreateModeAndRotation()
        {
            // Taller row to accommodate toggle groups with generous spacing
            var modeRow = CreateSettingsRow(-320, 100f);
            UI.Text(modeRow.RectTransform)
                .SetContent("Format:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1LabelX, 0)
                .SetWidth(120)
                .Build();

            _modeToggleGroup = UI.ToggleGroup(modeRow.RectTransform)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1ControlX, 0)
                .SetSize(380, 90)
                .Build();
            BuildModeToggles();

            UI.Text(modeRow.RectTransform)
                .SetContent("Arena Rotation:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column2LabelX, 0)
                .SetWidth(140)
                .Build();

            _arenaRotationToggleGroup = UI.ToggleGroup(modeRow.RectTransform)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column2ControlX, 0)
                .SetSize(280, 90)
                .Build();
            BuildArenaRotationToggles();
        }

        private void BuildModeToggles()
        {
            _modeToggles.Clear();
            var labels = new []{ "Groups → Finals", "Single Elimination" };
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                var t = _modeToggleGroup.AddToggle(labels[i]);
                t.IsOn = idx == (int)CurrentSettings.TournamentMode;
                t.OnValueChanged += on =>
                {
                    if (!on) return;
                    CurrentSettings.TournamentMode = (ETournamentMode)idx;
                    for (int j = 0; j < _modeToggles.Count; j++)
                        if (j != idx && _modeToggles[j].IsOn) _modeToggles[j].IsOn = false;
                };
                _modeToggles.Add(t);
            }
        }

        private void BuildArenaRotationToggles()
        {
            _arenaRotationToggles.Clear();
            var labels = new []{ "Fixed", "Alternate", "Random" };
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                var t = _arenaRotationToggleGroup.AddToggle(labels[i]);
                t.IsOn = idx == (int)CurrentSettings.ArenaRotation;
                t.OnValueChanged += on =>
                {
                    if (!on) return;
                    CurrentSettings.ArenaRotation = (EArenaRotation)idx;
                    for (int j = 0; j < _arenaRotationToggles.Count; j++)
                        if (j != idx && _arenaRotationToggles[j].IsOn) _arenaRotationToggles[j].IsOn = false;
                };
                _arenaRotationToggles.Add(t);
            }
        }

        private void CreateAdvancementControls()
        {
            // Move down and increase height for clearer separation from mode row
            var advRow = CreateSettingsRow(-430, 70f);
            UI.Text(advRow.RectTransform)
                .SetContent("Advance Per Group:")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1LabelX, 0)
                .SetWidth(200)
                .Build();

            _advanceSlider = UI.Slider(advRow.RectTransform)
                .SetHorizontal(200, 30)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1ControlX, 0)
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
                .SetPosition(Column2LabelX, 0)
                .SetWidth(250)
                .Build();

            _maxFfaSlider = UI.Slider(advRow.RectTransform)
                .SetHorizontal(160, 30)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column2ControlX, 0)
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
                .SetPosition(880, 0)
                .SetWidth(40)
                .Build();
        }

        private void CreateFlowControls()
        {
            // Position near the bottom with extra breathing room above
            var flowRow = CreateSettingsRow(-510, 50f);
            _healToggle = UI.Toggle(flowRow.RectTransform)
                .SetSize(40, 30)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1LabelX, 0)
                .SetIsOn(CurrentSettings.HealBetweenRounds)
                .OnValueChanged(on => CurrentSettings.HealBetweenRounds = on)
                .Build();

            UI.Text(flowRow.RectTransform)
                .SetContent("Heal winners between rounds")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(74, 0)
                .SetWidth(260)
                .Build();

            UI.Text(flowRow.RectTransform)
                .SetContent("Inter-round delay (sec):")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(420, 0)
                .SetWidth(200)
                .Build();

            _delaySlider = UI.Slider(flowRow.RectTransform)
                .SetHorizontal(200, 30)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(640, 0)
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
                .SetPosition(810, 0)
                .SetWidth(40)
                .Build();

            // Include Police toggle
            var policeRow = CreateSettingsRow(-560, 50f);
            var includePoliceToggle = UI.Toggle(policeRow.RectTransform)
                .SetSize(40, 30)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(Column1LabelX, 0)
                .SetIsOn(CurrentSettings.IncludePolice)
                .OnValueChanged(on => CurrentSettings.IncludePolice = on)
                .Build();
            UI.Text(policeRow.RectTransform)
                .SetContent("Include Police in matches")
                .SetFontSize(16)
                .SetColor(Color.white)
                .SetAnchor(0f, 0.5f)
                .SetPivot(0f, 0.5f)
                .SetPosition(74, 0)
                .SetWidth(300)
                .Build();
        }
        
        protected override void RefreshUI()
        {
            if (_shuffleToggle != null)
                _shuffleToggle.IsOn = CurrentSettings.ShuffleParticipants;
            
            if (_koToggle != null)
                _koToggle.IsOn = CurrentSettings.KnockoutCountsAsElim;

            if (_playModeToggleGroup != null && _playModeToggles.Count > 0)
            {
                int idx = (int)CurrentSettings.PlayMode;
                for (int i = 0; i < _playModeToggles.Count; i++)
                    _playModeToggles[i].IsOn = (i == idx);
            }

            if (_modeToggleGroup != null && _modeToggles.Count > 0)
            {
                int idx = (int)CurrentSettings.TournamentMode;
                for (int i = 0; i < _modeToggles.Count; i++)
                    _modeToggles[i].IsOn = (i == idx);
            }

            if (_arenaRotationToggleGroup != null && _arenaRotationToggles.Count > 0)
            {
                int idx = (int)CurrentSettings.ArenaRotation;
                for (int i = 0; i < _arenaRotationToggles.Count; i++)
                    _arenaRotationToggles[i].IsOn = (i == idx);
            }

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
            // No UI reference to update for IncludePolice beyond reading CurrentSettings when rebuilding
        }
    }
}
