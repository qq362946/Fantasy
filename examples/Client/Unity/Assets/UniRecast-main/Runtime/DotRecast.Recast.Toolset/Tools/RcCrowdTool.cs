using System;
using System.Collections.Generic;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Detour.Crowd;
using DotRecast.Recast.Toolset.Builder;

namespace DotRecast.Recast.Toolset.Tools
{
    public class RcCrowdTool : IRcToolable
    {
        private readonly DtCrowdAgentConfig _agCfg;
        private DtCrowd crowd;
        private readonly DtCrowdAgentDebugInfo _agentDebug;
        private long crowdUpdateTime;
        private readonly Dictionary<long, RcCrowdAgentTrail> _trails;
        private long _moveTargetRef;
        private RcVec3f _moveTargetPos;

        public RcCrowdTool()
        {
            _agCfg = new DtCrowdAgentConfig();
            _agentDebug = new DtCrowdAgentDebugInfo();
            _agentDebug.vod = new DtObstacleAvoidanceDebugData(2048);
            _trails = new Dictionary<long, RcCrowdAgentTrail>();
        }


        public string GetName()
        {
            return "Crowd Control";
        }

        public DtCrowdAgentConfig GetCrowdConfig()
        {
            return _agCfg;
        }

        public DtCrowdAgentDebugInfo GetCrowdAgentDebugInfo()
        {
            return _agentDebug;
        }

        public Dictionary<long, RcCrowdAgentTrail> GetCrowdAgentTrails()
        {
            return _trails;
        }

        public long GetMoveTargetRef()
        {
            return _moveTargetRef;
        }

        public RcVec3f GetMoveTargetPos()
        {
            return _moveTargetPos;
        }

        public void Setup(float agentRadius, DtNavMesh navMesh)
        {
            DtCrowdConfig config = new DtCrowdConfig(agentRadius);
            crowd = new DtCrowd(config, navMesh, __ => new DtQueryDefaultFilter(
                SampleAreaModifications.SAMPLE_POLYFLAGS_ALL,
                SampleAreaModifications.SAMPLE_POLYFLAGS_DISABLED,
                new float[] { 1f, 10f, 1f, 1f, 2f, 1.5f })
            );

            // Setup local avoidance option to different qualities.
            // Use mostly default settings, copy from dtCrowd.
            DtObstacleAvoidanceParams option = new DtObstacleAvoidanceParams(crowd.GetObstacleAvoidanceParams(0));

            // Low (11)
            option.velBias = 0.5f;
            option.adaptiveDivs = 5;
            option.adaptiveRings = 2;
            option.adaptiveDepth = 1;
            crowd.SetObstacleAvoidanceParams(0, option);

            // Medium (22)
            option.velBias = 0.5f;
            option.adaptiveDivs = 5;
            option.adaptiveRings = 2;
            option.adaptiveDepth = 2;
            crowd.SetObstacleAvoidanceParams(1, option);

            // Good (45)
            option.velBias = 0.5f;
            option.adaptiveDivs = 7;
            option.adaptiveRings = 2;
            option.adaptiveDepth = 3;
            crowd.SetObstacleAvoidanceParams(2, option);

            // High (66)
            option.velBias = 0.5f;
            option.adaptiveDivs = 7;
            option.adaptiveRings = 3;
            option.adaptiveDepth = 3;

            crowd.SetObstacleAvoidanceParams(3, option);
        }

        public void UpdateAgentParams()
        {
            if (crowd == null)
            {
                return;
            }

            foreach (DtCrowdAgent ag in crowd.GetActiveAgents())
            {
                DtCrowdAgentParams agOption = new DtCrowdAgentParams();
                agOption.radius = ag.option.radius;
                agOption.height = ag.option.height;
                agOption.maxAcceleration = ag.option.maxAcceleration;
                agOption.maxSpeed = ag.option.maxSpeed;
                agOption.collisionQueryRange = ag.option.collisionQueryRange;
                agOption.pathOptimizationRange = ag.option.pathOptimizationRange;
                agOption.obstacleAvoidanceType = ag.option.obstacleAvoidanceType;
                agOption.queryFilterType = ag.option.queryFilterType;
                agOption.userData = ag.option.userData;
                agOption.updateFlags = _agCfg.GetUpdateFlags();
                agOption.obstacleAvoidanceType = _agCfg.obstacleAvoidanceType;
                agOption.separationWeight = _agCfg.separationWeight;
                crowd.UpdateAgentParameters(ag, agOption);
            }
        }

        public DtCrowd GetCrowd()
        {
            return crowd;
        }

        public void Update(float dt)
        {
            if (crowd == null)
                return;

            DtNavMesh nav = crowd.GetNavMesh();
            if (nav == null)
                return;

            long startTime = RcFrequency.Ticks;
            crowd.Update(dt, _agentDebug);
            long endTime = RcFrequency.Ticks;

            // Update agent trails
            foreach (DtCrowdAgent ag in crowd.GetActiveAgents())
            {
                RcCrowdAgentTrail trail = _trails[ag.idx];
                // Update agent movement trail.
                trail.htrail = (trail.htrail + 1) % RcCrowdAgentTrail.AGENT_MAX_TRAIL;
                trail.trail[trail.htrail * 3] = ag.npos.X;
                trail.trail[trail.htrail * 3 + 1] = ag.npos.Y;
                trail.trail[trail.htrail * 3 + 2] = ag.npos.Z;
            }

            _agentDebug.vod.NormalizeSamples();

            // m_crowdSampleCount.addSample((float) crowd.GetVelocitySampleCount());
            crowdUpdateTime = (endTime - startTime) / TimeSpan.TicksPerMillisecond;
        }

