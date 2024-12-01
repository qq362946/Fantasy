#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

        protected override void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard.current.onTextInput += HandleCharacter;
#endif
        }

        protected override void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM 
            Keyboard.current.onTextInput -= HandleCharacter;
#endif
        }

        protected override void Update()
        {
            base.Update();

            if (!_isVisible)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            bool delete = Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame;
#else
            bool delete = (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete));
#endif

            if (_numbers.Count > 0 && delete)
            {
                _numbers.PopLast();
                RefreshState();
            }

#if !ENABLE_INPUT_SYSTEM
            var input = Input.inputString;

            for (var i = 0; i < input.Length; i++)
            {
                HandleCharacter(input[i]);
            }
#endif
        }

        private void HandleCharacter(char i)
        {
            if (!_isVisible)
            {
                return;
            }

            if (!char.IsNumber(i))
            {
                return;
            }

            var num = (int) char.GetNumericValue(i);

            if (num > 9 || num < 0)
            {
                return;
            }

            PushNumber(num);
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
