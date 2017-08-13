using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Models
{
    [Table("Temporada")]
    public class Temporada
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string nome { get; set; }

        [Display(Name = "Ativo")]
        public bool isAtivo { get; set; }

        public int barragemId { get; set; }

        [ForeignKey("barragemId")]
        public virtual BarragemView barragem { get; set; }

        public int? qtddRodadas { get; set; }
    }

    
}