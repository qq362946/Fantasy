using System.Collections.Generic;
using UnityEngine;

namespace Fantasy
{
    public class FantasyRef : MonoBehaviour, ISerializationCallbackReceiver
    {
        public bool isUI = true;
        public string moduleName;
        
        public string componentName;
        
        public List<FantasyUIData> list = new();
        private readonly Dictionary<string, Object> _referenceDic = new();
        
        public T GetReference<T>(string key) where T : Object
        {
            return _referenceDic.TryGetValue(key, out var obj) ? (T)obj : null;
        }

        public Object GetReference(string key)
        {
            return _referenceDic.GetValueOrDefault(key);
        }

        public void OnAfterDeserialize()
        {
            _referenceDic.Clear();
			
            foreach (var referenceCollectorData in list)
            {
                if (_referenceDic.ContainsKey(referenceCollectorData.key))
                {
                    continue;
                }
				
                _referenceDic.Add(referenceCollectorData.key, referenceCollectorData.gameObject);
            }
        }
		
        public void OnBeforeSerialize() { }
    }
}