namespace SRDebugger.UI.Other
{
    using Services;
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;

    public class ConsoleTabQuickViewControl : SRMonoBehaviourEx
    {
        private const int Max = 1000;
        private static readonly string MaxString = (Max - 1) + "+";
        private int _prevErrorCount = -1;
        private int _prevInfoCount = -1;
        private int _prevWarningCount = -1;

        [Import] public IConsoleService ConsoleService;

        [RequiredField] public Text ErrorCountText;

        [RequiredField] public Text InfoCountText;

        [RequiredField] public Text WarningCountText;

        protected override void Awake()
        {
            base.Awake();

            ErrorCountText.text = "0";
            WarningCountText.text = "0";
            InfoCountText.text = "0";
        }

        protected override void Update()
        {
            base.Update();

            if (ConsoleService == null)
            {
                return;
            }

            if (HasChanged(ConsoleService.ErrorCount, ref _prevErrorCount, Max))
            {
                ErrorCountText.text = Internal.SRDebuggerUtil.GetNumberString(ConsoleService.ErrorCount, Max, MaxString);
            }

            if (HasChanged(ConsoleService.WarningCount, ref _prevWarningCount, Max))
            {
                WarningCountText.text = Internal.SRDebuggerUtil.GetNumberString(ConsoleService.WarningCount, Max,
                    MaxString);
            }

            if (HasChanged(ConsoleService.InfoCount, ref _prevInfoCount, Max))
            {
                InfoCountText.text = Internal.SRDebuggerUtil.GetNumberString(ConsoleService.InfoCount, Max, MaxString);
            }
        }

        private static bool HasChanged(int newCount, ref int oldCount, int max)
        {
            var newCountClamped = Mathf.Clamp(newCount, 0, max);
            var oldCountClamped = Mathf.Clamp(oldCount, 0, max);

            var hasChanged = newCountClamped != oldCountClamped;

            oldCount = newCount;

            return hasChanged;
        }
    }
}
