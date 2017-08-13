using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    [Table("Rodada")]
    public class Rodada
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Código")]
        [StringLength(10)]
        [Required(ErrorMessage = "Campo Código obrigatório")]
        public string codigo { get; set; }

        [Display(Name = "Data Início")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Este campo é obrigatório")]
        public DateTime dataInicio { get; set; }

        [Display(Name = "Data Fim")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Este campo é obrigatório")]
        public DateTime dataFim { get; set; }

        public bool isAberta { get; set; }

        public bool isRodadaCarga { get; set; }

        public int sequencial { get; set; }

        [Display(Name = "Ranking")]
        public int barragemId { get; set; }

        [Display(Name = "Ranking")]
        [ForeignKey("barragemId")]
        public virtual BarragemView barragem { get; set; }

        [Display(Name = "Temporada")]
        public int? temporadaId { get; set; }

        [Display(Name = "Temporada")]
        [ForeignKey("temporadaId")]
        public virtual Temporada temporada { get; set; }

        public virtual ICollection<Jogo> jogos { get; set; }

        public virtual string codigoSeq { get { return codigo + sequencial; } }
    }
}