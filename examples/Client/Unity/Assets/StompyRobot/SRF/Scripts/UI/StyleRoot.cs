namespace SRF.UI
{
    using Internal;
    using UnityEngine;

    [ExecuteInEditMode]
    [AddComponentMenu(ComponentMenuPaths.StyleRoot)]
    public sealed class StyleRoot : SRMonoBehaviour
    {
        private StyleSheet _activeStyleSheet;
        public StyleSheet StyleSheet;

        public Style GetStyle(string key)
        {
            if (StyleSheet == null)
            {
                Debug.LogWarning("[StyleRoot] StyleSheet is not set.", this);
                return null;
            }

            return StyleSheet.GetStyle(key);
        }

        private void OnEnable()
        {
            _activeStyleSheet = null;

            if (StyleSheet != null)
            {
                OnStyleSheetChanged();
            }
        }

        private void OnDisable()
        {
            OnStyleSheetChanged();
        }

        private void Update()
        {
            if (_activeStyleSheet != StyleSheet)
            {
                OnStyleSheetChanged();
            }
        }

        private void OnStyleSheetChanged()
        {
            _activeStyleSheet = StyleSheet;

            BroadcastMessage("SRStyleDirty", SendMessageOptions.DontRequireReceiver);
        }

        public void SetDirty()
        {
            _activeStyleSheet = null;
        }
    }
}
