using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Lamson.Controllers
{
    public class PingPong : BaseController
    {
        [Route("lamson/ping")]
        public ActionResult Index()
        {
            return Content("pong");
        }
    }
}
