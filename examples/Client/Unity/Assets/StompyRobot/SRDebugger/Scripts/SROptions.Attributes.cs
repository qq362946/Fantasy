using System;

public partial class SROptions
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NumberRangeAttribute : Attribute
    {
        public readonly double Max;
        public readonly double Min;

        public NumberRangeAttribute(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IncrementAttribute : Attribute
    {
        public readonly double Increment;

        public IncrementAttribute(double increment)
        {
            Increment = increment;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class SortAttribute : Attribute
    {
        public readonly int SortPriority;

        public SortAttribute(int priority)
        {
            SortPriority = priority;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class DisplayNameAttribute : Attribute
    {
        public readonly string Name;

        public DisplayNameAttribute(string name)
        {
            Name = name;
        }
    }
}

#if NETFX_CORE

namespace System.ComponentModel
{

	[AttributeUsage(AttributeTargets.All)]
	public sealed class CategoryAttribute : Attribute
	{

		public readonly string Category;

		public CategoryAttribute(string category)
		{
			Category = category;
		}

	}

}

#endif
