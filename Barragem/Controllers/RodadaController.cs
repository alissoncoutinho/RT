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
    public class RodadaController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        private RodadaNegocio rn = new RodadaNegocio();
        string erroSorteio = "";
        //
        // GET: /Rodada/

        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult Index(string msg="", string detalheErro ="")
        {
            if (msg.Equals("ok")){
                ViewBag.Ok = msg;
            }else if (!msg.Equals("")){
                ViewBag.MsgErro = msg;
                ViewBag.DetalheErro = detalheErro;
            }
            
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            List<Rodada> rodada = null;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            int barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            //if (perfil.Equals("admin")||perfil.Equals("organizador")){
            //    var sqlJogos = db.Jogo.Where(r => r.dataCadastroResultado > r.rodada.dataFim && (r.situacao_Id == 4 || r.situacao_Id == 5));
            //    if (perfil.Equals("organizador")){
            //        sqlJogos = sqlJogos.Where(r => r.rodada.barragemId == barragemId);
            //    }
            //    List<Jogo> jogos = sqlJogos.ToList();
            //    foreach (Jogo jogo in jogos){
            //        ViewBag.Reprocessar = ViewBag.Reprocessar + " - " + jogo.rodada.codigoSeq;
            //    }
            //}
            if (perfil.Equals("admin")){
                rodada = db.Rodada.Where(r => r.isRodadaCarga == false).OrderByDescending(c => c.Id).ToList();
            } else {
                rodada = db.Rodada.Where(r => r.isRodadaCarga == false && r.barragemId==barragemId).OrderByDescending(c => c.Id).ToList();
            }
            
            return View(rodada);
        }
       
        //
        // GET: /Rodada/Create

        [Authorize(Roles = "admin, organizador")]
        public ActionResult Create(int barragemId=0)
        {
            try
            {
                if (barragemId == 0){
                    var userId = WebSecurity.GetUserId(User.Identity.Name);
                    barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
                }
                ViewBag.barraId = barragemId;
                ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");

                ViewBag.temporadaId = new SelectList(db.Temporada.Where(c => c.isAtivo == true && c.barragemId == barragemId), "Id", "nome");

            }catch (InvalidOperationException) {
                ViewBag.sequencial = 1;
                ViewBag.codigo = "A";
            }
            return View();
        }

        //
        // POST: /Rodada/Create

        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Rodada rodada)
        {
            if (ModelState.IsValid)
            {
                List<Rodada> rodadas = db.Rodada.Where(r => r.isAberta == true && r.barragemId==rodada.barragemId).ToList();

                if (rodadas.Count() > 0) {
                    var mensagem = "Não foi possível criar uma nova rodada, pois ainda existe rodada(s) em aberto.";
                    return RedirectToAction("Index", new { msg = mensagem });
                }
                try{
                    Rodada rd = db.Rodada.Where(r => r.barragemId == rodada.barragemId).OrderByDescending(r => r.Id).Take(1).Single();
                    if (rd.sequencial == 10){
                        string alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                        int pos = alfabeto.IndexOf(rd.codigo);
                        pos++;
                        rodada.sequencial = 1;
                        rodada.codigo = Convert.ToString(alfabeto[pos]);
                    } else {
                        rodada.sequencial = rd.sequencial + 1;
                        rodada.codigo = rd.codigo;
                    }
                }catch (InvalidOperationException){
                    rodada.sequencial = 1;
                    rodada.codigo = "A";
                }
                rodada.isAberta = true;
                rodada.dataFim = new DateTime(rodada.dataFim.Year, rodada.dataFim.Month, rodada.dataFim.Day, 23, 59, 59);
                db.Rodada.Add(rodada);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(rodada);
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult SortearJogos(int id, int barragemId)
        {
            string mensagem = "ok";
            try{
                List<Classe> classes = db.Classe.Where(c=>c.barragemId==barragemId).ToList();
                for (int i = 0; i < classes.Count(); i++){
                    EfetuarSorteio(id, barragemId, classes[i].Id);
                }
            }catch (Exception e){
                mensagem = mensagem + " - " + e.Message;
            }
            return RedirectToAction("Index", new { msg=mensagem});
        }

        private List<RankingView> selecionarJogadorParaFicarFora(List<RankingView> jogadores, int rodadaAnterior, int rodadaAtual, int classeId){
            
            List<Jogo> jogo = db.Jogo.Where(j => j.rodada_id == rodadaAnterior && j.situacao_Id != 4 && j.situacao_Id != 5 && j.desafiado.classeId==classeId).ToList();
            UserProfile jogador = null;
            RankingView rv = null;
            for (int i = 0; i<jogo.Count(); i++){
                if ((rodadaAnterior % 2 == 0) && (jogo[i].desafiante.situacao.Equals("ativo"))){
                    jogador = jogo[i].desafiante;
                }else if ((jogo[i].desafiado.situacao.Equals("ativo"))){
                    jogador = jogo[i].desafiado;
                }
            }
            UserProfile curinga = db.UserProfiles.Where(u => u.situacao.Equals("curinga")).Single();
            if (jogador != null){
                criarJogo(jogador.UserId,curinga.UserId, rodadaAtual,true);
                var itemToRemove = jogadores.SingleOrDefault(r => r.userProfile_id == jogador.UserId);
                if (itemToRemove != null)
                    jogadores.Remove(itemToRemove);
            } else {
                criarJogo(jogadores[0].userProfile_id, curinga.UserId, rodadaAtual, true);
                rv = jogadores[0];
                jogadores.Remove(rv);
            }
            
            return jogadores;
        }

        private void EfetuarSorteio(int idRodada, int barragemId, int classeId){
            // excluir os jogos já sorteados para o caso de estar sorteando novamente
            db.Database.ExecuteSqlCommand("DELETE j fROM jogo j INNER JOIN UserProfile u ON j.desafiado_id=u.UserId WHERE u.classeId = " + classeId + " AND j.rodada_id =" + idRodada);
            // monta a lista ordenada pelo último rancking consolidado
            int Id_rodadaAnterior = db.Rancking.Where(r=>r.rodada.isAberta==false && r.rodada_id<idRodada && r.rodada.barragemId==barragemId).Max(r => r.rodada_id);
            List<RankingView> jogadores = db.RankingView.Where(r => r.barragemId == barragemId && r.classeId == classeId && r.situacao.Equals("ativo")).OrderByDescending(r=>r.totalAcumulado).ToList();

            // se a quantidade de participantes ativos for impar o sistema escolherá, 
            // de acordo com a regra estabelecida, um jogador para ficar de fora
            if (jogadores.Count % 2 != 0){
                jogadores = selecionarJogadorParaFicarFora(jogadores, Id_rodadaAnterior, idRodada, classeId);
            }
            // o mais bem posicionado no ranking será desafiado por alguém pior posicionado
            RankingView desafiante = null;
            while (jogadores.Count > 0){
                RankingView desafiado = jogadores[0];
                desafiante = selecionarAdversario(jogadores, desafiado, Id_rodadaAnterior);
                criarJogo(desafiado.userProfile_id,desafiante.userProfile_id, idRodada);
            }
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult notificarGeracaoRodada(int Id)
        {
            Rodada rodada = db.Rodada.Find(Id);
            try
            {
                Mail mail = new Mail();
                mail.de = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
                mail.para = "barragemdocerrado@gmail.com";
                mail.assunto = "BDC - Nova Rodada - " + rodada.codigoSeq;
                mail.conteudo = "Olá Pessoal,<br><br>A nova rodada " + rodada.codigoSeq +" já está disponível no site e vai até o dia:" + rodada.dataFim + ".<br><br>Bons jogos a todos.";
                mail.formato = Tipos.FormatoEmail.Html;
                List<UserProfile> users = db.UserProfiles.Where(u => u.situacao == "ativo"  && u.barragemId==rodada.barragemId).ToList();
                List<string> bcc = new List<string>();
                foreach (UserProfile user in users)
                {
                    bcc.Add(user.email);
                }
                mail.bcc = bcc;
                mail.EnviarMail();
            } catch (Exception ex) { }
            return RedirectToAction("Index", new { msg = "ok" });
        }

        private RankingView selecionarAdversario(List<RankingView> listaJogadores, RankingView desafiado, int rodadaAnteriorId)
        {
            RankingView desafiante = null;
            if (listaJogadores.Count() == 2){
                desafiante = listaJogadores[1];
                listaJogadores.RemoveAt(1);
                listaJogadores.RemoveAt(0);
                return desafiante;
            }
            // O jogador não deve repetir o mesmo jogo das 3 últimas rodadas

            // busca os 3 últimos jogos do desafiado
            List<Jogo> jogosAnteriores = db.Jogo.Where(j => (j.rodada_id <= rodadaAnteriorId &&
                    (j.desafiado_id == desafiado.userProfile_id || j.desafiante_id == desafiado.userProfile_id))).
                    Take(3).OrderByDescending(j => j.Id).ToList();
            for (int i = 0; i <= 2; i++){
                // busca um oponente mais próximo que não tenha jogado nos últimos 3 jogos ou nos últimos 2 jogos ou no último jogo
                desafiante = buscarOponentesNaoRepetidos(listaJogadores, jogosAnteriores,i);
                if (desafiante != null){
                    return desafiante;
                }
            }
            // caso não encontre em nenhuma situação acima, realiza um sorteio
            Random r = new Random();
            int randomIndex = r.Next(1, listaJogadores.Count); //Choose a random object in the list
            desafiante = listaJogadores[randomIndex]; //add it 
            listaJogadores.RemoveAt(randomIndex);
            listaJogadores.RemoveAt(0);
            return desafiante;
        }

        private UserProfile selecionarDesafiante(List<Rancking> listaJogadores, UserProfile desafiado, int rodadaAnteriorId)
        {
            UserProfile desafiante = null;
            // caso a regra: selecionarJogadorParaFicarFora não tenha selecionado nehum jogador
            // o último da lista ficará de fora da rodada caso o número seja impar
            if (listaJogadores.Count() == 1){
                UserProfile curinga = db.UserProfiles.Where(u => u.situacao.Equals("curinga")).Single();
                listaJogadores.RemoveAt(0);
                return curinga;
                // caso só reste duas opções não há mais como aplicar as regras
            }else if (listaJogadores.Count() == 2){
                desafiante = listaJogadores[1].userProfile;
                listaJogadores.RemoveAt(1);
                listaJogadores.RemoveAt(0);
                return desafiante;
            }
            // o jogador deve jogar com o oponente mais próximo do ranking 
            // porém o jogador não deve repetir o mesmo jogo das 3 últimas rodadas

            // busca os 3 últimos jogos do desafiado
            List<Jogo> jogosAnteriores = db.Jogo.Where(j => (j.rodada_id <= rodadaAnteriorId &&
                    (j.desafiado_id == desafiado.UserId || j.desafiante_id == desafiado.UserId))).
                    Take(3).OrderByDescending(j=>j.Id).ToList();
            // busca um oponente mais próximo que não tenha jogado nos últimos 3 jogos 
            //desafiante = buscarOponentesNaoRepetidos(listaJogadores, jogosAnteriores);
            if (desafiante != null) {
                return desafiante;
            }
            // caso não encontre oponente na 1º verificação, busca um oponente mais próximo que não tenha jogado nos últimos 2 jogos 
            //desafiante = buscarOponentesNaoRepetidos(listaJogadores, jogosAnteriores,1);
            if (desafiante != null) {
                return desafiante;
            }
            // caso não encontre oponente na 2º verificação, busca um oponente mais próximo que não tenha jogado no último jogo 
            //desafiante = buscarOponentesNaoRepetidos(listaJogadores, jogosAnteriores, 2);
            if (desafiante != null){
                return desafiante;
            }
            // caso não encontre em nenhuma situação acima, busca o oponente mais próximo na pontuação
            desafiante = listaJogadores[1].userProfile;
            listaJogadores.RemoveAt(1);
            listaJogadores.RemoveAt(0);
            return desafiante;
        }

        private RankingView buscarOponentesNaoRepetidos(List<RankingView> listaJogadores, List<Jogo> ultimosJogos, int reduzVerificacao=0) {
            bool isDesafiante;
            int qtddTentativas = 0;
            Random r = new Random();
            while (qtddTentativas<30){
                int randomIndex = r.Next(1, listaJogadores.Count); //Choose a random object in the list
                isDesafiante = true;
                for (int j = 0; j < ultimosJogos.Count() - reduzVerificacao; j++){
                    if ((ultimosJogos[j].desafiado_id == listaJogadores[randomIndex].userProfile_id) || (ultimosJogos[j].desafiante_id == listaJogadores[randomIndex].userProfile_id)){
                        isDesafiante = false;
                        break;
                    }
                }
                if (isDesafiante){
                    RankingView desafiante = listaJogadores[randomIndex];
                    listaJogadores.RemoveAt(randomIndex);
                    listaJogadores.RemoveAt(0);
                    return desafiante;
                }
                qtddTentativas++;
            }
            return null;
        }

        private void criarJogo(int desafiadoId, int desafianteId, int idRodada, bool isCuringa=false){
            Jogo jogo = new Jogo();
            jogo.desafiado_id = desafiadoId;
            jogo.desafiante_id = desafianteId;
            jogo.rodada_id = idRodada;
            jogo.situacao_Id = 1;
            if (isCuringa){
                jogo.situacao_Id =4;
                jogo.qtddGames1setDesafiado=6;
                jogo.qtddGames2setDesafiado = 6;
                jogo.qtddGames1setDesafiante = 0;
                jogo.qtddGames2setDesafiante = 0;

            }
            db.Jogo.Add(jogo);
            db.SaveChanges();
        }
        //
        // GET: /Rodada/Edit/5
        [Authorize(Roles = "admin, organizador")]
        public ActionResult Edit(int id = 0)
        {
            Rodada rodada = db.Rodada.Find(id);
            if (rodada == null)
            {
                return HttpNotFound();
            }
            return View(rodada);
        }

        //
        // POST: /Rodada/Edit/5
        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Rodada rodada)
        {
            if (ModelState.IsValid)
            {
                rodada.dataFim = new DateTime(rodada.dataFim.Year, rodada.dataFim.Month, rodada.dataFim.Day, 23, 59, 59);
                db.Entry(rodada).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(rodada);
        }

        // este método não está mais sendo utilizado, mas pode vir a ser útil no futuro
        private UserProfile SorteiaJogadorTorneio(List<Rancking> ranckiados){
            UserProfile ranckiado;
            Random r = new Random();
            int randomIndex = 1;
            if (ranckiados.Count() == 1){
                UserProfile curinga = db.UserProfiles.Where(u => u.situacao.Equals("curinga")).Single();
                ranckiados.RemoveAt(0);
                return curinga;
                // caso só reste duas opções não há mais como aplicar as regras
            }else if (ranckiados.Count == 2){
                ranckiado = ranckiados[1].userProfile;  
                ranckiados.RemoveAt(1); 
                ranckiados.RemoveAt(0);
                return ranckiado;
            }

            randomIndex = r.Next(1, ranckiados.Count); //Choose a random object in the list
            ranckiado =  ranckiados[randomIndex].userProfile; //add it 
            ranckiados.RemoveAt(randomIndex); //remove to avoid duplicates
            ranckiados.RemoveAt(0);
            return ranckiado;
             
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult ProcessarJogosAtrasados(int id)
        {
            string msg = "";
            //situação: 4: finalizado -- 5: wo
            List<Jogo> jogos = db.Jogo.Where(r => r.rodada_id == id && r.dataCadastroResultado>r.rodada.dataFim && (r.situacao_Id==4 || r.situacao_Id==5)).ToList();
            var pontosDesafiante = 0.0;
            var pontosDesafiado = 0.0;
            try{
                using (TransactionScope scope = new TransactionScope()){
                    foreach (Jogo item in jogos)
                    {
                        pontosDesafiante = rn.calcularPontosDesafiante(item);
                        pontosDesafiado = rn.calcularPontosDesafiado(item);

                        rn.gravarPontuacaoNaRodada(id, item.desafiante, pontosDesafiante, true);
                        rn.gravarPontuacaoNaRodada(id, item.desafiado, pontosDesafiado, true);
                        item.dataCadastroResultado = item.rodada.dataFim;
                        if (item.desafiante.situacao.Equals("suspenso"))
                        {
                            UserProfile desafiante = db.UserProfiles.Find(item.desafiante_id);
                            desafiante.situacao = "ativo";
                        }
                        if (item.desafiado.situacao.Equals("suspenso"))
                        {
                            UserProfile desafiado = db.UserProfiles.Find(item.desafiado_id);
                            desafiado.situacao = "ativo";
                        }
                        db.SaveChanges();
                    }
                        scope.Complete();
                        msg = "ok";
                }
            }catch (Exception ex){
                msg = ex.Message;
            }
            return RedirectToAction("Index", new { msg = msg });
        }
        
        [Authorize(Roles = "admin, organizador")]
        public ActionResult FecharRodada(int id)
        {
            string msg = "";
            string detalheErro="";
            db.Database.ExecuteSqlCommand("Delete from Rancking where rodada_id=" + id);
            List<Jogo> jogos = db.Jogo.Where(r => r.rodada_id == id).ToList(); 
            var pontosDesafiante = 0.0;
            var pontosDesafiado = 0.0;
            try{
               using (TransactionScope scope = new TransactionScope()){
                    foreach (Jogo item in jogos){
                        pontosDesafiante = rn.calcularPontosDesafiante(item);
                        pontosDesafiado = rn.calcularPontosDesafiado(item);
                        msg = "pontosDesafio" + item.desafiado_id;    
                        if (!item.desafiante.situacao.Equals("curinga")){
                            rn.gravarPontuacaoNaRodada(id, item.desafiante, pontosDesafiante);
                            msg = "gravarPontuacaoNaRodadaDesafiante" + item.desafiante_id;
                        }
                        rn.gravarPontuacaoNaRodada(id, item.desafiado, pontosDesafiado);
                        msg = "gravarPontuacaoNaRodadaDesafiado" + item.desafiado_id;
                        verificarRegraSuspensao(item);
                        msg = "verificarRegraSuspensao" + item.desafiado_id;
                    }
                    Rodada rodada = db.Rodada.Find(id);
                    rn.gerarPontuacaoDosJogadoresForaDaRodada(id, rodada.barragemId);
                    msg = "gerarPontuacaoDosJogadoresForaDaRodada";
                    
                    gerarRancking(id);
                    msg = "gerarRanking";
                    List<Classe> classes = db.Classe.Where(c => c.barragemId == rodada.barragemId).ToList();
                    for (int i = 0; i < classes.Count(); i++){
                        gerarRanckingPorClasse(id, classes[i].Id);
                    }
                    msg = "gerarRankingPorClasse";
                    rodada.isAberta = false;
                    rodada.dataFim = DateTime.Now;
                    db.Entry(rodada).State = EntityState.Modified;
                    db.SaveChanges();
                    scope.Complete();
                    msg = "ok";
                }
               }catch (Exception ex){
                    msg = msg +": " +ex.Message;
                    if (ex.InnerException == null) { ViewBag.DetalheErro = ex.StackTrace; } else { ViewBag.DetalheErro = ex.InnerException.StackTrace; }
                    ViewBag.MsgErro = msg;
                    return View();
               }
            return RedirectToAction("Index", new { msg = msg, detalheErro = detalheErro });
        }

        private void verificarRegraSuspensao(Jogo jogo){
            // 5.6.O jogador que não receber pontuação ou deixar de jogar, mesmo que justificadamente, por 2 (dois) jogos seguidos
            //será retirado das rodadas e colocado em suspensão automática até que regularize seus jogos de forma que ele saia desta condição.
            if (jogo.gamesJogados == 0) { 
                // Caso no fechamento da rodada o jogo não tenha sido realizado, verificar a situação da rodada anterior
                verificarSeJogoRealizadoNaRodadaAnterior(jogo.desafiado_id, jogo.rodada_id, jogo.rodada.barragemId);
                verificarSeJogoRealizadoNaRodadaAnterior(jogo.desafiante_id, jogo.rodada_id, jogo.rodada.barragemId);
            }
        }

        private void verificarSeJogoRealizadoNaRodadaAnterior(int idJogador, int rodada_id, int barragemId)
        {
            int idRodadaAnterior = db.Rodada.Where(r => r.isAberta == false && r.Id < rodada_id && r.barragemId == barragemId).Max(r => r.Id);
            List<Jogo> jogoAnterior = db.Jogo.Where(j => j.rodada_id == idRodadaAnterior && (j.desafiado_id == idJogador || j.desafiante_id == idJogador))
                .ToList();
            if (jogoAnterior.Count > 0) { 
                if (jogoAnterior[0].gamesJogados == 0)
                {
                    UserProfile jogador = db.UserProfiles.Find(idJogador);
                    jogador.situacao = Tipos.Situacao.suspenso.ToString();
                    db.SaveChanges();
                }
            }
        
        }

        private void gerarRancking(int idRodada){
            int posicao = 1;
            List<Rancking> listaRancking = db.Rancking.Where(r => r.rodada_id == idRodada && r.userProfile.situacao != "desativado" && r.userProfile.situacao != "inativo").OrderByDescending(r => r.totalAcumulado).ToList();
            foreach (Rancking ran in listaRancking)
            {
                ran.posicao = posicao;
                db.Entry(ran).State = EntityState.Modified;
                db.SaveChanges();
                posicao++;
            }
        }

        private void gerarRanckingPorClasse(int idRodada, int classeId)
        {
            int posicao = 1;
            List<Rancking> listaRancking = db.Rancking.Where(r => r.rodada_id == idRodada && r.userProfile.classeId==classeId && r.userProfile.situacao != "desativado" && r.userProfile.situacao != "inativo").OrderByDescending(r => r.totalAcumulado).ToList();
            foreach (Rancking ran in listaRancking)
            {
                ran.posicaoClasse = posicao;
                db.Entry(ran).State = EntityState.Modified;
                db.SaveChanges();
                posicao++;
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}