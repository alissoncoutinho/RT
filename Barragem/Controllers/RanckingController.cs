using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Barragem.Models;
using Barragem.Context;
using WebMatrix.WebData;

namespace Barragem.Controllers
{
    public class RanckingController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();

        //
        // GET: /Rancking/

        public ActionResult Index(int id=0)
        {
            List<Rancking> rancking;
            int barragemId = 0;
            try{
                if (id == 0){
                    UserProfile usuario = null;
                    if (User.Identity.Name != ""){
                        usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                    }
                    if (usuario == null){
                        if (Request.Cookies["_barragemId"] != null){
                            HttpCookie cookie = new HttpCookie("_barragemId");
                            barragemId = Convert.ToInt32(cookie.Value.ToString());
                        }else{
                            barragemId = 1;
                        }
                    } else {
                        barragemId = usuario.barragemId;
                    }
                    id = db.Rancking.Where(r => r.rodada.isAberta == false && r.rodada.isRodadaCarga == false && r.rodada.barragemId == barragemId).Max(r => r.rodada_id);
                                        
                }
            }catch (InvalidOperationException){
                
            }
            rancking = db.Rancking.Include(r => r.userProfile).Include(r => r.rodada).
                Where(r => r.rodada_id == id && r.posicao > 0 && r.userProfile.situacao != "desativado" && r.userProfile.situacao != "inativo").OrderBy(r=>r.classe.nivel).ThenBy(r => r.posicao).ToList();
            ViewBag.Classes = db.Classe.Where(c => c.barragemId == barragemId).ToList();
            ViewBag.rankingGeral = rancking.OrderBy(r => r.posicao).ToList();
            if (rancking.Count() > 0){
                var barragem = rancking[0].rodada.barragemId;
                ViewBag.Rodada = rancking[0].rodada.codigoSeq;
                ViewBag.RodadaId = rancking[0].rodada.Id;
                ViewBag.dataRodada = (rancking[0].rodada.dataFim + "").Substring(0, 10);

                if (rancking[0].rodada.temporadaId != null){
                    var temporadaId = rancking[0].rodada.temporadaId;
                    var rodadaId = rancking[0].rodada.Id;
                    var qtddRodada = db.Rodada.Where(r => r.temporadaId == temporadaId && r.Id <= rodadaId && r.barragemId==barragem).Count();
                    ViewBag.Temporada = rancking[0].rodada.temporada.nome + " - Rodada " + qtddRodada + " de " + 
                        rancking[0].rodada.temporada.qtddRodadas;
                } else { ViewBag.Temporada = ""; }
            }

            return View(rancking);
        }

        
       public ActionResult listarRancking(int idRodadaAtual) {
           UserProfile usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
           int barragemId = 0;
           if (usuario==null){
               if (Request.Cookies["_barragemId"] != null){
                   HttpCookie cookie = new HttpCookie("_barragemId");
                   barragemId = Convert.ToInt32(cookie.Value.ToString());
               } else {
                   barragemId = 1; 
               }
           } else {
               barragemId = usuario.barragemId;
           }
           var listaRancking = db.Rodada.Where(c=>c.isAberta==false && c.isRodadaCarga==false && c.Id!=idRodadaAtual && c.barragemId == barragemId).OrderByDescending(c=>c.Id).ToList();
           return View(listaRancking);
        }

        public ActionResult historicoRanking(int idJogador)
        {
            List<Rancking> ranckingJogador = db.Rancking.Where(r => r.userProfile_id == idJogador && r.rodada.isRodadaCarga == false).OrderByDescending(r => r.rodada_id).ToList();
            return View(ranckingJogador);
        }

        
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}