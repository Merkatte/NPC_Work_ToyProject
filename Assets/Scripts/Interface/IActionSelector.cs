public interface IActionSelector<TContext, TPlan>
{
    bool TrySelectAction(TContext context, out TPlan plan);
}
