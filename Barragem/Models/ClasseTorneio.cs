using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Models
{
    [Table("ClasseTorneio")]
    public class ClasseTorneio
    {
        [Key]
        public int Id { get; set; }
        public string nome { get; set; }
        public int nivel { get; set; }
        public int torneioId { get; set; }
    }
}