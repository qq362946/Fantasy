# Fantasy.ConfigTable

Fantasy配置表扩展包，配合Tools里面的配置导出工具使用，注意该包依赖Fantasy包。

- Unity : 需要先安装Fantasy.Unity
- Net :  需要先安装Fantasy.Net
## Unity

在Unity中使用Package Manager，点击左上角➕选择add package from disk，选择package.json安装。

Unity端采用assetbundle方式加载配置文件，需要把配置文件放到一个包中，例如config的ab包。

可以在导出工具中设置导出到前端二进制文件的路径，设置到这个config包中。

用代码使用一个IConfigTableAssetBundle接口

```csharp
public sealed class AsserBundleManager : IConfigTableAssetBundle
    {
        public string Combine(string assetBundleDirectoryPath, string dataConfig)
        {
            // 该方法用于拼装ab包的路径，因为不同的ab包资源管理工具路径都不一样
            // 所以这里提供该方法自定义路径
            return Path.Combine(assetBundleDirectoryPath, $"{dataConfig}.bytes");
        }

        public byte[] LoadConfigTable(string assetBundlePath)
        {
            // 这里是加载包里的二进制文件的逻辑。
            // 正常的情况下需要分编辑器和打包环境下。
            // 这里只做了编辑器下拿取ab包里的某个配置文件的逻辑。
            // 正常情况下要把非编辑器环境的逻辑也要写好。
            if (File.Exists(assetBundlePath))
            {
                return AssetDatabase.LoadAssetAtPath<TextAsset>(assetBundlePath).bytes;
            }
            UnityEngine.Debug.LogError($"assetBundlePath:{assetBundlePath} not exist");
            return null;
        }
    }
```

上述工作完成够，可以在项目入口点执行初始化代码

```csharp
// 第一个参数是指定ab包的路径，但不包含配置名字，因为配置文件会在上面的AsserBundleManager里Combine方法拼接出的路径
// 同样这里因为是编辑环境下，所以我输入的是一个路径。
// 但如果打包后这个路径其实并不能使用，要把这个路径改成ab包发布都得正常路径。
ConfigTableHelper.Initialize("Assets/Bundles/Config/", new AsserBundleManager());
```

## Net

```csharp
// 设置配置表的路径 
// 导出工具会把配置表的二进制数据导出到一个目录中。
// 服务器启动的时候需要传入这个目录的路径。
// 我这里使用了相对路径，大家可以根据自己的环境更改目录
ConfigTableHelper.Initialize("../../../Config/Binary");
```

## 使用

初始化完成后，就可以在项目中使用配置文件了

```csharp
// 例如我配置了一个表名为UnitConfig，那在导出在代码中使用只需要再这个表名后面加上Data
// 然后在使用下面的Instance就可以访问表里的数据了

// 获得表里的所有数据
var instanceList = UnitConfigData.Instance.List;
// 根据表的主键Id来获得数据，这里的1就是表对应的Id
var unitConfig = UnitConfigData.Instance.Get(1);
```