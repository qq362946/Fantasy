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

namespace DotRecast.Detour.Crowd
{
    /// Configuration parameters for a crowd agent.
    /// @ingroup crowd
    public class DtCrowdAgentParams
    {
        public float radius; // < Agent radius. [Limit: >= 0]
        public float height; // < Agent height. [Limit: > 0]
        public float maxAcceleration; // < Maximum allowed acceleration. [Limit: >= 0]
        public float maxSpeed; // < Maximum allowed speed. [Limit: >= 0]

        /// Defines how close a collision element must be before it is considered for steering behaviors. [Limits: > 0]
        public float collisionQueryRange;

        public float pathOptimizationRange; // < The path visibility optimization range. [Limit: > 0]

        /// How aggresive the agent manager should be at avoiding collisions with this agent. [Limit: >= 0]
        public float separationWeight;

        /// the agent path.
        /// Flags that impact steering behavior. (See: #UpdateFlags)
        public int updateFlags;

        /// The index of the avoidance configuration to use for the agent.
        /// [Limits: 0 <= value < #DT_CROWD_MAX_OBSTAVOIDANCE_PARAMS]
        public int obstacleAvoidanceType;

        /// The index of the query filter used by this agent.
        public int queryFilterType;

        /// User defined data attached to the agent.
        public object userData;
    }
}