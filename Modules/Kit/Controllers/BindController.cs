using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared;
using Shared.Models.Base;
using Shared.Models.Online.Settings;
using Shared.Services;
using Shared.Services.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using IO = System.IO;

namespace KitMod.Controllers
{
    public class SiteConf
    {
        public OnlinesSettings Rezka { get; set; } = new OnlinesSettings("Rezka", "https://hdrezka.me");
        public OnlinesSettings Filmix { get; set; } = new OnlinesSettings("Filmix", "http://filmixapp.cyou");
        public OnlinesSettings VoKino { get; set; } = new OnlinesSettings("VoKino", "http://api.vokino.org");
        public OnlinesSettings KinoPub { get; set; } = new OnlinesSettings("KinoPub", "https://api.srvkp.com");
        public OnlinesSettings GetsTV { get; set; } = new OnlinesSettings("GetsTV", "https://getstv.com");
    }

    public class BindController : BaseOnlineController
    {
        static SiteConf siteConf = ModuleInvoke.DeserializeInit(new SiteConf());

        #region saveBind
        void saveBind(string aesGcmKey, JObject ob)
        {
            if (string.IsNullOrEmpty(aesGcmKey))
                return;

            string md5key = CrypTo.md5(aesGcmKey);
            string filePath = $"database/kit/{md5key[0]}/{md5key}";
            Directory.CreateDirectory($"database/kit/{md5key[0]}");

            string json = JsonConvert.SerializeObject(ob, Formatting.Indented);

            CryptoKit.Write(aesGcmKey, json, filePath);
        }
        #endregion

        #region renderHtml
        ContentResult renderHtml(string title, string body)
        {
            return Content(@"<!DOCTYPE html>
<html>
<head>
    <title>" + title + @"</title>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta charset='utf-8'>
    <style>
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }

        body {
            background-color: #fafafa;
            color: #ffffff;
            font-family: sans-serif;
            line-height: 1.6;
            padding: 20px;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
        }

        .container {
            background-color: #fdfdfd;
            padding: 30px;
            border-radius: 8px;
            max-width: 600px;
            width: 100%;
            box-shadow: 0 18px 24px rgb(175 175 175 / 30%);
            color: #000;
        }

        .steps {
            margin: 20px 0;
        }

        .step {
            margin-bottom: 20px;
            font-size: 18px;
        }

        .title {
            margin-bottom: 20px;
            font-size: 18px;
        }

        .code {
            background-color: #ffffff;
            padding: 15px;
            border-radius: 4px;
            font-size: 24px;
            font-weight: bold;
            text-align: center;
            margin: 10px 0;
            border: 1px solid #eaeaea;
            color: #4caf50;
        }

        a {
            color: #64B5F6;
            text-decoration: none;
            transition: color 0.3s;
        }

        a:hover {
            color: #90CAF9;
        }

        .button {
            display: inline-block;
            background-color: #8bc34a;
            color: white;
            padding: 12px 24px;
            border-radius: 4px;
            text-decoration: none;
            margin-top: 20px;
            transition: background-color 0.3s;
            border: none;
            font-size: 16px;
            cursor: pointer;
            width: 100%;
            text-align: center;
        }

        .button:hover {
            background-color: #45a049;
            color: #fff
        }

        .form-group {
            margin-bottom: 15px;
        }

        input[type='text'] {
            background-color: #ffffff;
            border: 1px solid #dbdbdb;
            color: #4e4b4b;
            padding: 12px;
            border-radius: 4px;
            width: 100%;
            margin-bottom: 10px;
            font-size: 16px;
        }

        input[type='text']:focus {
            border-color: #4CAF50;
            outline: none;
        }

        @media (max-width: 480px) {
            body {
                padding: 15px;
            }

            .container {
                padding: 20px;
            }

            .step {
                font-size: 16px;
            }

            .code {
                font-size: 20px;
            }
        }
    </style>
</head>
<body>
    <div class='container'>
        " + body + @"
    </div>
</body>
</html>", "text/html; charset=utf-8");
        }
        #endregion

