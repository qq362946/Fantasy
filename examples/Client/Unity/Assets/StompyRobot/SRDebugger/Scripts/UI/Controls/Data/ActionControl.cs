namespace SRDebugger.UI.Controls.Data
{
    using System;
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;

    public class ActionControl : OptionsControlBase
    {
        private SRF.Helpers.MethodReference _method;

        [RequiredField] public UnityEngine.UI.Button Button;

        [RequiredField] public Text Title;

        public SRF.Helpers.MethodReference Method
        {
            get { return _method; }
        }

        protected override void Start()
        {
            base.Start();
            Button.onClick.AddListener(ButtonOnClick);
        }

        private void ButtonOnClick()
        {
            if (_method == null)
            {
                Debug.LogWarning("[SRDebugger.Options] No method set for action control", this);
                return;
            }

            try
            {
                _method.Invoke(null);
            }
            catch (Exception e)
            {
                Debug.LogError("[SRDebugger] Exception thrown while executing action.");
                Debug.LogException(e);
            }
        }

        public void SetMethod(string methodName, SRF.Helpers.MethodReference method)
        {
            _method = method;
            Title.text = methodName;
        }
    }
}
