using UnityEngine;
using bGUI.Components;
using System;
using System.Collections.Generic;
using bGUI;

namespace NPCBattleRoyale.BattleRoyale.Pages
{
    /// <summary>
    /// Abstract base class for configuration pages.
    /// </summary>
    public abstract class ConfigPageBase : IDisposable
    {
        /// <summary>
        /// Gets the title of the page to display in tabs.
        /// </summary>
        public abstract string PageTitle { get; }
        
        /// <summary>
        /// The parent transform for this page.
        /// </summary>
        protected RectTransform ParentTransform { get; private set; }
        
        /// <summary>
        /// The main panel for this page.
        /// </summary>
        protected PanelWrapper PagePanel { get; private set; }
        
        /// <summary>
        /// Parent transform to use when adding page controls. Defaults to the page panel.
        /// Pages can change this to point at a scroll view content transform.
        /// </summary>
        protected RectTransform ContentParent { get; private set; }
        
        /// <summary>
        /// Whether the page is currently visible.
        /// </summary>
        public bool IsVisible { get; protected set; }
        
        /// <summary>
        /// Gets the underlying GameObject of this UI element.
        /// </summary>
        public GameObject GameObject => PagePanel.GameObject;
        
        /// <summary>
        /// Gets the RectTransform of this UI element.
        /// </summary>
        public RectTransform RectTransform => PagePanel.RectTransform;
        
        /// <summary>
        /// Current tournament settings reference.
        /// </summary>
        protected RoundSettings CurrentSettings { get; private set; }
        
        /// <summary>
        /// Available groups reference.
        /// </summary>
        protected List<GroupDefinition> Groups { get; private set; }
        
        /// <summary>
        /// Available presets reference.
        /// </summary>
        protected List<RoundSettings> Presets { get; private set; }
        
        /// <summary>
        /// Initializes a new config page.
        /// </summary>
        /// <param name="parent">The parent transform.</param>
        /// <param name="currentSettings">Current tournament settings.</param>
        /// <param name="groups">Available groups.</param>
        /// <param name="presets">Available presets.</param>
        protected ConfigPageBase(RectTransform parent, RoundSettings currentSettings, List<GroupDefinition> groups, List<RoundSettings> presets)
        {
            ParentTransform = parent;
            CurrentSettings = currentSettings;
            Groups = groups;
            Presets = presets;
            
            // Create the main panel for this page
            PagePanel = UI.Panel(parent)
                .SetSize(1040, 650)
                .SetAnchor(0.5f, 0.5f)
                .SetPivot(0.5f, 0.5f)
                .SetPosition(0, 0)
                .SetBackgroundColor(new Color(0.12f, 0.13f, 0.16f, 0.9f))
                .Build();
            
            // Default content parent is the page panel itself
            ContentParent = PagePanel.RectTransform;
            
            // Create page content
            SetupUI();
            
            // Initially hidden
            IsVisible = false;
            PagePanel.GameObject.SetActive(false);
        }
        
        /// <summary>
        /// Sets up the UI elements for this page.
        /// </summary>
        protected abstract void SetupUI();
        
        /// <summary>
        /// Updates this page.
        /// </summary>
        public virtual void Update() { }
        
        /// <summary>
        /// Shows this page.
        /// </summary>
        public virtual void Show()
        {
            PagePanel.GameObject.SetActive(true);
            IsVisible = true;
            OnShow();
        }
        
        /// <summary>
        /// Hides this page.
        /// </summary>
        public virtual void Hide()
        {
            PagePanel.GameObject.SetActive(false);
            IsVisible = false;
            OnHide();
        }
        
        /// <summary>
        /// Called when the page is shown. Override for custom logic.
        /// </summary>
        protected virtual void OnShow() { }
        
        /// <summary>
        /// Called when the page is hidden. Override for custom logic.
        /// </summary>
        protected virtual void OnHide() { }
        
        /// <summary>
        /// Updates the page with current settings. Call when settings change.
        /// </summary>
        public virtual void UpdateSettings(RoundSettings settings)
        {
            CurrentSettings = settings;
            RefreshUI();
        }
        
        /// <summary>
        /// Refreshes the UI with current settings. Override to implement.
        /// </summary>
        protected virtual void RefreshUI() { }
        
        /// <summary>
        /// Creates a section header with the given text.
        /// </summary>
        protected void CreateSectionHeader(string text, float yPosition)
        {
            UI.Text(ContentParent)
                .SetContent(text)
                .SetFontSize(24)
                .SetFontStyle(FontStyle.Bold)
                .SetColor(new Color(0.7f, 0.8f, 1f, 1f))
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(0, yPosition)
                .Build();
        }
        
        /// <summary>
        /// Creates a description text.
        /// </summary>
        protected void CreateDescription(string text, float yPosition)
        {
            UI.Text(ContentParent)
                .SetContent(text)
                .SetFontSize(16)
                .SetColor(new Color(0.6f, 0.6f, 0.7f, 1f))
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(0, yPosition)
                .SetWidth(1000)
                .Build();
        }
        
        /// <summary>
        /// Creates a settings row panel.
        /// </summary>
        protected PanelWrapper CreateSettingsRow(float yPosition, float height = 50f)
        {
            return UI.Panel(ContentParent)
                .SetSize(1000, height)
                .SetAnchor(0.5f, 1f)
                .SetPivot(0.5f, 1f)
                .SetPosition(0, yPosition)
                .SetBackgroundColor(new Color(0.18f, 0.2f, 0.24f, 0.6f))
                .Build();
        }

        /// <summary>
        /// Sets the parent transform for subsequent controls on this page.
        /// </summary>
        /// <param name="parent">The new parent transform to use.</param>
        protected void SetContentParent(RectTransform parent)
        {
            ContentParent = parent;
        }
        
        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public virtual void Dispose()
        {
            PagePanel?.Destroy();
        }
    }
}
