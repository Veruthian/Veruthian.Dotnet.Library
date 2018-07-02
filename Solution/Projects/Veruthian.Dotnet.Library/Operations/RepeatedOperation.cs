using System;
using System.Text;

namespace Veruthian.Dotnet.Library.Operations
{
    public class RepeatedOperation<TState> : NestedOperation<TState>
    {
        int minimum, maximum;

        public RepeatedOperation(IOperation<TState> operation, int minimum = 0, int maximum = 0)
            : base(operation)
        {
            if (minimum < 0 || maximum < 0 || (maximum != 0 && minimum > maximum))
                throw new ArgumentException("Minimum cannot be less that maximum");

            this.minimum = minimum;

            this.maximum = maximum;
        }

        public int Minimum => minimum;

        public int Maximum => maximum;

        public override string Description
        {
            get
            {
                if (maximum != 0)
                    return $"Repeat:{minimum}-{maximum}";
                else if (minimum != 0)
                    return $"Repeat:{minimum}";
                else
                    return $"Repeat";
            }
        }

        protected override bool DoAction(TState state, IOperationTracer<TState> tracer = null)
        {
            bool finalResult = true;

            int count = 0;

            while (true)
            {
                var result = Operation.Perform(state, tracer);

                if (result)
                {
                    count++;

                    if (maximum != 0 && count == maximum)
                    {
                        finalResult = true;
                        break;
                    }
                }
                else
                {
                    finalResult = count >= minimum;
                    break;
                }
            }

            return finalResult;
        }
    }
}