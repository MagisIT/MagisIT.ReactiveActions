using System.Collections.Generic;
using MagisIT.ReactiveActions.IntegrationTests.App.Models;

namespace MagisIT.ReactiveActions.IntegrationTests.App
{
    public interface IDataSource
    {
        IList<Foo> Foos { get; }

        IList<Bar> Bars { get; }
    }
}
