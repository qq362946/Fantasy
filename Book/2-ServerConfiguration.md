# 2-服务器配置 - Server Configuration

Fantasy服务器是分布式的服务器，所以需要四个配置表配合来制定每个进程或服务器。

在Fantasy/Examples/Config/Excel/Server下，分别有ProcessConfig.xlsx、WorldConfig.xlsx、MachineConfig.xlsx、SceneConfig.xlsx四个配置文件。

## ProcessConfig.xlsx:用来配置服务器有多少个进程。
  - Id : 进程的Id，不可以重复。
  - MachineId : 配置所在服务器的ID，配合MachineConfig.xlsx使用。
  - StartupGroup : 默认发布到平台上后只能通过参数启动某一个Process，设置了这个启动组后，可以通过启动参数来一次启动多个Process。
## MachineConfig.xlsx:用来配置服务器的信息，比如服务器的IP地址和内部通讯的地址。
  - Id : 配置的Id，不可以重复。
  - OuterIP : 外网的IP地址，主要用于客户端连接服务器使用。
  - OuterBindIP : 外网绑定IP，服务器的网络需要绑定一个IP地址，这个地址是给服务器SOCKET绑定地址用的。
  - InnerBindIP : 内网绑定IP，服务器之间相互通讯的一个地址。
## WorldConfig.xlsx:用来配置服务器的世界，也可以理解为游戏的区
  - WorldName : 世界的名称，方便查看是什么世界，比如一区、二区。
  - DbConnection : 每个区都有一个数据库，这个用来配置数据库的连接字符串。
  - DbName : 数据库名称。
  - DbType : 数据库类型，目前仅支持MongoDB。
## SceneConfig.xlsx:用来配置Scene的服务器。
  - Id : Scene的Id，这个Id有一些规则需要遵守
    - Id不能小于当前WorldConfigId \* 1000 +1。
    - Id不能大于当前WorldConfigId \* 1000 +255。
    - Id必须在1和2条件之间。
  - ProcessConfigId:进程Id,关联ProcessConfig.xlsx文件，用来表示这个Process有多少个Scene。
  - WorldConfigId:关联WorldConfig.xlsx表，用于表示这个Scene属于哪个世界。这个必须填写一个，不能留空。
  - SceneRuntimeType : 用于配置Scene在框架中运行的方式。
    - MainThread:设置Scene在当前进程的主线程中运行、如果多个Scene在同一个ProcessConfigId下，表示这些Scene都是在同一个主线程中运行。如果想提升效率可以配置多个进程（也就是Process）分别对应多个Scene，这样设置就是多进程单线程的配置方法。
    - MultiThread:设置Scene为一个单独的线程执行、但需要注意的情况下，如果设置了ThreadPool最好其他Scene不要设置为MultiThread了，因为资源抢占的问题，如果这个Scene不是一个长时间运行的可以不用考虑这个。
    - ThreadPool:根据当前服务器的CPU核心数创建线程，Scene会在这些线程中运行，这样的好处的避免了线程过多而出现的资源竞争的问题，这种配置非常适用于单服务器有多个进程的时候，改为一个进程多个Scene。
  - SceneTypeString:用来表示这个Scene是什么类型。这个可以在SceneConfig.xlsx的SceneTypeConfig工作簿进程配置。
  - NetworkProtocol:用于表示当前Scene对客户端的网络协议。
   - KCP : 一个UDP的可靠协议。
   - TCP : 标准的TCP协议。
   - WebSocket : 一般用于WebGL平台使用。
  - OuterPort:用来表示监听的外网连接的端口号，这里不填写或填0.表示不会建立监听，也就是不会接收到外网发送的消息。
  - InnerPort:用来表示监听的内部网络连接的端口号，这里不填写或填0.表示不会建立监听，也就是不会接受内部网络发送的消息。
  - SceneType:根据SceneTypeString列的值使用公式自动生成的，不需要手动修改。如果要修改在SceneTypeString列选择就可以了。
