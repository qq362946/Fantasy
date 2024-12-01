// Disable unreachable code warning caused by DEBUG
#pragma warning disable 0162

namespace SRF.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Components;
    using Helpers;
    using Internal;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [AddComponentMenu(ComponentMenuPaths.SRServiceManager)]
    public class SRServiceManager : SRAutoSingleton<SRServiceManager>
    {
#if SRDEBUG
		public const bool EnableLogging = true;
#else
        public const bool EnableLogging = false;
#endif

#if UNITY_EDITOR && ((!UNITY_2017 && !UNITY_2018 && !UNITY_2019) || UNITY_2019_3_OR_NEWER)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void RuntimeInitialize()
        {
            // To handle entering play mode without a domain reload, need to reset the state of the service manager.
            _hasQuit = false;
        }
#endif

        /// <summary>
        /// Register the assembly that contains type <typeparamref name="TType"/> with the service manager.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        public static void RegisterAssembly<TType>()
        {
#if NETFX_CORE
            var assembly = typeof(TType).GetTypeInfo().Assembly;
#else
            var assembly = typeof(TType).Assembly;
#endif

            if (_assemblies.Contains(assembly))
            {
                return;
            }

            _assemblies.Add(assembly);
        }

        /// <summary>
        /// Is there a service loading?
        /// </summary>
        public static bool IsLoading
        {
            get { return LoadingCount > 0; }
        }

        public static int LoadingCount = 0;

        public static T GetService<T>() where T : class
        {
            var s = GetServiceInternal(typeof(T)) as T;

            if (s == null && (!_hasQuit || EnableLogging))
            {
                Debug.LogWarning("Service {0} not found. (HasQuit: {1})".Fmt(typeof(T).Name, _hasQuit));
            }


            return s;
        }

        public static object GetService(Type t)
        {
            var s = GetServiceInternal(t);

            if (s == null && (!_hasQuit || EnableLogging))
            {
                Debug.LogWarning("Service {0} not found. (HasQuit: {1})".Fmt(t.Name, _hasQuit));
            }

            return s;
        }

        private static object GetServiceInternal(Type t)
        {
            if (_hasQuit || !Application.isPlaying)
            {
                return null;
            }

            var services = Instance._services;

            for (var i = 0; i < services.Count; i++)
            {
                var s = services[i];

                if (t.IsAssignableFrom(s.Type))
                {
                    if (s.Object == null)
                    {
                        UnRegisterService(t);
                        break;
                    }

                    return s.Object;
                }
            }

            return Instance.AutoCreateService(t);
        }

        public static bool HasService<T>() where T : class
        {
            return HasService(typeof(T));
        }

        public static bool HasService(Type t)
        {
            if (_hasQuit || !Application.isPlaying)
            {
                return false;
            }

            var services = Instance._services;

            for (var i = 0; i < services.Count; i++)
            {
                var s = services[i];

                if (t.IsAssignableFrom(s.Type))
                {
                    return s.Object != null;
                }
            }

            return false;
        }

        public static void RegisterService<T>(object service) where T : class
        {
            RegisterService(typeof(T), service);
        }

        private static void RegisterService(Type t, object service)
        {
            if (_hasQuit)
            {
                return;
            }

            if (HasService(t))
            {
                if (GetServiceInternal(t) == service)
                {
                    return;
                }

                throw new Exception("Service already registered for type " + t.Name);
            }

            UnRegisterService(t);

            if (!t.IsInstanceOfType(service))
            {
                throw new ArgumentException("service {0} must be assignable from type {1}".Fmt(service.GetType(), t));
            }

            Instance._services.Add(new Service {
                Object = service,
                Type = t
            });
        }

        public static void UnRegisterService<T>() where T : class
        {
            UnRegisterService(typeof(T));
        }

        private static void UnRegisterService(Type t)
        {
            if (_hasQuit || !HasInstance)
            {
                return;
            }

            if (!HasService(t))
            {
                return;
            }

            var services = Instance._services;

            for (var i = services.Count - 1; i >= 0; i--)
            {
                var s = services[i];

                if (s.Type == t)
                {
                    services.RemoveAt(i);
                }
            }
        }

        private class Service
        {
            public object Object;
            public Type Type;
        }

        private class ServiceStub
        {
            public Func<object> Constructor;
            public Type InterfaceType;
            public Func<Type> Selector;
            public Type Type;

            public override string ToString()
            {
                var s = InterfaceType.Name + " (";

                if (Type != null)
                {
                    s += "Type: " + Type;
                }
                else if (Selector != null)
                {
                    s += "Selector: " + Selector;
                }
                else if (Constructor != null)
                {
                    s += "Constructor: " + Constructor;
                }

                s += ")";

                return s;
            }
        }

        private static readonly List<Assembly> _assemblies = new List<Assembly>(2);

        private readonly SRList<Service> _services = new SRList<Service>();

        private List<ServiceStub> _serviceStubs;

        private static bool _hasQuit;

        protected override void Awake()
        {
            _hasQuit = false;
            base.Awake();
            DontDestroyOnLoad(CachedGameObject);

            CachedGameObject.hideFlags = HideFlags.NotEditable;
        }

        protected void UpdateStubs()
        {
            if (_serviceStubs != null)
            {
                return;
            }

            RegisterAssembly<SRServiceManager>();
          
            _serviceStubs = new List<ServiceStub>();

            var types = new List<Type>();

            foreach (var assembly in _assemblies)
            {
                try
                {
#if NETFX_CORE
                    types.AddRange(assembly.Types);
#else
                    types.AddRange(assembly.GetTypes());
#endif
                }
                catch (Exception e)
                {
                    Debug.LogError("[SRServiceManager] Error loading assembly {0}".Fmt(assembly.FullName), this);
                    Debug.LogException(e);
                }
            }

            foreach (var type in types)
            {
                ScanType(type);
            }

            if (EnableLogging)
            {
                var serviceStrings =
                    _serviceStubs.Select(p => "	{0}".Fmt(p)).ToArray();

                Debug.Log("[SRServiceManager] Services Discovered: {0} \n  {1}".Fmt(serviceStrings.Length,
                    string.Join("\n  ", serviceStrings)));
            }
        }

        protected object AutoCreateService(Type t)
        {
            UpdateStubs();

            foreach (var stub in _serviceStubs)
            {
                if (stub.InterfaceType != t)
                {
                    continue;
                }

                object service = null;

                if (stub.Constructor != null)
                {
                    service = stub.Constructor();
                }
                else
                {
                    var serviceType = stub.Type;

                    if (serviceType == null)
                    {
                        serviceType = stub.Selector();
                    }

                    service = DefaultServiceConstructor(t, serviceType);
                }

                if (!HasService(t))
                {
                    RegisterService(t, service);
                }

                if (EnableLogging)
                {
                    Debug.Log("[SRServiceManager] Auto-created service: {0} ({1})".Fmt(stub.Type, stub.InterfaceType),
                        service as Object);
                }

                return service;
            }

            return null;
        }

        protected void OnApplicationQuit()
        {
            _hasQuit = true;
            _assemblies.Clear();
        }

#if UNITY_EDITOR

        protected void OnDisable()
        {
            if (EnableLogging)
            {
                Debug.Log("[SRServiceManager] Cleaning up services");
            }

            // Script recompile is likely in progress - clear up everything.
            foreach (Service s in _services)
            {
                IDisposable disposable = s.Object as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }

                Behaviour behaviour = s.Object as Behaviour;
                if (behaviour != null)
                {
                    DestroyImmediate(behaviour.gameObject);
                } else if (s.Object is Object)
                {
                    DestroyImmediate(s.Object as Object);
                }
            }

            _services.Clear(clean: true);
        }
