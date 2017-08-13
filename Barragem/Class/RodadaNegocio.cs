using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Barragem.Models;
using Barragem.Context;

namespace Barragem.Class
{
    public class RodadaNegocio
    {
        private BarragemDbContext db = new BarragemDbContext();
        public float calcularPontosDesafiante(Jogo jogo)
        {
            float pontuacao = 0;
            // se a quantidade de games jogados for igual a zero, quer dizer que não houve jogo e o jogador ficará com zero de pontuação nesta rodada
            if (jogo.gamesJogados != 0 && !jogo.desafiante.situacao.Equals("curinga"))
            {
                int bonus = 1;
                float desempenho = calcularDesempenho(jogo.qtddSetsGanhosDesafiante, jogo.gamesGanhosDesafiante, jogo.setsJogados, jogo.gamesJogados);
                if (jogo.idDoVencedor == jogo.desafiante_id)
                {
                    bonus = 6;
                }
                Rancking ranking = db.Rancking.Where(r => r.userProfile_id == jogo.desafiado_id && r.rodada.isAberta == false && r.rodada_id < jogo.rodada_id).OrderByDescending(ran => ran.rodada_id).Take(1).Single();
                if (ranking.posicao == 0)
                {
                    ranking.posicao = 32; // jogador sem ranking ficara na posicao 32 para efeitos de calculos
                }
                pontuacao = (desempenho / (ranking.posicao + 4)) + bonus;
                pontuacao = bonificacaoPorDiferencaPlacar(jogo, "desafiante", pontuacao);
                if ((jogo.idDoVencedor == jogo.desafiante_id) && (jogo.situacao_Id == 5))
                { // WO
                    pontuacao = (pontuacao * 70) / 100;
                }
                if ((jogo.idDoVencedor != jogo.desafiante_id) && (jogo.situacao_Id == 5))
                { // WO
                    pontuacao = 0;
                }
                pontuacao = (float)Math.Round(pontuacao, 2);
            }
            return pontuacao;
        }

        public float calcularPontosDesafiado(Jogo jogo)
        {
            int bonus = 1;
            float pontuacao = 0;
            // se a quantidade de games jogados for igual a zero, quer dizer que não houve jogo e o jogador ficará com zero de pontuação nesta rodada
            if (jogo.gamesJogados != 0)
            {
                if (jogo.desafiante.situacao.Equals("curinga"))
                {
                    pontuacao = 4;
                    return pontuacao;
                }
                float desempenho = calcularDesempenho(jogo.qtddSetsGanhosDesafiado, jogo.gamesGanhosDesafiado, jogo.setsJogados, jogo.gamesJogados);
                //if (jogo.desafiado_id == 4)
                //{
                //    bonus = 1;
                //}
                if (jogo.idDoVencedor == jogo.desafiado_id)
                {
                    bonus = 6;
                }
                Rancking ranking = db.Rancking.Where(r => r.userProfile_id == jogo.desafiante_id && r.rodada.isAberta == false && r.rodada_id < jogo.rodada_id).OrderByDescending(ran => ran.rodada_id).Take(1).Single();
                if (ranking.posicao == 0)
                {
                    ranking.posicao = 32; // jogador sem ranking ficara na posicao 32 para efeitos de calculos
                }
                pontuacao = (desempenho / (ranking.posicao + 4)) + bonus;

                pontuacao = bonificacaoPorDiferencaPlacar(jogo, "desafiado", pontuacao);
                if ((jogo.idDoVencedor == jogo.desafiado_id) && (jogo.situacao_Id == 5))
                { // WO
                    pontuacao = (pontuacao * 70) / 100;
                }
                if ((jogo.idDoVencedor != jogo.desafiado_id) && (jogo.situacao_Id == 5))
                { // WO
                    pontuacao = 0;
                }
                pontuacao = (float)Math.Round(pontuacao, 2);
            }
            return pontuacao;
        }

        public float bonificacaoPorDiferencaPlacar(Jogo jogo, string tipoJogador, float pontuacao)
        {
            if (tipoJogador == "desafiado")
            {
                if ((jogo.gamesGanhosDesafiado > 11) && (jogo.gamesGanhosDesafiante < 2))
                {
                    pontuacao = pontuacao + (pontuacao / 2);
                }
            }
            else
            {
                if ((jogo.gamesGanhosDesafiante > 11) && (jogo.gamesGanhosDesafiado < 2))
                {
                    pontuacao = pontuacao + ((pontuacao * 30) / 100);
                }
            }
            return pontuacao;
        }

        public float calcularDesempenho(int setsGanhos, int gamesGanhos, int setsJogados, int gamesJogados)
        {
            float desempenho = 0;
            if (gamesJogados != 0)
            {
                desempenho = ((float)((6 * setsGanhos) + gamesGanhos) / ((6 * setsJogados) + gamesJogados)) * 100;
            }
            return desempenho;
        }

        public void gravarPontuacaoNaRodada(int idRodada, UserProfile jogador, double pontosConquistados, bool isReprocessamento = false)
        {
            if (jogador.situacao.Equals("curinga") || jogador.situacao.Equals("pendente"))
            {
                return;
            }
            Rancking ran = null;
            double pontuacaoTotal = db.Rancking.Where(r => r.rodada.isAberta == false && r.userProfile_id == jogador.UserId && r.rodada_id < idRodada).OrderByDescending(r => r.Id).Take(9).Sum(r => r.pontuacao);

            if (isReprocessamento)
            {
                ran = db.Rancking.Where(r => r.rodada_id == idRodada && r.userProfile_id == jogador.UserId).Single();
                ran.pontuacao = Math.Round(pontosConquistados, 2);
                //ran.totalAcumulado = Math.Round(pontuacaoTotal + pontosConquistados, 2);
                db.SaveChanges();
            }
            else
            {
                ran = new Rancking();
                ran.rodada_id = idRodada;
                ran.pontuacao = Math.Round(pontosConquistados, 2);
                ran.totalAcumulado = Math.Round(pontuacaoTotal + pontosConquistados, 2);
                ran.posicao = 0;
                ran.userProfile_id = jogador.UserId;
                ran.classeId = jogador.classeId;
                db.Rancking.Add(ran);
                db.SaveChanges();
            }

        }

        public void gerarPontuacaoDosJogadoresForaDaRodada(int idRodada, int barragemId = 1)
        {
            string suspenso = Tipos.Situacao.suspenso.ToString();
            List<UserProfile> jogadores = db.UserProfiles.Where(j => j.barragemId == barragemId).ToList();
            foreach (UserProfile user in jogadores)
            {
                int estaNaRodadaAtual = db.Jogo.Where(j => j.rodada_id == idRodada && (j.desafiante_id == user.UserId || j.desafiado_id == user.UserId)).Count();
                if (estaNaRodadaAtual == 0)
                {
                    if (user.situacao == suspenso)
                    {
                        gravarPontuacaoNaRodada(idRodada, user, 0.0);
                    }
                    else if ((user.situacao == "ativo") || (user.situacao == "licenciado") || (user.situacao == "desativado") || (user.situacao == "inativo"))
                    {
                        gravarPontuacaoNaRodada(idRodada, user, 3.0);
                    }
                }
            }
        }

    }
}