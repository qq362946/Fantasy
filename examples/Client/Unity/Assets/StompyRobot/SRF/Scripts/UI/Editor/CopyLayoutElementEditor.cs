namespace SRF.UI.Editor
{
    /*[CustomEditor(typeof(CopyLayoutElement))]
	[CanEditMultipleObjects]
	public class CopyLayoutElementEditor : UnityEditor.Editor
	{

		private SerializedProperty _copySourceProperty;

		private SerializedProperty _paddingWidthProperty;
		private SerializedProperty _paddingHeightProperty;

		protected void OnEnable()
		{

			_paddingWidthProperty = serializedObject.FindProperty("PaddingWidth");
			_paddingHeightProperty = serializedObject.FindProperty("PaddingHeight");
			_copySourceProperty = serializedObject.FindProperty("CopySource");

		}

		public override void OnInspectorGUI()
		{

			//base.OnInspectorGUI();

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(_copySourceProperty);
			EditorGUILayout.PropertyField(_paddingWidthProperty);
			EditorGUILayout.PropertyField(_paddingHeightProperty);
			serializedObject.ApplyModifiedProperties();

			serializedObject.Update();

		}

	}*/
}
