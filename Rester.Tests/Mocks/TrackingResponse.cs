namespace Rester.Mocks;

using System.Net;

internal sealed class TrackingResponse : HttpResponseMessage
{
    public bool WasDisposed { get; private set; }

    public TrackingResponse(HttpStatusCode statusCode, string? jsonContent = null, string? contentType = "application/json")
        : base(statusCode)
    {
        if (jsonContent is not null)
        {
            Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, contentType ?? "application/json");
        }
        else
        {
            Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "text/plain");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            WasDisposed = true;
        }

        base.Dispose(disposing);
    }
}
