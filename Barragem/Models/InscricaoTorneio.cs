using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public class InscricaoTorneio
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Participante")]
        public int userId { get; set; }

        [Display(Name = "Participante")]
        [ForeignKey("userId")]
        public virtual UserProfile participante { get; set; }

        public bool isAtivo { get; set; }
        public int classe { get; set; }

        public int torneioId { get; set; }

        [Display(Name = "Torneio")]
        [ForeignKey("torneioId")]
        public virtual Torneio torneio { get; set; }

        public int? colocacao { get; set; }
    }
}