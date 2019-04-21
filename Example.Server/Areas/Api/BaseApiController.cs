namespace Example.Server.Areas.Api
{
    using Microsoft.AspNetCore.Mvc;

    [Area("api")]
    [Route("[area]/[controller]/[action]")]
    public class BaseApiController : Controller
    {
    }
}
