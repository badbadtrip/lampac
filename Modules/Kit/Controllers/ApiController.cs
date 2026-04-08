using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shared;
using Shared.Services;
using Shared.Services.Utilities;
using System;
using System.IO;
using IO = System.IO;

namespace KitMod.Controllers
{
    public class ApiController : Controller
    {
        [AllowAnonymous]
        [Route("/kit")]
        public ActionResult KitIndex([FromForm] string json, string aesGcmKey)
        {
            if (string.IsNullOrWhiteSpace(aesGcmKey))
            {
                if (!HttpContext.Request.Cookies.TryGetValue("aesgcmkey", out aesGcmKey) || string.IsNullOrWhiteSpace(aesGcmKey))
                {
                    string html = IO.File.ReadAllText($"{ModInit.folder_mod}/html/auth.html");
                    string randomAesGcmKey = CryptoKit.RandomKey();

                    return Content(html.Replace("{randomAesGcmKey}", randomAesGcmKey), "text/html; charset=utf-8");
                }
            }

            if (!CryptoKit.TestKey(aesGcmKey))
            {
                HttpContext.Response.Cookies.Delete("aesgcmkey");
                return Content("incorrect key");
            }

            if (!string.IsNullOrWhiteSpace(aesGcmKey)) // если пароль был через html form, ставим cookie
                HttpContext.Response.Cookies.Append("aesgcmkey", aesGcmKey);

            string md5key = CrypTo.md5(aesGcmKey);
            string filePath = $"database/kit/{md5key[0]}/{md5key}";

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    JsonConvert.DeserializeObject<CoreInit>(json);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "CatchId={CatchId}", "id_9b16fa74");
                    return Json(new { error = true, ex = ex.Message });
                }

                Directory.CreateDirectory($"database/kit/{md5key[0]}");
                bool success = CryptoKit.Write(aesGcmKey, json, filePath);

                if (!success)
                    HttpContext.Response.StatusCode = 503;

                return Content(JsonConvert.SerializeObject(new { success }), "application/json; charset=utf-8");
            }
            else
            {
                string html = IO.File.ReadAllText($"{ModInit.folder_mod}/html/settings.html");
                string conf = IO.File.Exists(filePath) ? CryptoKit.ReadFile(aesGcmKey, filePath) : null;

                return Content(html.Replace("{conf}", conf ?? string.Empty), "text/html; charset=utf-8");
            }
        }
    }
}
