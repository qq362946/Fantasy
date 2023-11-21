using System;
using System.Reflection;
using UnityEngine;

namespace Fantasy
{
    public class Entry : MonoBehaviour
    {
        /// <summary>
        /// 初始化框架
        /// </summary>
        public static Scene Initialize()
        {
            // 初始化框架
            Application.Initialize();
            // 创建一个GameObject到Unity中
            new GameObject("Fantasy.Unity").AddComponent<Entry>();
            // 框架需要一个Scene来驱动、所以要创建一个Scene、后面所有的框架都会在这个Scene下
            // 也就是把这个Scene给卸载掉、框架的东西都会清除掉
            return Scene.Create();
        }

        public void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        private void Update()
        {
            ThreadSynchronizationContext.Main.Update();
            SingletonSystem.Update(); 
        }
        
        private void OnApplicationQuit()
        {
            EventSystem.Instance?.Publish(new OnAppClosed());
            Application.Close();
        }
    }
}