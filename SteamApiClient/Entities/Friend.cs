using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SteamApiClient.Models
{
    public class Friend
    {
        [Display(Name = "Steam ID", Description = "")]
        public string SteamID { get; set; }
        [Display(Name = "Relationship", Description = "")]
        public string Relationship { get; set; }
        [Display(Name = "Friend Since", Description = "")]
        public int Friend_Since { get; set; }
    }
}