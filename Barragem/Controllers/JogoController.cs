using System;
using System.Data;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebMatrix.WebData;
using Barragem.Filters;
using Barragem.Models;
using Barragem.Context;
using System.Web.Security;
using System.Transactions;
using Barragem.Class;

namespace Barragem.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class JogoController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        private RodadaNegocio rn = new RodadaNegocio();
        //
        // GET: /Jogo/

        [Authorize(Roles = "admin, organizador, usuario")]
        public ActionResult Index(int rodadaId=0)
        {
            string msg = "";
            try{
                if (rodadaId == 0)
                {
                    UserProfile usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                    rodadaId = db.Rodada.Where(r=> r.isRodadaCarga==false && r.barragemId==usuario.barragemId).Max(r => r.Id);
                }
            }catch (InvalidOperationException) { }
            try
            {
                var jogo = db.Jogo.Include(j => j.desafiado).Include(j => j.desafiante).Include(j => j.rodada).
                    Where(j => j.rodada_id == rodadaId).OrderBy(j => j.desafiado.classe.nivel).ToList();
                msg = "jogos";
                int barragemId = jogo[0].rodada.barragemId;
                msg = "barragemId";
                ViewBag.Classes = db.Classe.Where(c => c.barragemId == barragemId).ToList();
                if (jogo.Count() > 0)
                {
                    ViewBag.Rodada = jogo[0].rodada.codigoSeq;
                    ViewBag.DataInicial = jogo[0].rodada.dataInicio;
                    ViewBag.DataFinal = jogo[0].rodada.dataFim;
                    msg = "rodada";
                    if (jogo[0].rodada.temporadaId != null){
                        var temporadaId = jogo[0].rodada.temporadaId;
                        var qtddRodada = db.Rodada.Where(r => r.temporadaId == temporadaId && r.Id<=rodadaId && r.barragemId==barragemId).Count();
                        ViewBag.Temporada = jogo[0].rodada.temporada.nome + " - Rodada " + qtddRodada + " de " + jogo[0].rodada.temporada.qtddRodadas;
                        msg = "temporada";
                    }
                    else { ViewBag.Temporada = ""; }
                }
                return View(jogo);
            }catch (Exception ex){
                ViewBag.MsgErro = msg +" - " + ex.Message;
                return View();
            }
            
        }

        //
        // GET: /Jogo/Details/5

        [Authorize(Roles = "admin, organizador, usuario")]
        public ActionResult ListarJogosJogador(int idJogador=0)
        {
            if (idJogador == 0){
                UserProfile usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                idJogador = usuario.UserId;
            }
            List<Jogo> jogos = db.Jogo.Include(j => j.desafiado).Include(j => j.desafiante).Include(j => j.rodada).Where(j => j.desafiante_id == idJogador || j.desafiado_id == idJogador).OrderByDescending(j=>j.rodada_id).ToList();
            return View(jogos);
        }

        
        //
        // GET: /Jogo/Create
        [Authorize(Roles = "admin, organizador")]
        public ActionResult Create()
        {
            ViewBag.desafiado_id = new SelectList(db.UserProfiles, "UserId", "UserName");
            ViewBag.desafiante_id = new SelectList(db.UserProfiles, "UserId", "UserName");
            ViewBag.rodada_id = new SelectList(db.Rodada, "Id", "codigo");
            return View();
        }

        //
        // POST: /Jogo/Create
        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Jogo jogo)
        {
            if (ModelState.IsValid)
            {
                db.Jogo.Add(jogo);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.desafiado_id = new SelectList(db.UserProfiles, "UserId", "UserName", jogo.desafiado_id);
            ViewBag.desafiante_id = new SelectList(db.UserProfiles, "UserId", "UserName", jogo.desafiante_id);
            ViewBag.rodada_id = new SelectList(db.Rodada, "Id", "codigo", jogo.rodada_id);
            return View(jogo);
        }

        //
        // GET: /Jogo/Edit/5
        [Authorize(Roles = "admin, organizador")]
        public ActionResult Edit(int id = 0)
        {
            Jogo jogo = db.Jogo.Find(id);
            if (jogo == null){
                return HttpNotFound();
            }
            ViewBag.desafiado_id = new SelectList(db.UserProfiles.Where(u => u.situacao == "ativo" && u.barragemId == jogo.rodada.barragemId).OrderBy(u => u.nome), "UserId", "nome", jogo.desafiado_id);
            ViewBag.desafiante_id = new SelectList(db.UserProfiles.Where(u => u.situacao == "ativo" && u.barragemId == jogo.rodada.barragemId).OrderBy(u => u.nome), "UserId", "nome", jogo.desafiante_id);
            ViewBag.rodada_id = new SelectList(db.Rodada, "Id", "codigo", jogo.rodada_id);
            return View(jogo);
        }

        //
        // POST: /Jogo/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin, organizador")]
        public ActionResult Edit(Jogo jogo)
        {
            if (ModelState.IsValid)
            {
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.desafiado_id = new SelectList(db.UserProfiles, "UserId", "UserName", jogo.desafiado_id);
            ViewBag.desafiante_id = new SelectList(db.UserProfiles, "UserId", "UserName", jogo.desafiante_id);
            ViewBag.rodada_id = new SelectList(db.Rodada, "Id", "codigo", jogo.rodada_id);
            return View(jogo);
        }

        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult ControlarJogo(int id=0)
        {
            Jogo jogo = null;
            if (id == 0){
                try{
                    var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                    jogo = db.Jogo.Where(u => u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId)
                        .OrderByDescending(u=>u.Id).Take(1).Single();
                }catch (System.InvalidOperationException e){
                    ViewBag.MsgAlert = "Não foi possível encontrar jogos em aberto:" + e.Message;
                }
            }else{
                jogo = db.Jogo.Find(id);
            }
            if (jogo != null){
                //nao permitir edição caso a rodada já esteja fechada e o placar já tenha sido informado
                string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                if (!perfil.Equals("admin") && !perfil.Equals("organizador") && (jogo.rodada.isAberta==false) && (jogo.gamesJogados!=0)){
                    ViewBag.Editar = false;
                }else {
                    ViewBag.Editar = true;
                }
                ViewBag.situacao_Id = new SelectList(db.SituacaoJogo, "Id", "descricao", jogo.situacao_Id);
                ViewBag.ptDefendidosDesafiado = getPontosDefendidos(jogo.desafiado_id, jogo.rodada_id);
                ViewBag.ptDefendidosDesafiante = getPontosDefendidos(jogo.desafiante_id, jogo.rodada_id);

            }
            return View(jogo);
        }


        private double getPontosDefendidos(int UserId, int rodada_id) {
            double pontosDefendidos = 0;
            List<Rancking> listRanking = db.Rancking.Where(r => r.userProfile_id == UserId && r.rodada_id < rodada_id).Take(10).OrderByDescending(r =>r.Id).ToList();
            foreach (Rancking ranking in listRanking)
            {
                pontosDefendidos = ranking.pontuacao;
            }
            
            
            return pontosDefendidos;
        }

        //
        // POST: /Jogo/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult ControlarJogo(Jogo jogo)
        {
            int setDesafiante = 0;
            int setDesafiado = 0;
            if (ModelState.IsValid){
                jogo.dataCadastroResultado = DateTime.Now;
                jogo.usuarioInformResultado = User.Identity.Name;
                if (jogo.desafiado_id == WebSecurity.GetUserId(User.Identity.Name)){
                    setDesafiado=6;
                    setDesafiante=1;
                }else{
                    setDesafiado=1;
                    setDesafiante = 6;
                }
                jogo.idVencedor = 0;
                jogo.idPerderdor = 0;
                if (jogo.situacao_Id == 5) { //WO
                    jogo.qtddGames1setDesafiado = setDesafiado;
                    jogo.qtddGames1setDesafiante = setDesafiante;
                    jogo.qtddGames2setDesafiado = setDesafiado;
                    jogo.qtddGames2setDesafiante = setDesafiante;
                }
                if (jogo.situacao_Id == 1 || jogo.situacao_Id == 2) // pendente ou marcado
                {
                    jogo.qtddGames1setDesafiado = 0;
                    jogo.qtddGames1setDesafiante = 0;
                    jogo.qtddGames2setDesafiado = 0;
                    jogo.qtddGames2setDesafiante = 0;
                    jogo.qtddGames3setDesafiado = 0;
                    jogo.qtddGames3setDesafiante = 0;
                }
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
                ViewBag.Ok = "ok";
            }else{
                ViewBag.MsgAlert = "Não foi possível alterar os dados.";
            }
            
            jogo = db.Jogo.Include(j => j.rodada).Include(j => j.desafiado).Include(j => j.desafiante).Where(j => j.Id == jogo.Id).Single();
            
            ViewBag.situacao_Id = new SelectList(db.SituacaoJogo, "Id", "descricao", jogo.situacao_Id);
            
            ProcessarJogoAtrasado(jogo);

            return RedirectToAction("Index2", "Home");
            
        }

        private void ProcessarJogoAtrasado(Jogo jogo){
            string msg = "";
            //situação: 4: finalizado -- 5: wo
            //List<Jogo> jogos = db.Jogo.Where(r => r.rodada_id == id && r.dataCadastroResultado > r.rodada.dataFim && (r.situacao_Id == 4 || r.situacao_Id == 5)).ToList();
            if (jogo.dataCadastroResultado > jogo.rodada.dataFim && (jogo.situacao_Id == 4 || jogo.situacao_Id == 5))
            {
                var pontosDesafiante = 0.0;
                var pontosDesafiado = 0.0;
                try{
                    using (TransactionScope scope = new TransactionScope()){
                        pontosDesafiante = rn.calcularPontosDesafiante(jogo);
                        pontosDesafiado = rn.calcularPontosDesafiado(jogo);

                        rn.gravarPontuacaoNaRodada(jogo.rodada_id, jogo.desafiante, pontosDesafiante, true);
                        rn.gravarPontuacaoNaRodada(jogo.rodada_id, jogo.desafiado, pontosDesafiado, true);
                        jogo.dataCadastroResultado = jogo.rodada.dataFim;
                        if (jogo.desafiante.situacao.Equals("suspenso")){
                            UserProfile desafiante = db.UserProfiles.Find(jogo.desafiante_id);
                            desafiante.situacao = "ativo";
                        }
                        if (jogo.desafiado.situacao.Equals("suspenso")){
                            UserProfile desafiado = db.UserProfiles.Find(jogo.desafiado_id);
                            desafiado.situacao = "ativo";
                        }
                        db.SaveChanges();
                        scope.Complete();
                        msg = "ok";
                    }
                }catch (Exception ex){
                    msg = ex.Message;
                }
            }
          }

        //
        // GET: /Jogo/Delete/5
        [Authorize(Roles = "admin, organizador")]
        public ActionResult Delete(int id = 0)
        {
            Jogo jogo = db.Jogo.Find(id);
            if (jogo == null)
            {
                return HttpNotFound();
            }
            return View(jogo);
        }

        //
        // POST: /Jogo/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin, organizador")]
        public ActionResult DeleteConfirmed(int id)
        {
            Jogo jogo = db.Jogo.Find(id);
            db.Jogo.Remove(jogo);
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