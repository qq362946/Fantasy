using System.Collections.Generic;
using Fantasy;

namespace Fantasy{
        public static class WordFilterHelper
    {
        private class WordFilterNode
        {
            public bool IsEnd;

            public Dictionary<char, WordFilterNode> ChildrenDic = new Dictionary<char, WordFilterNode>(0);

            public bool ContainsChild(char ch)
            {
                return ChildrenDic.ContainsKey(ch);
            }

            public WordFilterNode AddChild(char ch)
            {
                var childNode = new WordFilterNode();
                ChildrenDic.Add(ch, childNode);
                return childNode;
            }

            public WordFilterNode GetChild(char ch)
            {
                if (ChildrenDic.ContainsKey(ch))
                {
                    return ChildrenDic[ch];
                }

                return null;
            }
        }
        
        private static WordFilterNode _root = new WordFilterNode();

        // private static string special = "[]^-_*×―()~!＠@＃#$…&%￥—+=<>《》！?？:：•`·、。，；,.;/\'\"{}（）‘’“”丶";

        private static HashSet<char> _skipSet = new HashSet<char>();

        /// <summary>
        /// 添加屏蔽词
        /// </summary>
        /// <param name="node"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        private static void AddValue(WordFilterNode node, string value, int index)
        {
            int len = value.Length;
            
            if (index >= len)
            {
                Log.Error($"构建屏蔽字失败 {value} 长度为 {value.Length} 但是构建索引为 {index}");
                return;
            }

            // 获取index对应字符
            char ch = value[index];

            // 获取ch对应节点
            if (!node.ContainsChild(ch))
            {
                node.AddChild(ch);
            }
            
            WordFilterNode childNode = node.GetChild(ch);

            // 标识结尾
            if (index == len - 1)
            {
                childNode.IsEnd = true;
                return;
            }

            index++;
            AddValue(childNode, value, index);
        }

        public static void Init()
        {
            // 从屏蔽词配置表，初始化屏蔽词数据
            // ...
        }
        
        /// <summary>
        /// 存在敏感词
        /// </summary>
        /// <returns></returns>
        public static bool HasSensitive(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            
            for (int i = 0; i < value.Length; i ++)
            {
                int b = Check(_root, value, i);

                if (b >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static string ReplaceSensitive(this string value, char replace = '*')
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            
            char[] chars = null;
            
            for (int i = 0; i < value.Length; i ++)
            {
                int b = Check(_root, value, i);

                if (b >= 0)
                {
                    for (int j = i; j <= b; j++)
                    {
                        if (chars == null)
                        {
                            chars = value.ToCharArray();
                        }

                        chars[j] = replace;

                        i = b + 1;
                    }
                }
            }

            if (chars != null)
            {
                return new string(chars);
            }

            return value;
        }
        
        private static int Check(WordFilterNode node, string value, int startIndex)
        {
            int len = value.Length;
            
            // 避免死循环
            for (int i = 0; i < short.MaxValue; i++)
            {
                char ch = value[startIndex];

                WordFilterNode child = node.GetChild(ch);

                // 没有子节点
                if (child == null)
                {
                    // 跳过列表
                    if (!_skipSet.Contains(ch))
                    {
                        return -1;
                    }
                }
                // 有子节点
                else
                {
                    // 如果是敏感词结束则直接判定为有
                    if (child.IsEnd)
                    {
                        return startIndex;
                    }
                    
                    // 未结束 node设置为子节点
                    node = child;
                }
                
                // 获取下一个字符
                int newIndex = startIndex + 1;

                if (newIndex >= len)
                {
                    return -1;
                }

                startIndex = newIndex;
            }

            // 太长直接标记屏蔽
            return len - 1;
        }
    }
}

