namespace SRF
{
    using UnityEngine;

    public static class SRFGameObjectExtensions
    {
        public static T GetIComponent<T>(this GameObject t) where T : class
        {
            return t.GetComponent(typeof (T)) as T;
        }

        /// <summary>
        /// Get the component T, or add it to the GameObject if none exists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetComponentOrAdd<T>(this GameObject obj) where T : Component
        {
            var t = obj.GetComponent<T>();

            if (t == null)
            {
                t = obj.AddComponent<T>();
            }

            return t;
        }

        /// <summary>
        /// Removed component of type T if it exists on the GameObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        public static void RemoveComponentIfExists<T>(this GameObject obj) where T : Component
        {
            var t = obj.GetComponent<T>();

            if (t != null)
            {
                Object.Destroy(t);
            }
        }

        /// <summary>
        /// Removed components of type T if it exists on the GameObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        public static void RemoveComponentsIfExists<T>(this GameObject obj) where T : Component
        {
            var t = obj.GetComponents<T>();

            for (var i = 0; i < t.Length; i++)
            {
                Object.Destroy(t[i]);
            }
        }

        /// <summary>
        /// Set enabled property MonoBehaviour of type T if it exists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="enable"></param>
        /// <returns>True if the component exists</returns>
        public static bool EnableComponentIfExists<T>(this GameObject obj, bool enable = true) where T : MonoBehaviour
        {
            var t = obj.GetComponent<T>();

            if (t == null)
            {
                return false;
            }

            t.enabled = enable;

            return true;
        }

        /// <summary>
        /// Set the layer of a gameobject and all child objects
        /// </summary>
        /// <param name="o"></param>
        /// <param name="layer"></param>
        public static void SetLayerRecursive(this GameObject o, int layer)
        {
            SetLayerInternal(o.transform, layer);
        }

        private static void SetLayerInternal(Transform t, int layer)
        {
            t.gameObject.layer = layer;

            foreach (Transform o in t)
            {
                SetLayerInternal(o, layer);
            }
        }
    }
}
