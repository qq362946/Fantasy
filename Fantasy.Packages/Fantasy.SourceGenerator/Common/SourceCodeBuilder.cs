using System.Text;

namespace Fantasy.SourceGenerator.Common
{
    /// <summary>
    /// 辅助构建生成代码的工具类
    /// </summary>
    internal sealed class SourceCodeBuilder
    {
        private readonly StringBuilder _builder;
        private int _indentLevel;
        private const string IndentString = "    "; // 4 空格缩进
        
        public int Length => _builder.Length;

        public SourceCodeBuilder(int indentLevel = 0)
        {
            _builder = new StringBuilder();
            _indentLevel = indentLevel;
        }

        /// <summary>
        /// 增加缩进级别
        /// </summary>
        public SourceCodeBuilder Indent(int indentLevel = 1)
        {
            _indentLevel += indentLevel;
            return this;
        }

        /// <summary>
        /// 减少缩进级别
        /// </summary>
        public SourceCodeBuilder Unindent()
        {
            if (_indentLevel > 0)
            {
                _indentLevel--;
            }
            return this;
        }

        public void Append(string code = "")
        {
            _builder.Append(code);
        }

        /// <summary>
        /// 添加一行代码（自动处理缩进）
        /// </summary>
        public SourceCodeBuilder AppendLine(string code = "", bool indent = true)
        {
            if (string.IsNullOrEmpty(code))
            {
                _builder.AppendLine();
            }
            else
            {
                if (indent)
                {
                    for (int i = 0; i < _indentLevel; i++)
                    {
                        _builder.Append(IndentString);
                    }
                }

                _builder.AppendLine(code);
            }

            return this;
        }

        /// <summary>
        /// 添加代码块开始 {
        /// </summary>
        public SourceCodeBuilder OpenBrace()
        {
            AppendLine("{");
            Indent();
            return this;
        }

        /// <summary>
        /// 添加代码块结束 }
        /// </summary>
        public SourceCodeBuilder CloseBrace(bool semicolon = false)
        {
            Unindent();
            AppendLine(semicolon ? "};" : "}");
            return this;
        }

        /// <summary>
        /// 添加 using 语句
        /// </summary>
        public SourceCodeBuilder AddUsing(string @namespace)
        {
            AppendLine($"using {@namespace};");
            return this;
        }

        /// <summary>
        /// 添加多个 using 语句
        /// </summary>
        public SourceCodeBuilder AddUsings(params string[] namespaces)
        {
            foreach (var ns in namespaces)
            {
                AddUsing(ns);
            }
            return this;
        }

        /// <summary>
        /// 开始命名空间
        /// </summary>
        public SourceCodeBuilder BeginNamespace(string @namespace)
        {
            AppendLine($"namespace {@namespace}");
            OpenBrace();
            return this;
        }

        /// <summary>
        /// 结束命名空间
        /// </summary>
        public SourceCodeBuilder EndNamespace()
        {
            CloseBrace();
            return this;
        }

        /// <summary>
        /// 开始类定义
        /// </summary>
        public SourceCodeBuilder BeginClass(string className, string? modifiers = "internal static", string? baseTypes = null)
        {
            var classDeclaration = $"{modifiers} class {className}";
            if (!string.IsNullOrEmpty(baseTypes))
            {
                classDeclaration += $" : {baseTypes}";
            }
            AppendLine(classDeclaration);
            OpenBrace();
            return this;
        }

        /// <summary>
        /// 结束类定义
        /// </summary>
        public SourceCodeBuilder EndClass()
        {
            CloseBrace();
            return this;
        }

        /// <summary>
        /// 开始方法定义
        /// </summary>
        public SourceCodeBuilder BeginMethod(string signature)
        {
            AppendLine(signature);
            OpenBrace();
            return this;
        }

        /// <summary>
        /// 结束方法定义
        /// </summary>
        public SourceCodeBuilder EndMethod()
        {
            CloseBrace();
            return this;
        }

        /// <summary>
        /// 添加注释
        /// </summary>
        public SourceCodeBuilder AddComment(string comment)
        {
            AppendLine($"// {comment}");
            return this;
        }

        /// <summary>
        /// 添加 XML 文档注释
        /// </summary>
        public SourceCodeBuilder AddXmlComment(string summary)
        {
            AppendLine("/// <summary>");
            AppendLine($"/// {summary}");
            AppendLine("/// </summary>");
            return this;
        }

        /// <summary>
        /// 构建最终代码
        /// </summary>
        public override string ToString()
        {
            return _builder.ToString();
        }

        /// <summary>
        /// 清空构建器
        /// </summary>
        public void Clear()
        {
            _builder.Clear();
            _indentLevel = 0;
        }
    }
}
