using System.Collections.Generic;
using System.Linq;
using static Sandbox.SomeClass;

namespace Sandbox;

internal class MainViewModel
{
    public IEnumerable<Data> Items { get => Enumerable.Range(0, 10).Select(_ => new Data()); }
}
