using System.Collections.Generic;
using MagisIT.ReactiveActions.IntegrationTests.App.Models;

namespace MagisIT.ReactiveActions.IntegrationTests.App
{
    public class StubDataSource : IDataSource
    {
        public IList<Foo> Foos { get; } = new List<Foo> { new Foo { Id = 0, Name = "Clock" }, new Foo { Id = 1, Name = "Cat" } };

        public IList<Bar> Bars { get; } = new List<Bar>();
    }
}
