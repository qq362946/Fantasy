using System;
using System.Collections.Generic;
using System.Linq;

namespace SRDebugger.Editor
{
    abstract class IntegrityIssue
    {
        private readonly string _title;
        private readonly string _description;
        private List<Fix> _fixes;

        public string Title
        {
            get { return _title; }
        }

        public string Description
        {
            get { return _description; }
        }

        public IList<Fix> GetFixes()
        {
            if (_fixes == null)
            {
                _fixes = CreateFixes().ToList();
            }

            return _fixes;
        }

        protected IntegrityIssue(string title, string description)
        {
            _title = title;
            _description = description;
        }

        protected abstract IEnumerable<Fix> CreateFixes();
    }

    abstract class Fix
    {
        private readonly string _name;
        private readonly string _description;
        private readonly bool _isAutoFix;

        public string Name
        {
            get { return _name; }
        }

        public string Description
        {
            get { return _description; }
        }

        public bool IsAutoFix
        {
            get { return _isAutoFix; }
        }

        protected Fix(string name, string description, bool isAutoFix)
        {
            _name = name;
            _description = description;
            _isAutoFix = isAutoFix;
        }

        public abstract void Execute();
    }

    class DelegateFix : Fix
    {
        private readonly Action _fixMethod;

        public DelegateFix(string name, string description, Action fixMethod) : base(name, description, true)
        {
            _fixMethod = fixMethod;
        }

        public override void Execute()
        {
            _fixMethod();
        }
    }
}