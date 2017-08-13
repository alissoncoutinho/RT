using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Models
{
    [Table("Configuracao")]
    public class Configuracao
    {
        public Configuracao()
        {

        }

        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Display(Name = "Regulamento")]
        [UIHint("tinymce_full_compressed"), AllowHtml]
        public string regulamento { get; set; }

        [Display(Name = "Dados de Contato")]
        [UIHint("tinymce_full_compressed"), AllowHtml]
        public string contatos { get; set; }

        
    }
}