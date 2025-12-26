using System;
using System.Collections.Generic;
using DotRecast.Core;
using DotRecast.Core.Buffers;
using DotRecast.Core.Collections;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Detour.Crowd;
using DotRecast.Recast.Toolset.Builder;

namespace DotRecast.Recast.Toolset.Tools
{
    public class RcCrowdAgentProfilingTool : IRcToolable
    {
        private RcCrowdAgentProfilingToolConfig _cfg;

        private DtCrowdConfig _crowdCfg;
        private DtCrowd _crowd;
        private readonly DtCrowdAgentConfig _agCfg;

        private DtNavMesh _navMesh;

        private IRcRand _rand;
        private readonly List<DtPolyPoint> _polyPoints;

        private const int SamplingCount = 500;
        private double _samplingUpdateTime;
        private readonly RcCyclicBuffer<long> _updateTimes;
        private double _curUpdateTime;
        private double _avgUpdateTime;
        private double _minUpdateTime;
        private double _maxUpdateTime;

        public RcCrowdAgentProfilingTool()
        {
            _cfg = new RcCrowdAgentProfilingToolConfig();
            _agCfg = new DtCrowdAgentConfig();
            _polyPoints = new List<DtPolyPoint>();
            _updateTimes = new RcCyclicBuffer<long>(SamplingCount);
        }

        public string GetName()
        {
            return "Crowd Agent Profiling";
        }

        public RcCrowdAgentProfilingToolConfig GetToolConfig()
        {
            return _cfg;
        }

        public DtCrowdAgentConfig GetCrowdConfig()
        {
            return _agCfg;
        }

        public DtCrowd GetCrowd()
        {
            return _crowd;
        }

        public void Setup(float maxAgentRadius, DtNavMesh nav)
        {
            _navMesh = nav;
            if (nav != null)
            {
                _crowdCfg = new DtCrowdConfig(maxAgentRadius);
            }
        }

        private DtCrowdAgentParams GetAgentParams(float agentRadius, float agentHeight, float agentMaxAcceleration, float agentMaxSpeed)
        {
            DtCrowdAgentParams ap = new DtCrowdAgentParams();
            ap.radius = agentRadius;
            ap.height = agentHeight;
            ap.maxAcceleration = agentMaxAcceleration;
            ap.maxSpeed = agentMaxSpeed;
            ap.collisionQueryRange = ap.radius * 12.0f;
            ap.pathOptimizationRange = ap.radius * 30.0f;
            ap.updateFlags = _agCfg.GetUpdateFlags();
            ap.obstacleAvoidanceType = _agCfg.obstacleAvoidanceType;
            ap.separationWeight = _agCfg.separationWeight;
            return ap;
        }

        private DtStatus GetMobPosition(DtNavMeshQuery navquery, IDtQueryFilter filter, out RcVec3f randomPt)
        {
            return navquery.FindRandomPoint(filter, _rand, out var randomRef, out randomPt);
        }

        private DtStatus GetVillagerPosition(DtNavMeshQuery navquery, IDtQueryFilter filter, out RcVec3f randomPt)
        {
            randomPt = RcVec3f.Zero;

            if (0 >= _polyPoints.Count)
                return DtStatus.DT_FAILURE;

            int zone = (int)(_rand.Next() * _polyPoints.Count);
            return navquery.FindRandomPointWithinCircle(_polyPoints[zone].refs, _polyPoints[zone].pt, _cfg.zoneRadius, filter, _rand,
                out var randomRef, out randomPt);
        }

        private void CreateZones()
        {
            _polyPoints.Clear();
            IDtQueryFilter filter = new DtQueryDefaultFilter();
            DtNavMeshQuery navquery = new DtNavMeshQuery(_navMesh);
            for (int i = 0; i < _cfg.numberOfZones; i++)
            {
                float zoneSeparation = _cfg.zoneRadius * _cfg.zoneRadius * 16;
                for (int k = 0; k < 100; k++)
                {
                    var status = navquery.FindRandomPoint(filter, _rand, out var randomRef, out var randomPt);
                    if (status.Succeeded())
                    {
                        bool valid = true;
                        foreach (var zone in _polyPoints)
                        {
                            if (RcVec3f.DistanceSquared(zone.pt, randomPt) < zoneSeparation)
                            {
                                valid = false;
                                break;
                            }
                        }

                        if (valid)
                        {
                            _polyPoints.Add(new DtPolyPoint(randomRef, randomPt));
                            break;
                        }
                    }
                }
            }
        }

