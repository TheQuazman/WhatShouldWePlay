using DotNetOpenAuth.OpenId.RelyingParty;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WhatShouldWePlayMVCNoAuth.Models;

namespace WhatShouldWePlayMVCNoAuth.Controllers
{
    public class HomeController : Controller
    {
        private static HttpClient _httpClient;
        private static string _steamId;
        private readonly string _key = ConfigurationManager.AppSettings["SteamAPIKey"];

        public ActionResult Index()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.BaseAddress = new Uri("https://api.steampowered.com");

            return View();
        }

        public ActionResult SteamLogin()
        {
            //Steam authentication code gotten from: https://stackoverflow.com/questions/20845146/steam-login-authentication-c-sharp
            var openid = new OpenIdRelyingParty();
            var response = openid.GetResponse();

            if (response != null)
            {
                switch (response.Status)
                {
                    case AuthenticationStatus.Authenticated:
                        // do success
                        var responseURI = response.ClaimedIdentifier.ToString();
                        _steamId = responseURI.Split('/').Last();
                        ViewBag.Message = "Your steam ID = " + _steamId;
                        //"http://steamcommunity.com/openid/id/76561197969877387"
                        // last part is steam user id
                        return RedirectToAction("Friends");
                    case AuthenticationStatus.Canceled:
                    case AuthenticationStatus.Failed:
                        // do fail
                        break;
                }
            }
            else
            {
                using (OpenIdRelyingParty openidd = new OpenIdRelyingParty())
                {
                    IAuthenticationRequest request = openidd.CreateRequest("http://steamcommunity.com/openid");
                    request.RedirectToProvider();
                }
            }

            return View();
        }

        public async Task<ActionResult> Friends()
        {
            var friends = await GetFriends();
            
            return View(friends);
        }

        public async Task<List<Friend>> GetFriends()
        {
            var friends = new List<Friend>();

            //Sending request to find web api REST service resource GetAllEmployees using HttpClient  
            var response = await _httpClient.GetAsync(string.Format("ISteamUser/GetFriendList/v1/?key={0}&steamid={1}", _key, _steamId));

            //Checking the response is successful or not which is sent using HttpClient  
            if (response.IsSuccessStatusCode)
            {
                //Storing the response details recieved from web api   
                var friendsResponseContent = response.Content.ReadAsStringAsync().Result;

                JObject friendsJsonObject = JsonConvert.DeserializeObject<JObject>(friendsResponseContent);

                string friendsJsonString = friendsJsonObject["friendslist"]["friends"].ToString();

                //Deserializing the response recieved from web api and storing into the Employee list  
                friends = JsonConvert.DeserializeObject<List<Friend>>(friendsJsonString);

            }

            return friends;
        }

        public async Task<List<SteamUser>> GetUserSummaries(List<Friend> friends)
        {
            var steamUsers = new List<SteamUser>();

            foreach (var friend in friends)
            {
                //Sending request to find web api REST service resource GetAllEmployees using HttpClient  
                var response = await _httpClient.GetAsync(string.Format("ISteamUser/GetFriendList/v1/?key={0}&steamid={1}", _key, _steamId));

                //Checking the response is successful or not which is sent using HttpClient  
                if (response.IsSuccessStatusCode)
                {
                    //Storing the response details recieved from web api   
                    var friendsResponseContent = response.Content.ReadAsStringAsync().Result;

                    JObject friendsJsonObject = JsonConvert.DeserializeObject<JObject>(friendsResponseContent);

                    string friendsJsonString = friendsJsonObject["friendslist"]["friends"].ToString();

                    //Deserializing the response recieved from web api and storing into the Employee list  
                    steamUsers = JsonConvert.DeserializeObject<List<SteamUser>>(friendsJsonString);

                }
            }

            return steamUsers;
        }
    }
}