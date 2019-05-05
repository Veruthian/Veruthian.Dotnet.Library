using System;

namespace Veruthian.Library.Operations
{
    public class RepeatedOperation<TState> : BaseNestedOperation<TState>
    {
        int? times;


        private RepeatedOperation(IOperation<TState> operation, int? times = null) : base(operation) => this.times = times;

        public override string Description => "repeat" + (times == null ? "?" : times < 0 ? $"?<{times}>" : $"<{times}>");

        protected override bool DoAction(TState state, ITracer<TState> tracer = null)
        {
            if (times == null)
            {
                while (Operation.Perform(state, tracer)) ;

                return true;
            }
            else if (times < 0)
            {
                for (int i = 0; i > times; i--)
                    if (!Operation.Perform(state, tracer))
                        break;

                return true;
            }
            else
            {
                for (int i = 0; i < times; i++)
                    if (!Operation.Perform(state, tracer))
                        return false;

                return true;
            }
        }


        public static RepeatedOperation<TState> Repeat(IOperation<TState> operation) => new RepeatedOperation<TState>(operation);

        public static RepeatedOperation<TState> Exactly(int times, IOperation<TState> operation) => new RepeatedOperation<TState>(operation, Math.Abs(times));

        public static RepeatedOperation<TState> AtMost(int times, IOperation<TState> operation) => new RepeatedOperation<TState>(operation, -(Math.Abs(times)));
    }
}