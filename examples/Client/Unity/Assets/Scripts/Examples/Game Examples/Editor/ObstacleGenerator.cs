using System;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;

/// <summary>
/// 障碍物生成器 - 编辑器脚本
/// 在Unity菜单栏 Tools → Generate Obstacles 即可在场景中直接生成障碍物
/// </summary>
public class ObstacleGenerator : EditorWindow
{
    // 障碍物设置
    private int obstacleCount = 10;
    private GameObject obstaclesParent; // 障碍物的父物体

    // 墙壁选择
    private GameObject wallNorth; // 北墙（Z轴正方向）
    private GameObject wallSouth; // 南墙（Z轴负方向）
    private GameObject wallEast;  // 东墙（X轴正方向）
    private GameObject wallWest;  // 西墙（X轴负方向）
    private float wallMargin = 0.5f; // 距离墙壁的安全边距

    // 自动计算的区域
    private Vector2 spawnAreaMin = new Vector2(-5, -5);
    private Vector2 spawnAreaMax = new Vector2(5, 5);

    private float obstacleMinSize = 0.5f;
    private float obstacleMaxSize = 3f;
    private float obstacleMaxLength = 6f; // 最长边的长度（用于生成长条状障碍物）
    private float obstacleHeight = 1f;
    private float longObstacleProbability = 0.3f; // 30%概率生成长条状障碍物
    private Material obstacleMaterial;

    // 障碍物类型
    private ObstacleShape obstacleShape = ObstacleShape.Cube;
    private enum ObstacleShape
    {
        Cube,       // 立方体
        Cylinder,   // 圆柱体
        Sphere,     // 球体
        Mixed       // 混合
    }

    // 打开编辑器窗口
    [MenuItem("Tools/Generate Obstacles")]
    public static void ShowWindow()
    {
        GetWindow<ObstacleGenerator>("障碍物生成器");
    }

