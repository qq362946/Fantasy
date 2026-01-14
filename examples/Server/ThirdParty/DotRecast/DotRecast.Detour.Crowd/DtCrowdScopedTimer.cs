namespace DotRecast.Detour.Crowd
{
    internal readonly ref struct DtCrowdScopedTimer
    {
        private readonly DtCrowdTimerLabel _label;
        private readonly DtCrowdTelemetry _telemetry;

        internal DtCrowdScopedTimer(DtCrowdTelemetry telemetry, DtCrowdTimerLabel label)
        {
            _telemetry = telemetry;
            _label = label;

            _telemetry.Start(_label);
        }

        public void Dispose()
        {
            _telemetry.Stop(_label);
        }
    }
}