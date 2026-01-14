namespace DotRecast.Recast
{
    public class RcCompactSpanBuilder
    {
        public int y;
        public int reg;
        public int con;
        public int h;

        public static RcCompactSpanBuilder NewBuilder(ref RcCompactSpan span)
        {
            var builder = NewBuilder();
            builder.y = span.y;
            builder.reg = span.reg;
            builder.con = span.con;
            builder.h = span.h;
            return builder;
        }
        
        public static RcCompactSpanBuilder NewBuilder()
        {
            return new RcCompactSpanBuilder();
        }

        private RcCompactSpanBuilder()
        {
        }

        public RcCompactSpanBuilder WithReg(int reg)
        {
            this.reg = reg;
            return this;
        }

        public RcCompactSpan Build()
        {
            return new RcCompactSpan(this);
        }
    }
}