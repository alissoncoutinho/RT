using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Barragem.Models;

namespace Barragem.Context
{
    public class BarragemDbContext : DbContext
    {
        public BarragemDbContext()
            : base("DefaultConnection")
        {

        }
        public DbSet<Rancking> Rancking { get; set; }

        public DbSet<RankingView> RankingView { get; set; }

        public DbSet<SituacaoJogo> SituacaoJogo { get; set; }
        public DbSet<Jogo> Jogo { get; set; }
        public DbSet<Rodada> Rodada { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }

        public DbSet<Barragens> Barragens { get; set; }

        public DbSet<BarragemView> BarragemView { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }

        public DbSet<Configuracao> Configuracao { get; set; }

        public DbSet<Temporada> Temporada { get; set; }

        public DbSet<Torneio> Torneio { get; set; }

        public DbSet<InscricaoTorneio> InscricaoTorneio { get; set; }

        public DbSet<Classe> Classe { get; set; }
                


    }
}