
#if NETFX_CORE
using UnityEngine.Windows;
#endif

namespace SRDebugger.UI.Other
{
    using System;
    using System.Collections;
    using System.Linq;
    using Internal;
    using Services;
    using SRF;
    using SRF.Service;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public class BugReportSheetController : SRMonoBehaviourEx
    {
        [RequiredField] public GameObject ButtonContainer;

        [RequiredField] public Text ButtonText;

        [RequiredField] public UnityEngine.UI.Button CancelButton;

        public Action CancelPressed;

        [RequiredField] public InputField DescriptionField;

        [RequiredField] public InputField EmailField;

        [RequiredField] public Slider ProgressBar;

        [RequiredField] public Text ResultMessageText;

        public Action ScreenshotComplete;

        [RequiredField] public UnityEngine.UI.Button SubmitButton;

        public Action<bool, string> SubmitComplete;
        public Action TakingScreenshot;

        public bool IsCancelButtonEnabled
        {
            get { return CancelButton.gameObject.activeSelf; }
            set { CancelButton.gameObject.SetActive(value); }
        }

        protected override void Start()
        {
            base.Start();

            SetLoadingSpinnerVisible(false);
            ClearErrorMessage();
            ClearForm();
        }

        public void Submit()
        {
            EventSystem.current.SetSelectedGameObject(null);

            ProgressBar.value = 0;
            ClearErrorMessage();
            SetLoadingSpinnerVisible(true);
            SetFormEnabled(false);

            if (!string.IsNullOrEmpty(EmailField.text))
            {
                SetDefaultEmailFieldContents(EmailField.text);
            }

            StartCoroutine(SubmitCo());
        }

        public void Cancel()
        {
            if (CancelPressed != null)
            {
                CancelPressed();
            }
        }

        private IEnumerator SubmitCo()
        {
            if (BugReportScreenshotUtil.ScreenshotData == null)
            {
                if (TakingScreenshot != null)
                {
                    TakingScreenshot();
                }

                yield return new WaitForEndOfFrame();

                yield return StartCoroutine(BugReportScreenshotUtil.ScreenshotCaptureCo());

                if (ScreenshotComplete != null)
                {
                    ScreenshotComplete();
                }
            }

            var s = SRServiceManager.GetService<IBugReportService>();

            var r = new BugReport();

            r.Email = EmailField.text;
            r.UserDescription = DescriptionField.text;

            r.ConsoleLog = Service.Console.AllEntries.ToList();
            r.SystemInformation = SRServiceManager.GetService<ISystemInformationService>().CreateReport();
            r.ScreenshotData = BugReportScreenshotUtil.ScreenshotData;

            BugReportScreenshotUtil.ScreenshotData = null;

            s.SendBugReport(r, OnBugReportComplete, OnBugReportProgress);
        }

        private void OnBugReportProgress(float progress)
        {
            ProgressBar.value = progress;
        }

        private void OnBugReportComplete(bool didSucceed, string errorMessage)
        {
            if (!didSucceed)
            {
                ShowErrorMessage("Error sending bug report", errorMessage);
            }
            else
            {
                ClearForm();
                ShowErrorMessage("Bug report submitted successfully");
            }

            SetLoadingSpinnerVisible(false);
            SetFormEnabled(true);

            if (SubmitComplete != null)
            {
                SubmitComplete(didSucceed, errorMessage);
            }
        }

        protected void SetLoadingSpinnerVisible(bool visible)
        {
            ProgressBar.gameObject.SetActive(visible);
            ButtonContainer.SetActive(!visible);
        }

        protected void ClearForm()
        {
            EmailField.text = GetDefaultEmailFieldContents();
            DescriptionField.text = "";
        }

        protected void ShowErrorMessage(string userMessage, string serverMessage = null)
        {
            var txt = userMessage;

            if (!string.IsNullOrEmpty(serverMessage))
            {
                txt += " (<b>{0}</b>)".Fmt(serverMessage);
            }

            ResultMessageText.text = txt;
            ResultMessageText.gameObject.SetActive(true);
        }

        protected void ClearErrorMessage()
        {
            ResultMessageText.text = "";
            ResultMessageText.gameObject.SetActive(false);
        }

        protected void SetFormEnabled(bool e)
        {
            SubmitButton.interactable = e;
            CancelButton.interactable = e;
            EmailField.interactable = e;
            DescriptionField.interactable = e;
        }

        private string GetDefaultEmailFieldContents()
        {
            return PlayerPrefs.GetString("SRDEBUGGER_BUG_REPORT_LAST_EMAIL", "");
        }

        private void SetDefaultEmailFieldContents(string value)
        {
            PlayerPrefs.SetString("SRDEBUGGER_BUG_REPORT_LAST_EMAIL", value);
            PlayerPrefs.Save();
        }
    }
}
