namespace Veruthian.Dotnet.Library.Data.Operations
{
    public interface IOperationTracer<TState>
    {
        void OperationStarting(IOperation<TState> operation, TState state);

        void OperationFinishing(IOperation<TState> operation, TState state, bool success);
    }
}