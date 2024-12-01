using System;
using SRF.Helpers;

namespace SRDebugger
{
    /// <summary>
    /// Class describing how an option should be presented within the options panel.
    /// </summary>
    public sealed class OptionDefinition
    {
        /// <summary>
        /// Display-name for the option.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The category that this option should be placed within.
        /// </summary>
        public string Category { get; private set; }

        /// <summary>
        /// Sort order within the category. Order is low to high, (options with lower SortPriority will appear before
        /// options with higher SortPriority).
        /// </summary>
        public int SortPriority { get; private set; }

        /// <summary>
        /// Whether this option is a method that should be invoked.
        /// </summary>
        public bool IsMethod
        {
            get { return Method != null; }
        }

        /// <summary>
        /// Whether this option is a property that has a value.
        /// </summary>
        public bool IsProperty
        {
            get { return Property != null; }
        }

        /// <summary>
        /// The underlying method for this OptionDefinition.
        /// Can be null if <see cref="IsMethod"/> is false.
        /// </summary>
        public SRF.Helpers.MethodReference Method { get; private set; }

        /// <summary>
        /// The underlying property for this OptionDefinition.
        /// Can be null if <see cref="IsProperty"/> is false.
        /// </summary>
        public SRF.Helpers.PropertyReference Property { get; private set; }

        private OptionDefinition(string name, string category, int sortPriority)
        {
            Name = name;
            Category = category;
            SortPriority = sortPriority;
        }

        public OptionDefinition(string name, string category, int sortPriority, SRF.Helpers.MethodReference method)
            : this(name, category, sortPriority)
        {
            Method = method;
        }

        public OptionDefinition(string name, string category, int sortPriority, SRF.Helpers.PropertyReference property)
            : this(name, category, sortPriority)
        {
            Property = property;
        }

        public static OptionDefinition FromMethod(string name, Action callback, string category = "Default", int sortPriority = 0)
        {
            return new OptionDefinition(name, category, sortPriority, callback);;
        }

        /// <summary>
        /// Create an option definition from a setter and getter lambda.
        /// </summary>
        /// <param name="name">Name to display in options menu.</param>
        /// <param name="getter">Method to get the current value of the property.</param>
        /// <param name="setter">Method to set the value of the property (can be null if read-only)</param>
        /// <param name="category">Category to display the option in.</param>
        /// <param name="sortPriority">Sort priority to arrange the option within the category.</param>
        /// <returns>The created option definition.</returns>
        public static OptionDefinition Create<T>(string name, Func<T> getter, Action<T> setter = null, string category = "Default", int sortPriority = 0)
        {
            return new OptionDefinition(name, category, sortPriority, PropertyReference.FromLambda(getter, setter));
        }
    }
}
