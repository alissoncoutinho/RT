using Barragem.Context;
using System;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Security;
using WebMatrix.WebData;
using Barragem.Models;
using System.Data;
using System.Data.Entity;
using System.Linq;


namespace Barragem.Helpers
{
    public static class HtmlHelpers
    {
        public static string GetLogo(this HtmlHelper htmlHelper, String userName)
        {
            var userId = WebSecurity.GetUserId(userName);
            if (userId == 0){
                return "logo";
            }
            BarragemDbContext db = new BarragemDbContext();
            var barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            return "logoClube" + barragemId;
        }

    }
}