        private void CreateCrowd()
        {
            _crowd = new DtCrowd(_crowdCfg, _navMesh, __ => new DtQueryDefaultFilter(
                SampleAreaModifications.SAMPLE_POLYFLAGS_ALL,
                SampleAreaModifications.SAMPLE_POLYFLAGS_DISABLED,
                new float[] { 1f, 10f, 1f, 1f, 2f, 1.5f })
            );

            DtObstacleAvoidanceParams option = new DtObstacleAvoidanceParams(_crowd.GetObstacleAvoidanceParams(0));
            // Low (11)
            option.velBias = 0.5f;
            option.adaptiveDivs = 5;
            option.adaptiveRings = 2;
            option.adaptiveDepth = 1;
            _crowd.SetObstacleAvoidanceParams(0, option);
            // Medium (22)
            option.velBias = 0.5f;
            option.adaptiveDivs = 5;
            option.adaptiveRings = 2;
            option.adaptiveDepth = 2;
            _crowd.SetObstacleAvoidanceParams(1, option);
            // Good (45)
            option.velBias = 0.5f;
            option.adaptiveDivs = 7;
            option.adaptiveRings = 2;
            option.adaptiveDepth = 3;
            _crowd.SetObstacleAvoidanceParams(2, option);
            // High (66)
            option.velBias = 0.5f;
            option.adaptiveDivs = 7;
            option.adaptiveRings = 3;
            option.adaptiveDepth = 3;
            _crowd.SetObstacleAvoidanceParams(3, option);
        }

        public void StartProfiling(float agentRadius, float agentHeight, float agentMaxAcceleration, float agentMaxSpeed)
        {
            if (null == _navMesh)
                return;

            // for benchmark
            _updateTimes.Clear();
            _samplingUpdateTime = 0;
            _curUpdateTime = 0;
            _avgUpdateTime = 0;
            _minUpdateTime = 0;
            _maxUpdateTime = 0;

            _rand = new RcRand(_cfg.randomSeed);
            CreateCrowd();
            CreateZones();
            DtNavMeshQuery navquery = new DtNavMeshQuery(_navMesh);
            IDtQueryFilter filter = new DtQueryDefaultFilter();
            for (int i = 0; i < _cfg.agents; i++)
            {
                float tr = _rand.Next();
                RcCrowdAgentType type = RcCrowdAgentType.MOB;
                float mobsPcnt = _cfg.percentMobs / 100f;
                if (tr > mobsPcnt)
                {
                    tr = _rand.Next();
                    float travellerPcnt = _cfg.percentTravellers / 100f;
                    if (tr > travellerPcnt)
                    {
                        type = RcCrowdAgentType.VILLAGER;
                    }
                    else
                    {
                        type = RcCrowdAgentType.TRAVELLER;
                    }
                }

                var status = DtStatus.DT_FAILURE;
                var randomPt = RcVec3f.Zero;
                switch (type)
                {
                    case RcCrowdAgentType.MOB:
                        status = GetMobPosition(navquery, filter, out randomPt);
                        break;
                    case RcCrowdAgentType.VILLAGER:
                        status = GetVillagerPosition(navquery, filter, out randomPt);
                        break;
                    case RcCrowdAgentType.TRAVELLER:
                        status = GetVillagerPosition(navquery, filter, out randomPt);
                        break;
                }

                if (status.Succeeded())
                {
                    AddAgent(randomPt, type, agentRadius, agentHeight, agentMaxAcceleration, agentMaxSpeed);
                }
            }
        }

        public void Update(float dt)
        {
            long startTime = RcFrequency.Ticks;
            if (_crowd != null)
            {
                _crowd.Config().pathQueueSize = _cfg.pathQueueSize;
                _crowd.Config().maxFindPathIterations = _cfg.maxIterations;
                _crowd.Update(dt, null);
            }

            long endTime = RcFrequency.Ticks;
            if (_crowd != null)
            {
                DtNavMeshQuery navquery = new DtNavMeshQuery(_navMesh);
                IDtQueryFilter filter = new DtQueryDefaultFilter();
                foreach (DtCrowdAgent ag in _crowd.GetActiveAgents())
                {
                    if (NeedsNewTarget(ag))
                    {
                        RcCrowdAgentData crowAgentData = (RcCrowdAgentData)ag.option.userData;
                        switch (crowAgentData.type)
                        {
                            case RcCrowdAgentType.MOB:
                                MoveMob(navquery, filter, ag, crowAgentData);
                                break;
                            case RcCrowdAgentType.VILLAGER:
                                MoveVillager(navquery, filter, ag, crowAgentData);
                                break;
                            case RcCrowdAgentType.TRAVELLER:
                                MoveTraveller(navquery, filter, ag, crowAgentData);
                                break;
                        }
                    }
                }
            }

            var currentTime = endTime - startTime;
            _updateTimes.PushBack(currentTime);

            // for benchmark
            _samplingUpdateTime = _updateTimes.Sum() / (double)TimeSpan.TicksPerMillisecond;
            _curUpdateTime = currentTime / (double)TimeSpan.TicksPerMillisecond;
            _avgUpdateTime = (_updateTimes.Average() / (double)TimeSpan.TicksPerMillisecond);
            _minUpdateTime = _updateTimes.Min() / (double)TimeSpan.TicksPerMillisecond;
            _maxUpdateTime = _updateTimes.Max() / (double)TimeSpan.TicksPerMillisecond;
        }

