using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Models
{
    [Table("Barragem")]
    public class Barragens
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "nome")]
        public string nome { get; set; }

        [Display(Name = "email")]
        public string email { get; set; }

        [Display(Name = "situação")]
        public bool isAtiva { get; set; }

        [Display(Name = "regulamento")]
        [UIHint("tinymce_full_compressed"), AllowHtml]
        public string regulamento { get; set; }

        [UIHint("tinymce_full_compressed"), AllowHtml]
        [Display(Name = "quemsomos")]
        public string quemsomos { get; set; }

        [UIHint("tinymce_full_compressed"), AllowHtml]
        [Display(Name = "contato")]
        public string contato { get; set; }
        [Display(Name = "link pagseguro")]
        public string linkPagSeguro { get; set; }
        [Display(Name = "Classe única")]
        public bool isClasseUnica { get; set; }
        [Display(Name = "Domínio")]
        public string dominio { get; set; }

    }

    [Table("BarragemView")]
    public class BarragemView
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "nome")]
        public string nome { get; set; }

        [Display(Name = "link pagSeguro")]
        public string linkPagSeguro { get; set; }

        [Display(Name = "situação")]
        public bool isAtiva { get; set; }

        [Display(Name = "Classe única")]
        public bool isClasseUnica { get; set; }
        [Display(Name = "Domínio")]
        public string dominio { get; set; }
    }
}