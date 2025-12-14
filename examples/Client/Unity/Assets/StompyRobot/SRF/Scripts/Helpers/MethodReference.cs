namespace SRF.Helpers
{
    using System.Reflection;

    public class MethodReference
    {
        private MethodInfo _method;
        private object _target;

        public MethodReference(object target, MethodInfo method)
        {
            SRDebugUtil.AssertNotNull(target);

            _target = target;
            _method = method;
        }

        public string MethodName
        {
            get { return _method.Name; }
        }

        public object Invoke(object[] parameters)
        {
            return _method.Invoke(_target, parameters);
        }
    }
}