        #region loadconf
        JObject loadconf(string aesGcmKey, string filePath)
        {
            if (!IO.File.Exists(filePath))
                return new JObject();

            string json = CryptoKit.ReadFile(aesGcmKey, filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new JObject();

            return JsonConvert.DeserializeObject<JObject>(json);
        }
        #endregion

        (string aesGcmKey, string filePath) userInfo()
        {
            if (HttpContext.Request.Cookies.TryGetValue("aesgcmkey", out string aesGcmKey) && !string.IsNullOrWhiteSpace(aesGcmKey))
            {
                if (CryptoKit.TestKey(aesGcmKey))
                {
                    string md5key = CrypTo.md5(aesGcmKey);
                    string filePath = $"database/kit/{md5key[0]}/{md5key}";

                    return (aesGcmKey, filePath);
                }
            }

            return default;
        }


        #region Filmix
        [HttpGet]
        [AllowAnonymous]
        [Route("/bind/filmix")]
        async public Task<ActionResult> Filmix(string filmix_token)
        {
            var filmix = siteConf.Filmix;

            (string aesGcmKey, string filePath) = userInfo();
            if (string.IsNullOrEmpty(aesGcmKey))
                return Content("ошибка, отсутствует параметр aesGcmKey", "text/html; charset=utf-8");

            if (string.IsNullOrEmpty(filmix_token))
            {
                var token_request = await Http.Get<JObject>($"{filmix.host}/api/v2/token_request?user_dev_apk=2.2.13&user_dev_id={UnicTo.Code(16)}&user_dev_name=Xiaomi&user_dev_os=12&user_dev_vendor=Xiaomi&user_dev_token=", timeoutSeconds: 10);

                if (token_request == null)
                    return Content($"{filmix.host} недоступен, повторите попытку позже", "text/html; charset=utf-8");

                string body = $@"<div class='steps'>
                    <div class='step'>
                        1. Откройте <a href='https://filmix.my/consoles' target='_blank'>https://filmix.my/consoles</a>
                    </div>
                    <div class='step'>
                        2. Добавьте идентификатор устройства
                        <div class='code'>{token_request.Value<string>("user_code")}</div>
                    </div>
                </div>
                <a href='/bind/filmix?filmix_token={token_request.Value<string>("code")}' class='button'>
                    завершить привязку устройства
                </a>";

                return renderHtml("Привязка Filmix", body);
            }
            else
            {
                bool pro = false;
                var root = await Http.Get<JObject>($"{filmix.host}/api/v2/user_profile?app_lang=ru_RU&user_dev_apk=2.2.13&user_dev_id={UnicTo.Code(16)}&user_dev_name=Xiaomi&user_dev_os=12&user_dev_vendor=Xiaomi&user_dev_token=" + filmix_token, timeoutSeconds: 10);
                if (root != null)
                {
                    if (!root.ContainsKey("user_data"))
                        return Content($"Указанный токен {filmix_token} не найден", "text/html; charset=utf-8");

                    var user_data = root["user_data"];
                    if (user_data != null)
                    {
                        pro = user_data.Value<bool>("is_pro");
                        if (pro == false)
                            pro = user_data.Value<bool>("is_pro_plus");
                    }
                }

                var bwaconf = loadconf(aesGcmKey, filePath);
                bwaconf["Filmix"] = new JObject()
                {
                    ["enable"] = true,
                    ["token"] = filmix_token,
                    ["pro"] = pro,
                };

                saveBind(aesGcmKey, bwaconf);
                return LocalRedirect("/kit");
            }
        }
        #endregion

        #region Vokino
        [HttpGet]
        [AllowAnonymous]
        [Route("/bind/vokino")]
        async public Task<ActionResult> Vokino(string login, string pass)
        {
            var vokino = siteConf.VoKino;

            (string aesGcmKey, string filePath) = userInfo();
            if (string.IsNullOrEmpty(aesGcmKey))
                return Content("ошибка, отсутствует параметр aesGcmKey", "text/html; charset=utf-8");

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(pass))
            {
                string body = $@"<div class='title'>Введите данные аккаунта <a href='https://vokino.pro' target='_blank'>vokino.pro</a></div>
                    <form method='get' action='/bind/vokino'>
                        <div class='form-group'>
                            <input type='text' name='login' placeholder='Email' required>
                        </div>
                        <div class='form-group'>
                            <input type='text' name='pass' placeholder='Пароль' required>
                        </div>
                        <button type='submit' class='button'>Добавить устройство</button>
                    </form>";

                return renderHtml("Привязка VoKino", body);
            }
            else
            {
                string deviceid = new string(DateTime.Now.ToBinary().ToString().Reverse().ToArray()).Substring(0, 8);
                var token_request = await Http.Get<JObject>($"{vokino.host}/v2/auth?email={HttpUtility.UrlEncode(login)}&passwd={HttpUtility.UrlEncode(pass)}&deviceid={deviceid}", timeoutSeconds: 10, headers: HeadersModel.Init(("user-agent", "lampac")));

                if (token_request == null)
                    return Content($"{vokino.host} недоступен, повторите попытку позже", "text/html; charset=utf-8");

                string authToken = token_request.Value<string>("authToken");
                if (string.IsNullOrEmpty(authToken))
                    return Content(token_request.Value<string>("error") ?? "Не удалось получить токен", "text/html; charset=utf-8");

                var bwaconf = loadconf(aesGcmKey, filePath);

                bwaconf["VoKino"] = new JObject()
                {
                    ["enable"] = true,
                    ["token"] = authToken,
                    ["online"] = new JObject()
                    {
                        ["vokino"] = true,
                        ["filmix"] = true,
                        ["alloha"] = true,
                        ["monframe"] = true,
                        ["remux"] = true,
                        ["ashdi"] = true,
                        ["hdvb"] = true,
                        ["vibix"] = true
                    }
                };

                saveBind(aesGcmKey, bwaconf);
                return LocalRedirect("/kit");
            }
        }
        #endregion

