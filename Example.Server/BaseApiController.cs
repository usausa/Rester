namespace Example.Server;

using Microsoft.AspNetCore.Mvc;

[Route("[controller]/[action]")]
public abstract class BaseApiController : Controller
{
}
