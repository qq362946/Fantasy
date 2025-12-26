using System;
using System.Collections.Generic;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Recast.Geom;

namespace DotRecast.Recast.Toolset.Tools
{
    public class RcConvexVolumeTool : IRcToolable
    {
        private readonly List<RcVec3f> _pts;
        private readonly List<int> _hull;

        public RcConvexVolumeTool()
        {
            _pts = new List<RcVec3f>();
            _hull = new List<int>();
        }

        public string GetName()
        {
            return "Convex Volumes";
        }

        public List<RcVec3f> GetShapePoint()
        {
            return _pts;
        }

        public List<int> GetShapeHull()
        {
            return _hull;
        }

        public void ClearShape()
        {
            _pts.Clear();
            _hull.Clear();
        }

        public bool PlottingShape(RcVec3f p, out List<RcVec3f> pts, out List<int> hull)
        {
            pts = null;
            hull = null;

            // Create
            // If clicked on that last pt, create the shape.
            if (_pts.Count > 0 && RcVec3f.DistanceSquared(p, _pts[_pts.Count - 1]) < 0.2f * 0.2f)
            {
                pts = new List<RcVec3f>(_pts);
                hull = new List<int>(_hull);

                _pts.Clear();
                _hull.Clear();

                return true;
            }

            // Add new point
            _pts.Add(p);

            // Update hull.
            if (_pts.Count > 3)
            {
                _hull.Clear();
                _hull.AddRange(RcConvexUtils.Convexhull(_pts));
            }
            else
            {
                _hull.Clear();
            }

            return false;
        }


        public bool TryRemove(IInputGeomProvider geom, RcVec3f pos, out RcConvexVolume volume)
        {
            // Delete
            int nearestIndex = -1;
            IList<RcConvexVolume> vols = geom.ConvexVolumes();
            for (int i = 0; i < vols.Count; ++i)
            {
                if (RcAreas.PointInPoly(vols[i].verts, pos) && pos.Y >= vols[i].hmin
                                                            && pos.Y <= vols[i].hmax)
                {
                    nearestIndex = i;
                }
            }

            // If end point close enough, delete it.
            if (nearestIndex == -1)
            {
                volume = null;
                return false;
            }

            var removal = geom.ConvexVolumes()[nearestIndex];
            geom.ConvexVolumes().RemoveAt(nearestIndex);
            volume = removal;
            return null != volume;
        }

        public bool TryAdd(IInputGeomProvider geom, RcVec3f p, RcAreaModification areaType, float boxDescent, float boxHeight, float polyOffset, out RcConvexVolume volume)
        {
            // Create

            // If clicked on that last pt, create the shape.
            if (_pts.Count > 0 && RcVec3f.DistanceSquared(p, _pts[^1]) < 0.2f * 0.2f)
            {
                // 
                if (_hull.Count > 2)
                {
                    volume = CreateConvexVolume(_pts, _hull, areaType, boxDescent, boxHeight, polyOffset);
                    geom.AddConvexVolume(volume);
                }

                _pts.Clear();
                _hull.Clear();
            }
            else
            {
                // Add new point
                _pts.Add(p);

                // Update hull.
                if (_pts.Count > 1)
                {
                    _hull.Clear();
                    _hull.AddRange(RcConvexUtils.Convexhull(_pts));
                }
                else
                {
                    _hull.Clear();
                }
            }

            volume = null;
            return false;
        }

        public static RcConvexVolume CreateConvexVolume(List<RcVec3f> pts, List<int> hull, RcAreaModification areaType, float boxDescent, float boxHeight, float polyOffset)
        {
            // Create shape.
            float[] verts = new float[hull.Count * 3];
            for (int i = 0; i < hull.Count; ++i)
            {
                verts[i * 3] = pts[hull[i]].X;
                verts[i * 3 + 1] = pts[hull[i]].Y;
                verts[i * 3 + 2] = pts[hull[i]].Z;
            }

            float minh = float.MaxValue, maxh = 0;
            for (int i = 0; i < hull.Count; ++i)
            {
                minh = Math.Min(minh, verts[i * 3 + 1]);
            }

            minh -= boxDescent;
            maxh = minh + boxHeight;

            if (polyOffset > 0.01f)
            {
                float[] offset = new float[verts.Length * 2];
                int noffset = RcAreas.OffsetPoly(verts, hull.Count, polyOffset, offset, offset.Length);
                if (noffset > 0)
                {
                    verts = RcArrays.CopyOf(offset, 0, noffset * 3);
                }
            }

            return new RcConvexVolume()
            {
                verts = verts,
                hmin = minh,
                hmax = maxh,
                areaMod = areaType,
            };
        }
    }
}