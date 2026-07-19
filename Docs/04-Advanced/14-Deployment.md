# Kubernetes 部署指南

Fantasy.Net 可以直接部署到 Kubernetes。推荐让一个 Pod 对应一个 Fantasy Process，并使用 StatefulSet 的 Pod 专属 DNS 作为 `innerBindIP`。

## 地址含义

| 配置 | 用途 | Kubernetes 推荐值 |
|---|---|---|
| `outerIP` | 公布给客户端的连接地址，不参与本地监听 | LoadBalancer 对外地址或公网域名 |
| `outerBindIP` | 外网端口的本地监听地址 | 通常使用 `0.0.0.0`；TCP/KCP 也可使用解析到当前 Pod IP 的域名 |
| `innerBindIP` | 内网端口的本地监听地址，同时作为服务器间通信地址注册 | StatefulSet Pod 专属 DNS |

TCP/KCP 在启动监听时支持 IP 或域名。域名会通过系统 DNS 解析，并且必须至少有一个地址属于当前 Pod 的网络接口；IPv4 和 IPv6 同时存在时优先 IPv4。监听地址的 DNS 解析只发生在启动时，Pod IP 变化应通过 Pod 重建重新解析。

`innerBindIP` 不只是监听地址，还会被其他服务器用于连接，因此它必须同时满足：

1. 解析到当前 Pod IP。
2. 能被其他 Fantasy Pod 解析和访问。
3. 不向玩家或公网暴露；使用 Security Group、Firewall 或 NetworkPolicy 控制访问。

## 不要使用普通 Service DNS 绑定

普通 ClusterIP Service 的 DNS 指向虚拟 Service IP，不属于 Pod 本地网卡，不能作为 `innerBindIP` 或 TCP/KCP 的 `outerBindIP`。

共享的 Headless Service 名称也不适合作为 `innerBindIP`，因为它可能解析到多个 Pod，其他服务器无法确定要连接哪一个实例。应使用 StatefulSet 的 Pod 专属 DNS：

```text
<pod-name>.<headless-service>.<namespace>.svc.cluster.local
```

例如：

```xml
<machine id="1"
         outerIP="game.example.com"
         outerBindIP="0.0.0.0"
         innerBindIP="fantasy-gate-0.fantasy-gate-headless.game.svc.cluster.local" />
```

## 最小部署示例

下面示例使用 TCP 内网和 TCP 外网。镜像中应包含与示例端口、Process ID 和 Pod DNS 一致的 `Fantasy.config`。

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: game
---
apiVersion: v1
kind: Service
metadata:
  name: fantasy-gate-headless
  namespace: game
spec:
  clusterIP: None
  publishNotReadyAddresses: true
  selector:
    app: fantasy-gate
  ports:
    - name: inner-tcp
      port: 11001
      targetPort: inner-tcp
      protocol: TCP
---
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: fantasy-gate
  namespace: game
spec:
  serviceName: fantasy-gate-headless
  replicas: 1
  selector:
    matchLabels:
      app: fantasy-gate
  template:
    metadata:
      labels:
        app: fantasy-gate
    spec:
      containers:
        - name: fantasy-gate
          image: registry.example.com/fantasy-server:latest
          command: ["dotnet", "YourServer.dll"]
          args: ["--m", "Release", "--pid", "1"]
          ports:
            - name: inner-tcp
              containerPort: 11001
              protocol: TCP
            - name: outer-tcp
              containerPort: 20000
              protocol: TCP
          readinessProbe:
            tcpSocket:
              port: inner-tcp
            periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: fantasy-gate-public
  namespace: game
spec:
  type: LoadBalancer
  selector:
    app: fantasy-gate
  ports:
    - name: outer-tcp
      port: 20000
      targetPort: outer-tcp
      protocol: TCP
```

`publishNotReadyAddresses: true` 很重要：Fantasy 在启动时需要先解析自己的 Pod DNS 才能完成监听；如果 DNS 只发布 Ready Pod，会形成“未监听所以未就绪、未就绪所以没有 DNS”的启动死锁。

## 协议与 Service

| Fantasy 协议 | Kubernetes Service `protocol` | 说明 |
|---|---|---|
| TCP | `TCP` | 可使用 TCP 探针 |
| KCP | `UDP` | Service 和 LoadBalancer 必须支持 UDP；不要配置 TCP 端口探针 |
| WebSocket | `TCP` | 框架监听全部接口，可由 Service、Ingress 或 Gateway 转发 |
| HTTP | `TCP` | 容器内推荐绑定 `0.0.0.0`，可由 Service、Ingress 或 Gateway 转发 |

内网端口的 Service 协议必须与 `<network inner="TCP|KCP" />` 一致。

## 多副本与服务发现

每个 Pod 都必须有唯一的 Machine/Process 身份和 Pod 专属 `innerBindIP`。不要只把上面 StatefulSet 的 `replicas` 调大并让所有 Pod 复用同一个 `--pid` 和 `Fantasy.config`；这会让服务器注册同一个 Host，内部连接可能进入错误 Pod。

扩容时可以选择：

- 为每个 Fantasy Process 建立对应的 Machine、Process、Pod DNS 和工作负载。
- 使用 Control Center 管理拓扑，再让每个 Pod 以自己的 Process ID 启动。

启用 Control Center 时，它的地址应使用集群内 Service DNS，并通过 NetworkPolicy 只向 Fantasy 服务器开放。当前 Control Center 使用 SQLite 保存拓扑、使用进程内索引保存服务实例，推荐单副本配合 PersistentVolume；不要在没有共享存储和共享注册状态的情况下直接运行多副本。

## 上线检查

1. 在目标 Pod 中解析 `innerBindIP`，结果包含当前 Pod IP。
2. 从另一个 Fantasy Pod 解析并连接该 Pod DNS 和 `innerPort`。
3. 确认日志中的内外网监听地址和端口正确。
4. TCP/KCP、WebSocket、HTTP 的 Service 协议与端口映射正确。
5. NetworkPolicy 只开放必要的内网端口、Control Center 端口和玩家入口。
6. 使用服务发现时，确认注册的 Host 是 Pod 专属 DNS，而不是 ClusterIP 或共享 Headless Service 名称。

## 相关文档

- [Fantasy.config 配置文件详解](../01-Server/01-ServerConfiguration.md)
- [命令行参数配置](../01-Server/03-CommandLineArguments.md)
- [服务发现使用指南](NetworkDevelopment/11-ServiceDiscovery.md)
- [Kubernetes StatefulSet](https://kubernetes.io/docs/concepts/workloads/controllers/statefulset/)
- [Kubernetes Service 与 Headless Service](https://kubernetes.io/docs/concepts/services-networking/service/#headless-services)
- [Kubernetes Service API：publishNotReadyAddresses](https://kubernetes.io/docs/reference/kubernetes-api/core/service-v1/)
