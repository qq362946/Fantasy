// 文本淡出效果
using UnityEngine;
using UnityEngine.UI;

public class TextMeshFadeAlpha : MonoBehaviour
{
    public Text textMesh;
    public float delay = 0;
    public float duration = 1;
    float perSecond;
    float startTime;

    void Start()
    {
        // 按每秒淡出多少来计算
        perSecond = textMesh.color.a / duration;

        // calculate start time
        startTime = Time.time + delay;
    }

    void Update()
    {
        if (Time.time >= startTime)
        {
            Color color = textMesh.color;
            color.a -= perSecond * Time.deltaTime;
            textMesh.color = color;
        }
    }
}
