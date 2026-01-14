namespace SRDebugger.Services.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Internal;
    using SRF;
    using SRF.Service;
    using UI.Controls;
    using UnityEngine;

    [Service(typeof (IPinEntryService))]
    public class PinEntryServiceImpl : SRServiceBase<IPinEntryService>, IPinEntryService
    {
        private PinEntryCompleteCallback _callback;
        private bool _isVisible;
        private PinEntryControl _pinControl;
        private List<int> _requiredPin = new List<int>(4);

        public bool IsShowingKeypad
        {
            get { return _isVisible; }
        }

        public void ShowPinEntry(IList<int> requiredPin, string message, PinEntryCompleteCallback callback,
            bool allowCancel = true)
        {
            if (_isVisible)
            {
                throw new InvalidOperationException("Pin entry is already in progress");
            }

            VerifyPin(requiredPin);

            if (_pinControl == null)
            {
                Load();
            }

            if (_pinControl == null)
            {
                Debug.LogWarning("[PinEntry] Pin entry failed loading, executing callback with fail result");
                callback(false);
                return;
            }

            _pinControl.Clear();
            _pinControl.PromptText.text = message;

            _pinControl.CanCancel = allowCancel;

            _callback = callback;

            _requiredPin.Clear();
            _requiredPin.AddRange(requiredPin);

            _pinControl.Show();

            _isVisible = true;

            SRDebuggerUtil.EnsureEventSystemExists();
        }

        [Obsolete]
        public void ShowPinEntry(IList<int> requiredPin, string message, PinEntryCompleteCallback callback,
            bool blockInput,
            bool allowCancel)
        {
            ShowPinEntry(requiredPin, message, callback, allowCancel);
        }

        protected override void Awake()
        {
            base.Awake();

            CachedTransform.SetParent(Hierarchy.Get("SRDebugger"));
        }

        private void Load()
        {
            var prefab = Resources.Load<PinEntryControl>(SRDebugPaths.PinEntryPrefabPath);

            if (prefab == null)
            {
                Debug.LogError("[PinEntry] Unable to load pin entry prefab");
                return;
            }

            _pinControl = SRInstantiate.Instantiate(prefab);
            _pinControl.CachedTransform.SetParent(CachedTransform, false);

            _pinControl.Hide();

            _pinControl.Complete += PinControlOnComplete;
        }

        private void PinControlOnComplete(IList<int> result, bool didCancel)
        {
            var isValid = _requiredPin.SequenceEqual(result);

            if (!didCancel && !isValid)
            {
                _pinControl.Clear();
                _pinControl.PlayInvalidCodeAnimation();

                return;
            }

            _isVisible = false;
            _pinControl.Hide();

            if (didCancel)
            {
                _callback(false);
                return;
            }

            _callback(isValid);
        }

        private void VerifyPin(IList<int> pin)
        {
            if (pin.Count != 4)
            {
                throw new ArgumentException("Pin list must have 4 elements");
            }

            for (var i = 0; i < pin.Count; i++)
            {
                if (pin[i] < 0 || pin[i] > 9)
                {
                    throw new ArgumentException("Pin numbers must be >= 0 && <= 9");
                }
            }
        }
    }
}
