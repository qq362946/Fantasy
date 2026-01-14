namespace SRDebugger.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Internal;

    public interface IOptionsService
    {
        /// <summary>
        /// Invoked when the <seealso cref="Options"/> collection changes.
        /// </summary>
        event EventHandler OptionsUpdated;

        /// <summary>
        /// Invoked when the value of an option has been updated.
        /// </summary>
        event EventHandler<PropertyChangedEventArgs> OptionsValueUpdated;

        ICollection<OptionDefinition> Options { get; }

        /// <summary>
        /// Scan <paramref name="obj" /> for options add them to the Options collection
        /// </summary>
        /// <param name="obj">Object to scan for options</param>
        [Obsolete("Use IOptionsService.AddContainer instead.")]
        void Scan(object obj);

        /// <summary>
        /// Scan <paramref name="obj"/> for options and add them to the Options collection.
        /// </summary>
        void AddContainer(object obj);

        /// <summary>
        /// Remove any options from the <paramref name="obj"/> container.
        /// </summary>
        void RemoveContainer(object obj);
    }
}
