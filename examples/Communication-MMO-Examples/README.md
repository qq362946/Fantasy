### 使用指南
1. 安装使用mongodb，详细过程查看 [MongoDB 的安装和使用 S3T](https://www.fantsida.com/d/89/6)
2. 使用S3T工具，创建连接数据库管理帐号，创建`fantasy_main`数据库，使用免密码登录模式即可。
3. 配置文件\Communication-MMO-Examples\Config\Excel\Server\WorldConfig.xlsx的数据库连接
   `mongodb://127.0.0.1`，如果你自己能在数据库创建好帐号密码，那连接就是这样的`mongodb://account:password@127.0.0.1:27017`
4. 在框架的Release页面下载mmo项目资源Communication-MMO-ResLibrary.zip [Releases · qq362946/Fantasy (github.com)](https://github.com/qq362946/Fantasy/releases)
5. 用unity引擎打开前端项目后，导入Communication-MMO-ResLibrary.unitypackage
6. 服务端用vs或vs code、rider运行Server解决方案
   也可以直接在命令行CD到 \Communication-MMO-Examples\Server\Bin\App目录下，执行命令运行
   `dotnet Fantasy.App.dll --AppType Game --Mode Develop`
7. 在Unity引擎中打开Init场景，Play运行测试

----
#### 更新概要
目前更新到角色进入地图场景
1. 退出地图，退出网关逻辑待更新
2. 角色移动控制，移动状态同步待更新
3. 服务端Aoi待更新
4. MapConfig，地图刷怪巡逻待更新
5. 简单的技能系统，攻击技能与buff待更新
6. 聊天频道与chat服务器待更新
7. 地图寻路待更新
8. 简单的任务系统待更新

----
#### unity引擎与.Net SDK要求版本
unity2022+
.Net8.0

