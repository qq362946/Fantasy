namespace SRF
{
    using UnityEngine;

    public static class SRFFloatExtensions
    {
        public static float Sqr(this float f)
        {
            return f*f;
        }

        public static float SqrRt(this float f)
        {
            return Mathf.Sqrt(f);
        }

        public static bool ApproxZero(this float f)
        {
            return Mathf.Approximately(0, f);
        }

        public static bool Approx(this float f, float f2)
        {
            return Mathf.Approximately(f, f2);
        }
    }
}
