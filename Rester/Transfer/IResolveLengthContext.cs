namespace Rester.Transfer
{
    using System.Collections.Generic;

    public interface ILengthResolveContext
    {
        IEnumerable<string> GetValues(string name);
    }
}
