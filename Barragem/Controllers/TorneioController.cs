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
using System.Web.Security;
using WebMatrix.WebData;

namespace Barragem.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class TorneioController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        //

        [Authorize(Roles = "admin,organizador")]
        public ActionResult Index()
        {
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            List<Torneio> torneio = null;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            int barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            if (perfil.Equals("admin")){
                torneio = db.Torneio.OrderByDescending(c => c.Id).ToList();
            }else{
                torneio = db.Torneio.Where(r => r.barragemId == barragemId).OrderByDescending(c => c.Id).ToList();
            }

            return View(torneio);
        }

        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult TorneiosDisponiveis()
        {
            List<Torneio> torneio = null;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            int barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            torneio = db.Torneio.Where(r => r.isAtivo && (r.barragemId == barragemId || r.isOpen)).OrderByDescending(c => c.Id).ToList();
            

            return View(torneio);
        }


        [Authorize(Roles = "admin,organizador")]
        public ActionResult EfetuarSorteioFase1(int torneioId){
            try
            {
                db.Database.ExecuteSqlCommand("Delete from Jogo where torneioId=" + torneioId + "and faseTorneio=1");
                var torneio = db.Torneio.Find(torneioId);
                InscricaoTorneio jogador1 = null;
                InscricaoTorneio jogador2 = null;
                List<InscricaoTorneio> inscricoes = null;
                for (int i = 1; i <= torneio.qtddClasses; i++)
                {
                    inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classe == i && r.isAtivo).ToList();
                    int j = 1;
                    foreach (InscricaoTorneio inscricao in inscricoes)
                    {
                        jogador1 = selecionarAdversario(inscricoes);
                        jogador2 = selecionarAdversario(inscricoes);
                        var jogador2Id = jogador2.userId;
                        criarJogo(jogador1.userId, jogador2Id, torneioId, i, 1, j++);
                    }
                }
                return RedirectToAction("TabelaJogos", new { torneioId = torneioId, Msg = "OK" });
            } catch (Exception e) {
                return RedirectToAction("TabelaJogos", new { torneioId = torneioId, Msg = e.Message });
            }
            
            
        }

        public ActionResult EfetuarSorteioFaseRepescagem(int torneioId)
        {
            try
            {
                db.Database.ExecuteSqlCommand("Delete from Jogo where torneioId=" + torneioId + "and faseTorneio=2");
                var torneio = db.Torneio.Find(torneioId);
                InscricaoTorneio jogador1 = null;
                InscricaoTorneio jogador2 = null;
                List<InscricaoTorneio> inscricoes = null;
                for (int i = 1; i <= torneio.qtddClasses; i++)
                {
                    inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classe == i && r.isAtivo && r.colocacao == 101).ToList();
                    int j = 1;
                    foreach (InscricaoTorneio inscricao in inscricoes)
                    {
                        jogador1 = selecionarAdversario(inscricoes);
                        jogador2 = selecionarAdversario(inscricoes);
                        var jogador2Id = jogador2.userId;
                        criarJogo(jogador1.userId, jogador2Id, torneioId, i, 2, j++);
                    }
                }
                return RedirectToAction("TabelaJogos", new { torneioId = torneioId, Msg = "OK" });
            }
            catch (Exception e)
            {
                return RedirectToAction("TabelaJogos", new { torneioId = torneioId, Msg = e.Message });
            }


        }

        private int getQtddJogadoresFake(int qtddInscritos, int qtddJogos)
        {
            int qtddJogadores = qtddJogos * 2;
            return qtddJogadores - qtddInscritos;
        }

        private int informarQtddJogos(int qtddInscritos){
            var isPar = (qtddInscritos % 2 == 0);
            int jogosNormais = qtddInscritos/2;
            if (jogosNormais % 2 == 0){
                jogosNormais = jogosNormais++;
            }
            if (jogosNormais <=32 && jogosNormais>16){ // fase 1
                return 32;
            }else if (jogosNormais <=16 && jogosNormais>8){ // oitavas de final
                return 16;
            }else if (jogosNormais <=8 && jogosNormais>4){ // quartas de final
                return 8;
            }else if (jogosNormais <=4 && jogosNormais>4){ // semi-final
                return 4;
            }else if (jogosNormais <= 2 && jogosNormais > 4){ // final
                return 2;
            }else { return 0; }
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult EfetuarSorteioFase2(int torneioId)
        {
            try
            {
                db.Database.ExecuteSqlCommand("Delete from Jogo where torneioId=" + torneioId + "and faseTorneio=3");
                var torneio = db.Torneio.Find(torneioId);
                InscricaoTorneio jogador1 = null;
                InscricaoTorneio jogador2 = null;
                List<InscricaoTorneio> inscricoes = null;
                for (int i = 1; i <= torneio.qtddClasses; i++){
                    inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classe == i && r.isAtivo && (r.colocacao==null || r.colocacao==101)).ToList();
                    int quantidadeJogos = informarQtddJogos(inscricoes.Count());
                    int qtddJogadoresFake = getQtddJogadoresFake(inscricoes.Count(), quantidadeJogos);
                    int ordemJogos = quantidadeJogos;
                    for (int j = 1; j <= quantidadeJogos; j++){
                        var jogador2Id = 0;
                        jogador1 = selecionarAdversario(inscricoes);
                        if (qtddJogadoresFake > j){
                            jogador2Id = 0;
                        } else {
                            jogador2 = selecionarAdversario(inscricoes);
                            jogador2Id = jogador2.userId;
                        }
                        criarJogo(jogador1.userId, jogador2Id, torneioId, i, 1, ordemJogos--);
                    }
                    int qtddRodadas = getQtddRodadas(quantidadeJogos);
                    int numeroRodada = 2;
                    for (int k = qtddRodadas; k >= 1; k--) {
                        var qtddJogosPorRodada = getQtddJogosPorRodada(k);
                        for (int y = 1; y <= qtddJogosPorRodada; y++){
                            criarJogo(0, 0, torneioId, i, numeroRodada, y);
                        }
                        numeroRodada++;
                    }
                }
                return RedirectToAction("TabelaJogos", new { torneioId = torneioId, Msg = "OK" });
            }
            catch (Exception e)
            {
                return RedirectToAction("TabelaJogos", new { torneioId = torneioId, Msg = e.Message });
            }


        }

        private int getQtddJogosPorRodada(int numeroRodada) {
            if (numeroRodada == 6) {
                return 32;
            }else if(numeroRodada == 5) {
                return 16;
            }else if(numeroRodada == 4) {
                return 8;
            }else if(numeroRodada == 3) {
                return 4;
            }else if (numeroRodada == 2) {
                return 2;
            }else if (numeroRodada == 1) {
                return 1;
            }else {
                return 0;
            }
        }

        private int getQtddRodadas(int qtddJogos)
        {
            if (qtddJogos == 64){
                return 6;
            }else if (qtddJogos == 32) {
                return 5;
            }else if (qtddJogos == 16) {
                return 4;
            }else if (qtddJogos == 8) {
                return 3;
            }else if (qtddJogos == 4) {
                return 2;
            }else if (qtddJogos == 2) {
                return 1;
            }else {
                return 0;
            }
        }

        private void criarJogo(int jogador1, int jogador2, int torneioId, int classeTorneio, int faseTorneio, int ordemJogo){
            Jogo jogo = new Jogo();
            jogo.desafiado_id = jogador1;
            jogo.desafiante_id = jogador2;
            jogo.torneioId = torneioId;
            jogo.situacao_Id = 1;
            jogo.classeTorneio = classeTorneio;
            jogo.faseTorneio = faseTorneio;
            jogo.ordemJogo = ordemJogo;
            if ((jogador2==0)&&(jogador1!=0)){
                jogo.situacao_Id = 4;
                jogo.qtddGames1setDesafiado = 6;
                jogo.qtddGames2setDesafiado = 6;
                jogo.qtddGames1setDesafiante = 0;
                jogo.qtddGames2setDesafiante = 0;

            }
            db.Jogo.Add(jogo);
            db.SaveChanges();
        }

        private InscricaoTorneio selecionarAdversario(List<InscricaoTorneio> participantes){
            var adversario = new InscricaoTorneio();
            if (participantes.Count == 0){
                adversario.Id = 0;
                return adversario;
            } else if (participantes.Count == 1) {
                adversario = participantes[0]; //add it 
                participantes.RemoveAt(0);
                return adversario;
            }
            Random r = new Random();
            int randomIndex = r.Next(1, participantes.Count); //Choose a random object in the list
            adversario = participantes[randomIndex]; //add it 
            participantes.RemoveAt(randomIndex);
            return adversario;
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult TabelaJogos(int torneioId, string Msg = ""){
            var jogos = db.Jogo.Where(r => r.torneioId == torneioId).OrderBy(r => r.faseTorneio).ThenBy(r => r.ordemJogo).ToList();
            var rodada = jogos.Where(r => r.torneioId == torneioId).Max(r => r.faseTorneio);
            var JogosFase2 = jogos.Where(r => r.faseTorneio == 2).ToList();
            ViewBag.JogosFase3 = jogos.Where(r => r.faseTorneio == 3).ToList();
            ViewBag.JogosFase4 = jogos.Where(r => r.faseTorneio == 4).ToList();
            ViewBag.JogosFase5 = jogos.Where(r => r.faseTorneio == 5).ToList();
            ViewBag.JogosFase6 = jogos.Where(r => r.faseTorneio == 6).ToList();

            var x= jogos.Count();

            var y = JogosFase2.Count();
            ViewBag.JogosFase2 = JogosFase2;
            mensagem(Msg);
            ViewBag.QtddRodada = rodada;
            ViewBag.torneioId = torneioId;
            return View(jogos.Where(r => r.faseTorneio==1).ToList());
        }

        private void mensagem(string Msg){
            if (Msg != ""){
                if (Msg == "OK"){
                    ViewBag.Ok = "Operação realizada com sucesso!";
                }else{
                    ViewBag.MsgErro = Msg;
                }
            }
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult InscricoesTorneio(int torneioId, string Msg="")
        {
            var inscricoes = db.InscricaoTorneio.Where(r => r.torneioId==torneioId).OrderBy(r => r.classe).ThenBy(r=>r.participante.nome).ToList();

            mensagem(Msg);
            
            return View(inscricoes);
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult Edit(int id = 0)
        {
            Torneio torneio = db.Torneio.Find(id);
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            var barragemId = 0;
            if (perfil.Equals("admin"))
            {
                barragemId = torneio.barragemId;
            }
            else
            {
                barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            }
            ViewBag.barraId = barragemId;
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome", barragemId);
            ViewBag.JogadoresClasses = db.RankingView.Where(r => r.barragemId == barragemId && (r.situacao.Equals("ativo") || r.situacao.Equals("suspenso") || r.situacao.Equals("licenciado"))).OrderBy(r => r.nivel).ThenByDescending(r => r.totalAcumulado).ToList();
            ViewBag.Classes = db.Classe.Where(c => c.barragemId == barragemId).ToList();
            ViewBag.temRodadaAberta = db.Rodada.Where(u => u.isAberta && u.barragemId == barragemId && !u.isRodadaCarga).Count();

            if (torneio == null)
            {
                return HttpNotFound();
            }
            return View(torneio);
        }

        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult Detalhes(int id = 0, String Msg="")
        {
            Torneio torneio = db.Torneio.Find(id);
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            ViewBag.isInscrito = false;
            var inscricao = db.InscricaoTorneio.Where(i => i.torneioId == id && i.userId == userId).Count();
            if (inscricao>0){
                ViewBag.isInscrito = true;
            }
            var jogador = db.UserProfiles.Find(userId);
            ViewBag.ClasseInscricao = 0;
            ViewBag.valor = torneio.valor+"";
            if (jogador.barragemId == torneio.barragemId){
                var ranking = db.Rancking.Where(r => r.userProfile_id == userId).Count();
                if (ranking > 10) {
                    ViewBag.valor = "gratuito";
                }
                if (jogador.classe.nivel <= torneio.qtddClasses){
                    ViewBag.ClasseInscricao = jogador.classe.nivel;
                }
            }
            mensagem(Msg);
            
            return View(torneio);
        }

        [Authorize(Roles = "admin,usuario,organizador")]
        [HttpPost]
        public ActionResult Inscricao(int torneioId, int classeInscricao, int vrInscricao)
        {
            try
            {
                var userId = WebSecurity.GetUserId(User.Identity.Name);
                var isInscricao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.userId == userId).Count();
                if (isInscricao>0){
                    InscricaoTorneio it = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.userId == userId).Single();
                    db.InscricaoTorneio.Remove(it);
                }else{
                    InscricaoTorneio inscricao = new InscricaoTorneio();
                    inscricao.classe = classeInscricao;
                    inscricao.torneioId = torneioId;
                    inscricao.userId = userId;
                    if(vrInscricao>0){
                        inscricao.isAtivo = false;
                    }else{
                        inscricao.isAtivo = true;
                    }
                    db.InscricaoTorneio.Add(inscricao);
                }
                db.SaveChanges();
                return RedirectToAction("Detalhes", new {id=torneioId,Msg="OK"});
            }catch (Exception ex){
                return RedirectToAction("Detalhes", new { id = torneioId, Msg = ex.Message });
            }
        }
        [Authorize(Roles = "admin, organizador")]
        public ActionResult Ativar(int inscricaoId)
        {
            var inscricao = db.InscricaoTorneio.Find(inscricaoId);
            try
            {
                if (inscricao.isAtivo){
                    inscricao.isAtivo = false;
                }else{
                    inscricao.isAtivo = true;
                }
                db.SaveChanges();
                return RedirectToAction("InscricoesTorneio", new { torneioId = inscricao.torneioId, Msg = "OK" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("InscricoesTorneio", new { torneioId = inscricao.torneioId, Msg = ex.Message });
            }
        }

        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Torneio torneio)
        {
            if (ModelState.IsValid)
            {
                db.Entry(torneio).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(torneio);
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult Create()
        {
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            var barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            ViewBag.barraId = barragemId;
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");

            return View();
        }

        //
        // POST: /Rodada/Create

        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Torneio torneio)
        {
            if (ModelState.IsValid)
            {

                db.Torneio.Add(torneio);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                var userId = WebSecurity.GetUserId(User.Identity.Name);
                var barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
                ViewBag.barraId = barragemId;
                ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");
            }

            return View(torneio);
        }

    }
}
