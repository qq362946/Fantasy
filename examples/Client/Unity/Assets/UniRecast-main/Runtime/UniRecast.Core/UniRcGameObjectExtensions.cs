using System.Collections.Generic;
using UnityEngine;

namespace UniRecast.Core
{
    public static class UniRcGameObjectExtensions
    {
        public static List<GameObject> ToHierarchyList(this GameObject root)
        {
            var outlet = new List<GameObject>();
            outlet.Add(root);
            AddChildrenTo(root.transform, outlet);
            return outlet;
        }

        public static void AddChildrenTo(Transform parent, List<GameObject> children)
        {
            foreach (Transform child in parent)
            {
                children.Add(child.gameObject);
                AddChildrenTo(child, children);
            }
        }
    }
}