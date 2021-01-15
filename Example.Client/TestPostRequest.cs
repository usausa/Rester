namespace Example.Client
{
    using System.Diagnostics.CodeAnalysis;

    public class TestPostRequest
    {
        public int Value { get; set; }

        [AllowNull]
        public string Text { get; set; }
    }
}
