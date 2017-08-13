using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    [Table("situacaoJogo")]
    public class SituacaoJogo
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "descricao")]
        public string descricao { get; set; }

        
    }
}