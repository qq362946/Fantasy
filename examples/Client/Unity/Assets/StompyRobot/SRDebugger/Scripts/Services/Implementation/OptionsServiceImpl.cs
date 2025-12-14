namespace SRDebugger.Services.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using Internal;
    using SRF.Service;

    [Service(typeof (IOptionsService))]
    public class OptionsServiceImpl : IOptionsService
    {
        public event EventHandler OptionsUpdated;
        public event EventHandler<PropertyChangedEventArgs> OptionsValueUpdated;

        public ICollection<OptionDefinition> Options
        {
            get { return _optionsReadonly; }
        }

        private readonly Dictionary<object, ICollection<OptionDefinition>> _optionContainerLookup = new Dictionary<object, ICollection<OptionDefinition>>();
        private readonly List<OptionDefinition> _options = new List<OptionDefinition>();
        private readonly IList<OptionDefinition> _optionsReadonly;

        public OptionsServiceImpl()
        {
            _optionsReadonly = new ReadOnlyCollection<OptionDefinition>(_options);

            Scan(SROptions.Current);
            SROptions.Current.PropertyChanged += OnSROptionsPropertyChanged;
        }

        public void Scan(object obj)
        {
            AddContainer(obj);
        }

        public void AddContainer(object obj)
        {
            if (_optionContainerLookup.ContainsKey(obj))
            {
                throw new Exception("An object should only be added once.");
            }

            var options = SRDebuggerUtil.ScanForOptions(obj);
            _optionContainerLookup.Add(obj, options);

            if (options.Count > 0)
            {
                _options.AddRange(options);
                OnOptionsUpdated();

                var changed = obj as INotifyPropertyChanged;
                if (changed != null)
                {
                    changed.PropertyChanged += OnPropertyChanged;
                }
            }
        }

        public void RemoveContainer(object obj)
        {
            if (!_optionContainerLookup.ContainsKey(obj))
            {
                return;
            }

            var list = _optionContainerLookup[obj];
            _optionContainerLookup.Remove(obj);
            foreach (var op in list)
            {
                _options.Remove(op);
            }

            var changed = obj as INotifyPropertyChanged;
            if (changed != null)
            {
                changed.PropertyChanged -= OnPropertyChanged;
            }

            OnOptionsUpdated();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (OptionsValueUpdated != null)
            {
                OptionsValueUpdated(this, propertyChangedEventArgs);
            }
        }
        private void OnSROptionsPropertyChanged(object sender, string propertyName)
        {
            OnPropertyChanged(sender, new PropertyChangedEventArgs(propertyName));
        }

        private void OnOptionsUpdated()
        {
            if (OptionsUpdated != null)
            {
                OptionsUpdated(this, EventArgs.Empty);
            }
        }
    }
}