        public void RemoveAgent(DtCrowdAgent agent)
        {
            crowd.RemoveAgent(agent);
            if (agent == _agentDebug.agent)
            {
                _agentDebug.agent = null;
            }
        }

        public void AddAgent(RcVec3f p, float agentRadius, float agentHeight, float agentMaxAcceleration, float agentMaxSpeed)
        {
            DtCrowdAgentParams ap = CreateAgentParams(agentRadius, agentHeight, agentMaxAcceleration, agentMaxSpeed);
            DtCrowdAgent ag = crowd.AddAgent(p, ap);
            if (ag != null)
            {
                if (_moveTargetRef != 0)
                    crowd.RequestMoveTarget(ag, _moveTargetRef, _moveTargetPos);

                // Init trail
                if (!_trails.TryGetValue(ag.idx, out var trail))
                {
                    trail = new RcCrowdAgentTrail();
                    _trails.Add(ag.idx, trail);
                }

                for (int i = 0; i < RcCrowdAgentTrail.AGENT_MAX_TRAIL; ++i)
                {
                    trail.trail[i * 3] = p.X;
                    trail.trail[i * 3 + 1] = p.Y;
                    trail.trail[i * 3 + 2] = p.Z;
                }

                trail.htrail = 0;
            }
        }

        private DtCrowdAgentParams CreateAgentParams(float agentRadius, float agentHeight, float agentMaxAcceleration, float agentMaxSpeed)
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

        public DtCrowdAgent HitTestAgents(RcVec3f s, RcVec3f p)
        {
            DtCrowdAgent isel = null;
            float tsel = float.MaxValue;

            foreach (DtCrowdAgent ag in crowd.GetActiveAgents())
            {
                RcVec3f bmin = new RcVec3f();
                RcVec3f bmax = new RcVec3f();
                GetAgentBounds(ag, ref bmin, ref bmax);
                if (RcIntersections.IsectSegAABB(s, p, bmin, bmax, out var tmin, out var tmax))
                {
                    if (tmin > 0 && tmin < tsel)
                    {
                        isel = ag;
                        tsel = tmin;
                    }
                }
            }

            return isel;
        }

        private void GetAgentBounds(DtCrowdAgent ag, ref RcVec3f bmin, ref RcVec3f bmax)
        {
            RcVec3f p = ag.npos;
            float r = ag.option.radius;
            float h = ag.option.height;
            bmin.X = p.X - r;
            bmin.Y = p.Y;
            bmin.Z = p.Z - r;
            bmax.X = p.X + r;
            bmax.Y = p.Y + h;
            bmax.Z = p.Z + r;
        }

        public void SetMoveTarget(RcVec3f p, bool adjust)
        {
            if (crowd == null)
                return;

            // Find nearest point on navmesh and set move request to that location.
            DtNavMeshQuery navquery = crowd.GetNavMeshQuery();
            IDtQueryFilter filter = crowd.GetFilter(0);
            RcVec3f halfExtents = crowd.GetQueryExtents();

            if (adjust)
            {
                // Request velocity
                if (_agentDebug.agent != null)
                {
                    RcVec3f vel = CalcVel(_agentDebug.agent.npos, p, _agentDebug.agent.option.maxSpeed);
                    crowd.RequestMoveVelocity(_agentDebug.agent, vel);
                }
                else
                {
                    foreach (DtCrowdAgent ag in crowd.GetActiveAgents())
                    {
                        RcVec3f vel = CalcVel(ag.npos, p, ag.option.maxSpeed);
                        crowd.RequestMoveVelocity(ag, vel);
                    }
                }
            }
            else
            {
                navquery.FindNearestPoly(p, halfExtents, filter, out _moveTargetRef, out _moveTargetPos, out var _);
                if (_agentDebug.agent != null)
                {
                    crowd.RequestMoveTarget(_agentDebug.agent, _moveTargetRef, _moveTargetPos);
                }
                else
                {
                    foreach (DtCrowdAgent ag in crowd.GetActiveAgents())
                    {
                        crowd.RequestMoveTarget(ag, _moveTargetRef, _moveTargetPos);
                    }
                }
            }
        }

        private RcVec3f CalcVel(RcVec3f pos, RcVec3f tgt, float speed)
        {
            RcVec3f vel = RcVec3f.Subtract(tgt, pos);
            vel.Y = 0.0f;
            vel = RcVec3f.Normalize(vel);
            return vel * speed;
        }

        public long GetCrowdUpdateTime()
        {
            return crowdUpdateTime;
        }

        public void HighlightAgent(DtCrowdAgent agent)
        {
            _agentDebug.agent = agent;
        }
    }
}