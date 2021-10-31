namespace Rester.Internal
{
    using System.Collections.Generic;
    using System.Net.Http;

    internal sealed class LengthResolveContext : ILengthResolveContext
    {
        private readonly HttpResponseMessage response;

        public LengthResolveContext(HttpResponseMessage response)
        {
            this.response = response;
        }

        public IEnumerable<string> GetValues(string name) => response.Headers.GetValues(name);
    }
}