        #region Kinopub
        [HttpGet]
        [AllowAnonymous]
        [Route("/bind/kinopub")]
        async public Task<ActionResult> Kinopub(string code)
        {
            var KinoPub = siteConf.KinoPub;

            (string aesGcmKey, string filePath) = userInfo();
            if (string.IsNullOrEmpty(aesGcmKey))
                return Content("ошибка, отсутствует параметр aesGcmKey", "text/html; charset=utf-8");

            if (string.IsNullOrWhiteSpace(code))
            {
                var token_request = await Http.Post<JObject>($"{KinoPub.host}/oauth2/device?grant_type=device_code&client_id=xbmc&client_secret=cgg3gtifu46urtfp2zp1nqtba0k2ezxh", "", timeoutSeconds: 10);
                if (token_request == null || string.IsNullOrWhiteSpace(token_request.Value<string>("user_code")))
                    return Content("api.srvkp.com недоступен, повторите попытку позже", "text/html; charset=utf-8");

                string body = $@"<div class='steps'>
                    <div class='step'>
                        1. Откройте <a href='https://kino.pub/device' target='_blank'>https://kino.pub/device</a>
                    </div>
                    <div class='step'>
                        2. Введите код устройства
                        <div class='code'>{token_request.Value<string>("user_code")}</div>
                    </div>
                </div>
                <a href='/bind/kinopub?code={token_request.Value<string>("code")}' class='button'>
                    завершить привязку устройства
                </a>";

                return renderHtml("Привязка KinoPub", body);
            }
            else
            {
                var device_token = await Http.Post<JObject>($"{KinoPub.host}/oauth2/device?grant_type=device_token&client_id=xbmc&client_secret=cgg3gtifu46urtfp2zp1nqtba0k2ezxh&code={code}", "");

                if (device_token == null)
                    return Content($"{KinoPub.host} недоступен, повторите попытку позже", "text/html; charset=utf-8");

                if (string.IsNullOrWhiteSpace(device_token.Value<string>("access_token")))
                    return Content($"Указанный токен {device_token.Value<string>("access_token")} не найден", "text/html; charset=utf-8");

                var bwaconf = loadconf(aesGcmKey, filePath);

                bwaconf["KinoPub"] = new JObject()
                {
                    ["enable"] = true,
                    ["token"] = device_token.Value<string>("access_token")
                };

                saveBind(aesGcmKey, bwaconf);
                return LocalRedirect("/kit");
            }
        }
        #endregion

