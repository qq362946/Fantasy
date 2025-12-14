namespace SRDebugger.Internal
{
    public class OptionDefinition
    {
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

        public string Name { get; private set; }
        public string Category { get; private set; }
        public int SortPriority { get; private set; }
        public SRF.Helpers.MethodReference Method { get; private set; }
        public SRF.Helpers.PropertyReference Property { get; private set; }
    }
}
