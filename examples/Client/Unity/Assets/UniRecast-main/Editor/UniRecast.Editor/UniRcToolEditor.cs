using UnityEditor;
using UnityEngine;

namespace UniRecast.Editor
{
    public class UniRcToolEditor : UnityEditor.Editor
    {
        private void ProcessMouse()
        {
            Debug.Log("step 0");
            // 마우스 왼쪽 버튼을 클릭하고 있을 때만 처리
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Debug.Log("step 1");

                // 씬뷰에서의 마우스 위치를 가져옴
                Vector2 mousePosition = Event.current.mousePosition;

                // GUI 좌표를 월드 좌표로 변환
                Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
                RaycastHit hitInfo;

                if (Physics.Raycast(ray, out hitInfo))
                {
                    Vector3 hitPoint = hitInfo.point;
                    Debug.Log("Picked Position: " + hitPoint);

                    // 여기에서 hitPoint를 사용하여 피킹된 위치에 대한 작업을 수행할 수 있습니다.
                }
            }
        }

        private void OnSceneGUI()
        {
            ProcessMouse();
        }

        // tool 
        public override void OnInspectorGUI()
        {
            Layout();
        }


        protected virtual void Layout()
        {
        }
    }
}