using Barragem.Class;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Models
{
    public class Email
    {

        public string de { get; set; }
        public string para { get; set; }
        public string assunto { get; set; }
        [AllowHtml]
        public string conteudo { get; set; }
        public Tipos.FormatoEmail formato { get; set; }

    }
}