using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
// ReSharper disable PossibleNullReferenceException

namespace Fantasy
{
	[Serializable]
	public class FantasyUIData
	{
		public string key;
		public UnityEngine.Object gameObject;
	}
	
	public class FantasyUIDataComparer : IComparer<FantasyUIData>
	{
		public int Compare(FantasyUIData x, FantasyUIData y)
		{
			return string.Compare(x.key, y.key, StringComparison.Ordinal);
		}
	}
	
	public class FantasyUI : MonoBehaviour, ISerializationCallbackReceiver
	{
		public string assetName;
		public string bundleName;
		public string componentName;
		public UILayer uiLayer = UILayer.BaseRoot;
		public List<FantasyUIData> list = new List<FantasyUIData>();
		private readonly Dictionary<string, Object> _referenceDic = new Dictionary<string, Object>();

		public T GetReference<T>(string key) where T : Object
		{
			return _referenceDic.TryGetValue(key, out var obj) ? (T)obj : null;
		}

		public Object GetReference(string key)
		{
			return _referenceDic.TryGetValue(key, out var obj) ? obj : null;
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