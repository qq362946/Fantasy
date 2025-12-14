namespace SRF
{
    public static class SRFStringExtensions
    {
#if UNITY_EDITOR
        [JetBrains.Annotations.StringFormatMethod("formatString")]
#endif
        public static string Fmt(this string formatString, params object[] args)
        {
            return string.Format(formatString, args);
        }
    }
}