    [Obsolete("Obsolete")]
    void OnGUI()
    {
        GUILayout.Label("障碍物生成设置", EditorStyles.boldLabel);

        obstacleCount = EditorGUILayout.IntField("障碍物数量", obstacleCount);

        GUILayout.Space(5);
        obstaclesParent = (GameObject)EditorGUILayout.ObjectField("障碍物父物体", obstaclesParent, typeof(GameObject), true);
        EditorGUILayout.HelpBox("障碍物会作为子物体添加到这个GameObject下。留空则自动创建'Obstacles'", MessageType.Info);

        GUILayout.Space(10);
        GUILayout.Label("生成区域（基于墙壁）", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("拖拽场景中的4面墙到下方，脚本会自动计算中间区域", MessageType.Info);

        // 墙壁选择
        wallNorth = (GameObject)EditorGUILayout.ObjectField("北墙 (上/Z+)", wallNorth, typeof(GameObject), true);
        wallSouth = (GameObject)EditorGUILayout.ObjectField("南墙 (下/Z-)", wallSouth, typeof(GameObject), true);
        wallEast = (GameObject)EditorGUILayout.ObjectField("东墙 (右/X+)", wallEast, typeof(GameObject), true);
        wallWest = (GameObject)EditorGUILayout.ObjectField("西墙 (左/X-)", wallWest, typeof(GameObject), true);

        wallMargin = EditorGUILayout.FloatField("距离墙壁边距", wallMargin);

        // 计算区域按钮
        if (GUILayout.Button("根据墙壁计算生成区域", GUILayout.Height(30)))
        {
            CalculateAreaFromWalls();
        }

        // 显示计算出的区域
        EditorGUILayout.LabelField("当前生成区域：", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        spawnAreaMin = EditorGUILayout.Vector2Field("区域最小值 (X,Z)", spawnAreaMin);
        spawnAreaMax = EditorGUILayout.Vector2Field("区域最大值 (X,Z)", spawnAreaMax);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);
        GUILayout.Label("障碍物尺寸 (会随机生成不同长宽)", EditorStyles.boldLabel);
        obstacleMinSize = EditorGUILayout.FloatField("最小尺寸", obstacleMinSize);
        obstacleMaxSize = EditorGUILayout.FloatField("最大尺寸", obstacleMaxSize);
        obstacleMaxLength = EditorGUILayout.FloatField("最长边长度", obstacleMaxLength);
        longObstacleProbability = EditorGUILayout.Slider("长条障碍物概率", longObstacleProbability, 0f, 1f);
        obstacleHeight = EditorGUILayout.FloatField("高度范围", obstacleHeight);

        EditorGUILayout.HelpBox("会生成部分长条状障碍物（像墙一样），其余为普通大小", MessageType.Info);

        GUILayout.Space(10);
        obstacleShape = (ObstacleShape)EditorGUILayout.EnumPopup("障碍物形状", obstacleShape);
        obstacleMaterial = (Material)EditorGUILayout.ObjectField("材质（可选）", obstacleMaterial, typeof(Material), false);

        GUILayout.Space(20);

        if (GUILayout.Button("生成障碍物", GUILayout.Height(40)))
        {
            GenerateObstacles();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("清除所有障碍物", GUILayout.Height(30)))
        {
            ClearObstacles();
        }
    }

    [Obsolete("Obsolete")]
    void GenerateObstacles()
    {
        // 使用用户指定的父物体，或创建默认的
        GameObject obstaclesContainer = obstaclesParent;
        if (obstaclesContainer == null)
        {
            obstaclesContainer = GameObject.Find("Obstacles");
            if (obstaclesContainer == null)
            {
                obstaclesContainer = new GameObject("Obstacles");
                Undo.RegisterCreatedObjectUndo(obstaclesContainer, "Create Obstacles Container");
                Debug.Log("自动创建了 'Obstacles' 容器");
            }
        }
        else
        {
            Debug.Log($"将障碍物添加到: {obstaclesContainer.name}");
        }

        Debug.Log($"=== 开始生成 {obstacleCount} 个障碍物 ===");

        for (int i = 0; i < obstacleCount; i++)
        {
            // 随机位置
            float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            float z = Random.Range(spawnAreaMin.y, spawnAreaMax.y);

            // 随机大小 - 根据概率决定是否生成长条状障碍物
            float sizeX, sizeZ;
            bool isLongObstacle = Random.value < longObstacleProbability;

            if (isLongObstacle)
            {
                // 生成长条状障碍物
                if (Random.value < 0.5f)
                {
                    // 横向长条
                    sizeX = Random.Range(obstacleMaxSize, obstacleMaxLength);
                    sizeZ = Random.Range(obstacleMinSize, obstacleMaxSize * 0.5f);
                }
                else
                {
                    // 纵向长条
                    sizeX = Random.Range(obstacleMinSize, obstacleMaxSize * 0.5f);
                    sizeZ = Random.Range(obstacleMaxSize, obstacleMaxLength);
                }
            }
            else
            {
                // 普通障碍物
                sizeX = Random.Range(obstacleMinSize, obstacleMaxSize);
                sizeZ = Random.Range(obstacleMinSize, obstacleMaxSize);
            }

            float sizeY = Random.Range(obstacleHeight * 0.5f, obstacleHeight * 1.5f); // 高度也随机

            // 确定形状
            PrimitiveType shape;
            if (obstacleShape == ObstacleShape.Mixed)
            {
                shape = (PrimitiveType)Random.Range(0, 3); // 0=Sphere, 1=Capsule, 2=Cylinder, 3=Cube
            }
            else
            {
                shape = GetPrimitiveType(obstacleShape);
            }

            // 创建障碍物
            GameObject obstacle = GameObject.CreatePrimitive(shape);
            obstacle.name = $"Obstacle_{i + 1}_{shape}";
            obstacle.tag = "NavMesh"; // 设置Tag为NavMesh
            obstacle.transform.SetParent(obstaclesContainer.transform);

            // 地板高度 - 所有障碍物的底部都在这个平面上
            const float groundY = 0f;

            // 根据形状设置大小和位置
            if (shape == PrimitiveType.Cube)
            {
                // 立方体：X、Y、Z都独立随机
                obstacle.transform.localScale = new Vector3(sizeX, sizeY, sizeZ);
                // 底部在地面上，所以Y位置 = 地面高度 + 高度的一半
                obstacle.transform.position = new Vector3(x, groundY + sizeY / 2, z);
            }
            else if (shape == PrimitiveType.Cylinder)
            {
                // 圆柱体：X、Z控制半径，Y控制高度
                obstacle.transform.localScale = new Vector3(sizeX, sizeY / 2, sizeZ);
                // 圆柱体的scale.y是实际高度的一半，所以位置的计算需要考虑这一点
                obstacle.transform.position = new Vector3(x, groundY + sizeY / 2, z);
            }
            else if (shape == PrimitiveType.Sphere)
            {
                // 球体：使用平均大小
                float sphereSize = (sizeX + sizeZ) / 2 * 0.8f;
                obstacle.transform.localScale = new Vector3(sphereSize, sphereSize, sphereSize);
                // 球体的底部是半径处
                obstacle.transform.position = new Vector3(x, groundY + sphereSize / 2, z);
            }

            // 应用材质
            if (obstacleMaterial != null)
            {
                MeshRenderer renderer = obstacle.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = obstacleMaterial;
                }
            }

            // 确保有碰撞器
            Collider collider = obstacle.GetComponent<Collider>();
            if (collider == null)
            {
                obstacle.AddComponent<BoxCollider>();
            }

            // 添加到场景的NavMesh烘焙中（标记为障碍物）
            GameObjectUtility.SetStaticEditorFlags(obstacle, StaticEditorFlags.NavigationStatic);

            // 注册Undo
            Undo.RegisterCreatedObjectUndo(obstacle, "Create Obstacle");
        }

        Debug.Log($"=== 障碍物生成完成！共生成 {obstacleCount} 个 ===");
        Debug.Log("提示：生成障碍物后，需要烘焙NavMesh才能进行寻路测试");
        Debug.Log("烘焙NavMesh: Window → AI → Navigation → Bake");
    }

