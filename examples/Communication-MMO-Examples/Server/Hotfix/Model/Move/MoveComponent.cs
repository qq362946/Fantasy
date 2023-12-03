using Fantasy;
using UnityEngine;

namespace BestGame;

public class MoveComponent : Entity 
{
    public long StartTime;
    public Vector3 StartPos;    // 移动起始点的位置
    public Vector3 TargetPos;   // 要移动的目标点位置
    public float Speed;         // 移动的速度
    public Quaternion From;     // 当前Unit的朝向
    public Quaternion To;       // 要移动的目标的朝向
    public long NeedTime;       // 移动到目标所需要的时间
    public long MoveEndTime;    // 移动到目标的绝对时间
    public long MoveTimerId;    // 移动时间任务的ID
    public FTask<bool> Task;    // 移动的时候执行的协程

    public MoveInfo startInfo;
    public MoveInfo stopInfo;
    
}

public static class MoveComponentSystem
{
    public static async FTask<bool> MoveTo(this MoveComponent self, float speed,Vector3 target, NoticeClientType notice, FCancellationToken cancellationToken = null)
    {
        // 开始移动之前要先停止下当前移动
        self.StopMove(NoticeClientType.NoNotice);
        // 定义和设置变量
        self.Speed = speed;
        var unit = (Unit)self.Parent;
        var runtimeId = self.RuntimeId;
        // 计算移动到目标需要多少时间
        var faceVector = self.TargetPos - unit.Position;
        var distance = faceVector.magnitude;
        self.MoveEndTime = TimeHelper.Now + (long)(distance / speed * 1000);

        self.startInfo = new MoveInfo()
        {
            RoleId = unit.RoleInfo.RoleId,
            Position = unit.Position.ToPosition(),
            Rotation = unit.Rotation.ToRotation(),
            MoveEndTime = self.MoveEndTime
        };
        // 发送开始移动的事件
        EventSystem.Instance.Publish(new StartMove()
        {
            Unit = unit,
            MoveInfo = self.startInfo,
            Notice = notice,
            MoveEndTime = self.MoveEndTime
        });
        // 执行移动逻辑
        self.StartMove(unit, target);
        // 创建异步任务
        void Cancel()
        {
            if (runtimeId != self.RuntimeId)
            {
                return;
            }
                
            self.StopMove(NoticeClientType.Aoi);
        }
        
        self.Task = FTask<bool>.Create();
        bool b;
        
        try
        {
            cancellationToken?.Add(Cancel);
            b = await self.Task;
        }
        finally
        {
            cancellationToken?.Remove(Cancel);
        }
        
        return b;
    }

    private static void StartMove(this MoveComponent self, Unit unit, Vector3 target)
    {
        // 记录下Unit移动的开始时间和目标位置
        self.StartTime = TimeHelper.Now;
        self.TargetPos = target;
        // 计算移动所需要的信息
        self.Calculate(unit);
        // 开始移动到目标、这里用的是服务器每帧移动一次
        self.MoveTimerId = TimerScheduler.Instance.Core.NewFrameTimer(() =>
        {
            self.MoveForward(false);
        });
    }

    public static void StopMove(this MoveComponent self, NoticeClientType noticeClientType)
    {
        // 获得需要的变量
        var unit = (Unit)self.Parent;
        
        // 玩家停止移动的时候、因为网络延迟的问题、服务器收到的时候可能玩家又往前移动了一点、所以需要服务器也要移动一下。
        
        if (self.StartTime > 0)
        {
            self.MoveForward(true);
        }
        
        // 如果正在移动中、停止正在执行的移动
        self.Clear();
        // 发布停止移动的事件
        if (noticeClientType != NoticeClientType.NoNotice)
        {
            self.stopInfo = new MoveInfo()
            {
                RoleId = unit.RoleInfo.RoleId,
                Position = unit.Position.ToPosition(),
                Rotation = unit.Rotation.ToRotation(),
                MoveEndTime = self.MoveEndTime
            };
            EventSystem.Instance.Publish(new StopMove()
            {
                Unit = unit, 
                MoveInfo = self.stopInfo,
                NoticeClientType = noticeClientType
            });
        }
    }

