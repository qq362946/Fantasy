namespace SRDebugger.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SRF;
    using UI.Controls;
    using UI.Controls.Data;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public static class OptionControlFactory
    {
        private static IList<DataBoundControl> _dataControlPrefabs;
        private static ActionControl _actionControlPrefab;

        /// <summary>
        /// Create a control from an <c>OptionDefinition</c>, optionally providing <paramref name="categoryPrefix" /> to remove
        /// the category name from the start of the control.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="categoryPrefix"></param>
        /// <returns></returns>
        public static OptionsControlBase CreateControl(OptionDefinition from, string categoryPrefix = null)
        {
            if (_dataControlPrefabs == null)
            {
                _dataControlPrefabs = Resources.LoadAll<DataBoundControl>(SRDebugPaths.DataControlsResourcesPath);
            }

            if (_actionControlPrefab == null)
            {
                _actionControlPrefab =
                    Resources.LoadAll<ActionControl>(SRDebugPaths.DataControlsResourcesPath).FirstOrDefault();
            }

            if (_actionControlPrefab == null)
            {
                Debug.LogError("[SRDebugger.Options] Cannot find ActionControl prefab.");
            }

            if (from.Property != null)
            {
                return CreateDataControl(from, categoryPrefix);
            }

            if (from.Method != null)
            {
                return CreateActionControl(from, categoryPrefix);
            }

            throw new Exception("OptionDefinition did not contain property or method.");
        }

        private static ActionControl CreateActionControl(OptionDefinition from, string categoryPrefix = null)
        {
            var control = SRInstantiate.Instantiate(_actionControlPrefab);

            if (control == null)
            {
                Debug.LogWarning("[SRDebugger.OptionsTab] Error creating action control from prefab");
                return null;
            }

            control.SetMethod(from.Name, from.Method);
            control.Option = from;

            return control;
        }

        private static DataBoundControl CreateDataControl(OptionDefinition from, string categoryPrefix = null)
        {
            var prefab = _dataControlPrefabs.FirstOrDefault(p => p.CanBind(@from.Property.PropertyType, !from.Property.CanWrite));

            if (prefab == null)
            {
                Debug.LogWarning(
                    "[SRDebugger.OptionsTab] Can't find data control for type {0}".Fmt(from.Property.PropertyType));
                return null;
            }

            var instance = SRInstantiate.Instantiate(prefab);

            try
            {
                var n = from.Name;

                // Remove category name from the start of the property name
                if (!string.IsNullOrEmpty(categoryPrefix) && n.StartsWith(categoryPrefix))
                {
                    n = n.Substring(categoryPrefix.Length);
                }

                instance.Bind(n, from.Property);
                instance.Option = from;
            }
            catch (Exception e)
            {
                Debug.LogError("[SRDebugger.Options] Error binding to property {0}".Fmt(from.Name));
                Debug.LogException(e);

                Object.Destroy(instance);
                instance = null;
            }

            return instance;
        }
    }
}
