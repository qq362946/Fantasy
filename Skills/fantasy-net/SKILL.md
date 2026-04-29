---
name: fantasy-net
description: This guide applies to development and code review for Fantasy / Fantasy.Net / Fantasy.Unity written in C#. Use it when a task involves Fantasy server code or Unity client code using Fantasy, ECS entities/components/systems, scenes and subscenes, FTask, network handlers/messages/protocols, Address or Roaming routing, cross-server events and subscriptions, Fantasy.config, scene or database access, HTTP controllers/services, session or client connection logic, or distributed runtime architecture. It may also be used for Fantasy-related code review, troubleshooting, compliance checks, risk analysis, and best practices, even when the user does not explicitly mention Fantasy.
---

# Fantasy-net

Fantasy is a high-performance C# distributed game server framework based on ECS architecture, using `FTask` for async operations.

## Core Principles

### Fantasy Technical Specifications

- Use `FTask` for all async operations, not `Task`
- Separate Entity data from logic (Handler/System); multi-assembly projects must separate to support hot reload
- All registration is done at compile-time by source generators; don't manually register, don't modify `.g.cs`
- Entities, components, and Handlers use `sealed class`; all classes except structs must be created via Entity
- Use file-scoped namespaces (`namespace Fantasy;`)
- Use `Log.Debug/Info/Error()` for logging; return error codes via `response.ErrorCode`; business logic should not throw exceptions
- Use Event system for module decoupling: publish events instead of direct calls; prefer Struct events (zero GC), use Entity events for complex logic; use EventSystem for sync, AsyncEventSystem for async
- Strictly follow SOLID principles

### Development Behavioral Guidelines

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

See `references/guidelines-examples.md` for detailed Fantasy scenario examples.

#### 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

**Fantasy Key Points:** Before implementing, clarify: architecture pattern (single-server/distributed), Entity ownership (which Scene), communication method (Roaming/Address/SphereEvent), configuration state (whether Fantasy.config has relevant nodes configured). When uncertain, ask; don't assume.

#### 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

**Fantasy Key Points:** Avoid premature abstraction of Entity/Component structures; don't design factory/strategy patterns for single scenarios. When users only need basic functionality, write Component + necessary AwakeSystem directly; refactor when extension is needed.

#### 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

**Fantasy Key Points:** Never modify `.g.cs` generated files (if you find issues, modify source files and regenerate). Don't manually adjust source generator registration code. Don't "optimize" existing Entity/Component structures unless user explicitly requests refactoring.

#### 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

**Fantasy Key Points:** Define verifiable steps and criteria: protocol export (`dotnet fantasy-export` successfully generates `.g.cs`), compilation passes (`dotnet build` with no errors), Handler registration (check generated registration code), message flow (Log.Debug outputs key nodes, confirm request/response correctness).

## Reference File Navigation

Read the corresponding file based on the requirement; for complex tasks, read multiple files.

