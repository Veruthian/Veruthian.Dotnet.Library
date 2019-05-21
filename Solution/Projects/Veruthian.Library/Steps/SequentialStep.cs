using System.Collections.Generic;
using Veruthian.Library.Collections;
using Veruthian.Library.Numeric;

namespace Veruthian.Library.Steps
{
    public class SequentialStep : Step
    {
        IVector<IStep> sequence;


        public SequentialStep() : this((IVector<IStep>)null) { }

        public SequentialStep(IVector<IStep> sequence) => Sequence = sequence;

        public SequentialStep(IEnumerable<IStep> sequence) => Sequence = DataVector<IStep>.Withdraw(sequence);

        public SequentialStep(params IStep[] sequence) => Sequence = DataVector<IStep>.From(sequence);


        public IVector<IStep> Sequence
        {
            get => this.sequence;
            set => this.sequence = value ?? new DataVector<IStep>();
        }

        public override string Description => $"sequence<{sequence.Count}>";

        protected override Number SubStepCount => sequence == null ? 0 : sequence.Count;

        protected override IStep GetSubStep(Number verifiedAddress) => sequence[verifiedAddress];
    }
}