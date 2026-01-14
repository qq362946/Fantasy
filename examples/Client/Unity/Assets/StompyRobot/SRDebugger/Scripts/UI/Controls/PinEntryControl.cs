namespace SRDebugger.UI.Controls
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;

    public delegate void PinEntryControlCallback(IList<int> result, bool didCancel);

    public class PinEntryControl : SRMonoBehaviourEx
    {
        private bool _isVisible = true;
        private List<int> _numbers = new List<int>(4);

        [RequiredField] public Image Background;

        public bool CanCancel = true;

        [RequiredField] public UnityEngine.UI.Button CancelButton;

        [RequiredField] public Text CancelButtonText;

        [RequiredField] public CanvasGroup CanvasGroup;

        [RequiredField] public Animator DotAnimator;

        public UnityEngine.UI.Button[] NumberButtons;
        public Toggle[] NumberDots;

        [RequiredField] public Text PromptText;

        public event PinEntryControlCallback Complete;

        protected override void Awake()
        {
            base.Awake();

            for (var i = 0; i < NumberButtons.Length; i++)
            {
                var number = i;

                NumberButtons[i].onClick.AddListener(() => { PushNumber(number); });
            }

            CancelButton.onClick.AddListener(CancelButtonPressed);

            RefreshState();
        }

        protected override void Update()
        {
            base.Update();

            if (!_isVisible)
            {
                return;
            }

            if (_numbers.Count > 0 && (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete)))
            {
                _numbers.PopLast();
                RefreshState();
            }

            var input = Input.inputString;

            for (var i = 0; i < input.Length; i++)
            {
                if (!char.IsNumber(input, i))
                {
                    continue;
                }

                var num = (int) char.GetNumericValue(input, i);

                if (num > 9 || num < 0)
                {
                    continue;
                }

                PushNumber(num);
            }
        }

        public void Show()
        {
            CanvasGroup.alpha = 1f;
            CanvasGroup.blocksRaycasts = CanvasGroup.interactable = true;
            _isVisible = true;
        }

        public void Hide()
        {
            CanvasGroup.alpha = 0f;
            CanvasGroup.blocksRaycasts = CanvasGroup.interactable = false;
            _isVisible = false;
        }

        public void Clear()
        {
            _numbers.Clear();
            RefreshState();
        }

        public void PlayInvalidCodeAnimation()
        {
            DotAnimator.SetTrigger("Invalid");
        }

        protected void OnComplete()
        {
            if (Complete != null)
            {
                Complete(new ReadOnlyCollection<int>(_numbers), false);
            }
        }

        protected void OnCancel()
        {
            if (Complete != null)
            {
                Complete(new int[] {}, true);
            }
        }

        private void CancelButtonPressed()
        {
            if (_numbers.Count > 0)
            {
                _numbers.PopLast();
            }
            else
            {
                OnCancel();
            }

            RefreshState();
        }

        public void PushNumber(int number)
        {
            if (_numbers.Count >= 4)
            {
                Debug.LogWarning("[PinEntry] Expected 4 numbers");
                return;
            }

            _numbers.Add(number);

            if (_numbers.Count >= 4)
            {
                OnComplete();
            }

            RefreshState();
        }

        private void RefreshState()
        {
            for (var i = 0; i < NumberDots.Length; i++)
            {
                NumberDots[i].isOn = i < _numbers.Count;
            }

            if (_numbers.Count > 0)
            {
                CancelButtonText.text = "Delete";
            }
            else
            {
                CancelButtonText.text = CanCancel ? "Cancel" : "";
            }
        }
    }
}