        #region Rezka
        [HttpGet]
        [AllowAnonymous]
        [Route("/bind/rezka")]
        async public Task<ActionResult> Rezka(string login, string pass)
        {
            var rezka = siteConf.Rezka;

            (string aesGcmKey, string filePath) = userInfo();
            if (string.IsNullOrEmpty(aesGcmKey))
                return Content("ошибка, отсутствует параметр aesGcmKey", "text/html; charset=utf-8");

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(pass))
            {
                string body = $@"<div class='title'>Введите данные PREMUIM аккаунта <a href='{siteConf.Rezka.host}' target='_blank'>HDRezka</a></div>
                    <form method='get' action='/bind/rezka'>
                        <div class='form-group'>
                            <input type='text' name='login' placeholder='Email' required>
                        </div>
                        <div class='form-group'>
                            <input type='text' name='pass' placeholder='Пароль' required>
                        </div>
                        <button type='submit' class='button'>Привязать устройство</button>
                    </form>";

                return renderHtml("Привязка HDRezka", body);
            }
            else
            {
                string cookie = await getCookieRezka(login, pass);
                if (string.IsNullOrEmpty(cookie))
                    return Content("Ошибка авторизации, повторите попытку позже", "text/html; charset=utf-8");

                bool premium = false;
                string rezka_main = await Http.Get(siteConf.Rezka.host, cookie: cookie, timeoutSeconds: 10);
                if (rezka_main != null && (rezka_main.Contains("b-premium_user__body") || rezka_main.Contains("b-hd_prem")))
                    premium = true;

                var bwaconf = loadconf(aesGcmKey, filePath);

                if (premium)
                {
                    bwaconf["RezkaPrem"] = new JObject()
                    {
                        ["enable"] = true,
                        ["cookie"] = cookie
                    };

                    if (bwaconf.ContainsKey("Rezka"))
                    {
                        bwaconf["Rezka"]["enable"] = false;
                    }
                    else
                    {
                        bwaconf["Rezka"] = new JObject()
                        {
                            ["enable"] = false
                        };
                    }
                }
                else
                {
                    bwaconf["Rezka"] = new JObject()
                    {
                        ["enable"] = true,
                        ["cookie"] = cookie
                    };
                }

                saveBind(aesGcmKey, bwaconf);
                return LocalRedirect("/kit");
            }
        }
        #endregion

