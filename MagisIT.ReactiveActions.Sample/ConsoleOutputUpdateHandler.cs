using System;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Reactivity.UpdateHandling;

namespace MagisIT.ReactiveActions.Sample
{
    public class ConsoleOutputUpdateHandler : IActionResultUpdateHandler
    {
        public Task HandleResultChangedAsync(string trackingSession, Action action, IActionDescriptor actionDescriptor)
        {
            Console.WriteLine($"EVENT [{trackingSession}] Action {action.Name} ({actionDescriptor?.CombinedIdentifier}) result changed.");
            return Task.CompletedTask;
        }

        public Task HandleResultItemChangedAsync<TModel>(string trackingSession, Action action, IActionDescriptor actionDescriptor, TModel itemBeforeUpdate, TModel itemAfterUpdate)
            where TModel : class
        {
            Console.WriteLine(
                $"EVENT [{trackingSession}] Action {action.Name} ({actionDescriptor?.CombinedIdentifier}) result item changed. Before: {itemBeforeUpdate?.ToString() ?? "Null"}, After: {itemAfterUpdate?.ToString() ?? "Null"}");
            return Task.CompletedTask;
        }
    }
}
