global using MicrosoftJsonSerializer = System.Text.Json.JsonSerializer;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Fantasy.Assembly;
using Fantasy.Entitas;
using Newtonsoft.Json;
using static Fantasy.Helper.JsonHelper;
#pragma warning disable CS8603

namespace Fantasy.Helper
{
    /// <summary>
    /// 一个Json包装器, 用于包装额外的可解析标头或标尾信息到框架序列化的Json中
    /// </summary>
    public class JsonWrapper<T>
    {
        /// <summary>
        /// 表示库类型
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName(MetaPropertyStr.L)]
        [Newtonsoft.Json.JsonProperty(MetaPropertyStr.L)]
        public string? L { get; set; }

        /// <summary>
        /// 表示数据存放处
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName(MetaPropertyStr.D)]
        [Newtonsoft.Json.JsonProperty(MetaPropertyStr.D)]
        public T? Data { get; set; }
    }

    /// <summary>
    /// Json序列化器的控制选项。Newtonsoft 和 Microsoft的库通用。
    /// </summary>
    public struct JsonSettings
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public JsonSettings(){ }
        /// <summary>
        /// 选择序列化库。
        /// </summary>
        public Library Library = Library.Microsoft;
        /// <summary>
        /// 采用缩进格式, 默认开启。
        /// </summary>
        public bool IsIndented = true;
        /// <summary>
        /// 必要时把类型信息写到Json当中。默认开启。
        /// <para>
        /// 当派生类实例以基类的形式序列化时会生效, 典型的情况是List序列化时保存了各种不同类型的实体，需要记录真实类型，才能正确反序列化。
        /// </para>
        /// <para>
        ///  ( 注: Newtonsoft库支持写出任意类型; Microsoft库开启这个后,框架仅默认支持派生自<see cref="Entity"/>的情况, 如需拓展自定义派生类型写出, 需自行实现微软的Json库多态配置, 框架不予额外支持。)
        /// </para>
        /// </summary>
        public bool WriteTypeWhenNecessary = true;
        /// <summary>
        /// 禁用循环引用。默认不禁用。
        /// </summary>
        public bool NoCycles = false;
        /// <summary>
        /// 关闭Null值写出, 默认为true。
        /// </summary>
        public bool NoNull = true;

        // ... NOTE: 除了以上, 未来可拓展
    }

    /// <summary>
    /// 提供用不同库安全操作 JSON 数据的辅助方法或拓展方法。
    /// </summary>
    public static partial class JsonHelper
    {
        /// <summary>
        /// Json库类型
        /// </summary>
        public enum Library
        {
            /// <summary>
            /// .NET自带, 微软提供的Json库, 性能通常略占优势
            /// </summary>
            Microsoft,
            /// <summary>
            /// 第三方开发者Newtonsoft提供的Json库  
            /// </summary>
            Newtonsoft
        }

        /// <summary>
        /// 标识符
        /// </summary>
        private class Mark
        {
            /// <summary>
            /// 代表 微软
            /// </summary>
            public const string M = "M";
            /// <summary>
            /// 代表 Newtonsoft
            /// </summary>
            public const string N = "N";
        }

        /// <summary>
        /// Json额外元属性名
        /// </summary>
        public class MetaPropertyStr
        {
            /// <summary>
            /// 序列化库
            /// </summary>
            public const string L = "$L";
            /// <summary>
            /// 数据存放处
            /// </summary>
            public const string D = "$D";
            /// <summary>
            /// 类型标识
            /// </summary>
            public const string T = "$T";
        }

        #region 把JsonSettings分别映射到M和N两家的设置项

        // ** 这里缓存采用 List 而不是 HashSet, 因为元素少的情况下遍历列表取值很快 **//
        private static readonly List<(JsonSettings, JsonSerializerOptions)> _serializerSettingsCache_M = new();
        private static readonly List<(JsonSettings, JsonSerializerSettings)> _serializerSettingsCache_N = new();

        // ** 线程安全缓存 **//
        private static readonly List<(JsonSettings, JsonSerializerOptions)> _lockedCache_M = new();
        private static readonly List<(JsonSettings, JsonSerializerSettings)> _lockedCache_N = new();
        private static readonly object _lock_M = new object();
        private static readonly object _lock_N = new object();

        // Newtonsoft序列化器默认设置
        private readonly static JsonSerializerSettings _defaultSettings = new()
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize
        };

        // Microsoft序列化器默认设置 
        private readonly static JsonSerializerOptions _defaultOptions = new()
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve
        };

        private static readonly IJsonTypeInfoResolver ResolverWithPolymorphism = JsonTypeInfoResolver.Combine(
                            new EntityPolymorphismResolver()
                        ); //开启多态鉴别
        private static readonly IJsonTypeInfoResolver ResolverWithoutPolymorphism = JsonTypeInfoResolver.Combine(
                       new DisablePolymorphismResolver()
                   ); //关闭多态鉴别

        private static JsonSerializerOptions MakeMicrosoftOptions(JsonSettings settings, bool threadSafe = false)
        {

            if (!threadSafe)
            {
                for (int i = _serializerSettingsCache_M.Count - 1; i >= 0; i--)
                {
                    var (item1, item2) = _serializerSettingsCache_M[i];
                    if (!item1.Equals(settings))
                        continue;

                    if (item2 == null)
                        _serializerSettingsCache_M.RemoveAt(i);
                    else return item2;
                }
            }
            else lock (_lock_M)
                {
                    for (int i = _lockedCache_M.Count - 1; i >= 0; i--)
                    {
                        var (item1, item2) = _lockedCache_M[i];
                        if (!item1.Equals(settings))
                            continue;

                        if (item2 == null)
                            _lockedCache_M.RemoveAt(i);
                        else return item2;
                    }
                }

            var opt = new JsonSerializerOptions
            {
                WriteIndented = settings.IsIndented,
                ReferenceHandler = settings.NoCycles ? ReferenceHandler.IgnoreCycles : ReferenceHandler.Preserve,
                TypeInfoResolver = settings.WriteTypeWhenNecessary ? ResolverWithPolymorphism : ResolverWithoutPolymorphism,
                DefaultIgnoreCondition = settings.NoNull ? JsonIgnoreCondition.WhenWritingNull : JsonIgnoreCondition.Never,
            };           

            if (!threadSafe)
            {
                _serializerSettingsCache_M.Add((settings, opt));
            }
            else lock (_lock_M)
            {
                _lockedCache_M.Add((settings, opt));
            }

            return opt;
        }

        private static JsonSerializerSettings MakeNewtonsoftSettings(JsonSettings settings, bool threadSafe = false)
        {
            if (!threadSafe)
            {
                for (int i = _serializerSettingsCache_N.Count - 1; i >= 0; i--)
                {
                    var (item1, item2) = _serializerSettingsCache_N[i];

                    if (!item1.Equals(settings))
                        continue;

                    if (item2 == null)
                        _serializerSettingsCache_N.RemoveAt(i);
                    else return item2; //直接返回已缓存的设置
                }
            }
            else lock (_lock_N)
                {
                    for (int i = _lockedCache_N.Count - 1; i >= 0; i--)
                    {
                        var (item1, item2) = _lockedCache_N[i];

                        if (!item1.Equals(settings))
                            continue;

                        if (item2 == null)
                            _lockedCache_N.RemoveAt(i);
                        else return item2; //直接返回已缓存的设置
                    }
                }

            var setting = new JsonSerializerSettings
            {
                ReferenceLoopHandling = settings.NoCycles ? ReferenceLoopHandling.Ignore: ReferenceLoopHandling.Serialize,
                TypeNameHandling = settings.WriteTypeWhenNecessary ? TypeNameHandling.Auto : TypeNameHandling.None,
                Formatting = settings.IsIndented ? Formatting.Indented : Formatting.None,
                NullValueHandling = settings.NoNull ? NullValueHandling.Ignore : NullValueHandling.Include,
            };

            // 添加到缓存
            if (!threadSafe)
            {
                _serializerSettingsCache_N.Add((settings, setting));
            }
            else lock (_lock_N)
                {
                    _lockedCache_N.Add((settings, setting));
                }

            return setting;
        }

        #endregion
     
        /// <summary>
        /// 将对象序列化为 JSON 字符串。允许传入序列化器的相关设置。
        /// </summary>
        /// <typeparam name="T">要序列化的对象类型。</typeparam>
        /// <param name="t">要序列化的对象。</param>
        /// <param name="settings">序列化器设置</param>
        /// <param name="isCacheThreadSafe">将缓存设置为线程安全, 默认为 false ;如果开启线程安全, 自动加锁会导致性能略微降低. </param>
        public static string ToJson<T>(this T t, JsonSettings? settings = null, bool isCacheThreadSafe = false)
        {
            // 默认直接用Newton的库
            if (settings == null)
                return JsonConvert.SerializeObject(t, _defaultSettings);

            // 创建包装器
            var wrapper = new JsonWrapper<T>
            {
                L = default,
                Data = t
            };

            string json = string.Empty;
            var lib = settings.Value.Library;
            wrapper.L = lib == Library.Microsoft ? Mark.M : Mark.N;

            switch (lib)
            {
                case Library.Microsoft:
                    json = MicrosoftJsonSerializer.Serialize(wrapper, MakeMicrosoftOptions(settings.Value, isCacheThreadSafe));
                    break;

                case Library.Newtonsoft:
                    json = JsonConvert.SerializeObject(wrapper, MakeNewtonsoftSettings(settings.Value, isCacheThreadSafe));
                    break;

                default:
                    throw new Exception("Unexpected: librarySelection is Unknown.");
            }

            return json;
        }

        /// <summary>
        /// 反序列化 JSON 字符串为指定类型的对象。
        /// </summary>
        /// <param name="json">要反序列化的 JSON 字符串。</param>
        /// <param name="type">目标对象的类型。</param>    
        /// <param name="settings">序列化器设置</param>
        /// <param name="isCacheThreadSafe">将缓存设置为线程安全, 默认为 false ;如果开启线程安全, 自动加锁会导致性能略微降低. </param>
        /// <returns>反序列化后的对象。</returns>
        public static object Deserialize(this string json, Type type, JsonSettings? settings = null, bool isCacheThreadSafe = false)
        {
            // 探测是不是 Wrapper 结构
            settings ??= new JsonSettings();
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            JsonElement LibMark_Element = default;
            JsonElement Data_Element = default;

            bool isWrapped = root.ValueKind == JsonValueKind.Object &&
                             root.TryGetProperty(MetaPropertyStr.L, out LibMark_Element) &&
                             root.TryGetProperty(MetaPropertyStr.D, out Data_Element);

            if (isWrapped)
            {
                string? libraryMark = LibMark_Element.GetString();
                Type wrapperType = typeof(JsonWrapper<>).MakeGenericType(type); // 构造闭合泛型 Wrapper<T> 类型

                switch (libraryMark)
                {
                    case Mark.M:  //使用微软库
                        {
                            JsonSerializerOptions options = MakeMicrosoftOptions(settings.Value, isCacheThreadSafe);
                            dynamic? wrapper = MicrosoftJsonSerializer.Deserialize(json, wrapperType, options);
                            return wrapper?.Data;
                        }
                    case Mark.N:  //使用Newtonsoft库
                        {
                            JsonSerializerSettings options = MakeNewtonsoftSettings(settings.Value, isCacheThreadSafe);
                            dynamic? wrapper = JsonConvert.DeserializeObject(json, wrapperType, options);
                            return wrapper?.Data;
                        }
                    default: throw new Exception("Unexpected: Detected unknown Json library mark. Deserialize failed. ");
                }
            }
            else  // --- 处理未包装的JSON  ---
            {
                JsonSerializerSettings options = MakeNewtonsoftSettings(settings.Value, isCacheThreadSafe);

                try
                {
                    return JsonConvert.DeserializeObject(json, type, MakeNewtonsoftSettings(settings.Value, isCacheThreadSafe));
                }
                catch (Exception ex)
                {
                    // 如果 Newtonsoft 失败，尝试改用 Microsoft
                    try
                    {
                        return MicrosoftJsonSerializer.Deserialize(json, type, MakeMicrosoftOptions(settings.Value, isCacheThreadSafe));
                    }
                    catch (Exception)
                    {
                        // 两个都失败
                        throw
                        new Exception($"Deserialize UnWrapped Type Failed for {type.Name}. Msg: \n {ex.Message}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// 反序列化 JSON 字符串为指定类型的对象。
        /// </summary>
        /// <typeparam name="T">目标对象的类型。</typeparam>
        /// <param name="json">要反序列化的 JSON 字符串。</param>
        /// <param name="settings">序列化器设置</param>
        /// <param name="isCacheThreadSafe">将缓存设置为线程安全, 默认为 false ;如果开启线程安全, 自动加锁会导致性能略微降低. </param>
        /// <returns>反序列化后的对象。</returns>
        public static T Deserialize<T>(this string json, JsonSettings? settings = null, bool isCacheThreadSafe = false)
        {
            return (T)Deserialize(json, typeof(T), settings, isCacheThreadSafe);
        }

        /// <summary>
        /// 克隆对象，通过将对象序列化为 JSON，然后再进行反序列化。
        /// </summary>
        /// <typeparam name="T">要克隆的对象类型。</typeparam>
        /// <param name="t">要克隆的对象。</param>
        /// <returns>克隆后的对象。</returns>
        public static T Clone<T>(T t)
        {
            return t.ToJson().Deserialize<T>();
        }
    }

    /// <summary>
    /// 打开实体多态鉴别配置器。这个类是针对采用微软的Json库的情况下序列化和反序列化的类型写入拓展，
    /// 用于将一个"<see langword="$T"/>"字段注入Json, 以记录基类的派生类的真实类型。
    /// </summary>
    internal class EntityPolymorphismResolver : DefaultJsonTypeInfoResolver
    {
        /// <summary>
        /// 覆写
        /// </summary>
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo info = base.GetTypeInfo(type, options);

            // 只针对框架里的基类 Entity 进行修改
            if (info.Type == typeof(Entity))
            {
                info.PolymorphismOptions ??= new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = MetaPropertyStr.T,
                    IgnoreUnrecognizedTypeDiscriminators = false, // 这个设置为false的效果: 不识别的鉴别器Id会抛出异常。
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization //如果遇到未知的子类将会 Fail
                };

                // 配置所有实体派生类
                foreach (var kv in AssemblyManifest.Manifests) {
                   var allEntityTypes = kv.Value.EntityTypeCollectionRegistrar.GetEntityTypes();
                    foreach(var one in allEntityTypes)
                    {
                        info.PolymorphismOptions.DerivedTypes.Add(
                           new JsonDerivedType(one, one.FullName ?? one.Name)
                       );
                    }
                }               
            }

            return info;
        }
    }


    /// <summary>
    /// 关闭多态鉴别
    /// </summary>
    internal class DisablePolymorphismResolver : DefaultJsonTypeInfoResolver
    {
        /// <summary>
        /// 覆写
        /// </summary>
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

            if (jsonTypeInfo != null)
            {
                jsonTypeInfo.PolymorphismOptions = null; // 强制检测为非多态类型。
            }

            return jsonTypeInfo;
        }
    }

    /// <summary>
    /// 实体Json序列化上下文。
    /// <para>
    /// 注: 这个类不用写任何逻辑, 因为STJ的源码生成器会自动生成 ...
    /// </para>
    /// <para>
    /// Note : 目前的所有派生实体是通过<see cref="DefaultJsonTypeInfoResolver.GetTypeInfo"/>方法动态注册的。
    /// 由于STJ的源生成器无法识别自行实现的 IIncrementalGenerator , 所以
    /// 理论上要达到最高性能的Json序列化, 有可能需要模仿STJ的官方源生成模板, 来自行实现一套STJ源生成。
    /// 这个工作量浩大, 在NativeAOT场景下的确具备高收益, 但是目前没有时间来做, 只能以后再考虑了.
    /// 然而, 经过测试, 即便没有STJ源码优化, .NET的Json库也比Newtonsoft的要快。
    /// </para>
    /// </summary>
    [JsonSerializable(typeof(Entity))]
    public partial class EntityJsonContext : JsonSerializerContext
    {
        // 注: 这个类不用写任何逻辑, 因为STJ的源码生成器会自动生成 ...
    }
}