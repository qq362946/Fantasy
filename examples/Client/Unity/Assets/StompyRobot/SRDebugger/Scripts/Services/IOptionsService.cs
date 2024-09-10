namespace SRDebugger.Services
{
    using System;
    using System.Collections.Generic;
    using Internal;

    public interface IOptionsService
    {
        /// <summary>
        /// Invoked when the <seealso cref="Options"/> collection changes.
        /// </summary>
        event EventHandler OptionsUpdated;

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
        /// Add an options container to the options collection.
        /// </summary>
        void AddContainer(IOptionContainer optionContainer);

        /// <summary>
        /// Remove any options that were added from the <paramref name="obj"/> container.
        /// </summary>
        void RemoveContainer(object obj);

        /// <summary>
        /// Remove an options container from the options collection.
        /// </summary>
        void RemoveContainer(IOptionContainer optionContainer);
    }
}
