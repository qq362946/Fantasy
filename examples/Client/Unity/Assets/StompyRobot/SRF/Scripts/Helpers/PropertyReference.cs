using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using UnityEngine;

namespace SRF.Helpers
{
    using System;
    using System.Linq;
    using System.Reflection;

    public delegate void PropertyValueChangedHandler(PropertyReference property);

    public sealed class PropertyReference
    {
        public event PropertyValueChangedHandler ValueChanged
        {
            add
            {
                if (_valueChangedListeners == null)
                {
                    _valueChangedListeners = new List<PropertyValueChangedHandler>();
                }

                _valueChangedListeners.Add(value);
                if (_valueChangedListeners.Count == 1 && _target is INotifyPropertyChanged)
                {
                    // Subscribe to value changed event on target.
                    ((INotifyPropertyChanged)_target).PropertyChanged += OnTargetPropertyChanged;
                }
            }

            remove
            {
                if (_valueChangedListeners == null)
                {
                    return;
                }

                if (_valueChangedListeners.Remove(value) && _valueChangedListeners.Count == 0 &&
                    _target is INotifyPropertyChanged)
                {
                    // Unsubscribe from value changed event on target.
                    ((INotifyPropertyChanged) _target).PropertyChanged -= OnTargetPropertyChanged;
                }
            }
        }

        [CanBeNull] private readonly PropertyInfo _property;
        [CanBeNull] private readonly object _target;

        [CanBeNull] private readonly Attribute[] _attributes;

        [CanBeNull] private readonly Func<object> _getter;

        [CanBeNull] private readonly Action<object> _setter;

        [CanBeNull] private List<PropertyValueChangedHandler> _valueChangedListeners;


        public static PropertyReference FromLambda<T>(Func<T> getter, Action<T> setter = null, params Attribute[] attributes)
        {
            Action<object> internalSetter = null;
            if (setter != null)
            {
                internalSetter = o => setter((T)o);
            }
            return new PropertyReference(typeof(T), () => getter(), internalSetter, attributes);
        }

        /// <summary>
        /// Create a property reference from an object target and reflection PropertyInfo.
        /// This represents a property on an object.
        /// </summary>
        public PropertyReference(object target, PropertyInfo property)
        {
            SRDebugUtil.AssertNotNull(target);
            SRDebugUtil.AssertNotNull(property);

            PropertyType = property.PropertyType;
            _property = property;
            _target = target;

#if NETFX_CORE
            if(_property.GetMethod != null && _property.GetMethod.IsPublic)
#else
            if (property.GetGetMethod() != null)
#endif
            {
                _getter = () => SRReflection.GetPropertyValue(target, property);
            }


#if NETFX_CORE
            if(_property.SetMethod != null && _property.SetMethod.IsPublic)
#else
            if (property.GetSetMethod() != null)
#endif
            {
                _setter = (v) => SRReflection.SetPropertyValue(target, property, v);
            }
        } 
        
        /// <summary>
        /// Create a property reference from lambdas. This has no underlying reflection or object associated with it.
        /// </summary>
        public PropertyReference(Type type, Func<object> getter = null, Action<object> setter = null, Attribute[] attributes = null)
        {
            SRDebugUtil.AssertNotNull(type);

            PropertyType = type;
            _attributes = attributes;
            _getter = getter;
            _setter = setter;
        }

        public Type PropertyType { get; private set; }

        public bool CanRead
        {
            get
            {
                return _getter != null;
            }
        }

        public bool CanWrite
        {
            get
            {
                return _setter != null;
            }
        }

        /// <summary>
        /// Notify any listeners to <see cref="ValueChanged"/> that the value has been updated.
        /// </summary>
        public void NotifyValueChanged()
        {
            if (_valueChangedListeners == null)
            {
                return;
            }

            foreach (var handler in _valueChangedListeners)
            {
                handler(this);
            }
        }

        public object GetValue()
        {
            if (_getter != null)
            {
                return _getter();
            }

            return null;
        }

        public void SetValue(object value)
        {
            if (_setter != null)
            {
                _setter(value);
            }
            else
            {
                throw new InvalidOperationException("Can not write to property");
            }
        }

        public T GetAttribute<T>() where T : Attribute
        {
            if (_attributes != null)
            {
                return _attributes.FirstOrDefault(p => p is T) as T;
            }

            if (_property != null)
            {
                return _property.GetCustomAttributes(typeof(T), true).FirstOrDefault() as T;
            }

            return null;
        }
        private void OnTargetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_valueChangedListeners == null || _valueChangedListeners.Count == 0)
            {
                Debug.LogWarning("[PropertyReference] Received property value changed event when there are no listeners. Did the event not get unsubscribed correctly?");
                return;
            }

            NotifyValueChanged();
        }

        public override string ToString()
        {
            if (_property != null)
            {
                return "{0}.{1}".Fmt(_property.DeclaringType.Name, _property.Name);
            }

            return "<delegate>";
        }
    }
}
