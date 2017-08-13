using Barragem.Class;
using Barragem.Context;
using Barragem.Filters;
using Barragem.Models;
using DotNetOpenAuth.AspNet;
using Microsoft.Web.WebPages.OAuth;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using WebMatrix.WebData;


namespace Barragem.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class AccountController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        //
        // GET: /Account/Login

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            //ViewBag.ReturnUrl = returnUrl;
            ViewBag.ReturnUrl = "";
            return View();
        }

        //
        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (ModelState.IsValid && WebSecurity.Login(model.UserName, model.Password, persistCookie: model.RememberMe))
            {
                var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(model.UserName));
                HttpCookie cookie = new HttpCookie("_barragemId");
                cookie.Value = usuario.barragemId.ToString();
                DateTime dtNow = DateTime.Now;
                TimeSpan tsMinute = new TimeSpan(0, 0, 59, 0);
                cookie.Expires = dtNow + tsMinute;
                Response.Cookies.Add(cookie);
                HttpCookie cookieNome = new HttpCookie("_barragemNome");
                cookieNome.Value = usuario.barragem.nome;
                cookieNome.Expires = dtNow + tsMinute;
                Response.Cookies.Add(cookieNome);
                return RedirectToLocal(returnUrl);
            }

            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "O login ou a senha estão incorretos.");
            return View(model);
        }

        //
        // POST: /Account/LogOff

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            WebSecurity.Logout();

            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/Register

        [AllowAnonymous]
        public ActionResult Register(int barragemId=0)
        {
            HttpCookie cookie = Request.Cookies["_barragemId"];
            if ((barragemId == 0)&&(cookie != null)){
                barragemId = Convert.ToInt32(cookie.Value.ToString());
            }
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome", barragemId);
            ViewBag.classeId = new SelectList(db.Classe.Where(c=>c.barragemId==barragemId).ToList(), "Id", "nome");
            return View();
        }

        //
        // POST: /Account/Register

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model, HttpPostedFileBase inputfoto)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                try{
                    if (!WebSecurity.UserExists(model.UserName)){
                        if (!Funcoes.IsValidEmail(model.email)){
                            ViewBag.MsgErro = string.Format("E-mail inválido. '{0}'", model.email);
                            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome", model.barragemId);
                            ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == model.barragemId).ToList(), "Id", "nome");
                            return View(model);
                        }
                        if (inputfoto != null){
                            int length = inputfoto.ContentLength;
                            byte[] buffer = new byte[length];
                            inputfoto.InputStream.Read(buffer, 0, length);
                            model.foto = buffer;

                            WebSecurity.CreateUserAndAccount(model.UserName, model.Password, new{
                                nome = model.nome, dataNascimento = model.dataNascimento, altura = model.altura, 
                                telefoneFixo = model.telefoneFixo, telefoneCelular = model.telefoneCelular, telefoneCelular2 = model.telefoneCelular2,
                                email = model.email, situacao = Tipos.Situacao.pendente.ToString(),
                                bairro = model.bairro, dataInicioRancking = DateTime.Now,
                                naturalidade = model.naturalidade, lateralidade = model.lateralidade, nivelDeJogo = model.nivelDeJogo, barragemId=model.barragemId, classeId=model.classeId,
                                foto = model.foto
                            });
                        } else {
                            WebSecurity.CreateUserAndAccount(model.UserName, model.Password, new{
                                nome = model.nome, dataNascimento = model.dataNascimento, altura = model.altura, 
                                telefoneFixo = model.telefoneFixo, telefoneCelular = model.telefoneCelular, telefoneCelular2 = model.telefoneCelular2,
                                email = model.email, situacao = Tipos.Situacao.pendente.ToString(),
                                bairro = model.bairro, dataInicioRancking = DateTime.Now,
                                naturalidade = model.naturalidade, lateralidade = model.lateralidade, nivelDeJogo = model.nivelDeJogo, barragemId=model.barragemId, classeId=model.classeId
                            });
                        }

                        Roles.AddUserToRole(model.UserName, "usuario");
                        WebSecurity.Login(model.UserName, model.Password);
                        try{
                            notificarJogador(model.nome, model.email);
                            notificarOrganizadorCadastro(model.nome, model.barragemId);
                        } catch (Exception ex) { } // não tratar o erro pois caso não seja possível notificar o administrador não prejudicará o cadastro do usuário
                        
                        return RedirectToAction("Index", "Home");
                    }else{
                        ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome", model.barragemId);
                        ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == model.barragemId).ToList(), "Id", "nome");
                        ViewBag.MsgErro = string.Format("Login já existente. Favor escolha outro nome. '{0}'", model.UserName);
                    }
                    
                }catch (MembershipCreateUserException e){
                    ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));
                }

            }
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");    
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        private void notificarOrganizadorCadastro(string nome, int idBarragem)
        {
            Mail mail = new Mail();
            mail.de = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
            var barragem = db.Barragens.Find(idBarragem);
            if (barragem.email.Equals("")){
                mail.para = "toptenisranking@gmail.com";
            }else{
                mail.para = barragem.email;
            }
            mail.assunto = "Um Novo cadastro foi realizado";
            //mail.conteudo = nome + " fez uma soliticação de adesão à barragem do cerrado.<br><br>Entre no sistema e modifique o status de "+ nome + " de pendente para ativo.";
            mail.conteudo = nome + " acabou de se cadastrar no site.<br>";
            mail.formato = Tipos.FormatoEmail.Html;
            mail.EnviarMail();
        }

        private void notificarOrganizadorSolicitacaoAtivar(string nome, int idBarragem)
        {
            Mail mail = new Mail();
            mail.de = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
            var barragem = db.Barragens.Find(idBarragem);
            if (barragem.email.Equals("")){
                mail.para = "toptenisranking@gmail.com";
            }else{
                mail.para = barragem.email;
            }
            mail.assunto = "Solicitação de ativação";
            mail.conteudo = nome + " fez uma soliticação de ativação à barragem do cerrado.<br><br>Entre no sistema e modifique o status de "+ nome + " de ativamento solicitado para ativo.";
            mail.formato = Tipos.FormatoEmail.Html;
            mail.EnviarMail();
        }

        private void notificarJogador(string nome, string email)
        {
            Mail mail = new Mail();
            mail.de = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
            mail.para = email;
            mail.assunto = "Cadastro realizado com sucesso.";
            mail.conteudo = "Olá " + nome + ",<br> Parabéns você acabou de se cadastrar no top tênis ranking.<br><br>" +
            "Para participar das próximas rodadas você deve solicitar a ativação do seu cadastro clicando no botão localizado em sua página principal no site." +
            "após a ativação do seu cadastro você será notificado por email e já estará apto a participar dos jogos do ranking. <br><br>" +
            "Atenciosamente, Top Tênis Ranking.<br>";
            mail.formato = Tipos.FormatoEmail.Html;
            mail.EnviarMail();
        }

        [AllowAnonymous]
        public ActionResult SolicitarAtivacao()
        {
            //string userName = MD5Crypt.Descriptografar(token);
            var userName = User.Identity.Name;
            UserProfile user = null;
            try
            {
                user = db.UserProfiles.Where(u => u.UserName.ToLower() == userName.ToLower()).FirstOrDefault();
                if (user != null)
                {
                    
                    user.situacao = "Ativamento solicitado";
                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();
                    notificarOrganizadorSolicitacaoAtivar(user.nome, user.barragemId);
                    return View("SolicitarAtivacao");
                }else{
                    ViewBag.MsgErro = "Este usuário não existe.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                var routeData = new RouteData();
                routeData.Values["controller"] = "Erros";
                routeData.Values["exception"] = ex;
                routeData.Values["action"] = "General";
                return RedirectToAction("General", "Erros", routeData);
            }
            finally
            {
                if (db != null)
                    db.Dispose();
            }
            //return View();
        }
        
        [Authorize(Roles = "admin,organizador,usuario")]
        public ActionResult Detalhes(int userId) {
            UserProfile jogador = db.UserProfiles.Find(userId);
            UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if (!perfil.Equals("admin")){
                if (usu.barragemId != jogador.barragemId){
                    userId = usu.UserId;
                    jogador = usu;
                }
            }
            List<Rancking> ranckingJogador = db.Rancking.Where(r=>r.userProfile_id==userId).OrderByDescending(r=>r.rodada_id).ToList();
            ViewBag.RanckingJogador = ranckingJogador;
            List<Jogo> jogosJogador = db.Jogo.Where(r => r.desafiante_id == userId || r.desafiado_id==userId)
                .OrderByDescending(r => r.rodada_id).ToList();
            ViewBag.jogosJogador = jogosJogador;
            
            if (ranckingJogador.Count > 0)            {
                ViewBag.posicao = ranckingJogador[0].posicao+"º";
                ViewBag.pontos = Math.Round(ranckingJogador[0].totalAcumulado,2);
            }else {
                ViewBag.pontos = 0;
                ViewBag.posicao = "sem rancking"; 
            }

            return View(jogador);
        }

        [HttpGet]
        [Authorize(Roles = "admin,organizador")]
        public ActionResult EditPontuacao(int Id)
        {
            Rancking r = db.Rancking.Find(Id);
            return View(r);
        }

        [HttpPost]
        [Authorize(Roles = "admin,organizador")]
        public ActionResult EditPontuacao(Rancking r)
        {
            double pontuacaoTotal = db.Rancking.Where(ran => ran.rodada.isAberta == false && ran.userProfile_id == r.userProfile_id 
                && ran.rodada_id < r.rodada_id).OrderByDescending(ran => ran.Id).Take(9).Sum(ran => ran.pontuacao);
            r.totalAcumulado = r.pontuacao + pontuacaoTotal;
            db.Entry(r).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Detalhes", new { userId = r.userProfile_id });
        }


        //
        // POST: /Account/Disassociate

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Disassociate(string provider, string providerUserId)
        {
            string ownerAccount = OAuthWebSecurity.GetUserName(provider, providerUserId);
            ManageMessageId? message = null;

            // Only disassociate the account if the currently logged in user is the owner
            if (ownerAccount == User.Identity.Name)
            {
                // Use a transaction to prevent the user from deleting their last login credential
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.Serializable }))
                {
                    bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
                    if (hasLocalAccount || OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name).Count > 1)
                    {
                        OAuthWebSecurity.DeleteAccount(provider, providerUserId);
                        scope.Complete();
                        message = ManageMessageId.RemoveLoginSuccess;
                    }
                }
            }

            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage

        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Sua senha foi alterada com sucesso."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : "";
            ViewBag.HasLocalPassword = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Manage(LocalPasswordModel model)
        {
            bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
            ViewBag.HasLocalPassword = hasLocalAccount;
            ViewBag.ReturnUrl = Url.Action("Manage");
            
            if (hasLocalAccount)
            {
                if (ModelState.IsValid)
                {
                    // ChangePassword will throw an exception rather than return false in certain failure scenarios.
                    bool changePasswordSucceeded;
                    try
                    {
                        changePasswordSucceeded = WebSecurity.ChangePassword(User.Identity.Name, model.OldPassword, model.NewPassword);
                    }
                    catch (Exception)
                    {
                        changePasswordSucceeded = false;
                    }

                    if (changePasswordSucceeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                    }
                }
            }
            else
            {
                // User does not have a local password so remove any validation errors caused by a missing
                // OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        WebSecurity.CreateAccount(User.Identity.Name, model.NewPassword);
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    catch (Exception)
                    {
                        ModelState.AddModelError("", String.Format("Unable to create local account. An account with the name \"{0}\" may already exist.", User.Identity.Name));
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/ExternalLogin

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            return new ExternalLoginResult(provider, Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalLoginCallback

        [AllowAnonymous]
        public ActionResult ExternalLoginCallback(string returnUrl)
        {
            AuthenticationResult result = OAuthWebSecurity.VerifyAuthentication(Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
            if (!result.IsSuccessful)
            {
                return RedirectToAction("ExternalLoginFailure");
            }

            if (OAuthWebSecurity.Login(result.Provider, result.ProviderUserId, createPersistentCookie: false))
            {
                return RedirectToLocal(returnUrl);
            }

            if (User.Identity.IsAuthenticated)
            {
                // If the current user is logged in add the new account
                OAuthWebSecurity.CreateOrUpdateAccount(result.Provider, result.ProviderUserId, User.Identity.Name);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // User is new, ask for their desired membership name
                string loginData = OAuthWebSecurity.SerializeProviderUserId(result.Provider, result.ProviderUserId);
                ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(result.Provider).DisplayName;
                ViewBag.ReturnUrl = returnUrl;
                return View("ExternalLoginConfirmation", new RegisterExternalLoginModel { UserName = result.UserName, ExternalLoginData = loginData });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLoginConfirmation(RegisterExternalLoginModel model, string returnUrl)
        {
            string provider = null;
            string providerUserId = null;

            if (User.Identity.IsAuthenticated || !OAuthWebSecurity.TryDeserializeProviderUserId(model.ExternalLoginData, out provider, out providerUserId))
            {
                return RedirectToAction("Manage");
            }

            if (ModelState.IsValid)
            {
                // Insert a new user into the database
                using (BarragemDbContext db = new BarragemDbContext())
                {
                    UserProfile user = db.UserProfiles.FirstOrDefault(u => u.UserName.ToLower() == model.UserName.ToLower());
                    // Check if user already exists
                    if (user == null)
                    {
                        // Insert name into the profile table
                        db.UserProfiles.Add(new UserProfile { UserName = model.UserName });
                        db.SaveChanges();

                        OAuthWebSecurity.CreateOrUpdateAccount(provider, providerUserId, model.UserName);
                        OAuthWebSecurity.Login(provider, providerUserId, createPersistentCookie: false);

                        return RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        ModelState.AddModelError("UserName", "User name already exists. Please enter a different user name.");
                    }
                }
            }

            ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(provider).DisplayName;
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // GET: /Account/ExternalLoginFailure

        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        [AllowAnonymous]
        [ChildActionOnly]
        public ActionResult ExternalLoginsList(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return PartialView("_ExternalLoginsListPartial", OAuthWebSecurity.RegisteredClientData);
        }

        [ChildActionOnly]
        public ActionResult RemoveExternalLogins()
        {
            ICollection<OAuthAccount> accounts = OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name);
            List<ExternalLogin> externalLogins = new List<ExternalLogin>();
            foreach (OAuthAccount account in accounts)
            {
                AuthenticationClientData clientData = OAuthWebSecurity.GetOAuthClientData(account.Provider);

                externalLogins.Add(new ExternalLogin
                {
                    Provider = account.Provider,
                    ProviderDisplayName = clientData.DisplayName,
                    ProviderUserId = account.ProviderUserId,
                });
            }

            ViewBag.ShowRemoveButton = externalLogins.Count > 1 || OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
            return PartialView("_RemoveExternalLoginsPartial", externalLogins);
        }


        [HttpGet]
        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult Excluir(int Id)
        {
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if ((perfil.Equals("admin")) || (perfil.Equals("organizador")) || (WebSecurity.GetUserId(User.Identity.Name) == Id)){
                var usuario = db.UserProfiles.Find(Id);
                if (usuario.situacao == "pendente")
                {
                    var listaJogos = db.Jogo.Where(j => j.desafiado_id == Id || j.desafiante_id == Id).ToList();
                    if (listaJogos.Count == 0)
                    {
                        db.Database.ExecuteSqlCommand("delete from rancking where USERPROFILE_ID=" + Id);
                        db.Database.ExecuteSqlCommand("Delete from webpages_UsersInRoles where UserId=" + Id);
                        db.Database.ExecuteSqlCommand("Delete from UserProfile where UserId=" + Id);
                    }
                    else {
                        ViewBag.MsgErro = "Não foi possível excluir o usuário pois ele já possui jogo(s) realizado(s).";
                    }
                 }
            }
            return RedirectToAction("ListarUsuarios");
        }

        [HttpGet]
        [Authorize(Roles = "admin,organizador,usuario")]
        public ActionResult EditaUsuario(string UserName)
        {
            var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(UserName));
            ViewBag.solicitarAtivacao = "";
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome", usuario.barragemId);
            if ((usuario.situacao == "desativado") || (usuario.situacao == "pendente")){
                ViewBag.solicitarAtivacao = "sim";
            }
            return View(usuario);
        }

        [Authorize(Roles = "admin")]
        public ActionResult acertoDuplicacao()
        {
            List<Rancking> ranking = db.Rancking.ToList();
            int qtdd = 0;
            foreach (var item in ranking){
                int existe = db.Rancking.Where(r => r.rodada_id == item.rodada_id && r.userProfile_id == item.userProfile_id).Count();
                if (existe > 1){
                        db.Database.ExecuteSqlCommand("delete from rancking where id=" + item.Id);
                        qtdd = qtdd + 1;
                }
            }
            ViewBag.Retorno = qtdd;
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,organizador,usuario")]
        public ActionResult EditaUsuario(UserProfile model, HttpPostedFileBase inputfoto)
        {
            ViewBag.solicitarAtivacao = "";
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if ((!perfil.Equals("admin")) && (!perfil.Equals("organizador")) && (WebSecurity.GetUserId(User.Identity.Name) != model.UserId))
            {
                ViewBag.MsgErro = string.Format("Você não tem permissão para alterar este usuário '{0}'", model.nome);
                ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");
                return View(model);
            }
            if (ModelState.IsValid){
                //UserProfile usuario = null;
                try{
                    //usuario = db.UserProfiles.Where(u => u.email.Equals(model.email) && !u.UserName.Equals(model.UserName)).FirstOrDefault();
                    //if (usuario != null){
                    //    ViewBag.MsgErro = string.Format("Já existe um usuário com este e-mail '{0}'", model.email);
                    //    ViewBag.barragemId = new SelectList(db.Barragens, "Id", "nome");
                    //    return View(model);
                    //}else{
                        if (!Funcoes.IsValidEmail(model.email)){
                            ViewBag.MsgErro = string.Format("E-mail inválido. '{0}'", model.email);
                            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");
                            return View(model);
                        }
                    //}
                    if (inputfoto != null){
                        int length = inputfoto.ContentLength;
                        byte[] buffer = new byte[length];
                        inputfoto.InputStream.Read(buffer, 0, length);
                        model.foto = buffer;
                    } else {
                        model.foto = (from up in db.UserProfiles where up.UserId == model.UserId select up.foto).Single();
                    }
                    model.dataInicioRancking = (from up in db.UserProfiles where up.UserId == model.UserId select up.dataInicioRancking).Single(); 
                    db.Entry(model).State = EntityState.Modified;
                    db.SaveChanges();
                    if ((model.situacao == "desativado") || (model.situacao == "pendente"))
                    {
                        ViewBag.solicitarAtivacao = Class.MD5Crypt.Criptografar(model.UserName);
                    }
                }catch (MembershipCreateUserException ex){
                    ViewBag.MsgErro = ex.Message;
                }
                try{
                    gerarRankingInicial(model, perfil);
                    ViewBag.Ok = "ok";
                }catch(Exception e){
                    ViewBag.MsgErro = "";
                    if (e.InnerException == null)
                    {
                        ViewBag.MsgErro = "Erro ao gerar a pontuação inicial do usuário: " + e.Message;
                        ViewBag.DetalheErro = e.StackTrace;
                    }
                    else
                    {
                        ViewBag.MsgErro = "Erro ao gerar a pontuação inicial do usuário: " + e.Message + ", " + e.InnerException.Message;
                        ViewBag.DetalheErro = e.InnerException.StackTrace;
                    }
                    
                }
                
            }

            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");

            return View(model);

        }

        private int getPontuacaoPorNivel(Rodada rodada, string nivelDeJogo)
        {
            int qtddJogadores = 0;
            int posicao = 0;
            var rodadaId = 0;
            if (rodada!=null){
                rodadaId = rodada.Id;
            }
            try
            {
                var ranking = db.Rancking.Where(r => r.rodada_id == rodadaId && r.posicao != 0).ToList();
                qtddJogadores = ranking.Count();
                int totalAcumulado = 50;
                if (nivelDeJogo == "intermediario")
                {
                    totalAcumulado = 40;
                    posicao = qtddJogadores / 2;
                }
                else if (nivelDeJogo == "avancado")
                {
                    totalAcumulado = 60;
                    posicao = qtddJogadores / 6;
                }
                else
                {
                    totalAcumulado = 20;
                    posicao = qtddJogadores - (qtddJogadores / 3);
                }
                if (posicao == 0){
                    return totalAcumulado;
                }
                var rankingPosicao = ranking.Where(p => p.posicao == posicao).FirstOrDefault();
                totalAcumulado = Convert.ToInt32(rankingPosicao.totalAcumulado);
                return totalAcumulado;
            }
            catch (Exception e) {
                throw new Exception(e.Message + ": qtddJogadores:" + qtddJogadores + ", posicao:" + posicao + ", rodada: " + rodada.Id);
            }
        }

        private int getPontuacaoPorClasse(Rodada rodada, int? classeId)
        {
            int qtddJogadores = 0;
            int posicao = 0;
            var rodadaId = 0;
            int totalAcumulado = 50;
            if (rodada != null){
                rodadaId = rodada.Id;
            }
            try
            {
                var ranking = db.Rancking.Where(r => r.rodada_id == rodadaId && r.posicao != 0 && r.classeId==classeId).OrderByDescending(r=>r.totalAcumulado).ToList();
                qtddJogadores = ranking.Count();
                if (qtddJogadores > 1){
                    posicao = (qtddJogadores / 2);
                    var rankingPosicao = ranking[posicao];// .ToArray()[posicao];
                    totalAcumulado = Convert.ToInt32(rankingPosicao.totalAcumulado);
                }
                return totalAcumulado;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + ": qtddJogadores:" + qtddJogadores + ", posicao:" + posicao + ", rodada: " + rodada.Id);
            }
        }

        private void gerarRankingInicial(UserProfile model, string perfil){
            if (((perfil.Equals("admin")) || (perfil.Equals("organizador"))) && (!model.isRanckingGerado)){
                List<Rodada> rodadas = db.Rodada.Where(r => r.isAberta == false && r.barragemId==model.barragemId).OrderByDescending(r => r.Id).Take(10).ToList();
                Rancking ranking = null;
                Rodada rodada = null;
                if (rodadas.Count() != 0){
                    rodada = rodadas[0];
                }
                int totalAcumulado = getPontuacaoPorClasse(rodada, model.classeId);
                double pontuacao = (double) totalAcumulado / 10;
                foreach (var item in rodadas){
                    int existe = db.Rancking.Where(r => r.rodada_id == item.Id && r.userProfile_id == model.UserId).Count();
                    if (existe == 0){
                        ranking = new Rancking();
                        ranking.rodada_id = item.Id;
                        ranking.pontuacao = pontuacao;
                        ranking.posicao = 0;
                        ranking.totalAcumulado = totalAcumulado;
                        ranking.userProfile_id = model.UserId;
                        ranking.classeId = model.classeId;
                        db.Rancking.Add(ranking);
                        db.SaveChanges();
                    }
                }
                model.isRanckingGerado = true;
                db.Entry(model).State = EntityState.Modified;
                db.SaveChanges();
            }
        }


        #region Helpers
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index2", "Home");
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
        }

        internal class ExternalLoginResult : ActionResult
        {
            public ExternalLoginResult(string provider, string returnUrl)
            {
                Provider = provider;
                ReturnUrl = returnUrl;
            }

            public string Provider { get; private set; }
            public string ReturnUrl { get; private set; }

            public override void ExecuteResult(ControllerContext context)
            {
                OAuthWebSecurity.RequestAuthentication(Provider, ReturnUrl);
            }
        }

        public string HaveFoto(string userName)
        {
            UserProfile usuario = null;
            usuario = db.UserProfiles.Where(u => u.UserName.Equals(userName)).Single();
            if (usuario.foto != null){
                return "true";
            }
            return "false";

        }
        public FileContentResult BuscaFoto(int id=0, string userName="")
        {
            UserProfile usuario = null;
            if (id == 0){
                usuario = db.UserProfiles.Where(u=>u.UserName.Equals(userName)).Single();
            }else{
                usuario = db.UserProfiles.Find(id);
            }
            if (usuario.foto != null)
            {
                return File(usuario.foto, "image/png");
            }
            return null;
        }

        public ActionResult ListarUsuarios(String filtroSituacao="", int filtroBarragem=0)
        {
            UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            ViewBag.situacao = usu.situacao;
            ViewBag.filtro = filtroSituacao;
            ViewBag.filtroBarragem = filtroBarragem;
            List<UserProfile> usuarios;
            IQueryable<UserProfile> consulta = null;
            if (filtroSituacao == ""){
                consulta = db.UserProfiles.Where(u => u.situacao.Equals("ativo") || u.situacao.Equals("licenciado") || u.situacao.Equals("suspenso"));
            } else if(filtroSituacao=="todos") {
                consulta = db.UserProfiles.Where(u => !u.situacao.Equals("curinga"));
            } else {
                consulta = db.UserProfiles.Where(u => u.situacao.Equals(filtroSituacao));    
            }
            if (filtroBarragem != 0){
                consulta = consulta.Where(u => u.barragemId == filtroBarragem);
            }
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if (!perfil.Equals("admin")){
                usuarios = consulta.Where(u => u.barragemId == usu.barragemId).OrderBy(u => u.nome).ToList();
            } else {
                usuarios = consulta.OrderBy(u => u.nome).ToList();
            }
                return View(usuarios);
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult ResetarSenha()
        {
            ViewBag.MsgErro = "";
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetarSenha(UserProfile model)
        {
            UserProfile user = null;
            try
            {

                user = db.UserProfiles.Where(u => u.UserName == model.UserName).FirstOrDefault();
                if (user != null)
                {
                    if (String.IsNullOrEmpty(user.email))
                    {
                        ViewBag.MsgErro = "Este usuário não possui e-mail cadastrado. Por favor, entre em contato com o administrador";
                        return View();
                    }else{
                        string confirmationToken = WebSecurity.GeneratePasswordResetToken(user.UserName);
                        EnviarMailSenha(confirmationToken, user.nome, user.email);
                        return View("ConfirmaEnvio");
                    }
                }else{
                    ViewBag.MsgErro = "Este usuário não existe.";
                    return View();
                }
            }catch (Exception ex){
                var routeData = new RouteData();
                routeData.Values["controller"] = "Erros";
                routeData.Values["exception"] = ex;
                routeData.Values["action"] = "General";
                return RedirectToAction("General", "Erros", routeData);
            }
            finally
            {
                if (db != null)
                    db.Dispose();
            }
            //return View();
        }

        private void EnviarMailSenha(string token, string nomeUsuario, string emailUsuario)
        {
            string hostHeader = this.Request.Headers["host"];
            string strUrl = string.Format("{0}://{1}/Account/ConfirmaSenha/{2}", this.Request.Url.Scheme, hostHeader, token);
            string strConteudo = "<html> <head> </head> <body> Prezado(a) " + nomeUsuario + ", <br /> Você fez uma solicitação de reinicio de senha. <br />";
            strConteudo += "Para continuar, clique no link abaixo: <br /> " + strUrl + " </body> </html>";

            Mail email = new Mail();
            email.assunto = "Solicitação de troca de senha";
            email.conteudo = strConteudo;
            email.formato = Class.Tipos.FormatoEmail.Html;
            email.para = emailUsuario;
            email.EnviarMail();

        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult ConfirmaSenha(string id)
        {
            ViewBag.MsgErro = "";
            ConfirmaSenhaModel model = new ConfirmaSenhaModel();
            model.TokenId = id;
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmaSenha(ConfirmaSenhaModel model)
        {
            ViewBag.MsgErro = "";
            try
            {
                if (WebSecurity.ResetPassword(model.TokenId, model.Senha))
                {
                    ViewBag.MsgSucesso = "Senha alterada com sucesso.";
                    return RedirectToAction("Login");
                }
                else
                {
                    ViewBag.MsgErro = "Não foi possível trocar a senha!";
                }
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.MsgErro = "Não foi possível trocar a senha. Erro: " + ex.Message;
            }

            return View(model);
        }

        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
        #endregion
    }
}