    private static void MoveForward(this MoveComponent self, bool isCancel)
    {
        var unit = (Unit)self.Parent;
        var now = TimeHelper.Now;
        var moveTime = now - self.StartTime;

        while (true)
        {
            if (moveTime <= 0)
            {
                // 有一种可能是玩家没有移动、连续发送了多次停止移动的操作、所以这里要过滤一下。
                return;
            }

            if (moveTime >= self.NeedTime)
            {
                // 如果当前经过的时间已经大于移动需要的时间、不需要用差值了直接更改位置就可以了。
                // 因为客户端其实已经移动到目标了、如果在用插值会造成位置不一致的问题。
                unit.Rotation = self.To;
                unit.Position = self.TargetPos;
            }
            else
            {
                // 计算当前帧的插值
                var amount = moveTime * 1f / self.NeedTime;
                // 计算当前帧的朝向
                unit.Rotation = Quaternion.Slerp(self.From, self.To, amount);
                // 计算当前帧的移动的位置
                if (amount > 0)
                {
                    unit.Position = Vector3.Lerp(self.StartPos, self.TargetPos, amount);
                }
            }

            moveTime -= self.NeedTime;

            if (moveTime < 0)
            {
                // 如果当前帧的时间不足以完成移动到目标点、返回掉、等待下一帧再进行移动
                return;
            }

            // 如果移动移动到目标点、停止移动
            
            unit.Rotation = self.To;
            unit.Position = self.TargetPos;
            
            var task = self.Task;
            self.Task = null;
            
            self.Clear();
            self.StopMove(NoticeClientType.Aoi);
            task.SetResult(!isCancel);
            return;
        }
    }

    private static void Calculate(this MoveComponent self, Unit unit)
    {
        // 设置Unit的位置为起始点位置
        self.StartPos = unit.Position;
        // 计算按照当前速度移动到目标需要的时间。
        var faceVector = self.TargetPos - unit.Position;
        var distance = faceVector.magnitude;
        self.NeedTime = (long)(distance / self.Speed * 1000);
        // 如果转向角度非常非常小、可以理解为不需要转向。
        if (faceVector.sqrMagnitude < 0.0001f)
        {
            return;
        }
        // 拿到unit现在的朝向。
        self.From = unit.Rotation;
        // 计算出目标的朝向。
        if (Math.Abs(faceVector.x) > 0.01 || Math.Abs(faceVector.z) > 0.01)
        {
            self.To = Quaternion.LookRotation(faceVector, Vector3.up);
        }
    }

    public static void Clear(this MoveComponent self)
    {
        self.StartTime = 0;
        self.StartPos = Vector3.zero;
        self.NeedTime = 0;
        self.Speed = 0;

        if (self.MoveTimerId != 0)
        {
            TimerScheduler.Instance.Core.RemoveByRef(ref self.MoveTimerId);
        }

        if (self.Task == null)
        {
            return;
        }

        var task = self.Task;
        self.Task = null;
        task.SetResult(false);
    }
}

public class MoveComponentDestroySystem : DestroySystem<MoveComponent>
{
    protected override void Destroy(MoveComponent self)
    {
        self.StartTime = 0;
        self.StartPos = Vector3.zero;
        self.TargetPos = Vector3.zero;
        self.Speed = 0;
        self.From = default;
        self.To = default;
        self.NeedTime = 0;
        self.MoveEndTime = 0;

        if (self.MoveTimerId != 0)
        {
            TimerScheduler.Instance.Core.RemoveByRef(ref self.MoveTimerId);
        }

        if (self.Task != null)
        {
            var selfTask = self.Task;
            selfTask.SetResult(false);
            self.Task = null;
        }
    }
}