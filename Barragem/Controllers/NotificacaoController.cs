using Barragem.Context;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Uol.PagSeguro.Domain;
using Uol.PagSeguro.Resources;
using Uol.PagSeguro.Service;

namespace Barragem.Controllers
{
    public class NotificacaoController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        //
        // GET: /Notificacao/

        public ActionResult RetornoPagamento()
        {
            return View();
        }
        [HttpPost]
        public void Request(HttpRequest Request)
        {
            bool isSandbox = true;

            EnvironmentConfiguration.ChangeEnvironment(isSandbox);

            AccountCredentials credentials = PagSeguroConfiguration.Credentials(isSandbox);

            string notificationType = Request.Form["notificationType"];
            string notificationCode = Request.Form["notificationCode"];

            if (notificationType == "transaction")
            {
                // obtendo o objeto transaction a partir do código de notificação
                Transaction transaction = NotificationService.CheckTransaction(credentials, notificationCode);
                // Data da criação 
                DateTime date = transaction.Date;
                // Data da última atualização 
                DateTime lastEventDate = transaction.LastEventDate;
                // Código da transação 
                string code = transaction.Code;
                // Refência 
                string reference = transaction.Reference;
                // Valor bruto  
                decimal grossAmount = transaction.GrossAmount;
                // Tipo 
                int type = transaction.TransactionType;
                // Status 
                /* Código	Significado
                1	Aguardando pagamento: o comprador iniciou a transação, mas até o momento o PagSeguro não recebeu nenhuma informação sobre o pagamento.
                2	Em análise: o comprador optou por pagar com um cartão de crédito e o PagSeguro está analisando o risco da transação.
                3	Paga: a transação foi paga pelo comprador e o PagSeguro já recebeu uma confirmação da instituição financeira responsável pelo processamento.
                4	Disponível: a transação foi paga e chegou ao final de seu prazo de liberação sem ter sido retornada e sem que haja nenhuma disputa aberta.
                5	Em disputa: o comprador, dentro do prazo de liberação da transação, abriu uma disputa.
                6	Devolvida: o valor da transação foi devolvido para o comprador.
                7	Cancelada: a transação foi cancelada sem ter sido finalizada.
                8	Debitado: o valor da transação foi devolvido para o comprador.
                9	Retenção temporária: o comprador contestou o pagamento junto à operadora do cartão de crédito ou abriu uma demanda judicial ou administrativa (Procon).
                */
                int status = transaction.TransactionStatus;
                // Valor líquido  
                decimal netAmount = transaction.NetAmount;
                // Valor das taxas cobradas  
                decimal feeAmount = transaction.FeeAmount;
                // Valor extra ou desconto
                decimal extraAmount = transaction.ExtraAmount;
                // Tipo de meio de pagamento
                PaymentMethod paymentMethod = transaction.PaymentMethod;

                string[] refs = reference.Split('-');
                if (refs[0].Equals("T")){ // se for torneio
                    var inscricao = db.InscricaoTorneio.Find(refs[1]);
                    if (status == 3) {
                        inscricao.isAtivo = true;
                    }
                    inscricao.statusPagamento = status+"";
                    inscricao.formaPagamento = paymentMethod.PaymentMethodType + "";
                    inscricao.valor = (float)transaction.GrossAmount;
                    db.Entry(inscricao).State = EntityState.Modified;
                    db.SaveChanges();
                }

            }
        }

    }
}
