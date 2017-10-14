using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    [Table("Jogo")]
    public class Jogo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Rodada")]
        [Required(ErrorMessage = "Campo rodada obrigatório")]
        public int rodada_id { get; set; }

        [Display(Name = "Data")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}", ConvertEmptyStringToNull = true)]
        public DateTime? dataCadastroResultado { get; set; }

        public DateTime? dataLimiteJogo { get; set; }

        [Display(Name = "Data do jogo")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}", ConvertEmptyStringToNull = true)]
        public DateTime? dataJogo { get; set; }

        [Display(Name = "Hora")]
        public string horaJogo { get; set; }

        public string usuarioInformResultado { get; set; }

        [Display(Name = "Desafiante")]
        public int desafiante_id { get; set; }
        [Display(Name = "Desafiado")]
        public int desafiado_id { get; set; }

        public int qtddGames1setDesafiante { get; set; }
        public int qtddGames2setDesafiante { get; set; }
        public int qtddGames3setDesafiante { get; set; }

        public int qtddGames1setDesafiado { get; set; }
        public int qtddGames2setDesafiado { get; set; }
        public int qtddGames3setDesafiado { get; set; }

        [Display(Name = "Vencedor")]
        public int idVencedor { get; set; }

        [Display(Name = "Perderdor")]
        public int idPerderdor { get; set; }
        [Display(Name = "Desafiado")]
        [ForeignKey("desafiado_id")]
        public virtual UserProfile desafiado { get; set; }

        [Display(Name = "Desafiante")]
        [ForeignKey("desafiante_id")]
        public virtual UserProfile desafiante { get; set; }

        [Display(Name = "Rodada")]
        [ForeignKey("rodada_id")]
        public virtual Rodada rodada { get; set; }

        public int situacao_Id { get; set; }

        [Display(Name = "Situação")]
        [ForeignKey("situacao_Id")]
        public virtual SituacaoJogo situacao{get; set;}

        public int? torneioId { get; set; }

        public int? faseTorneio { get; set; }

        public int? classeTorneio { get; set; }

        public int? ordemJogo { get; set; }

        public virtual int setsJogados
        {
            get { 
                var qtddSetsJogados = 0;
                if (qtddGames1setDesafiado>0 || qtddGames1setDesafiante>0){
                    qtddSetsJogados++;
                }
                if (qtddGames2setDesafiado>0 || qtddGames2setDesafiante>0){
                    qtddSetsJogados++;
                }
                if (qtddGames3setDesafiado>0 || qtddGames3setDesafiante>0){
                    qtddSetsJogados++;
                }
                return qtddSetsJogados;
            }
        
        }
        public virtual int gamesJogados
        {
            get
            {
                return qtddGames1setDesafiado + qtddGames1setDesafiante + qtddGames2setDesafiado + qtddGames2setDesafiante + qtddGames3setDesafiado + qtddGames3setDesafiante;
            }

        }

        public virtual int gamesGanhosDesafiante
        {
            get
            {
                return qtddGames1setDesafiante + qtddGames2setDesafiante + qtddGames3setDesafiante;
            }

        }

        public virtual int gamesGanhosDesafiado
        {
            get
            {
                return qtddGames1setDesafiado + qtddGames2setDesafiado + qtddGames3setDesafiado;
            }

        }

        public virtual int qtddSetsGanhosDesafiante
        {
            get
            {
                var qtddSetsGanhos=0;
                if (qtddGames1setDesafiante > qtddGames1setDesafiado){
                    qtddSetsGanhos++;
                }
                if (qtddGames2setDesafiante > qtddGames2setDesafiado)
                {
                    qtddSetsGanhos++;
                }
                if (qtddGames3setDesafiante > qtddGames3setDesafiado)
                {
                    qtddSetsGanhos++;
                }
                return qtddSetsGanhos;
            }

        }

        public virtual int qtddSetsGanhosDesafiado
        {
            get
            {
                var qtddSetsGanhos = 0;
                if (qtddGames1setDesafiado > qtddGames1setDesafiante)
                {
                    qtddSetsGanhos++;
                }
                if (qtddGames2setDesafiado > qtddGames2setDesafiante)
                {
                    qtddSetsGanhos++;
                }
                if (qtddGames3setDesafiado > qtddGames3setDesafiante)
                {
                    qtddSetsGanhos++;
                }
                return qtddSetsGanhos;
            }

        }

        public virtual int idDoVencedor
        {
            get
            {
                if (qtddSetsGanhosDesafiado > qtddSetsGanhosDesafiante)
                {
                    return desafiado_id;
                }
                if (qtddSetsGanhosDesafiado < qtddSetsGanhosDesafiante)
                {
                    return desafiante_id;
                }
                return 0;
            }

        }

        public virtual int idDoPerdedor
        {
            get
            {
                if (qtddSetsGanhosDesafiado > qtddSetsGanhosDesafiante)
                {
                    return desafiante_id;
                }
                if (qtddSetsGanhosDesafiado < qtddSetsGanhosDesafiante)
                {
                    return desafiado_id;
                }
                return 0;
            }

        }
    }
}