namespace SRF
{
    using System.Collections.Generic;
    using UnityEngine;

    public static class SRFTransformExtensions
    {
        public static IEnumerable<Transform> GetChildren(this Transform t)
        {
            var i = 0;

            while (i < t.childCount)
            {
                yield return t.GetChild(i);
                ++i;
            }
        }

        /// <summary>
        /// Reset all local values on a transform to identity
        /// </summary>
        /// <param name="t"></param>
        public static void ResetLocal(this Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        /// <summary>
        /// Create an empty child object of this transform
        /// </summary>
        /// <param name="t"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static GameObject CreateChild(this Transform t, string name)
        {
            var go = new GameObject(name);
            go.transform.parent = t;
            go.transform.ResetLocal();
            go.gameObject.layer = t.gameObject.layer;

            return go;
        }

        /// <summary>
        /// Set the parent of this transform, but maintain the localScale, localPosition, localRotation values.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="parent"></param>
        public static void SetParentMaintainLocals(this Transform t, Transform parent)
        {
            t.SetParent(parent, false);
        }

        /// <summary>
        /// Copy local position,rotation,scale from other transform
        /// </summary>
        /// <param name="t"></param>
        /// <param name="from"></param>
        public static void SetLocals(this Transform t, Transform from)
        {
            t.localPosition = from.localPosition;
            t.localRotation = from.localRotation;
            t.localScale = from.localScale;
        }

        /// <summary>
        /// Set position/rotation to from. Scale is unchanged
        /// </summary>
        /// <param name="t"></param>
        /// <param name="from"></param>
        public static void Match(this Transform t, Transform from)
        {
            t.position = from.position;
            t.rotation = from.rotation;
        }

        /// <summary>
        /// Destroy all child game objects
        /// </summary>
        /// <param name="t"></param>
        public static void DestroyChildren(this Transform t)
        {
            foreach (var child in t)
            {
                Object.Destroy(((Transform) child).gameObject);
            }
        }
    }
}