| File | When to Use |
|---|---|
| `references/ecs/index.md` | ECS entry: routes to Scene / SubScene / Entity definition / component operations / object pool / lifecycle; shared by server and Unity; **read this first when Entity definition, component management, or ECS mechanism selection is involved** |
| `references/review.md` | Fantasy code review entry: routes checks by ECS / Event / Timer / Protocol / Roaming / SphereEvent / HTTP / Database / Config; **read this first when user wants review, code check, or Fantasy compliance verification** |
| `references/guidelines-examples.md` | Development behavioral guidelines Fantasy scenario examples: Think Before Coding (clarify assumptions), Simplicity First (avoid over-engineering), Surgical Changes (precise modifications), Goal-Driven Execution (verifiable goals) with detailed comparison cases; **read when understanding guideline application in Fantasy, or when code review reveals guideline violations** |
| `references/ecs/scene.md` | **Scene is the container and lifecycle boundary for all Entity/Component**: cascade destruction when Scene disposes, OnCreateScene event, access system components via `self.Scene` (TimerComponent/EventComponent/NetworkMessagingComponent etc.); **read when Scene concept, Scene initialization, OnCreateScene event, or Entity ownership is involved** |
| `references/ecs/ecs-check.md` | ECS review checklist: Entity / Component / System / Scene / object pool / lifecycle common issues; **read when user wants to check ECS code for Fantasy compliance** |
| `references/timer/index.md` | Timer entry: routes to async wait / callback timers / event integration / best practices; **read first when user needs delayed execution, repeated tasks, countdown, Wait, OnceTimer, RepeatedTimer** |
| `references/timer/implement.md` | Timer implementation: `FTask.Wait`, `WaitTill`, `WaitFrame`, `OnceTimer`, `RepeatedTimer`, cancel timers; **read only when directly writing Timer code** |
| `references/timer/event.md` | Timer and Event integration: event-based timers, hot reload differences, when to use events instead of Action; **read only when hot-reload-friendly Timer logic is needed** |
| `references/timer/best-practices.md` | Timer best practices and troubleshooting: performance tips, common errors, Scene destruction, precision, cancel strategies; **read only when optimizing or troubleshooting Timer code** |
| `references/ecs/subscene.md` | **SubScene dynamic child scenes**: lightweight isolated spaces created at runtime from parent Scene, sharing parent Scene core components but with independent entity lists; `Scene.CreateSubScene()` API details (params, callback execution order), sending Address messages to SubScene, using Addressable on SubScene, destroying SubScene; **read when needing instances, match rooms, instanced maps, dynamic battlefields, player private spaces, or on-demand scene creation/destruction** |
| `references/ecs/lifecycle.md` | ECS lifecycle Systems: AwakeSystem, UpdateSystem, DestroySystem, DeserializeSystem, TransferOutSystem/TransferInSystem (cross-server transfer only) and trigger order; **read when responding to Entity lifecycle events** |
| `references/event/index.md` | Event entry: determine whether to use EventAwaiter or Event, then follow Workflow to corresponding doc; **read first when requirement involves "wait for result" or "publish event" mechanism selection** |
| `references/event/event-awaiter.md` | EventAwaiter entry: routes to implementation / modeling / troubleshooting; **read first when user needs to wait for a condition, wait for player action, do request-response async flow, or explicitly mentions EventAwaiter/EventAwaiterComponent** |
| `references/event/struct-event.md` | Struct event full workflow: Step 1 define event → Step 2 create listener → Step 3 publish event; **recommended for most scenarios, read when creating Struct events** |
| `references/event/entity-event.md` | Entity event full workflow: Step 1 create listener → Step 2 publish existing Entity (note isDisposed param); **read only when passing existing Entity** |
| `references/event/check-event.md` | Event system code review: Struct event checklist, Entity event checklist, listener checklist, common error comparisons; **read when checking existing event code** |
| `references/server/setup-server.md` | Create new Fantasy server project, integrate Fantasy into existing project (.NET/NuGet), three-layer structure setup, logging system quick config |
| `references/unity/index.md` | Unity client entry: routes to installation, connection, Session, receiving pushes; **read first when Fantasy Unity client is involved** |
| `references/unity/unity-check.md` | Unity review checklist: version consistency, compile macros, connection methods, Session usage, push Handler common issues; **read when user wants to check Unity client code** |
| `references/unity/setup-unity.md` | Unity client install Fantasy.Unity, configure compile symbols, import protocols; **read only during installation or initial integration** |
| `references/unity/unity-connection.md` | Unity client connect to server: FantasyRuntime component, `scene.Connect`, `Runtime.Connect`, protocol selection; **read only during connection initialization** |
| `references/unity/unity-session.md` | Unity client Session usage: send messages, RPC, connection holding, disconnect; **read only for how to send messages after connection** |
| `references/logging.md` | Logging system details: Fantasy.NLog full config, Fantasy config or add logging, NLog.config explanation, custom ILog implementation (Serilog/file logging etc.) |
| `references/logging-check.md` | Logging review checklist: logging initialization, NLog rules, config copy, mode switching, custom ILog common issues; **read when user wants to check logging integration** |
| `references/protocol/index.md` | Protocol entry: define `.proto`, export C#, install export tool routing; **read first when `.proto`, Outer/Inner, protocol export is involved** |
| `references/protocol/protocol-check.md` | Protocol review checklist: Outer/Inner selection, interface matching, naming, export, Handler alignment; **read when user wants to check protocol or Handler definitions** |
| `references/protocol/define.md` | Protocol definition entry: locate protocol root directory and route to Outer/Inner; **read when creating new protocol files or determining where to place protocols** |
| `references/protocol/define-outer.md` | Outer protocol: client↔server messages, `IMessage` / `IRequest` / `IResponse`; **read only when defining Outer protocols** |
| `references/protocol/define-inner.md` | Inner protocol: server↔server messages, `IAddressMessage` / `IAddressRequest` / `IAddressResponse`; **read only when defining Inner protocols** |
| `references/protocol/define-common.md` | Protocol common features: fields, collections, Map, enums, serialization, code injection; **read only when field or serialization details are needed** |
| `references/protocol/export.md` | Protocol export: check tool, run export, verify results; **read after protocol definition is complete or when user requests re-export** |
| `references/protocol/export-install.md` | Export tool installation and `ExporterSettings.json` configuration; **read when tool is not installed or paths are not configured** |
| `references/server/server-message-handler.md` | Server-side Handler for client messages, **only for messages implementing IMessage/IRequest/IResponse interfaces**; Message\<T\>/MessageRPC\<TReq,TRes\> templates, reply() usage, error code patterns, Session push; see respective files for Addressable/Roaming |
| `references/server/server-message-handler-check.md` | Server message Handler review checklist: base class selection, error codes, reply(), Session lifecycle, duplicate Handler common issues; **read when user wants to check client message Handlers** |
| `references/unity/unity-message-handler.md` | Unity client Handler for server push messages: `Message<Session,T>`, file location conventions, compile verification; **read when user needs to create a Handler in Unity to receive server messages** |
| `references/server/address.md` | Server-to-server messaging based on Entity.Address (RuntimeId): **only for messages implementing IAddressMessage/IAddressRequest/IAddressResponse interfaces; read when defining Address message Handlers** |
| `references/server/address-check.md` | Address review checklist: message patterns, entry address retrieval, Handler types, first communication and cached address common errors; **read when user wants to check Address code** |
| `references/server/sphere-event/index.md` | SphereEvent entry: cross-server domain events, subscribe, publish, unsubscribe, choosing between Event/Roaming; **read first when cross-server event notifications, `SphereEventComponent`, `SphereEventArgs`, `SphereEventSystem` are involved** |
| `references/server/sphere-event/implement.md` | SphereEvent implementation: define event class, implement handler, subscribe to remote events, publish events, unsubscribe; **read only when directly writing SphereEvent code** |
| `references/server/sphere-event/best-practices.md` | SphereEvent best practices and troubleshooting: object pool, hot reload, event size, disconnect cleanup, differences from Event/Roaming; **read only when optimizing or troubleshooting SphereEvent logic** |
| `references/server/roaming/index.md` | Roaming concept entry: core concepts (SessionRoamingComponent/Terminus/RoamingType) and Workflow decision tree; **read this first, then sub-files as needed** |
| `references/server/roaming/roaming-check.md` | Roaming review checklist: protocol, link establishment, Terminus lifecycle, message flow, transfer common issues; **read when user wants to check Roaming code** |
| `references/server/roaming/protocol.md` | Define roaming protocols: RoamingType.Config configuration, IRoamingMessage/IRoamingRequest/IRoamingResponse format; **read only when defining protocols and adding roaming types or servers** |
| `references/server/roaming/setup.md` | Establish roaming routes: Gate-side TryCreateRoaming/Link, parameter passing; **read only when establishing routes** |
| `references/server/roaming/on-create-terminus.md` | OnCreateTerminus event: event parameters, LinkTerminusEntity API, Args memory management rules, independent Handler implementation per server; **read only when handling Terminus creation/reconnection** |
| `references/server/roaming/on-dispose-terminus.md` | OnDisposeTerminus event: trigger timing, DisposeTerminusType distinction, independent Handler implementation per server; **read only when handling Terminus disposal** |
| `references/server/roaming/handler.md` | Roaming Handler entry: routes to message handling / push / cross-server send / transfer; **read first when user wants to write Roaming Handlers or do transfers** |
| `references/server/roaming/messaging.md` | Roaming message handling: client send, Gate proactive send to backend, Roaming Handler, backend push to client, cross-server send; **read only when implementing message flow** |
| `references/server/roaming/transfer.md` | Terminus transfer: `StartTransfer`, `TransferOutSystem`, `TransferInSystem`, lifecycle and considerations; **read only when implementing cross-server transfer** |
| `references/server/roaming/error-codes.md` | Roaming error codes: meanings and troubleshooting methods; **read only when encountering Roaming-related errors** |
| `references/http.md` | HTTP entry: routes to server configuration events and Controller writing; **read first when HTTP server, Controller, OnConfigureHttpServices, OnConfigureHttpApplication are involved** |
| `references/http-check.md` | HTTP review checklist: service configuration phase, middleware order, SceneContextFilter, return patterns, route mapping common issues; **read when user wants to check HTTP code** |
| `references/http-server.md` | HTTP server configuration: `OnConfigureHttpServices`, `OnConfigureHttpApplication`, authentication, authorization, CORS, middleware; **read only when configuring HTTP services and middleware** |
| `references/http-controller.md` | HTTP Controller writing: `SceneContextFilter`, `Scene` injection, Action return values, thread switching, Controller examples; **read only when writing or troubleshooting Controllers** |
| `references/database/index.md` | Database entry: MongoDB configuration, getting database instance, persistence, queries, indexes, concurrent modification routing; **read first when MongoDB, `IDatabase`, `scene.World.Database`, data persistence is involved** |
| `references/database/database-check.md` | Database review checklist: `scene.World` access, `ISupportedSerialize`, `isDeserialize`, coroutine locks, SeparateTable applicability; **read when user wants to check database code** |
| `references/database/mongodb.md` | MongoDB usage: `ISupportedSerialize`, `Save`, `Insert`, `Query`, `Remove`, indexes, `isDeserialize`, concurrent modification; **read only when directly writing database code** |
| `references/database/separate-table.md` | SeparateTable separate storage: aggregate entity split storage, `[SeparateTable]`, `PersistAggregate`, `LoadWithSeparateTables`; **read only when aggregate entity child data is too large and needs table separation optimization** |
| `references/database/best-practices.md` | MongoDB best practices and troubleshooting: config association, query optimization, save strategies, common issues; **read only when optimizing or troubleshooting database logic** |
| `references/config.md` | Fantasy.config entry: routes by "add machine / process / World / Scene / database / port"; **read first when Fantasy.config is involved** |
| `references/config-check.md` | Fantasy.config review checklist: machine/process/world/scene/database reference relationships, ports, World mode ID range common issues; **read when user wants to check configuration correctness** |
| `references/config-scenarios.md` | Fantasy.config common scenarios: new project, add/remove Scene, change database, change port, multi-zone; **read when modifying config for specific scenarios** |
| `references/server/setup-server-check.md` | Server project integration review checklist: three-layer structure, target framework, Fantasy-Net reference, compile macros, AssemblyHelper, Program entry common issues; **read when user wants to check server project Fantasy integration** |
| `templates/Fantasy.config` | Full annotated template: all nodes, attributes, possible values, and examples; **read only when writing actual XML** |
