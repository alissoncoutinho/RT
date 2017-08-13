using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Barragem.Models;
using Barragem.Context;
using System.Transactions;

namespace Barragem.Controllers
{
    public class ConfiguracaoController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();


        [Authorize(Roles = "admin")]
        public ActionResult Edit(int id = 0, string msg="")
        {
            if (msg.Equals("ok")){
                ViewBag.Ok = msg;
            }else if (!msg.Equals("")){
                ViewBag.MsgErro = msg;
            }
            Configuracao configuracao = db.Configuracao.Find(id);
            if (configuracao == null)
            {
                return HttpNotFound();
            }
            return View(configuracao);
        }

        [Authorize(Roles = "admin")]
        public ActionResult Resetar()
        {
            string mensagem="";
            try{
                using (TransactionScope scope = new TransactionScope()){
                    db.Database.ExecuteSqlCommand("Delete from Jogo");
                    db.Database.ExecuteSqlCommand("Delete from Rancking");
                    db.Database.ExecuteSqlCommand("Delete from Rodada where isRodadaCarga=0");
                    db.Database.ExecuteSqlCommand("update UserProfile set situacao='ativo' where situacao='suspenso'");
                    Rancking ranking = null;
                    List<Rodada> rodadas = db.Rodada.ToList();
                    List<UserProfile> jogadores = db.UserProfiles.ToList();
                    foreach (var rodada in rodadas) {
                        foreach (var jogador in jogadores) {
                            ranking = new Rancking();
                            ranking.rodada_id = rodada.Id;
                            ranking.pontuacao = 5.0;
                            ranking.posicao = 0;
                            ranking.totalAcumulado = 50;
                            ranking.userProfile_id = jogador.UserId;
                            db.Rancking.Add(ranking);
                            
                        }
                    }
                    db.SaveChanges();
                    scope.Complete();
                    mensagem = "ok";
                }
            }catch (Exception ex){
                mensagem = ex.Message;
            } 
           return RedirectToAction("Edit", "Configuracao",new{id=1,msg=mensagem});
        }

        //
        // POST: /Configuracao/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public ActionResult Edit(Configuracao configuracao)
        {
            if (ModelState.IsValid)
            {
                db.Entry(configuracao).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(configuracao);
        }


        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}