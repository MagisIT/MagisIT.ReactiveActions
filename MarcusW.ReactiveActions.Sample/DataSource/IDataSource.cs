using System.Collections.Generic;
using System.Threading.Tasks;
using MarcusW.ReactiveActions.Sample.Models;

namespace MarcusW.ReactiveActions.Sample.DataSource
{
    public interface IDataSource
    {
        List<Product> Products { get; }
    }
}
