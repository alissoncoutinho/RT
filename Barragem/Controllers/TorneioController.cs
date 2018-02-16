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
using Uol.PagSeguro.Constants;
using Uol.PagSeguro.Domain;
using Uol.PagSeguro.Service;
using Uol.PagSeguro.Resources;
using Uol.PagSeguro.Exception;

namespace Barragem.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class TorneioController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        //

        [HttpPost]
        public ActionResult AlterarClassesTorneio(IEnumerable<InscricaoTorneio> inscricaoTorneio)
        {
            try
            {
                InscricaoTorneio jogador = null;
                foreach (InscricaoTorneio user in inscricaoTorneio)
                {
                    jogador = db.InscricaoTorneio.Find(user.Id);
                    jogador.classe = user.classe;
                    db.Entry(jogador).State = EntityState.Modified;
                    db.SaveChanges();
                }
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }


        [Authorize(Roles = "admin,organizador")]
        public ActionResult Index()
        {
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            List<Torneio> torneio = null;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            int barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            if (perfil.Equals("admin"))
            {
                torneio = db.Torneio.OrderByDescending(c => c.Id).ToList();
            }
            else
            {
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
        public ActionResult EfetuarSorteioFase1(int torneioId)
        {
            try
            {
                db.Database.ExecuteSqlCommand("Delete from Jogo where torneioId=" + torneioId + "and faseTorneio=100");
                var torneio = db.Torneio.Find(torneioId);
                InscricaoTorneio jogador1 = null;
                InscricaoTorneio jogador2 = null;
                List<InscricaoTorneio> inscricoes = null;
                for (int i = 1; i <= torneio.qtddClasses; i++)
                {
                    inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classe == i && r.isAtivo).ToList();
                    //colocarJogadoresEmLicencaNoRanking(inscricoes);
                    int j = 1;
                    while (inscricoes.Count > 0)
                    {
                        jogador1 = selecionarAdversario(inscricoes);
                        jogador2 = selecionarAdversario(inscricoes);
                        var jogador2Id = jogador2.userId;
                        criarJogo(jogador1.userId, jogador2Id, torneioId, i, 100, j++);
                    }
                }
                return RedirectToAction("TabelaJogos", new { torneioId = torneioId, Msg = "OK" });
            }
            catch (Exception e)
            {
                return RedirectToAction("TabelaJogos", new { torneioId = torneioId, Msg = e.Message });
            }


        }

        public ActionResult EfetuarSorteioFaseRepescagem(int torneioId)
        {
            try
            {
                db.Database.ExecuteSqlCommand("Delete from Jogo where torneioId=" + torneioId + "and faseTorneio=101");
                var torneio = db.Torneio.Find(torneioId);
                InscricaoTorneio jogador1 = null;
                InscricaoTorneio jogador2 = null;
                List<InscricaoTorneio> inscricoes = null;
                for (int i = 1; i <= torneio.qtddClasses; i++)
                {
                    inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classe == i && r.isAtivo && r.colocacao == 101).ToList();
                    int j = 1;
                    while (inscricoes.Count > 0)
                    {
                        jogador1 = selecionarAdversario(inscricoes);
                        jogador2 = selecionarAdversario(inscricoes);
                        var jogador2Id = jogador2.userId;
                        criarJogo(jogador1.userId, jogador2Id, torneioId, i, 101, j++);
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

        private int informarQtddJogos(int qtddInscritos)
        {
            var isPar = (qtddInscritos % 2 == 0);
            int jogosNormais = qtddInscritos / 2;
            if (jogosNormais % 2 == 0)
            {
                jogosNormais = jogosNormais++;
            }
            if (jogosNormais <= 32 && jogosNormais > 16)
            { // fase 1
                return 32;
            }
            else if (jogosNormais <= 16 && jogosNormais > 8)
            { // oitavas de final
                return 16;
            }
            else if (jogosNormais <= 8 && jogosNormais > 4)
            { // quartas de final
                return 8;
            }
            else if (jogosNormais <= 4 && jogosNormais > 2)
            { // semi-final
                return 4;
            }
            else if (jogosNormais <= 2)
            { // final
                return 2;
            }
            else { return 0; }
        }

        private void colocarJogadoresEmLicencaNoRanking(List<InscricaoTorneio> inscricoes)
        {
            foreach (InscricaoTorneio inscricao in inscricoes)
            {
                var user = db.UserProfiles.Find(inscricao.userId);
                if (user.situacao == "ativo")
                {
                    user.situacao = Tipos.Situacao.licenciado.ToString();
                    db.SaveChanges();
                }
            }
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult EfetuarSorteioFaseEliminatoria(int torneioId)
        {
            try
            {
                db.Database.ExecuteSqlCommand("Delete from Jogo where torneioId=" + torneioId + "and faseTorneio>=1 and faseTorneio<100");
                var torneio = db.Torneio.Find(torneioId);
                InscricaoTorneio jogador1 = null;
                InscricaoTorneio jogador2 = null;
                List<InscricaoTorneio> inscricoes = null;
                for (int i = 1; i <= torneio.qtddClasses; i++)
                {
                    inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classe == i && r.isAtivo && (r.colocacao == null || r.colocacao == 101)).ToList();
                    if (inscricoes.Count() == 0)
                    {
                        continue;
                    }
                    //colocarJogadoresEmLicencaNoRanking(inscricoes);
                    int quantidadeJogos = informarQtddJogos(inscricoes.Count());
                    int qtddJogadoresFake = getQtddJogadoresFake(inscricoes.Count(), quantidadeJogos);
                    int ordemJogos = quantidadeJogos;
                    for (int j = 1; j <= quantidadeJogos; j++)
                    {
                        var jogador2Id = 0;
                        jogador1 = selecionarAdversario(inscricoes);
                        if (qtddJogadoresFake > j)
                        {
                            jogador2Id = 0;
                        }
                        else
                        {
                            jogador2 = selecionarAdversario(inscricoes);
                            jogador2Id = jogador2.userId;
                        }
                        criarJogo(jogador1.userId, jogador2Id, torneioId, i, 1, ordemJogos--);
                    }
                    int qtddRodadas = getQtddRodadas(quantidadeJogos);
                    int numeroRodada = 2;
                    for (int k = qtddRodadas; k >= 1; k--)
                    {
                        var qtddJogosPorRodada = getQtddJogosPorRodada(k);
                        for (int y = 1; y <= qtddJogosPorRodada; y++)
                        {
                            criarJogo(0, 0, torneioId, i, numeroRodada, y);
                        }
                        numeroRodada++;
                    }

                    ProcessarJogosComBye(torneioId, i);

                }
                return RedirectToAction("TabelaJogos", new { torneioId = torneioId, Msg = "OK" });
            }
            catch (Exception e)
            {
                return RedirectToAction("TabelaJogos", new { torneioId = torneioId, Msg = e.Message });
            }


        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult TesteProcessarJogosComBye()
        {
            ProcessarJogosComBye(1, 1);
            return View();
        }
        private void ProcessarJogosComBye(int torneioId, int classeId)
        {
            var jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == classeId && r.faseTorneio == 1 && r.desafiante_id == 0 && r.situacao_Id == 4).ToList();
            foreach (Jogo jogo in jogos)
            {
                int ordemJogo = 0;
                if (jogo.ordemJogo % 2 != 0)
                {
                    int var1 = 1;
                    int teste = (var1 / 2) + 1;
                    ordemJogo = (int)(jogo.ordemJogo / 2) + 1;
                    var proximoJogo = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == classeId && r.faseTorneio == 2 && r.ordemJogo == ordemJogo).Single();
                    proximoJogo.desafiado_id = jogo.desafiado_id;
                }
                else
                {
                    ordemJogo = (int)jogo.ordemJogo / 2;
                    var proximoJogo = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == classeId && r.faseTorneio == 2 && r.ordemJogo == ordemJogo).Single();
                    proximoJogo.desafiante_id = jogo.desafiado_id;
                }
                db.SaveChanges();
            }
        }

        private int getQtddJogosPorRodada(int numeroRodada)
        {
            if (numeroRodada == 6)
            {
                return 32;
            }
            else if (numeroRodada == 5)
            {
                return 16;
            }
            else if (numeroRodada == 4)
            {
                return 8;
            }
            else if (numeroRodada == 3)
            {
                return 4;
            }
            else if (numeroRodada == 2)
            {
                return 2;
            }
            else if (numeroRodada == 1)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private int getQtddRodadas(int qtddJogos)
        {
            if (qtddJogos == 64)
            {
                return 6;
            }
            else if (qtddJogos == 32)
            {
                return 5;
            }
            else if (qtddJogos == 16)
            {
                return 4;
            }
            else if (qtddJogos == 8)
            {
                return 3;
            }
            else if (qtddJogos == 4)
            {
                return 2;
            }
            else if (qtddJogos == 2)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private void criarJogo(int jogador1, int jogador2, int torneioId, int classeTorneio, int faseTorneio, int ordemJogo)
        {
            Jogo jogo = new Jogo();
            jogo.desafiado_id = jogador1;
            jogo.desafiante_id = jogador2;
            jogo.torneioId = torneioId;
            jogo.situacao_Id = 1;
            jogo.classeTorneio = classeTorneio;
            jogo.faseTorneio = faseTorneio;
            jogo.ordemJogo = ordemJogo;
            if ((jogador2 == 0) && (jogador1 != 0))
            {
                jogo.situacao_Id = 4;
                jogo.qtddGames1setDesafiado = 6;
                jogo.qtddGames2setDesafiado = 6;
                jogo.qtddGames1setDesafiante = 0;
                jogo.qtddGames2setDesafiante = 0;

            }
            db.Jogo.Add(jogo);
            db.SaveChanges();
        }

        private InscricaoTorneio selecionarAdversario(List<InscricaoTorneio> participantes)
        {
            var adversario = new InscricaoTorneio();
            if (participantes.Count == 0)
            {
                adversario.Id = 0;
                return adversario;
            }
            else if (participantes.Count == 1)
            {
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

        [Authorize(Roles = "admin, organizador")]
        public ActionResult ListarJogos(int torneioId)
        {
            string msg = "";
            try
            {
                var jogo = db.Jogo.Include(j => j.desafiado).Include(j => j.desafiante).
                    Where(j => j.torneioId == torneioId && j.desafiado_id != 0 && j.desafiante_id != 0).OrderBy(j => j.classeTorneio).ThenBy(r => r.faseTorneio).ThenBy(r => r.ordemJogo).ToList();
                var torneio = db.Torneio.Find(torneioId);
                ViewBag.Classes = db.Classe.Where(c => c.barragemId == torneio.barragemId).ToList();
                return View(jogo);
            }
            catch (Exception ex)
            {
                ViewBag.MsgErro = msg + " - " + ex.Message;
                return View();
            }

        }

        [Authorize(Roles = "admin,organizador,usuario")]
        public ActionResult TabelaJogos(int torneioId, int classe = 1, string Msg = "")
        {
            var torneio = db.Torneio.Find(torneioId);
            ViewBag.temRepescagem = torneio.temRepescagem;
            var jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == classe).OrderBy(r => r.faseTorneio).ThenBy(r => r.ordemJogo).ToList();
            var rodada = jogos.Where(r => r.torneioId == torneioId && r.classeTorneio == classe && r.faseTorneio < 100).Max(r => r.faseTorneio);
            ViewBag.JogosFaseClassificatoria = jogos.Where(r => r.faseTorneio == 100).ToList();
            ViewBag.JogosFaseRepescagem = jogos.Where(r => r.faseTorneio == 101).ToList();
            var JogosFase2 = jogos.Where(r => r.faseTorneio == 2).ToList();
            ViewBag.JogosFase3 = jogos.Where(r => r.faseTorneio == 3).ToList();
            ViewBag.JogosFase4 = jogos.Where(r => r.faseTorneio == 4).ToList();
            ViewBag.JogosFase5 = jogos.Where(r => r.faseTorneio == 5).ToList();
            ViewBag.JogosFase6 = jogos.Where(r => r.faseTorneio == 6).ToList();

            var x = jogos.Count();

            var y = JogosFase2.Count();
            ViewBag.JogosFase2 = JogosFase2;
            ViewBag.QtddRodada = rodada;
            ViewBag.torneioId = torneioId;
            ViewBag.Classe = classe + "";
            ViewBag.isSorteado = 0;
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if ((perfil.Equals("admin")) || (perfil.Equals("organizador")))
            {
                ViewBag.isSorteado = db.Jogo.Where(r => r.torneioId == torneioId).Count();
            }

            mensagem(Msg);
            return View(jogos.Where(r => r.faseTorneio == 1).ToList());
        }

        private void mensagem(string Msg)
        {
            if (Msg != "")
            {
                if (Msg == "OK")
                {
                    ViewBag.Ok = "Operação realizada com sucesso!";
                }
                else
                {
                    ViewBag.MsgErro = Msg;
                }
            }
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult InscricoesTorneio(int torneioId, string Msg = "")
        {
            var inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId).OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();

            mensagem(Msg);

            return View(inscricoes);
        }

        [Authorize(Roles = "admin,organizador,usuario")]
        public ActionResult InscricoesTorneio2(int torneioId, string Msg = "")
        {
            var inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId).OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();

            mensagem(Msg);

            return View(inscricoes);
        }

        [Authorize(Roles = "admin,organizador,usuario")]
        public ActionResult EfetuarPagamento(int inscricaoId)
        {
            string MsgErro = "";
            var inscricao = db.InscricaoTorneio.Find(inscricaoId);

            try
            {
                Uri paymentRedirectUri = Register(inscricao);
                Response.Redirect(paymentRedirectUri.AbsoluteUri);
            }
            catch (Exception e)
            {
                MsgErro = e.Message;
            }
            return RedirectToAction("Detalhes", new { id = inscricao.torneioId, Msg = MsgErro });
        }


        private Uri Register(InscricaoTorneio inscricao)
        {
            //Use global configuration
            //PagSeguroConfiguration.UrlXmlConfiguration = "../../../../../PagSeguroConfig.xml";

            bool isSandbox = true;
            EnvironmentConfiguration.ChangeEnvironment(isSandbox);

            // Instantiate a new payment request
            PaymentRequest payment = new PaymentRequest();

            // Sets the currency
            payment.Currency = Currency.Brl;

            // Add an item for this payment request
            payment.Items.Add(new Item(inscricao.torneioId + "", inscricao.torneio.nome, 1, (decimal)inscricao.valor));

            // Sets a reference code for this payment request, it is useful to identify this payment in future notifications.
            payment.Reference = "T-" + inscricao.Id;

            // Sets your customer information.
            //payment.Sender = new Sender(inscricao.participante.nome,inscricao.participante.email,new Phone("61", "99999999"));
            payment.Sender = new Sender(inscricao.participante.nome, "c07322876609220741939@sandbox.pagseguro.com.br", new Phone("61", "99999999"));

            //SenderDocument document = new SenderDocument(Documents.GetDocumentByType("CPF"), "12345678909");
            //payment.Sender.Documents.Add(document);

            // Sets the url used by PagSeguro for redirect user after ends checkout process
            //payment.RedirectUri = new Uri("http://www.rankingdetenis.com.br/ConfirmacaoPagamento");

            try
            {
                /// Create new account credentials
                /// This configuration let you set your credentials from your ".cs" file.
                //AccountCredentials credentials = new AccountCredentials("backoffice@lojamodelo.com.br", "256422BF9E66458CA3FE41189AD1C94A");

                /// @todo with you want to get credentials from xml config file uncommend the line below and comment the line above.
                AccountCredentials credentials = PagSeguroConfiguration.Credentials(isSandbox);

                return payment.Register(credentials);
            }
            catch (PagSeguroServiceException exception)
            {
                Console.WriteLine(exception.Message + "\n");

                foreach (ServiceError element in exception.Errors)
                {
                    Console.WriteLine(element + "\n");
                }
                throw new ArgumentException("Erro ao registrar pagamento", exception.Message);
                //Console.ReadKey();
            }
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
            ViewBag.JogadoresClasses = db.InscricaoTorneio.Where(i => i.torneioId == id && i.isAtivo == true).OrderBy(i => i.classe).ThenBy(i => i.participante.nome).ToList();

            if (torneio == null)
            {
                return HttpNotFound();
            }
            return View(torneio);
        }

        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult Detalhes(int id = 0, String Msg = "")
        {
            Torneio torneio = db.Torneio.Find(id);
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            ViewBag.isInscrito = false;
            var inscricao = db.InscricaoTorneio.Where(i => i.torneio.Id == id && i.userId == userId).ToList();
            var classes = db.ClasseTorneio.Where(i => i.torneioId == id).OrderBy(c => c.nome).ToList();
            ViewBag.Classes = classes;
            ViewBag.ClasseInscricao = 0;
            ViewBag.ClasseInscricao2 = 0;
            if (inscricao.Count > 0)
            {
                ViewBag.isInscrito = true;
                ViewBag.statusPagamento = inscricao[0].statusPagamento;
                ViewBag.ClasseInscricao = inscricao[0].classe;
                ViewBag.InscricaoId = inscricao[0].Id;

                if (inscricao.Count > 1)
                {
                    ViewBag.ClasseInscricao2 = inscricao[1].classe;
                }
            }
            var jogador = db.UserProfiles.Find(userId);

            ViewBag.valor = torneio.valor + "";

            mensagem(Msg);

            return View(torneio);
        }

        [Authorize(Roles = "admin,usuario,organizador")]
        [HttpPost]
        public ActionResult Inscricao(int torneioId, int classeInscricao, string operacao = "", bool isMaisDeUmaClasse = false, int classeInscricao2 = 0, string observacao = "")
        {
            try
            {
                var userId = WebSecurity.GetUserId(User.Identity.Name);
                var torneio = db.Torneio.Find(torneioId);
                var isInscricao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.userId == userId).Count();
                if (isInscricao > 0)
                {
                    var it = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.userId == userId).ToList();
                    if (operacao == "cancelar"){
                        db.InscricaoTorneio.Remove(it[0]);
                        if (it.Count() > 1)
                        {
                            db.InscricaoTorneio.Remove(it[1]);
                        }
                    }else{
                        it[0].classe = classeInscricao;
                    }
                }else{
                    InscricaoTorneio inscricao = new InscricaoTorneio();
                    inscricao.classe = classeInscricao;
                    inscricao.torneioId = torneioId;
                    inscricao.userId = userId;
                    if (isMaisDeUmaClasse){
                        inscricao.valor = torneio.valorMaisClasses;
                    }else{
                        inscricao.valor = torneio.valor;
                    }
                    inscricao.observacao = observacao;
                    if (torneio.valor > 0){
                        inscricao.isAtivo = false;
                    }else{
                        inscricao.isAtivo = true;
                    }
                    db.InscricaoTorneio.Add(inscricao);
                    if (isMaisDeUmaClasse){
                        InscricaoTorneio inscricao2 = new InscricaoTorneio();
                        inscricao2.classe = classeInscricao2;
                        inscricao2.torneioId = torneioId;
                        inscricao2.userId = userId;
                        if (isMaisDeUmaClasse){
                            inscricao2.valor = torneio.valorMaisClasses;
                        }else{
                            inscricao2.valor = torneio.valor;
                        }
                        inscricao2.observacao = observacao;
                        if (torneio.valor > 0){
                            inscricao2.isAtivo = false;
                        }else{
                            inscricao2.isAtivo = true;
                        }
                        db.InscricaoTorneio.Add(inscricao2);
                    }
                }
                db.SaveChanges();
                return RedirectToAction("Detalhes", new { id = torneioId, Msg = "OK" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Detalhes", new { id = torneioId, Msg = ex.Message });
            }
        }
        [Authorize(Roles = "admin, organizador")]
        public ActionResult Ativar(int inscricaoId)
        {
            var inscricao = db.InscricaoTorneio.Find(inscricaoId);
            try
            {
                if (inscricao.isAtivo)
                {
                    inscricao.isAtivo = false;
                }
                else
                {
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
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if (ModelState.IsValid)
            {
                db.Entry(torneio).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
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