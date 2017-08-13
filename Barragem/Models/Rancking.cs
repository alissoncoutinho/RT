using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
        
    [Table("Rancking")]
    public class Rancking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Rodada")]
        [Required(ErrorMessage = "Campo rodada obrigatório")]
        public int rodada_id { get; set; }

        [Display(Name = "Pontuação")]
        [Required(ErrorMessage = "Campo pontuação obrigatório")]
        public double pontuacao { get; set; }

        [Display(Name = "Total de Pontos")]
        public double totalAcumulado { get; set; }

        [Display(Name = "Posição")]
        public int posicao { get; set; }

        [Display(Name = "Participante")]
        public int userProfile_id { get; set; }

        [Display(Name = "Participante")]
        [ForeignKey("userProfile_id")]
        public virtual UserProfile userProfile { get; set; }

        [Display(Name = "Classe")]
        public int? classeId { get; set; }

        [Display(Name = "Classe")]
        [ForeignKey("classeId")]
        public virtual Classe classe { get; set; }

        [Display(Name = "Posição Classe")]
        public int? posicaoClasse { get; set; }

        [Display(Name = "Rodada")]
        [ForeignKey("rodada_id")]
        public virtual Rodada rodada { get; set; }


    }
}