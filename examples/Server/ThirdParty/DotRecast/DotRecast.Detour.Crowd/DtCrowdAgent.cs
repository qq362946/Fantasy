/*
Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
recast4j copyright (c) 2015-2019 Piotr Piastucki piotr@jtilia.org
DotRecast Copyright (c) 2023-2024 Choi Ikpil ikpil@naver.com

This software is provided 'as-is', without any express or implied
warranty.  In no event will the authors be held liable for any damages
arising from the use of this software.
Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:
1. The origin of this software must not be misrepresented; you must not
 claim that you wrote the original software. If you use this software
 in a product, an acknowledgment in the product documentation would be
 appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
 misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
*/

using System;
using DotRecast.Core.Numerics;

namespace DotRecast.Detour.Crowd
{
    /// Represents an agent managed by a #dtCrowd object.
    /// @ingroup crowd
    public class DtCrowdAgent
    {
        public readonly int idx;

        /// The type of mesh polygon the agent is traversing. (See: #CrowdAgentState)
        public DtCrowdAgentState state;

        /// True if the agent has valid path (targetState == DT_CROWDAGENT_TARGET_VALID) and the path does not lead to the requested position, else false.
        public bool partial;

        /// The path corridor the agent is using.
        public readonly DtPathCorridor corridor;

        /// The local boundary data for the agent.
        public readonly DtLocalBoundary boundary;

        /// Time since the agent's path corridor was optimized.
        public float topologyOptTime;

        /// The known neighbors of the agent.
        public readonly DtCrowdNeighbour[] neis = new DtCrowdNeighbour[DtCrowdConst.DT_CROWDAGENT_MAX_NEIGHBOURS];
        
        /// The number of neighbors.
        public int nneis;

        /// The desired speed.
        public float desiredSpeed;

        public RcVec3f npos = new RcVec3f(); // < The current agent position. [(x, y, z)]
        public RcVec3f disp = new RcVec3f(); // < A temporary value used to accumulate agent displacement during iterative collision resolution. [(x, y, z)]
        public RcVec3f dvel = new RcVec3f(); // < The desired velocity of the agent. Based on the current path, calculated from scratch each frame. [(x, y, z)]
        public RcVec3f nvel = new RcVec3f(); // < The desired velocity adjusted by obstacle avoidance, calculated from scratch each frame. [(x, y, z)]
        public RcVec3f vel = new RcVec3f(); // < The actual velocity of the agent. The change from nvel -> vel is constrained by max acceleration. [(x, y, z)]

        /// The agent's configuration parameters.
        public DtCrowdAgentParams option;

        /// The local path corridor corners for the agent.
        public DtStraightPath[] corners = new DtStraightPath[DtCrowdConst.DT_CROWDAGENT_MAX_CORNERS];

        /// The number of corners.
        public int ncorners;

        public DtMoveRequestState targetState; // < State of the movement request.
        public long targetRef; // < Target polyref of the movement request.
        public RcVec3f targetPos = new RcVec3f(); // < Target position of the movement request (or velocity in case of DT_CROWDAGENT_TARGET_VELOCITY).
        public DtPathQueryResult targetPathQueryResult; // < Path finder query
        public bool targetReplan; // < Flag indicating that the current path is being replanned.
        public float targetReplanTime; // <Time since the agent's target was replanned.
        public float targetReplanWaitTime;

        public DtCrowdAgentAnimation animation;

        public DtCrowdAgent(int idx)
        {
            this.idx = idx;
            corridor = new DtPathCorridor();
            boundary = new DtLocalBoundary();
            animation = new DtCrowdAgentAnimation();
        }

        public void Integrate(float dt)
        {
            // Fake dynamic constraint.
            float maxDelta = option.maxAcceleration * dt;
            RcVec3f dv = RcVec3f.Subtract(nvel, vel);
            float ds = dv.Length();
            if (ds > maxDelta)
                dv = dv * (maxDelta / ds);
            vel = RcVec3f.Add(vel, dv);

            // Integrate
            if (vel.Length() > 0.0001f)
                npos = RcVec.Mad(npos, vel, dt);
            else
                vel = RcVec3f.Zero;
        }

        public bool OverOffmeshConnection(float radius)
        {
            if (0 == ncorners)
                return false;

            bool offMeshConnection = ((corners[ncorners - 1].flags
                                       & DtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION) != 0)
                ? true
                : false;
            if (offMeshConnection)
            {
                float distSq = RcVec.Dist2DSqr(npos, corners[ncorners - 1].pos);
                if (distSq < radius * radius)
                    return true;
            }

            return false;
        }

        public float GetDistanceToGoal(float range)
        {
            if (0 == ncorners)
                return range;

            bool endOfPath = ((corners[ncorners - 1].flags & DtStraightPathFlags.DT_STRAIGHTPATH_END) != 0) ? true : false;
            if (endOfPath)
                return Math.Min(RcVec.Dist2D(npos, corners[ncorners - 1].pos), range);

            return range;
        }

        public RcVec3f CalcSmoothSteerDirection()
        {
            RcVec3f dir = new RcVec3f();
            if (0 < ncorners)
            {
                int ip0 = 0;
                int ip1 = Math.Min(1, ncorners - 1);
                var p0 = corners[ip0].pos;
                var p1 = corners[ip1].pos;

                var dir0 = RcVec3f.Subtract(p0, npos);
                var dir1 = RcVec3f.Subtract(p1, npos);
                dir0.Y = 0;
                dir1.Y = 0;

                float len0 = dir0.Length();
                float len1 = dir1.Length();
                if (len1 > 0.001f)
                    dir1 = dir1 * (1.0f / len1);

                dir.X = dir0.X - dir1.X * len0 * 0.5f;
                dir.Y = 0;
                dir.Z = dir0.Z - dir1.Z * len0 * 0.5f;
                dir = RcVec3f.Normalize(dir);
            }

            return dir;
        }

        public RcVec3f CalcStraightSteerDirection()
        {
            RcVec3f dir = new RcVec3f();
            if (0 < ncorners)
            {
                dir = RcVec3f.Subtract(corners[0].pos, npos);
                dir.Y = 0;
                dir = RcVec3f.Normalize(dir);
            }

            return dir;
        }

        public void SetTarget(long refs, RcVec3f pos)
        {
            targetRef = refs;
            targetPos = pos;
            targetPathQueryResult = null;
            if (targetRef != 0)
            {
                targetState = DtMoveRequestState.DT_CROWDAGENT_TARGET_REQUESTING;
            }
            else
            {
                targetState = DtMoveRequestState.DT_CROWDAGENT_TARGET_FAILED;
            }
        }
    }
}