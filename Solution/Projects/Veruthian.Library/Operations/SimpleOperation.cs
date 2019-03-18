namespace Veruthian.Library.Operations
{
    public abstract class SimpleOperation<TState> : Operation<TState>
    {
        protected override int Count => 0;

        protected override IOperation<TState> GetSubOperation(int verifiedIndex)
        {
            return null;
        }
    }
}