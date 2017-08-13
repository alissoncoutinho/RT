using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;

namespace Barragem.Class
{
    public static class Tipos
    {
        public enum FormatoEmail { Texto = 1, Html = 2 }

        public enum Situacao { desativado, ativo, pendente, licenciado, suspenso }

    }
}