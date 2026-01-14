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
using DotRecast.Core;

namespace DotRecast.Recast
{
    
    using static RcRecast;

    public static class RcFilters
    {
        /// Marks non-walkable spans as walkable if their maximum is within @p walkableClimb of the span below them.
        ///
        /// This removes small obstacles that the agent would be able to walk over such as curbs, and also allows agents to move up structures such as stairs.
        /// This removes small obstacles and rasterization artifacts that the agent would be able to walk over
        /// such as curbs.  It also allows agents to move up terraced structures like stairs.
        /// 
        /// Obstacle spans are marked walkable if: <tt>obstacleSpan.smax - walkableSpan.smax < walkableClimb</tt>
        /// 
        /// @warning Will override the effect of #rcFilterLedgeSpans.  If both filters are used, call #rcFilterLedgeSpans only after applying this filter.
        ///
        /// @see rcHeightfield, rcConfig
        /// 
        /// @ingroup recast
        /// @param[in,out]	context			The build context to use during the operation.
        /// @param[in]		walkableClimb	Maximum ledge height that is considered to still be traversable. 
        /// 								[Limit: >=0] [Units: vx]
        /// @param[in,out]	heightfield		A fully built heightfield.  (All spans have been added.)
        public static void FilterLowHangingWalkableObstacles(RcContext context, int walkableClimb, RcHeightfield heightfield)
        {
            using var timer = context.ScopedTimer(RcTimerLabel.RC_TIMER_FILTER_LOW_OBSTACLES);

            int xSize = heightfield.width;
            int zSize = heightfield.height;

            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    RcSpan previousSpan = null;
                    bool previousWasWalkable = false;
                    int previousAreaID = RC_NULL_AREA;

                    // For each span in the column...
                    for (RcSpan span = heightfield.spans[x + z * xSize]; span != null; previousSpan = span, span = span.next)
                    {
                        bool walkable = span.area != RC_NULL_AREA;
                        // If current span is not walkable, but there is walkable span just below it and the height difference
                        // is small enough for the agent to walk over, mark the current span as walkable too.
                        if (!walkable && previousWasWalkable && span.smax - previousSpan.smax <= walkableClimb)
                        {
                            span.area = previousAreaID;
                        }

                        // Copy the original walkable value regardless of whether we changed it.
                        // This prevents multiple consecutive non-walkable spans from being erroneously marked as walkable.
                        previousWasWalkable = walkable;
                        previousAreaID = span.area;
                    }
                }
            }
        }

        /// Marks spans that are ledges as not-walkable.
        ///
        /// A ledge is a span with one or more neighbors whose maximum is further away than @p walkableClimb
        /// from the current span's maximum.
        /// This method removes the impact of the overestimation of conservative voxelization 
        /// so the resulting mesh will not have regions hanging in the air over ledges.
        /// 
        /// A span is a ledge if: <tt>rcAbs(currentSpan.smax - neighborSpan.smax) > walkableClimb</tt>
        /// 
        /// @see rcHeightfield, rcConfig
        /// 
        /// @ingroup recast
        /// @param[in,out]	context				The build context to use during the operation.
        /// @param[in]		walkableHeight	Minimum floor to 'ceiling' height that will still allow the floor area to 
        /// 								be considered walkable. [Limit: >= 3] [Units: vx]
        /// @param[in]		walkableClimb	Maximum ledge height that is considered to still be traversable. 
        /// 								[Limit: >=0] [Units: vx]
        /// @param[in,out]	heightfield			A fully built heightfield.  (All spans have been added.)
        public static void FilterLedgeSpans(RcContext context, int walkableHeight, int walkableClimb, RcHeightfield heightfield)
        {
            using var timer = context.ScopedTimer(RcTimerLabel.RC_TIMER_FILTER_BORDER);

            int xSize = heightfield.width;
            int zSize = heightfield.height;

            // Mark spans that are adjacent to a ledge as unwalkable..
            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    for (RcSpan span = heightfield.spans[x + z * xSize]; span != null; span = span.next)
                    {
                        // Skip non-walkable spans.
                        if (span.area == RC_NULL_AREA)
                        {
                            continue;
                        }

                        int floor = (span.smax);
                        int ceiling = span.next != null ? span.next.smin : RC_SPAN_MAX_HEIGHT;

                        // The difference between this walkable area and the lowest neighbor walkable area.
                        // This is the difference between the current span and all neighbor spans that have
                        // enough space for an agent to move between, but not accounting at all for surface slope.
                        int lowestNeighborFloorDifference = RC_SPAN_MAX_HEIGHT;

                        // Min and max height of accessible neighbours.
                        int lowestTraversableNeighborFloor = span.smax;
                        int highestTraversableNeighborFloor = span.smax;

                        for (int direction = 0; direction < 4; ++direction)
                        {
                            int neighborX = x + GetDirOffsetX(direction);
                            int neighborZ = z + GetDirOffsetY(direction);

                            // Skip neighbours which are out of bounds.
                            if (neighborX < 0 || neighborZ < 0 || neighborX >= xSize || neighborZ >= zSize)
                            {
                                lowestNeighborFloorDifference = (-walkableClimb - 1);
                                break;
                            }

                            RcSpan neighborSpan = heightfield.spans[neighborX + neighborZ * xSize];

                            // The most we can step down to the neighbor is the walkableClimb distance.
                            // Start with the area under the neighbor span                            
                            int neighborCeiling = neighborSpan != null ? neighborSpan.smin : RC_SPAN_MAX_HEIGHT;

                            // Skip neightbour if the gap between the spans is too small.
                            if (Math.Min(ceiling, neighborCeiling) - floor >= walkableHeight)
                            {
                                lowestNeighborFloorDifference = (-walkableClimb - 1);
                                break;
                            }

                            // For each span in the neighboring column...
                            for (; neighborSpan != null; neighborSpan = neighborSpan.next)
                            {
                                int neighborFloor = neighborSpan.smax;
                                neighborCeiling = neighborSpan.next != null ? neighborSpan.next.smin : RC_SPAN_MAX_HEIGHT;

                                // Only consider neighboring areas that have enough overlap to be potentially traversable.
                                if (Math.Min(ceiling, neighborCeiling) - Math.Max(floor, neighborFloor) < walkableHeight)
                                {
                                    // No space to traverse between them.
                                    continue;
                                }

                                int neighborFloorDifference = neighborFloor - floor;
                                lowestNeighborFloorDifference = Math.Min(lowestNeighborFloorDifference, neighborFloorDifference);

                                // Find min/max accessible neighbor height.
                                // Only consider neighbors that are at most walkableClimb away.
                                if (MathF.Abs(neighborFloorDifference) <= walkableClimb)
                                {
                                    // There is space to move to the neighbor cell and the slope isn't too much.
                                    lowestTraversableNeighborFloor = Math.Min(lowestTraversableNeighborFloor, neighborFloor);
                                    highestTraversableNeighborFloor = Math.Max(highestTraversableNeighborFloor, neighborFloor);
                                }
                                else if (neighborFloorDifference < -walkableClimb)
                                {
                                    // We already know this will be considered a ledge span so we can early-out
                                    break;
                                }
                            }
                        }

                        // The current span is close to a ledge if the magnitude of the drop to any neighbour span is greater than the walkableClimb distance.
                        // That is, there is a gap that is large enough to let an agent move between them, but the drop (surface slope) is too large to allow it.
                        // (If this is the case, then biggestNeighborStepDown will be negative, so compare against the negative walkableClimb as a means of checking
                        // the magnitude of the delta)
                        if (lowestNeighborFloorDifference < -walkableClimb)
                        {
                            span.area = RC_NULL_AREA;
                        }
                        // If the difference between all neighbor floors is too large, this is a steep slope, so mark the span as an unwalkable ledge.
                        else if ((highestTraversableNeighborFloor - lowestTraversableNeighborFloor) > walkableClimb)
                        {
                            span.area = RC_NULL_AREA;
                        }
                    }
                }
            }
        }

        /// Marks walkable spans as not walkable if the clearance above the span is less than the specified walkableHeight.
        /// 
        /// For this filter, the clearance above the span is the distance from the span's 
        /// maximum to the minimum of the next higher span in the same column.
        /// If there is no higher span in the column, the clearance is computed as the
        /// distance from the top of the span to the maximum heightfield height.
        /// 
        /// @see rcHeightfield, rcConfig
        /// @ingroup recast
        /// 
        /// @param[in,out]	context			The build context to use during the operation.
        /// @param[in]		walkableHeight	Minimum floor to 'ceiling' height that will still allow the floor area to 
        /// 								be considered walkable. [Limit: >= 3] [Units: vx]
        /// @param[in,out]	heightfield		A fully built heightfield.  (All spans have been added.)
        public static void FilterWalkableLowHeightSpans(RcContext context, int walkableHeight, RcHeightfield heightfield)
        {
            using var timer = context.ScopedTimer(RcTimerLabel.RC_TIMER_FILTER_WALKABLE);

            int xSize = heightfield.width;
            int zSize = heightfield.height;

            // Remove walkable flag from spans which do not have enough
            // space above them for the agent to stand there.
            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    for (RcSpan span = heightfield.spans[x + z * xSize]; span != null; span = span.next)
                    {
                        int floor = (span.smax);
                        int ceiling = span.next != null ? span.next.smin : RC_SPAN_MAX_HEIGHT;
                        if ((ceiling - floor) < walkableHeight)
                        {
                            span.area = RC_NULL_AREA;
                        }
                    }
                }
            }
        }
    }
}