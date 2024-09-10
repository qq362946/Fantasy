using SRF.UI;

namespace SRDebugger.UI.Tabs
{
    using System.Collections.Generic;
    using System.Text;
    using Controls;
    using Services;
    using SRF;
    using SRF.Service;
    using UnityEngine;

    public class InfoTabController : SRMonoBehaviourEx
    {
        public const char Tick = '\u2713';
        public const char Cross = '\u00D7';
        public const string NameColor = "#BCBCBC";
        private Dictionary<string, InfoBlock> _infoBlocks = new Dictionary<string, InfoBlock>();

        [RequiredField] public InfoBlock InfoBlockPrefab;

        [RequiredField] public RectTransform LayoutContainer;

        [RequiredField] public FlashGraphic ToggleButton;

        private bool _updateEveryFrame;

        protected override void OnEnable()
        {
            base.OnEnable();
            InternalRefresh();

            if (_updateEveryFrame)
            {
                ToggleButton.FlashAndHoldUntilNextPress();
            }
        }

        public void Refresh()
        {
            ToggleButton.Flash(); // flash to disable any "press and hold" that is going on
            _updateEveryFrame = false;
            InternalRefresh();
        }

        protected override void Update()
        {
            if (_updateEveryFrame)
            {
                InternalRefresh();
            }
        }

        public void ActivateRefreshEveryFrame()
        {
            ToggleButton.FlashAndHoldUntilNextPress();
            _updateEveryFrame = true;
            InternalRefresh();
        }

        private void InternalRefresh()
        {
            var s = SRServiceManager.GetService<ISystemInformationService>();

            foreach (var category in s.GetCategories())
            {
                if (!_infoBlocks.ContainsKey(category))
                {
                    var block = CreateBlock(category);
                    _infoBlocks.Add(category, block);
                }
            }

            foreach (var kv in _infoBlocks)
            {
                FillInfoBlock(kv.Value, s.GetInfo(kv.Key));
            }
        }

        private void FillInfoBlock(InfoBlock block, IList<InfoEntry> info)
        {
            var sb = new StringBuilder();

            var maxTitleLength = 0;

            foreach (var systemInfo in info)
            {
                if (systemInfo.Title.Length > maxTitleLength)
                {
                    maxTitleLength = systemInfo.Title.Length;
                }
            }

            maxTitleLength += 2;

            var first = true;
            foreach (var i in info)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.AppendLine();
                }

                sb.Append("<color=");
                sb.Append(NameColor);
                sb.Append(">");

                sb.Append(i.Title);
                sb.Append(": ");

                sb.Append("</color>");

                for (var j = i.Title.Length; j <= maxTitleLength; ++j)
                {
                    sb.Append(' ');
                }

                if (i.Value is bool)
                {
                    sb.Append((bool) i.Value ? Tick : Cross);
                }
                else
                {
                    sb.Append(i.Value);
                }
            }

            block.Content.text = sb.ToString();
        }

        private InfoBlock CreateBlock(string title)
        {
            var block = SRInstantiate.Instantiate(InfoBlockPrefab);
            block.Title.text = title;

            block.CachedTransform.SetParent(LayoutContainer, false);

            return block;
        }
    }
}