        #region GetsTV
        [HttpGet]
        [AllowAnonymous]
        [Route("/bind/getstv")]
        async public Task<ActionResult> GetsTV(string login, string pass)
        {
            var getstv = siteConf.GetsTV;

            (string aesGcmKey, string filePath) = userInfo();
            if (string.IsNullOrEmpty(aesGcmKey))
                return Content("ошибка, отсутствует параметр aesGcmKey", "text/html; charset=utf-8");

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(pass))
            {
                string body = $@"<div class='title'>Введите данные аккаунта <a href='https://getstv.com/user' target='_blank'>getstv.com</a></div>
                    <form method='get' action='/bind/getstv'>
                        <div class='form-group'>
                            <input type='text' name='login' placeholder='Email' required>
                        </div>
                        <div class='form-group'>
                            <input type='text' name='pass' placeholder='Пароль' required>
                        </div>
                        <button type='submit' class='button'>Добавить устройство</button>
                    </form>";

                return renderHtml("Привязка GetsTV", body);
            }
            else
            {
                string postdata = $"{{\"email\":\"{login}\",\"password\":\"{pass}\",\"fingerprint\":\"{CrypTo.md5(DateTime.Now.ToString())}\",\"device\":{{}}}}";
                var result = await Http.Post<JObject>($"{siteConf.GetsTV.host}/api/login", new System.Net.Http.StringContent(postdata, Encoding.UTF8, "application/json"), headers: httpHeaders(siteConf.GetsTV));

                if (result == null)
                    return ContentTo($"{siteConf.GetsTV.host} недоступен, повторите попытку позже");

                string token = result.Value<string>("token");
                if (string.IsNullOrEmpty(token))
                    return ContentTo(JsonConvert.SerializeObject(result, Formatting.Indented));

                var bwaconf = loadconf(aesGcmKey, filePath);

                bwaconf["GetsTV"] = new JObject()
                {
                    ["enable"] = true,
                    ["token"] = token
                };

                saveBind(aesGcmKey, bwaconf);
                return LocalRedirect("/kit");
            }
        }
        #endregion

        #region IptvOnline
        [HttpGet]
        [AllowAnonymous]
        [Route("/bind/iptvonline")]
        public ActionResult IptvOnline(string login, string pass)
        {
            (string aesGcmKey, string filePath) = userInfo();
            if (string.IsNullOrEmpty(aesGcmKey))
                return Content("ошибка, отсутствует параметр aesGcmKey", "text/html; charset=utf-8");

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(pass))
            {
                string body = $@"<div class='title'>Введите данные <a href='https://iptv.online/ru/dealers/api' target='_blank'>https://iptv.online/ru/dealers/api</a></div>
                    <form method='get' action='/bind/iptvonline'>
                        <div class='form-group'>
                            <input type='text' name='login' placeholder='X-API-KEY' required>
                        </div>
                        <div class='form-group'>
                            <input type='text' name='pass' placeholder='X-API-ID' required>
                        </div>
                        <button type='submit' class='button'>Добавить устройство</button>
                    </form>";

                return renderHtml("Привязка iptv.online", body);
            }
            else
            {
                var bwaconf = loadconf(aesGcmKey, filePath);

                bwaconf["IptvOnline"] = new JObject()
                {
                    ["enable"] = true,
                    ["token"] = $"{login}:{pass}"
                };

                saveBind(aesGcmKey, bwaconf);
                return LocalRedirect("/kit");
            }
        }
        #endregion


        #region getCookie
        async Task<string> getCookieRezka(string login, string passwd)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(passwd))
                return null;

            try
            {
                var clientHandler = new System.Net.Http.HttpClientHandler()
                {
                    AllowAutoRedirect = false
                };

                clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                using (var client = new System.Net.Http.HttpClient(clientHandler))
                {
                    client.Timeout = TimeSpan.FromSeconds(20);
                    client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                    var postParams = new Dictionary<string, string>
                    {
                        { "login_name", login },
                        { "login_password", passwd },
                        { "login_not_save", "0" }
                    };

                    using (var postContent = new System.Net.Http.FormUrlEncodedContent(postParams))
                    {
                        using (var response = await client.PostAsync($"https://hdrezka.me/ajax/login/", postContent))
                        {
                            if (response.Headers.TryGetValues("Set-Cookie", out var cook))
                            {
                                string cookie = string.Empty;

                                foreach (string line in cook)
                                {
                                    if (string.IsNullOrEmpty(line))
                                        continue;

                                    if (line.Contains("=deleted;"))
                                        continue;

                                    if (line.Contains("dle_user_id") || line.Contains("dle_password"))
                                        cookie += $"{line.Split(";")[0]}; ";
                                }

                                if (cookie.Contains("dle_user_id") && cookie.Contains("dle_password"))
                                    return Regex.Replace(cookie.Trim(), ";$", "");
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Serilog.Log.Error(ex, "{Class} {CatchId}", "PremiumConf", "id_xwwb0e5e");
            }

            return null;
        }
        #endregion
    }
}
