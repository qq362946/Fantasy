# Kubernetes 部署

## 推荐模型

```text
一个 Pod = 一个 Fantasy Process
  -> StatefulSet 提供稳定 Pod 名称
  -> Headless Service 提供 Pod 专属 DNS
  -> innerBindIP 使用 Pod 专属 DNS
  -> outerBindIP 通常使用 0.0.0.0
  -> outerIP 使用玩家实际访问的公网域名
```

## 地址规则

- `outerIP` 是公布地址，不是监听地址，可以使用公网 IP 或域名。
- `outerBindIP` 是外网监听地址；TCP/KCP 可使用 IP，或解析到当前 Pod 网卡地址的域名。
- `innerBindIP` 同时用于内网监听和服务器间连接，必须解析到当前 Pod IP，并能从其他 Fantasy Pod 访问。
- TCP/KCP 绑定域名时由系统 DNS 解析，只接受属于当前 Pod 网络接口的解析结果；IPv4 优先，IPv6 兜底。
- WebSocket 监听全部接口；HTTP 在容器内优先使用 `0.0.0.0`。

不要把普通 ClusterIP Service DNS 配置为绑定地址：它解析到虚拟 Service IP，不属于 Pod 网卡。也不要把共享 Headless Service 名称用作 `innerBindIP`：它可能返回多个 Pod，无法作为特定服务器的注册地址。

正确格式：

```text
<pod-name>.<headless-service>.<namespace>.svc.cluster.local
```

```xml
<machine id="1"
         outerIP="game.example.com"
         outerBindIP="0.0.0.0"
         innerBindIP="fantasy-gate-0.fantasy-gate-headless.game.svc.cluster.local" />
```

## Headless Service 必要配置

```yaml
apiVersion: v1
kind: Service
metadata:
  name: fantasy-gate-headless
spec:
  clusterIP: None
  publishNotReadyAddresses: true
  selector:
    app: fantasy-gate
  ports:
    - name: inner
      port: 11001
      targetPort: inner
      protocol: TCP
```

必须设置 `publishNotReadyAddresses: true`，否则 Pod 可能需要先 Ready 才获得 DNS，而 Fantasy 又需要先解析 DNS 才能完成监听。

## 协议映射

- TCP -> Service `protocol: TCP`。
- KCP -> Service `protocol: UDP`，并确认 LoadBalancer 支持 UDP。
- WebSocket / HTTP -> Service `protocol: TCP`，需要时使用 Ingress 或 Gateway。
- 内网 Service 协议必须与 `<network inner="TCP|KCP" />` 一致。

## 多副本

每个 Pod 必须有独立的 Machine/Process 身份和 Pod 专属 `innerBindIP`。不要让多个副本复用同一个 `--pid`、Machine 和 `Fantasy.config`。扩容时为每个 Process 建立对应拓扑，或使用 Control Center 管理拓扑后按不同 Process ID 启动。

Control Center 当前使用 SQLite 和进程内服务实例索引，Kubernetes 中优先部署单副本并挂载 PersistentVolume；没有共享存储和共享注册状态时不要直接扩成多副本。

## 检查清单

1. Pod 内解析 `innerBindIP`，结果包含当前 Pod IP。
2. 其他 Fantasy Pod 能解析并连接该 DNS 和 `innerPort`。
3. `innerBindIP` 不是 ClusterIP DNS 或共享 Headless Service 名称。
4. Headless Service 设置了 `publishNotReadyAddresses: true`。
5. Service 的 TCP/UDP 与 Fantasy 协议一致。
6. 服务发现注册的 Host 是 Pod 专属 DNS。
7. NetworkPolicy 只放行服务器间端口、Control Center 和玩家入口。

