using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    [Table("RankingView")]
    public class RankingView
    {
        public int Id { get; set; }
        public int rodada_id { get; set; }

        [Display(Name = "Participante")]
        public int userProfile_id { get; set; }
        public string nome { get; set; }

        public int classeId { get; set; }

        public int nivel { get; set; }

        public string situacao { get; set; }

        [Display(Name = "Posição")]
        public int posicao { get; set; }
        
        public double totalAcumulado { get; set; }

        public int barragemId { get; set; }

    }
}