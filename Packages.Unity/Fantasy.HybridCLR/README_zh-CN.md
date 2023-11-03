1、需要手动安装hybridclr、具体可以去hybridclr查看、下面是hybridclr的官网
官网 https://code-philosophy.com/
文档 http://doc.code-philosophy.com/
hybridclr文档 http://hybridclr.doc.code-philosophy.com/
2、插件会在每次编译代码后自动把需要的dll拷贝到一个文件夹里、用来打AB包来进行热更、具体如下
AOTAssembly: Assets/Bundles/AOTAssembly
HotUpdate: Assets/Bundles/HotUpdate
3、Unity上的Fantasy菜单也可以手动来处理
拷贝AOTAssembly的DLL到文件夹:Fantasy->CopyAOTAssembly
拷贝HotUpdate到的DLL文件夹:Fantasy->CopyHotUpdateDll
4、如果不熟悉检查仔细看看hybridclr的文件、看完了就明白AOTAssembly和HotUpdate都是干什么用了。
5、以上完毕后、就可以使用框架的ab资源更新（UpdateAssetBundle）来进行热更代码了。