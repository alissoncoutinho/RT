using Barragem.Context;
using Barragem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Controllers
{
    public class MasculinaController : Controller
    {
        //
        // GET: /BarragemDoCerrado/

        private BarragemDbContext db = new BarragemDbContext();
        public ActionResult Index()
        {
            DateTime dtNow = DateTime.Now;
            TimeSpan tsMinute = new TimeSpan(0, 0, 59, 0);

            //Cria a estancia do obj HttpCookie passando o nome do mesmo
            HttpCookie cookie = new HttpCookie("_barragemId");
            cookie.Value = "1";
            cookie.Expires = dtNow + tsMinute;
            Response.Cookies.Add(cookie);

            BarragemView barragem = db.BarragemView.Find(1);
            HttpCookie cookieNome = new HttpCookie("_barragemNome");
            cookieNome.Value = barragem.nome;
            cookieNome.Expires = dtNow + tsMinute;
            Response.Cookies.Add(cookieNome);


            return RedirectToAction("IndexBarragens", "Home");
        }

    }
}
