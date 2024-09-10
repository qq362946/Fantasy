namespace SRDebugger.Services.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Internal;
    using SRF.Service;
    using SRF.Helpers;
    using UnityEngine;

    [Service(typeof (IOptionsService))]
    public partial class OptionsServiceImpl : IOptionsService
    {
        public event EventHandler OptionsUpdated;

        public ICollection<OptionDefinition> Options
        {
            get { return _optionsReadonly; }
        }

        private void OptionsContainerOnOptionAdded(IOptionContainer container, OptionDefinition optionDefinition)
        {
            List<OptionDefinition> options;
            if(!_optionContainerLookup.TryGetValue(container, out options))
            {
                Debug.LogWarning("[SRDebugger] Received event from unknown option container.");
                return;
            }

            if (options.Contains(optionDefinition))
            {
                Debug.LogWarning("[SRDebugger] Received option added event from option container, but option has already been added.");
                return;
            }

            options.Add(optionDefinition);
            _options.Add(optionDefinition);
            OnOptionsUpdated();
        }

        private void OptionsContainerOnOptionRemoved(IOptionContainer container, OptionDefinition optionDefinition)
        {
            List<OptionDefinition> options;
            if (!_optionContainerLookup.TryGetValue(container, out options))
            {
                Debug.LogWarning("[SRDebugger] Received event from unknown option container.");
                return;
            }

            if (options.Remove(optionDefinition))
            {
                _options.Remove(optionDefinition);
                OnOptionsUpdated();
            }
            else
            {
                Debug.LogWarning("[SRDebugger] Received option removed event from option container, but option does not exist.");
            }
        }

        private readonly Dictionary<IOptionContainer, List<OptionDefinition>> _optionContainerLookup = new Dictionary<IOptionContainer, List<OptionDefinition>>();

        private readonly Dictionary<IOptionContainer, OptionContainerEventHandler> _optionContainerEventHandlerLookup = new Dictionary<IOptionContainer, OptionContainerEventHandler>();

        private readonly List<OptionDefinition> _options = new List<OptionDefinition>();

        private readonly IList<OptionDefinition> _optionsReadonly;

        public OptionsServiceImpl()
        {
            _optionsReadonly = new ReadOnlyCollection<OptionDefinition>(_options);
        }

        public void Scan(object obj)
        {
            AddContainer(obj);
        }

        public void AddContainer(object obj)
        {
            var container = obj as IOptionContainer ?? new ReflectionOptionContainer(obj);
            AddContainer(container);
        }

        public void AddContainer(IOptionContainer optionContainer)
        {
            if (_optionContainerLookup.ContainsKey(optionContainer))
            {
                throw new Exception("An options container should only be added once.");
            }

            List<OptionDefinition> options = new List<OptionDefinition>();
            options.AddRange(optionContainer.GetOptions());

            _optionContainerLookup.Add(optionContainer, options);

            if (optionContainer.IsDynamic)
            {
                var handler = new OptionContainerEventHandler(this, optionContainer);
                _optionContainerEventHandlerLookup.Add(optionContainer, handler);
            }

            if (options.Count > 0)
            {
                _options.AddRange(options);
                OnOptionsUpdated();
            }
        }
        
        public void RemoveContainer(object obj)
        {
            var container = obj as IOptionContainer ?? new ReflectionOptionContainer(obj);
            RemoveContainer(container);
        }

        public void RemoveContainer(IOptionContainer optionContainer)
        {
            if (!_optionContainerLookup.ContainsKey(optionContainer))
            {
                return;
            }

            bool isDirty = false;
            var list = _optionContainerLookup[optionContainer];
            _optionContainerLookup.Remove(optionContainer);
            foreach (var op in list)
            {
                _options.Remove(op);
                isDirty = true;
            }

            OptionContainerEventHandler handler;
            if (_optionContainerEventHandlerLookup.TryGetValue(optionContainer,
                out handler))
            {
                handler.Dispose();
                _optionContainerEventHandlerLookup.Remove(optionContainer);
            }

            if (isDirty)
            {
                OnOptionsUpdated();
            }
        }

        private void OnOptionsUpdated()
        {
            if (OptionsUpdated != null)
            {
                OptionsUpdated(this, EventArgs.Empty);
            }
        }

        class OptionContainerEventHandler : IDisposable
        {
            private readonly OptionsServiceImpl _service;
            private readonly IOptionContainer _container;

            public OptionContainerEventHandler(OptionsServiceImpl service, IOptionContainer container)
            {
                _container = container;
                _service = service;

                container.OptionAdded += ContainerOnOptionAdded;
                container.OptionRemoved += ContainerOnOptionRemoved;
            }

            private void ContainerOnOptionAdded(OptionDefinition obj)
            {
                _service.OptionsContainerOnOptionAdded(_container, obj);
            }

            private void ContainerOnOptionRemoved(OptionDefinition obj)
            {
                _service.OptionsContainerOnOptionRemoved(_container, obj);
            }

            public void Dispose()
            {
                _container.OptionAdded -= ContainerOnOptionAdded;
                _container.OptionRemoved -= ContainerOnOptionRemoved;
            }
        }
    }
}