    /// <summary>
    /// 根据4面墙自动计算生成区域
    /// </summary>
    void CalculateAreaFromWalls()
    {
        if (wallNorth == null || wallSouth == null || wallEast == null || wallWest == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择所有4面墙！", "确定");
            Debug.LogError("请选择所有4面墙：北、南、东、西");
            return;
        }

        // 获取墙壁位置
        Vector3 northPos = wallNorth.transform.position;
        Vector3 southPos = wallSouth.transform.position;
        Vector3 eastPos = wallEast.transform.position;
        Vector3 westPos = wallWest.transform.position;

        Debug.Log($"墙壁位置 - 北:{northPos}, 南:{southPos}, 东:{eastPos}, 西:{westPos}");

        // 计算边界（考虑边距）
        float minX = westPos.x + wallMargin;
        float maxX = eastPos.x - wallMargin;
        float minZ = southPos.z + wallMargin;
        float maxZ = northPos.z - wallMargin;

        // 验证边界是否合理
        if (minX >= maxX || minZ >= maxZ)
        {
            EditorUtility.DisplayDialog("错误",
                $"墙壁位置不正确！\n" +
                $"X范围: {minX:F1} 到 {maxX:F1}\n" +
                $"Z范围: {minZ:F1} 到 {maxZ:F1}\n\n" +
                $"请确保：\n" +
                $"- 东墙在西墙右侧\n" +
                $"- 北墙在南墙上方",
                "确定");
            return;
        }

        // 设置生成区域
        spawnAreaMin = new Vector2(minX, minZ);
        spawnAreaMax = new Vector2(maxX, maxZ);

        Debug.Log($"=== 生成区域计算成功 ===");
        Debug.Log($"X范围: {minX:F2} 到 {maxX:F2} (宽度: {maxX - minX:F2})");
        Debug.Log($"Z范围: {minZ:F2} 到 {maxZ:F2} (深度: {maxZ - minZ:F2})");

        EditorUtility.DisplayDialog("成功",
            $"生成区域计算完成！\n\n" +
            $"X范围: {minX:F1} 到 {maxX:F1}\n" +
            $"Z范围: {minZ:F1} 到 {maxZ:F1}\n\n" +
            $"区域大小: {maxX - minX:F1} x {maxZ - minZ:F1}",
            "确定");
    }

    void ClearObstacles()
    {
        // 优先清除用户指定的父物体
        GameObject targetContainer = obstaclesParent != null ? obstaclesParent : GameObject.Find("Obstacles");

        if (targetContainer != null)
        {
            if (EditorUtility.DisplayDialog("确认清除",
                $"确定要删除 '{targetContainer.name}' 及其所有子物体吗？",
                "确定", "取消"))
            {
                Undo.DestroyObjectImmediate(targetContainer);
                Debug.Log($"已清除: {targetContainer.name}");
                obstaclesParent = null; // 清空引用
            }
        }
        else
        {
            EditorUtility.DisplayDialog("提示", "未找到要清除的障碍物容器", "确定");
        }
    }

    PrimitiveType GetPrimitiveType(ObstacleShape shape)
    {
        switch (shape)
        {
            case ObstacleShape.Cube:
                return PrimitiveType.Cube;
            case ObstacleShape.Cylinder:
                return PrimitiveType.Cylinder;
            case ObstacleShape.Sphere:
                return PrimitiveType.Sphere;
            default:
                return PrimitiveType.Cube;
        }
    }
}

/// <summary>
/// 快捷菜单 - 右键点击Hierarchy即可生成
/// </summary>
public class QuickObstacleMenu
{
    [MenuItem("GameObject/3D Object/Quick Generate Obstacles", false, 0)]
    [Obsolete("Obsolete")]
    static void QuickGenerateObstacles()
    {
        // 快速生成10个障碍物
        GameObject obstaclesContainer = GameObject.Find("Obstacles");
        if (obstaclesContainer == null)
        {
            obstaclesContainer = new GameObject("Obstacles");
            Undo.RegisterCreatedObjectUndo(obstaclesContainer, "Create Obstacles Container");
        }

        for (int i = 0; i < 10; i++)
        {
            float x = Random.Range(-5f, 5f);
            float z = Random.Range(-5f, 5f);
            float size = Random.Range(0.5f, 2f);
            float height = 1f;

            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"Obstacle_{i + 1}";
            obstacle.tag = "NavMesh"; // 设置Tag为NavMesh
            // 地板高度为0，障碍物底部在地面上
            obstacle.transform.position = new Vector3(x, height / 2, z);
            obstacle.transform.localScale = new Vector3(size, height, size);
            obstacle.transform.SetParent(obstaclesContainer.transform);

            GameObjectUtility.SetStaticEditorFlags(obstacle, StaticEditorFlags.NavigationStatic);
            Undo.RegisterCreatedObjectUndo(obstacle, "Create Obstacle");
        }

        Debug.Log("快速生成了10个障碍物！");
    }
}
