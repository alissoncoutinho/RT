using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;

namespace Barragem.Class
{
    public class Mail
    {


        private string HostIP { get; set; }
        private string SMTPPort { get; set; }
        private string Usuario { get; set; }
        private string Senha { get; set; }

        public Mail()
        {
            Usuario = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
            Senha = System.Configuration.ConfigurationManager.AppSettings.Get("SenhaMail");
            SMTPPort = System.Configuration.ConfigurationManager.AppSettings.Get("PortaSMTP");
            HostIP = System.Configuration.ConfigurationManager.AppSettings.Get("ServidorSMTP");
        }

        public Mail(string de, string para, string assunto, string conteudo, Tipos.FormatoEmail formato, List<string> bcc=null)
        {
            this.de = de;
            this.para = para;
            this.assunto = assunto;
            this.conteudo = conteudo;
            this.formato = formato;
            this.bcc = bcc;

            Usuario = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
            Senha = System.Configuration.ConfigurationManager.AppSettings.Get("SenhaMail");
            SMTPPort = System.Configuration.ConfigurationManager.AppSettings.Get("PortaSMTP");
            HostIP = System.Configuration.ConfigurationManager.AppSettings.Get("ServidorSMTP");
        }

        public string de { get; set; }
        public string para { get; set; }
        public string assunto { get; set; }
        public string conteudo { get; set; }
        public Tipos.FormatoEmail formato { get; set; }

        public List<string> bcc { get; set; }
        public void EnviarMail()
        {
            try
            {
                SmtpClient scMail = new SmtpClient();
                scMail.Host = HostIP;
                scMail.Port = int.Parse(SMTPPort);
                scMail.Credentials = new System.Net.NetworkCredential(Usuario, Senha);

                if (de == string.Empty || de == null)
                    de = this.Usuario;

                MailAddress _de = new MailAddress(de);
                MailAddress _para = new MailAddress(para);
                MailMessage message = new MailMessage(_de, _para);

                if (formato == Tipos.FormatoEmail.Html)
                {
                    message.IsBodyHtml = true;
                }
                else
                {
                    message.IsBodyHtml = false;
                }

                message.Body = conteudo;
                message.Subject = assunto;

                if (bcc != null)
                {
                    foreach (string bccEmail in bcc){
                        message.Bcc.Add(new MailAddress(bccEmail)); //Adding Multiple BCC email Id
                    }
                }
                scMail.Send(message);

                message.Dispose();
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }

    }
}