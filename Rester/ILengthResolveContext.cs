namespace Rester;

public interface ILengthResolveContext
{
    IEnumerable<string> GetValues(string name);
}
