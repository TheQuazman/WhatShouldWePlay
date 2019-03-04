using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WhatShouldWePlayMVCNoAuth.Models
{
    public class SteamUser
    {
        [Display(Name = "Steam ID", Description = "")]
        public string SteamID { get; set; }
        public int CommunityVisibilityState { get; set; }
        public string ProfileState { get; set; }
        [Display(Name = "Steam Name", Description = "")]
        public string PersonaName { get; set; }
        public int LastLogOff { get; set; }
        public int CommentPermission { get; set; }
        public string ProfileUrl { get; set; }
        public string Avatar { get; set; }
        public string AvatarMedium { get; set; }
        public string AvatarFull { get; set; }
        public int PersonaState { get; set; }
        public string RealName { get; set; }
        public string PrimaryClanID { get; set; }
        public int TimeCreated { get; set; }
        public int PersonaStateFlags { get; set; }
        public string GameExtraInfo { get; set; }
        public string GameID { get; set; }
        public string LocCountryCode { get; set; }
        public string LocStateCode { get; set; }
    }
}