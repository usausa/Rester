namespace Example.Client
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    public class TestSingleResponse
    {
        [AllowNull]
        public string Code { get; set; }

        public DateTimeOffset DateTime { get; set; }
    }
}