        private void MoveMob(DtNavMeshQuery navquery, IDtQueryFilter filter, DtCrowdAgent ag, RcCrowdAgentData crowAgentData)
        {
            // Move somewhere
            var status = navquery.FindNearestPoly(ag.npos, _crowd.GetQueryExtents(), filter, out var nearestRef, out var nearestPt, out var _);
            if (status.Succeeded())
            {
                status = navquery.FindRandomPointAroundCircle(nearestRef, crowAgentData.home, _cfg.zoneRadius * 2f, filter, _rand,
                    out var randomRef, out var randomPt);
                if (status.Succeeded())
                {
                    _crowd.RequestMoveTarget(ag, randomRef, randomPt);
                }
            }
        }

        private void MoveVillager(DtNavMeshQuery navquery, IDtQueryFilter filter, DtCrowdAgent ag, RcCrowdAgentData crowAgentData)
        {
            // Move somewhere close
            var status = navquery.FindNearestPoly(ag.npos, _crowd.GetQueryExtents(), filter, out var nearestRef, out var nearestPt, out var _);
            if (status.Succeeded())
            {
                status = navquery.FindRandomPointAroundCircle(nearestRef, crowAgentData.home, _cfg.zoneRadius * 0.2f, filter, _rand,
                    out var randomRef, out var randomPt);
                if (status.Succeeded())
                {
                    _crowd.RequestMoveTarget(ag, randomRef, randomPt);
                }
            }
        }

        private void MoveTraveller(DtNavMeshQuery navquery, IDtQueryFilter filter, DtCrowdAgent ag, RcCrowdAgentData crowAgentData)
        {
            // Move to another zone
            List<DtPolyPoint> potentialTargets = new List<DtPolyPoint>();
            foreach (var zone in _polyPoints)
            {
                if (RcVec3f.DistanceSquared(zone.pt, ag.npos) > _cfg.zoneRadius * _cfg.zoneRadius)
                {
                    potentialTargets.Add(zone);
                }
            }

            if (0 < potentialTargets.Count)
            {
                potentialTargets.Shuffle();
                _crowd.RequestMoveTarget(ag, potentialTargets[0].refs, potentialTargets[0].pt);
            }
        }

        private bool NeedsNewTarget(DtCrowdAgent ag)
        {
            if (ag.targetState == DtMoveRequestState.DT_CROWDAGENT_TARGET_NONE
                || ag.targetState == DtMoveRequestState.DT_CROWDAGENT_TARGET_FAILED)
            {
                return true;
            }

            if (ag.targetState == DtMoveRequestState.DT_CROWDAGENT_TARGET_VALID)
            {
                float dx = ag.targetPos.X - ag.npos.X;
                float dy = ag.targetPos.Y - ag.npos.Y;
                float dz = ag.targetPos.Z - ag.npos.Z;
                return dx * dx + dy * dy + dz * dz < 0.3f;
            }

            return false;
        }

        private DtCrowdAgent AddAgent(RcVec3f p, RcCrowdAgentType type, float agentRadius, float agentHeight, float agentMaxAcceleration, float agentMaxSpeed)
        {
            DtCrowdAgentParams ap = GetAgentParams(agentRadius, agentHeight, agentMaxAcceleration, agentMaxSpeed);
            ap.userData = new RcCrowdAgentData(type, p);
            return _crowd.AddAgent(p, ap);
        }

        public void UpdateAgentParams()
        {
            if (_crowd != null)
            {
                foreach (DtCrowdAgent ag in _crowd.GetActiveAgents())
                {
                    DtCrowdAgentParams option = new DtCrowdAgentParams();
                    option.radius = ag.option.radius;
                    option.height = ag.option.height;
                    option.maxAcceleration = ag.option.maxAcceleration;
                    option.maxSpeed = ag.option.maxSpeed;
                    option.collisionQueryRange = ag.option.collisionQueryRange;
                    option.pathOptimizationRange = ag.option.pathOptimizationRange;
                    option.queryFilterType = ag.option.queryFilterType;
                    option.userData = ag.option.userData;
                    option.updateFlags = _agCfg.GetUpdateFlags();
                    option.obstacleAvoidanceType = _agCfg.obstacleAvoidanceType;
                    option.separationWeight = _agCfg.separationWeight;
                    _crowd.UpdateAgentParameters(ag, option);
                }
            }
        }

        public double GetCrowdUpdateSamplingTime()
        {
            return _samplingUpdateTime;
        }

        public double GetCrowdUpdateTime()
        {
            return _curUpdateTime;
        }

        public double GetCrowdUpdateAvgTime()
        {
            return _avgUpdateTime;
        }

        public double GetCrowdUpdateMinTime()
        {
            return _minUpdateTime;
        }

        public double GetCrowdUpdateMaxTime()
        {
            return _maxUpdateTime;
        }
    }
}