using System.Collections.Generic;
using System.Threading.Tasks;
using MagisIT.ReactiveActions.Sample.Models;

namespace MagisIT.ReactiveActions.Sample.DataSource
{
    public interface IDataSource
    {
        List<Product> Products { get; }
    }
}
