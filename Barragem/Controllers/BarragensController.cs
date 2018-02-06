using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Barragem.Models;
using Barragem.Context;
using Barragem.Filters;
using System.Data.EntityClient;
using System.Transactions;
using Barragem.Class;
using WebMatrix.WebData;
using System.Web.Security;

namespace Barragem.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class BarragensController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();

        //
        // GET: /Rodada/

        [Authorize(Roles = "admin,organizador")]
        public ActionResult Index(string msg = "", string detalheErro = "")
        {
            if (msg.Equals("ok"))
            {
                ViewBag.Ok = msg;
            }
            else if (!msg.Equals(""))
            {
                ViewBag.MsgErro = msg;
                ViewBag.DetalheErro = detalheErro;
            }
            List<Barragens> barragens = null;
            
            if (Roles.IsUserInRole("organizador")){
                UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                barragens = new List<Barragens>();
                barragens.Add(db.Barragens.Find(usu.barragemId));
            }else{
                barragens = db.Barragens.ToList();
            }
            return View(barragens);
        }

        [Authorize(Roles = "admin")]
        public ActionResult Create()
        {
            return View();
        }

       
        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Barragens barragens)
        {
            try
            {
                var codigo=91;
                var sql="";
                using (TransactionScope scope = new TransactionScope())
                {
                    if (ModelState.IsValid){
                        if (!barragens.email.Equals("")) { 
                            if (!Funcoes.IsValidEmail(barragens.email)){
                                ViewBag.MsgErro = string.Format("E-mail inválido. '{0}'", barragens.email);
                                return View(barragens);
                            }
                        }
                        var meuRanking = db.Barragens.Find(8);
                        barragens.regulamento = meuRanking.regulamento;
                        db.Barragens.Add(barragens);
                        db.SaveChanges();
                        for (int i = 1; i <= 10; i++){
                            sql = "INSERT INTO Rodada(codigo, dataInicio, dataFim, isAberta, sequencial, isRodadaCarga, barragemId) " +
                            "VALUES (" + codigo + ",'2000-01-01','2000-01-01', 0, "+ i +", 1, " + barragens.Id + ")";
                            db.Database.ExecuteSqlCommand(sql);
                            codigo = codigo + 1;
                        }
                        for (int i = 1; i <= 5; i++){
                            sql = "INSERT INTO Classe (nome, nivel, barragemId) VALUES ('"+ i +"ª Classe',"+ i +", " + barragens.Id + ")";
                            db.Database.ExecuteSqlCommand(sql);
                        }
                        scope.Complete();
                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.MsgErro = ex.Message;
            }
            
            return View(barragens);
        }

        // GET: /Rodada/Edit/5
        [Authorize(Roles = "admin,organizador")]
        public ActionResult Edit(int id = 0)
        {
            Barragens barragens = db.Barragens.Find(id);
            if (barragens == null)
            {
                return HttpNotFound();
            }
            return View(barragens);
        }

        //
        // POST: /Rodada/Edit/5
        [Authorize(Roles = "admin,organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Barragens barragens)
        {
            if (Roles.IsUserInRole("organizador")){
                UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                if (usu.barragemId != barragens.Id){
                    ViewBag.MsgErro = "Você não pertence a esta barragem.";
                    return View(barragens);
                }
            }
            if (ModelState.IsValid){
                if (barragens.email!=null){
                    if (!Funcoes.IsValidEmail(barragens.email)){
                        ViewBag.MsgErro = string.Format("E-mail inválido. '{0}'", barragens.email);
                        return View(barragens);
                    }
                }
                db.Entry(barragens).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(barragens);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public ActionResult Delete(int id)
        {
            Barragens barragens = db.Barragens.Find(id);
            db.Barragens.Remove(barragens);
            db.SaveChanges();
            return RedirectToAction("Index");
        }



        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}