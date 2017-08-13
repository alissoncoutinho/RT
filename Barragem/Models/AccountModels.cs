using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Web.Security;

namespace Barragem.Models
{
    

    [Table("UserProfile")]
    public class UserProfile
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Display(Name = "login")]
        public string UserName { get; set; }

        [Display(Name = "nome (ou apelido)")]
        [Required(ErrorMessage = "O campo nome é obrigatório")]
        public string nome { get; set; }

        [Display(Name = "data nascimento")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Este campo é obrigatório")]
        public DateTime dataNascimento { get; set; }

        [Display(Name = "naturalidade")]
        public string naturalidade { get; set; }

        [Display(Name = "altura (cm)")]
        [Required(ErrorMessage = "Este campo é obrigatório")]
        public int altura { get; set; }

        [Display(Name = "lateralidade")]
        public string lateralidade { get; set; }

        [Display(Name = "telefone")]
        public string telefoneFixo { get; set; }

        [Required(ErrorMessage = "O campo celular é obrigatório")]
        [Display(Name = "celular/operadora")]
        public string telefoneCelular { get; set; }

        [Display(Name = "celular2/operadora")]
        public string telefoneCelular2 { get; set; }

        [Required(ErrorMessage = "O campo bairro é obrigatório")]
        public string bairro { get; set; }

        [Display(Name = "Matrícula (apenas para clubes)")]
        public string matriculaClube { get; set; }
        public string situacao { get; set; }

        [Required(ErrorMessage = "O campo email é obrigatório")]
        [DataType(DataType.EmailAddress)]
        public string email { get; set; }
        [Display(Name = "Foto")]
        public byte[] foto { get; set; }

        [Display(Name = "Já possui rancking")]
        public bool isRanckingGerado { get; set; }

        public DateTime dataInicioRancking { get; set; }
        
        [Display(Name = "Nível de jogo")]
        public string nivelDeJogo { get; set; }

        [Display(Name = "Ranking")]
        public int barragemId { get; set; }

        [Display(Name = "Ranking")]
        [ForeignKey("barragemId")]
        public virtual BarragemView barragem { get; set; }

        [Display(Name = "Classe")]
        public int? classeId { get; set; }

        [Display(Name = "Classe")]
        [ForeignKey("classeId")]
        public virtual Classe classe { get; set; }
    }

    public class RegisterExternalLoginModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        public string ExternalLoginData { get; set; }
    }

    public class LocalPasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Senha atual")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nova senha")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nova senha")]
        [Compare("NewPassword", ErrorMessage = "A nova senha e a confirmação de senha não estão iguais.")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Manter-me logado")]
        public bool RememberMe { get; set; }
    }

    public class RegisterModel
    {
        [Required]
        [Display(Name = "Login")]
        public string UserName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar senha")]
        [Compare("Password", ErrorMessage = "A senha e a confirmação de senha não estão iguais.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "nome (ou apelido)")]
        [Required(ErrorMessage = "O campo nome é obrigatório")]
        public string nome { get; set; }

        [Display(Name = "data nascimento")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Este campo é obrigatório")]
        public DateTime dataNascimento { get; set; }
        [Display(Name = "naturalidade")]
        public string naturalidade { get; set; }

        [Display(Name = "altura (cm)")]
        [Required(ErrorMessage = "Este campo é obrigatório")]
        public int altura { get; set; }

        [Display(Name = "lateralidade")]
        public string lateralidade { get; set; }

        [Display(Name = "telefone")]
        public string telefoneFixo { get; set; }

        [Required(ErrorMessage = "O campo celular é obrigatório")]
        [Display(Name = "celular/operadora")]
        public string telefoneCelular { get; set; }

        [Display(Name = "celular2/operadora")]
        public string telefoneCelular2 { get; set; }

        [Required(ErrorMessage = "O campo bairro é obrigatório")]
        public string bairro { get; set; }

        [Display(Name = "Matrícula (apenas para clubes)")]
        public string matriculaClube { get; set; }
        public string situacao { get; set; }

        [Required(ErrorMessage = "O campo email é obrigatório")]
        [DataType(DataType.EmailAddress)]
        public string email { get; set; }
        [Display(Name = "Foto")]
        public byte[] foto { get; set; }

        public DateTime dataInicioRancking { get; set; }
        
        [Display(Name = "Nível de jogo")]
        public string nivelDeJogo { get; set; }

        [Display(Name = "Barragem")]
        public int barragemId { get; set; }

        [Display(Name = "Barragem")]
        [ForeignKey("barragemId")]
        public virtual Barragens barragem { get; set; }

        [Display(Name = "Classe")]
        public int? classeId { get; set; }

        [Display(Name = "Classe")]
        [ForeignKey("classeId")]
        public virtual Classe classe { get; set; }
        
    }

    public class ExternalLogin
    {
        public string Provider { get; set; }
        public string ProviderDisplayName { get; set; }
        public string ProviderUserId { get; set; }
    }

    public class ConfirmaSenhaModel
    {
        public string TokenId { get; set; }
        [Required(ErrorMessage = "Campo obrigatório")]
        public string Senha { get; set; }
        [Required(ErrorMessage = "Campo obrigatório")]
        public string SenhaConfirmacao { get; set; }
    }
}