#endif

        private static object DefaultServiceConstructor(Type serviceIntType, Type implType)
        {
            // If mono-behaviour based, create a gameobject for this service
            if (typeof(MonoBehaviour).IsAssignableFrom(implType))
            {
                var go = new GameObject("_S_" + serviceIntType.Name);
                return go.AddComponent(implType);
            }

            // If ScriptableObject based, create an instance
            if (typeof(ScriptableObject).IsAssignableFrom(implType))
            {
                var obj = ScriptableObject.CreateInstance(implType);
                return obj;
            }

            // If just a standard C# object, just create an instance
            return Activator.CreateInstance(implType);
        }

#region Type Scanning

        private void ScanType(Type type)
        {
            var attribute = SRReflection.GetAttribute<ServiceAttribute>(type);

            if (attribute != null)
            {
                _serviceStubs.Add(new ServiceStub {
                    Type = type,
                    InterfaceType = attribute.ServiceType
                });
            }

            ScanTypeForConstructors(type, _serviceStubs);
            ScanTypeForSelectors(type, _serviceStubs);
        }

        private static void ScanTypeForSelectors(Type t, List<ServiceStub> stubs)
        {
            var methods = GetStaticMethods(t);

            foreach (var method in methods)
            {
                var attrib = SRReflection.GetAttribute<ServiceSelectorAttribute>(method);

                if (attrib == null)
                {
                    continue;
                }

                if (method.ReturnType != typeof(Type))
                {
                    Debug.LogError("ServiceSelector must have return type of Type ({0}.{1}())".Fmt(t.Name, method.Name));
                    continue;
                }

                if (method.GetParameters().Length > 0)
                {
                    Debug.LogError("ServiceSelector must have no parameters ({0}.{1}())".Fmt(t.Name, method.Name));
                    continue;
                }

                var stub = stubs.FirstOrDefault(p => p.InterfaceType == attrib.ServiceType);

                if (stub == null)
                {
                    stub = new ServiceStub {
                        InterfaceType = attrib.ServiceType
                    };

                    stubs.Add(stub);
                }

#if NETFX_CORE
                stub.Selector = (Func<Type>)method.CreateDelegate(typeof(Func<Type>));
#else
                stub.Selector = (Func<Type>)Delegate.CreateDelegate(typeof(Func<Type>), method);
#endif
            }
        }

        private static void ScanTypeForConstructors(Type t, List<ServiceStub> stubs)
        {
            var methods = GetStaticMethods(t);

            foreach (var method in methods)
            {
                var attrib = SRReflection.GetAttribute<ServiceConstructorAttribute>(method);

                if (attrib == null)
                {
                    continue;
                }

                if (method.ReturnType != attrib.ServiceType)
                {
                    Debug.LogError("ServiceConstructor must have return type of {2} ({0}.{1}())".Fmt(t.Name, method.Name,
                        attrib.ServiceType));
                    continue;
                }

                if (method.GetParameters().Length > 0)
                {
                    Debug.LogError("ServiceConstructor must have no parameters ({0}.{1}())".Fmt(t.Name, method.Name));
                    continue;
                }

                var stub = stubs.FirstOrDefault(p => p.InterfaceType == attrib.ServiceType);

                if (stub == null)
                {
                    stub = new ServiceStub {
                        InterfaceType = attrib.ServiceType
                    };

                    stubs.Add(stub);
                }

                var m = method;
                stub.Constructor = () => m.Invoke(null, null);
            }
        }

#endregion

#region Reflection

        private static MethodInfo[] GetStaticMethods(Type t)
        {
#if !NETFX_CORE
            return t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
#else
            return t.GetTypeInfo().DeclaredMethods.Where(p => p.IsStatic).ToArray();
#endif
        }

#endregion
    }
}
