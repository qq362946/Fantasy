namespace DotRecast.Core
{
    public readonly ref struct RcScopedTimer
    {
        private readonly RcContext _context;
        private readonly RcTimerLabel _label;

        internal RcScopedTimer(RcContext context, RcTimerLabel label)
        {
            _context = context;
            _label = label;
            
            _context.StartTimer(_label);
        }

        public void Dispose()
        {
            _context.StopTimer(_label);
        }
    }
}