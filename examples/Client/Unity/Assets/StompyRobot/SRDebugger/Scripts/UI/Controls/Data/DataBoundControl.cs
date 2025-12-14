namespace SRDebugger.UI.Controls
{
    using System;
    using UnityEngine;

    public abstract class DataBoundControl : OptionsControlBase
    {
        private bool _hasStarted;
        private bool _isReadOnly;
        private object _prevValue;
        private SRF.Helpers.PropertyReference _prop;

        public SRF.Helpers.PropertyReference Property
        {
            get { return _prop; }
        }

        public bool IsReadOnly
        {
            get { return _isReadOnly; }
        }

        public string PropertyName { get; private set; }

        #region Data Binding

        public void Bind(string propertyName, SRF.Helpers.PropertyReference prop)
        {
            PropertyName = propertyName;
            _prop = prop;

            _isReadOnly = !prop.CanWrite;

            OnBind(propertyName, prop.PropertyType);
            Refresh();
        }

        protected void UpdateValue(object newValue)
        {
            if (newValue == _prevValue)
            {
                return;
            }

            if (IsReadOnly)
            {
                return;
            }

            _prop.SetValue(newValue);
            _prevValue = newValue;
        }

        public override void Refresh()
        {
            if (_prop == null)
            {
                return;
            }

            var currentValue = _prop.GetValue();

            if (currentValue != _prevValue)
            {
                try
                {
                    OnValueUpdated(currentValue);
                }
                catch (Exception e)
                {
                    Debug.LogError("[SROptions] Error refreshing binding.");
                    Debug.LogException(e);
                }
            }

            _prevValue = currentValue;
        }

        protected virtual void OnBind(string propertyName, Type t) {}
        protected abstract void OnValueUpdated(object newValue);

        public abstract bool CanBind(Type type, bool isReadOnly);

        #endregion

        #region Unity

        protected override void Start()
        {
            base.Start();

            Refresh();
            _hasStarted = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_hasStarted)
            {
                Refresh();
            }
        }

        #endregion
    }
}
