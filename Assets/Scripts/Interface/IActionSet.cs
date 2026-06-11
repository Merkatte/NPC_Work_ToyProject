public interface IActionSet<TKey>
{
    bool TryGetAction(TKey key, out IAction action);
}
