using Fantasy.Core;
using Fantasy.Helper;
using Fantasy.Unity;
// ReSharper disable HeuristicUnreachableCode
#pragma warning disable CS0162

namespace Fantasy.Model
{
    public static class LoadingHelper
    {
        public static async FTask Start()
        {
            // 这里我只是写一个例子、正常的做法应该有个UI界面来显示更新进度
            // 各位可以参数我写的这个例子、做一个UI写一下
            // 因为这就是一个demo我就不写了
#if UNITY_EDITOR
            // 编辑器模式下就不需要热更新了、如果为了测试可以把这段代码注释掉来测试
            return;
#endif
            var timerId = 0L;
            // 创建一个下载器、给后面更新资源包使用
            var download = Download.Create;
            // 在远程服务器上更新AssetBundle
            await foreach (var assetBundleCheckStage in AssetBundleHelper.StartUpdate(download))
            {
                switch (assetBundleCheckStage)
                {
                    // 1、下载资源版本文件MD5、用来查看是否有需要更新的资源
                    // 执行到这里有两种情况:
                    //  1、检测到MD5不一致、这种情况会直接进行到第3步开始下载资源清单进行对比需要更新的资源。
                    //  2、检测到MD5一致、后面的就不需要执行了、因为资源已经是最新的了。
                    // 其实这步是可以省略的、但这样做会减少很多流量、因为一个MD5文字肯定会比一个资源清单要小
                    case AssetBundleCheckStage.CheckVersionMD5:
                    {
                        Log.Info("检查是否有可更新的文件");
                        break;
                    }
                    // 2、下载资源版本文件来对比需要更新的资源列表
                    case AssetBundleCheckStage.DownloadVersion:
                    {
                        Log.Info("分析更新的资源");
                        break;
                    }
                    // 3、下载资源、并替换到本地的旧资源
                    // 这里可以写下载进度、下载速度等操作
                    case AssetBundleCheckStage.DownloadAssetBundle:
                    {
                        Log.Info("请稍等正在更新资源");
                        // 拿到需要更新的字节大小
                        var needUpdateSize = AssetBundleHelper.NeedUpdateSize;
                        // 每33毫秒更新下进度条
                        timerId = TimerScheduler.Instance.Unity.RepeatedTimer(33, () =>
                        {
                            // 通过当前已经下载的字节/总字节,来计算出一个百分比 
                            var fillAmount = download.TotalDownloadedBytes * 1f / needUpdateSize;
                            Log.Info($"fillAmount:{fillAmount}");
                        });
                        
                        break;
                    }
                    // 更新失败、一般是无法连接到资源服务器进行更新导致
                    case AssetBundleCheckStage.UpdateFailed:
                    {
                        TimerScheduler.Instance.Unity.RemoveByRef(ref timerId);
                        Log.Error("无法连接到资源服务器，请联系管理员！");
                        return;
                    }
                    // 更新完成
                    // 有两种可能如下:
                    //  1、没有需要更新的。
                    //  2、有需要更新的、并且更新完成了。
                    case AssetBundleCheckStage.Complete:
                    {
                        TimerScheduler.Instance.Unity.RemoveByRef(ref timerId);
                        Log.Info("fillAmount:1");
                        Log.Info("资源更新完成");
                        await TimerScheduler.Instance.Unity.WaitAsync(1000);
                        UIComponent.Instance.AddComponent<LoginUI>();
                        return;
                    }
                }
            }
        }
    }
}