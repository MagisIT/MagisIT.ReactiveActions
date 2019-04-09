namespace MagisIT.ReactiveActions.Helpers
{
    public abstract class ActionProviderBase : IActionProvider
    {
        public ActionExecutor ActionExecutor { get; set; }
    }
}
