namespace SRDebugger.UI.Other
{
    using System;
    using System.Collections.Generic;
    using Controls;
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;

    public class SRTabController : SRMonoBehaviourEx
    {
        private readonly SRList<SRTab> _tabs = new SRList<SRTab>();
        private SRTab _activeTab;

        [RequiredField] public RectTransform TabButtonContainer;

        [RequiredField] public SRTabButton TabButtonPrefab;

        [RequiredField] public RectTransform TabContentsContainer;

        [RequiredField] public RectTransform TabHeaderContentContainer;

        [RequiredField] public Text TabHeaderText;

        public SRTab ActiveTab
        {
            get { return _activeTab; }
            set { MakeActive(value); }
        }

        public IList<SRTab> Tabs
        {
            get { return _tabs.AsReadOnly(); }
        }

        public event Action<SRTabController, SRTab> ActiveTabChanged;

        public void AddTab(SRTab tab, bool visibleInSidebar = true)
        {
            tab.CachedTransform.SetParent(TabContentsContainer, false);
            tab.CachedGameObject.SetActive(false);

            if (visibleInSidebar)
            {
                // Create a tab button for this tab
                var button = SRInstantiate.Instantiate(TabButtonPrefab);
                button.CachedTransform.SetParent(TabButtonContainer, false);
                button.TitleText.text = tab.Title.ToUpper();

                if (tab.IconExtraContent != null)
                {
                    var extraContent = SRInstantiate.Instantiate(tab.IconExtraContent);
                    extraContent.SetParent(button.ExtraContentContainer, false);
                }

                button.IconStyleComponent.StyleKey = tab.IconStyleKey;
                button.IsActive = false;

                button.Button.onClick.AddListener(() => MakeActive(tab));

                tab.TabButton = button;
            }

            _tabs.Add(tab);
            SortTabs();

            if (_tabs.Count == 1)
            {
                ActiveTab = tab;
            }
        }

        private void MakeActive(SRTab tab)
        {
            if (!_tabs.Contains(tab))
            {
                throw new ArgumentException("tab is not a member of this tab controller", "tab");
            }

            if (_activeTab != null)
            {
                _activeTab.CachedGameObject.SetActive(false);

                if (_activeTab.TabButton != null)
                {
                    _activeTab.TabButton.IsActive = false;
                }

                if (_activeTab.HeaderExtraContent != null)
                {
                    _activeTab.HeaderExtraContent.gameObject.SetActive(false);
                }
            }

            _activeTab = tab;

            if (_activeTab != null)
            {
                _activeTab.CachedGameObject.SetActive(true);
                TabHeaderText.text = _activeTab.LongTitle;

                if (_activeTab.TabButton != null)
                {
                    _activeTab.TabButton.IsActive = true;
                }

                if (_activeTab.HeaderExtraContent != null)
                {
                    _activeTab.HeaderExtraContent.SetParent(TabHeaderContentContainer, false);
                    _activeTab.HeaderExtraContent.gameObject.SetActive(true);
                }
            }

            if (ActiveTabChanged != null)
            {
                ActiveTabChanged(this, _activeTab);
            }
        }

        private void SortTabs()
        {
            _tabs.Sort((t1, t2) => t1.SortIndex.CompareTo(t2.SortIndex));

            for (var i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i].TabButton != null)
                {
                    _tabs[i].TabButton.CachedTransform.SetSiblingIndex(i);
                }
            }
        }
    }
}
