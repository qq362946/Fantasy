using System;
using System.Collections.Generic;

namespace SRDebugger
{
    /// <summary>
    /// You can implement this interface to create a dynamic "options container".
    /// Add the container to SRDebugger via the SRDebug API.
    /// </summary>
    /// <remarks>
    /// When the container is added via the API, the initial set of options will be fetched via <see cref="GetOptions"/>.
    /// Options that are added or removed after this point must fire the <see cref="OptionAdded"/> and <see cref="OptionRemoved"/> events in order
    /// for those options to be added/removed from the debug panel.
    /// If you do not intend to fire any events (i.e. this is a static container) then <see cref="IsDynamic"/> should return false.
    /// </remarks>
    public interface IOptionContainer
    {
        /// <summary>
        /// Get the initial set of options contained in this object.
        /// </summary>
        IEnumerable<OptionDefinition> GetOptions();

        /// <summary>
        /// Will the options collection be changed via events?
        /// If true, changes to the option set can be provided via the events <see cref="OptionAdded"/> and <see cref="OptionRemoved"/>.
        /// If false, the events will be ignored.
        /// </summary>
        bool IsDynamic { get; }

        event Action<OptionDefinition> OptionAdded;
        event Action<OptionDefinition> OptionRemoved;
    }

    public sealed class DynamicOptionContainer : IOptionContainer
    {
        public IList<OptionDefinition> Options
        {
            get { return _optionsReadOnly; }
        }

        private readonly List<OptionDefinition> _options = new List<OptionDefinition>();
        private readonly IList<OptionDefinition> _optionsReadOnly;

        public DynamicOptionContainer()
        {
            _optionsReadOnly = _options.AsReadOnly();
        }

        public void AddOption(OptionDefinition option)
        {
            _options.Add(option);

            if (OptionAdded != null)
            {
                OptionAdded(option);
            }
        }

        public bool RemoveOption(OptionDefinition option)
        {
            if (_options.Remove(option))
            {
                if (OptionRemoved != null)
                {
                    OptionRemoved(option);
                }

                return true;
            }

            return false;
        }
        
        IEnumerable<OptionDefinition> IOptionContainer.GetOptions()
        {
            return _options;
        }

        public bool IsDynamic
        {
            get { return true; }
        }

        public event Action<OptionDefinition> OptionAdded;
        public event Action<OptionDefinition> OptionRemoved;
    }

}