
#region 测试用的, 之后会全部删掉的
#if DEBUG

namespace Fantasy.Database
{
    using Fantasy.Async;
    using Fantasy.Entitas;
    using Fantasy.Helper;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using static System.Formats.Asn1.AsnWriter;


    /// 限流锁测试。
    public enum 限流锁测试
    {
        /// 注意到, 之前MongoDb访问层的API大量使用了(随机数 % FTask限额)的运算方式来限流, 缺点是: 可读性差、不可监控、会产生少量随机数重复阻塞
        随机数锁,
        /// 
        信号量锁,
        /// 
        信号量锁住Id
    }

    public sealed partial class MongoDb
    {
        const int 并发数 = MongoDb.FTaskCountLimit * 5;  //同时发起远超任务数量上限,达5倍的请求
        //const int 并发数 = 1024; //同时少量请求, 不超过任务数量上限
        const int 最后几条必Log = 10;

        private Stopwatch? 随机数协程锁总耗时;
        private Stopwatch? 信号量总耗时;

        private CoroutineLock? TestLockForRandomNumber;

        /// <summary>
        /// 在配置了MongoDB的任何Scene当中调用这里, 测试两种限流锁在处理并发请求的效果
        /// </summary>
        public async FTask 开始测试(限流锁测试 lockType) {

            TestLockForRandomNumber = Scene.CoroutineLockComponent.Create(GetType().TypeHandle.Value.ToInt64());
            await FTask.CompletedTask;
            switch (lockType)
            {
                case 限流锁测试.随机数锁:
                    {
                        Log.Debug($"Scene({Scene.SceneConfigId})随机数锁 控流测试开始");
                        随机数协程锁总耗时 = System.Diagnostics.Stopwatch.StartNew();
                        for (int t = 0; t < 并发数; t++)
                        {
                            随机数协程锁流量控制测试(t, 并发数, Random.Shared.NextDouble() < 0.005).Coroutine();
                        }
                        break;
                    }
                case 限流锁测试.信号量锁:
                    {
                        Log.Debug($"Scene({Scene.SceneConfigId})信号量锁 控流测试开始");
                        信号量总耗时 = System.Diagnostics.Stopwatch.StartNew();
                        for (int t = 0; t < 并发数; t++)
                        {
                            信号量流量控制测试(t, 并发数, Random.Shared.NextDouble() < 0.005).Coroutine();
                        }
                        break;
                    }
                case 限流锁测试.信号量锁住Id:
                    {
                        信号量总耗时 = System.Diagnostics.Stopwatch.StartNew();
                        for (int t = 0; t < 并发数; t++)
                        {
                            信号量针对Id测试(t, 并发数, 1, Random.Shared.NextDouble() < 0.005).Coroutine();
                        }
                        break;
                    }
            }
                     
        }

        internal class TestEntity : Entity { }
        private async FTask 测试跑的内容()
        {
            await _dbSession.Exist<TestEntity>();
            await _dbSession.Count<TestEntity>();
            await FTask.Wait(Scene, 200);
        }

        internal async FTask 随机数协程锁流量控制测试(int index, int 总并发, bool 随机打印)
        {
            Stopwatch 单次耗时;
            long random = RandomHelper.RandInt64() % (MongoDb.FTaskCountLimit / 2);
            //这里由于随机生成的int可能为正负, 所以要取绝对值, 否则没法正确限制Task数量
            using (await TestLockForRandomNumber!.Wait(Math.Abs(RandomHelper.RandInt64() % MongoDb.FTaskCountLimit))) 
            {
                 单次耗时 = Stopwatch.StartNew();
                await 测试跑的内容();
            }
            单次耗时.Stop();
            if (index > 总并发 - 最后几条必Log || 随机打印)
                Log.Info($"FTask随机锁限流 操作数据库 {index} 次: {单次耗时.ElapsedMilliseconds} ms; 总耗时: {随机数协程锁总耗时!.ElapsedMilliseconds} ms");
        }

        internal async FTask 信号量流量控制测试(int index, int 总并发, bool 随机打印)
        {
            Stopwatch 单次耗时;
            using (await FlowLock.WaitIfTooMuch())
            {
                单次耗时 = Stopwatch.StartNew();
                await 测试跑的内容();
            }

            单次耗时.Stop();
            if (index > 总并发 - 最后几条必Log || 随机打印)
                Log.Debug($"FTask信号量限流 操作数据库 {index} 次: {单次耗时.ElapsedMilliseconds} ms; 总耗时: {信号量总耗时!.ElapsedMilliseconds} ms");
        }       

        internal async FTask 信号量针对Id测试(int index, int 总并发, int waitForId, bool 随机打印)
        {
            Stopwatch 单次耗时;
            using (await FlowLock.Wait(waitForId))//等待
            {
                单次耗时 = Stopwatch.StartNew();
                await 测试跑的内容();
                Log.Debug($"index{index} waitForId:{waitForId}");
            }

            单次耗时.Stop();
            if (index > 总并发 - 最后几条必Log || 随机打印)
                Log.Debug($"单次耗时：{单次耗时.ElapsedMilliseconds} ms; 总耗时：{信号量总耗时!.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// 检查信号量锁的并发有效性
        /// </summary>
        internal async FTask 开始信号量锁有效性测试()
        {
            const int 并发数 = MongoDb.FTaskCountLimit * 2;
            const int 流量上限 = MongoDb.FTaskCountLimit;

            Log.Debug($"[信号量锁有效性测试] 启动 {并发数} 个任务，流量上限 {流量上限}");

            int 正在执行数 = 0;
            int 最大并发数 = 0;
            bool 违反限流 = false;

            var 测试锁 = FlowLock;

            // 启动所有任务
            var tasks = new List<FTask>();
            for (int i = 0; i < 并发数; i++)
            {
                tasks.Add(TestOne(i));
            }

            // 等待所有任务执行完毕
            await FTask.WaitAll(tasks);

            if (违反限流)
                Log.Error($"❌ [信号量锁有效性测试] 失败！并发超出限流值 {流量上限}！");
            else
                Log.Debug($"✅ [信号量锁有效性测试] 通过！最大并发={最大并发数}");

            async FTask TestOne(int index)
            {
                using (await 测试锁.WaitIfTooMuch($"Test{index}"))
                {
                    int 当前 = Interlocked.Increment(ref 正在执行数);
                    int 当前最大 = Math.Max(最大并发数, 当前);
                    Interlocked.Exchange(ref 最大并发数, 当前最大);

                    if (当前 > 流量上限)
                    {
                        违反限流 = true;
                        Log.Error($"❌ 并发数超过上限！当前={当前}, 限制={流量上限}");
                    }

                    // 模拟执行时间（越短越容易暴露锁问题）
                    await FTask.WaitFrame(Scene);
                    await FTask.WaitFrame(Scene);
                    Interlocked.Decrement(ref 正在执行数);
                }
            }
        }

    }
}
#endif
#endregion