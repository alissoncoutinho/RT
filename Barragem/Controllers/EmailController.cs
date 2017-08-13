using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Barragem.Models;
using Barragem.Class;
namespace PontoAdm.Controllers
{
    [Authorize]
    public class EmailController : Controller
    {
        [HttpGet]
        public ActionResult Enviar()
        {
            ViewBag.MsgOk = "";
            ViewBag.MsgErro = "";
            return View();
        }

        [HttpPost]
        public ActionResult Enviar(Email email)
        {
            try
            {
                Mail mail = new Mail();
                mail.de = email.de;
                mail.para = email.para;
                mail.assunto = email.assunto;
                mail.conteudo = email.conteudo;
                mail.formato = email.formato;
                mail.EnviarMail();
                ViewBag.MsgOk = "E-mail enviado com sucesso";
                ViewBag.MsgErro = "";
            }
            catch (Exception ex)
            {
                ViewBag.MsgOk = "Erro ao Enviar o e-mail";
                ViewBag.MsgErro = ex.Message;
            }

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }


    }
}
