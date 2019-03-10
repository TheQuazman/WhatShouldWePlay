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
            var friends = await GetFriendList();
            var userSummaries = await GetPlayerSummaries(friends.Select(f => f.SteamID));
            return View(userSummaries);
        }

        public async Task<ActionResult> Games(FormCollection collection)
        {
            var steamIDs = collection["SteamIds"];
            steamIDs += string.Format(",{0}", _steamId);

            //Get the intersection of all games owned by a list of Steam users
            var commonAppIds = new List<int>();
            foreach(var steamID in steamIDs.Split(','))
            {
                //Send request to get Steam users' owned games
                var response = await _httpClient.GetAsync(string.Format("IPlayerService/GetOwnedGames/v1/?key={0}&steamid={1}", _key, steamID));

                //Checking the response is successful or not which is sent using HttpClient  
                if (response.IsSuccessStatusCode)
                {
                    //Storing the response details recieved from web api   
                    var userOwnedAppsResponseContent = response.Content.ReadAsStringAsync().Result;
                    JObject userOwnedAppsJsonObject = JsonConvert.DeserializeObject<JObject>(userOwnedAppsResponseContent);
                    string userOwnedAppsJsonString = userOwnedAppsJsonObject["response"]["games"].ToString();

                    var currentUserOwnedApps = JsonConvert.DeserializeObject<List<PlayerAppInfo>>(userOwnedAppsJsonString);
                    //Deserializing the response recieved from web api and storing into the Employee list  
                    if (commonAppIds.Count == 0)
                        commonAppIds = currentUserOwnedApps.Select(c => c.AppID).ToList();
                    else
                        commonAppIds = commonAppIds.Intersect(currentUserOwnedApps.Select(c => c.AppID)).ToList();
                }
            }

            var allAppDetails = new List<AppDetails>();
            foreach (var appID in commonAppIds)
            {
                /* Send request to get game deatils
                 * The request used to be able to get details of multiple games by passing in a comma delimited list of App ID's, but doesn't seem
                 * to be working at the time of developing this. */
                var response = await _httpClient.GetAsync(string.Format("http://store.steampowered.com/api/appdetails/?appids={0}&filters=basic&type=game", appID.ToString()));

                //Checking the response is successful or not which is sent using HttpClient  
                if (response.IsSuccessStatusCode)
                {
                    //Storing the response details recieved from web api   
                    var userOwnedAppsResponseContent = response.Content.ReadAsStringAsync().Result;
                    JObject userOwnedAppsJsonObject = JsonConvert.DeserializeObject<JObject>(userOwnedAppsResponseContent);
                    string userOwnedAppsJsonString = userOwnedAppsJsonObject[appID.ToString()]["data"].ToString();

                    var appDetails = JsonConvert.DeserializeObject<AppDetails>(userOwnedAppsJsonString);

                    allAppDetails.Add(appDetails);
                }
            }

            return View(allAppDetails);
        }

        /// <summary>
        /// Gets the logged in user's friend list
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Friend>> GetFriendList()
        {
            var friends = new List<Friend>();

            //Send request to get the logged in Steam user's friend list 
            var response = await _httpClient.GetAsync(string.Format("ISteamUser/GetFriendList/v1/?key={0}&steamid={1}", _key, _steamId));

            //Checking the response is successful or not which is sent using HttpClient  
            if (response.IsSuccessStatusCode)
            {
                //Storing the response details recieved from web api   
                var friendsResponseContent = response.Content.ReadAsStringAsync().Result;
                JObject friendsJsonObject = JsonConvert.DeserializeObject<JObject>(friendsResponseContent);
                string friendsJsonString = friendsJsonObject["friendslist"]["friends"].ToString(); 
                friends = JsonConvert.DeserializeObject<List<Friend>>(friendsJsonString);
            }

            return friends;
        }

        /// <summary>
        /// Get player summaries for all given Steam ID's
        /// </summary>
        /// <param name="steamIds">The Steam ID's to get summaries for.</param>
        /// <returns></returns>
        public async Task<IEnumerable<PlayerSummary>> GetPlayerSummaries(IEnumerable<string> steamIds)
        {
            var steamUsers = new List<PlayerSummary>();
            var steamIdArgStrings = new List<string>();

            //Can only pass 100 steam ID's at a time, so iterate through until entire list of ID's are processed
            for (int i = 0; i < steamIds.Count(); i += 100)
            {
                //Using Skip and Take to take 100 item slices of the entire Steam ID list passed in
                var steamIdsArg = string.Join(",", steamIds.Skip(i).Take(i + 100));

                //Sending request to find web api REST service resource GetAllEmployees using HttpClient  
                var response = await _httpClient.GetAsync(string.Format("ISteamUser/GetPlayerSummaries/v2/?key={0}&steamids={1}", _key, steamIdsArg));

                //Checking the response is successful or not which is sent using HttpClient  
                if (response.IsSuccessStatusCode)
                {
                    //Storing the response details recieved from web api   
                    var steamUsersResponseContent = response.Content.ReadAsStringAsync().Result;
                    JObject steamUsersJsonObject = JsonConvert.DeserializeObject<JObject>(steamUsersResponseContent);
                    string steamUsersJsonString = steamUsersJsonObject["response"]["players"].ToString();
                    steamUsers.AddRange(JsonConvert.DeserializeObject<List<PlayerSummary>>(steamUsersJsonString));
                }
            }
            

            return steamUsers;
        }
    }
}