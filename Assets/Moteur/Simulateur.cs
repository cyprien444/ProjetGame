using System;
using System.Collections.Generic;

namespace ProjetGame.Moteur
{
    // Le moteur de combat. Il ne dépend pas de Unity : il pourra tourner tel quel sur un serveur.
    public static class Simulateur
    {
        public const int MaxRounds = 100;

        static int Pourcent(int x, int v)
        {
            return (int)Math.Floor(x * (100 + v) / 100.0);
        }

        // Crée un personnage prêt à combattre, avec ses Reliques (niveau 0, 6 ou 12).
        public static Unite CreerUnite(string equipe, int indexPerso, int position, int niveauReliques)
        {
            Perso p = Donnees.Persos[indexPerso];
            double[] b = Donnees.Base[p.Role];
            int m = Donnees.MultRarete[p.Rarete];
            double pv, atq, def;
            if (p.StatsFixes != null)
            {
                pv = p.StatsFixes[0]; atq = p.StatsFixes[1]; def = p.StatsFixes[2];
            }
            else
            {
                pv = Math.Floor(b[0] * m / 100); atq = Math.Floor(b[1] * m / 100); def = Math.Floor(b[2] * m / 100);
            }
            Bonus e = Donnees.BonusReliques(p.Role, niveauReliques);
            pv = Math.Floor((pv + e.PvFixe) * (100 + e.PvPct) / 100);
            atq = Math.Floor((atq + e.AtqFixe) * (100 + e.AtqPct) / 100);
            def = Math.Floor((def + e.DefFixe) * (100 + e.DefPct) / 100);
            return new Unite
            {
                Equipe = equipe, Position = position, IndexPerso = indexPerso,
                Nom = equipe + (position + 1) + " " + p.Nom,
                Role = p.Role, Rarete = p.Rarete,
                Max = (int)pv, Pv = 0, Atq = (int)atq, Def = (int)def,
                Spd = p.Spd + e.Spd, Crit = p.Crit + e.Crit, DegCrit = e.DegCrit, Soin = 0,
                Competences = p.Competences,
                Recharges = new int[p.Competences.Length],
            };
        }

        // Applique le passif du meneur à toute son équipe. Renvoie son texte, ou null s'il n'y en a pas.
        static string AppliquerMeneur(List<Unite> unites, string equipe, int[] persos, int meneur)
        {
            if (meneur < 0) return null;
            Passif p = Donnees.Persos[persos[meneur]].Meneur;
            if (p == null) return null;
            foreach (Unite u in unites)
            {
                if (u.Equipe != equipe) continue;
                switch (p.Genre)
                {
                    case "atq": u.Atq = Pourcent(u.Atq, p.Valeur); break;
                    case "atqdps": if (u.Role == "DPS") u.Atq = Pourcent(u.Atq, p.Valeur); break;
                    case "def": u.Def = Pourcent(u.Def, p.Valeur); break;
                    case "pv": u.Max = Pourcent(u.Max, p.Valeur); break;
                    case "spd": u.Spd = Math.Floor(u.Spd * (100 + p.Valeur) / 100.0); break;
                    case "heal": u.Soin += p.Valeur; break;
                    case "crit": u.Crit += p.Valeur; break;
                }
            }
            return p.Texte;
        }

        static List<Unite> Vivants(List<Unite> unites, string equipe)
        {
            var l = new List<Unite>();
            foreach (Unite u in unites) if (u.Equipe == equipe && u.Pv > 0) l.Add(u);
            return l;
        }

        // equipeA / equipeB : les 5 persos (index dans Donnees.Persos), dans l'ordre de la ligne.
        // meneurA / meneurB : position du meneur (0 à 4), ou -1 pour aucun.
        // graine : fixe les tirages aléatoires (même graine = même combat).
        // niveauReliques : 0, 6 ou 12.
        // cibleLePremier : vrai = les compétences sur 1 ennemi visent le premier de la ligne.
        public static Resultat Combattre(int[] equipeA, int meneurA, int[] equipeB, int meneurB,
                                         uint graine, int niveauReliques,
                                         bool cibleLePremier = true, bool avecJournal = false)
        {
            if (!Donnees.Charge)
                throw new Exception("Les données ne sont pas chargées : appelle d'abord ChargeurDonnees.Charger() (ou Donnees.Charger).");
            var rng = new Mulberry32(graine);
            var unites = new List<Unite>();
            for (int i = 0; i < equipeA.Length; i++) unites.Add(CreerUnite("A", equipeA[i], i, niveauReliques));
            for (int i = 0; i < equipeB.Length; i++) unites.Add(CreerUnite("B", equipeB[i], i, niveauReliques));
            string pa = AppliquerMeneur(unites, "A", equipeA, meneurA);
            string pb = AppliquerMeneur(unites, "B", equipeB, meneurB);
            foreach (Unite u in unites) u.Pv = u.Max;

            var journal = new List<Evenement>();
            if (avecJournal)
            {
                if (pa != null) journal.Add(new Evenement { Type = "Meneur", Texte = "Meneur A : " + pa });
                if (pb != null) journal.Add(new Evenement { Type = "Meneur", Texte = "Meneur B : " + pb });
            }

            for (int round = 1; round <= MaxRounds; round++)
            {
                if (avecJournal) journal.Add(new Evenement { Type = "Round", Round = round });

                // Ordre de jeu : SPD décroissante. À égalité : l'équipe B d'abord, puis par position.
                List<Unite> ordre = Vivants(unites, "A");
                ordre.AddRange(Vivants(unites, "B"));
                ordre.Sort((x, y) =>
                {
                    int c = y.Spd.CompareTo(x.Spd);
                    if (c != 0) return c;
                    if (x.Equipe == y.Equipe) return x.Position - y.Position;
                    return x.Equipe == "B" ? -1 : 1;
                });

                foreach (Unite u in ordre)
                {
                    if (u.Pv <= 0) continue;
                    for (int j = 1; j < u.Competences.Length; j++)
                        if (u.Recharges[j] > 0) u.Recharges[j]--;

                    List<Unite> ennemis = Vivants(unites, u.Equipe == "A" ? "B" : "A");
                    List<Unite> allies = Vivants(unites, u.Equipe);
                    bool blesse = false;
                    foreach (Unite a in allies) if (a.Pv < a.Max) blesse = true;

                    // Compétence la plus haute disponible (un soin seulement si un allié est blessé).
                    int k = 0;
                    for (int j = u.Competences.Length - 1; j > 0; j--)
                    {
                        Competence cj = u.Competences[j];
                        if (u.Recharges[j] == 0 && (!cj.EstSoin || blesse)) { k = j; break; }
                    }
                    Competence s = u.Competences[k];
                    int[] rechargesAvant = avecJournal ? (int[])u.Recharges.Clone() : null;
                    u.Recharges[k] = s.Recharge;

                    var ev = new Evenement
                    {
                        Type = "Action", Round = round, Equipe = u.Equipe, Acteur = u.Nom, ActeurPosition = u.Position,
                        IndexCompetence = k, RechargesAvant = rechargesAvant, Competence = s.Nom, EstSoin = s.EstSoin
                    };

                    if (s.EstSoin)
                    {
                        List<Unite> cibles = new List<Unite>();
                        if (s.Type == "HA") cibles = allies;
                        else
                        {
                            Unite pire = allies[0];
                            for (int i = 1; i < allies.Count; i++)
                            {
                                Unite b = allies[i];
                                if (!((double)pire.Pv * b.Max <= (double)b.Pv * pire.Max)) pire = b;
                            }
                            cibles.Add(pire);
                        }
                        foreach (Unite m in cibles)
                        {
                            int h = (int)Math.Min(m.Max - m.Pv, Math.Floor(u.Max * (double)s.Puissance * (100 + u.Soin) / 10000));
                            m.Pv += h;
                            ev.Cibles.Add(new EffetCible { Nom = m.Nom, Valeur = h, Equipe = m.Equipe, Position = m.Position, PvApres = m.Pv });
                        }
                    }
                    else
                    {
                        List<Unite> cibles = new List<Unite>();
                        if (s.Type == "A") cibles = ennemis;
                        else
                        {
                            Unite choix = ennemis[0];
                            for (int i = 1; i < ennemis.Count; i++)
                            {
                                Unite b = ennemis[i];
                                bool garder = cibleLePremier ? choix.Position <= b.Position : choix.Pv <= b.Pv;
                                if (!garder) choix = b;
                            }
                            cibles.Add(choix);
                        }
                        foreach (Unite m in cibles)
                        {
                            int total = 0, crits = 0;
                            for (int h = 0; h < s.Coups && m.Pv > 0; h++)
                            {
                                bool critique = rng.Suivant() * 100 < Math.Min(100.0, u.Crit);
                                double facteur = critique ? 150 + u.DegCrit : 100;
                                int d = (int)Math.Floor(u.Atq * (double)s.Puissance * 1000 * facteur / ((1000 + 3.0 * m.Def) * 10000));
                                m.Pv = Math.Max(0, m.Pv - d);
                                total += d;
                                if (critique) crits++;
                            }
                            ev.Cibles.Add(new EffetCible { Nom = m.Nom, Valeur = total, Critiques = crits, Mort = m.Pv == 0, Equipe = m.Equipe, Position = m.Position, PvApres = m.Pv });
                        }
                    }
                    if (avecJournal) journal.Add(ev);

                    bool aVivant = Vivants(unites, "A").Count > 0;
                    bool bVivant = Vivants(unites, "B").Count > 0;
                    if (!aVivant || !bVivant)
                    {
                        return new Resultat { Gagnant = aVivant ? "A" : "B", Rounds = round, Unites = unites, Journal = journal };
                    }
                }
            }
            return new Resultat { Gagnant = "B", Rounds = MaxRounds, LimiteAtteinte = true, Unites = unites, Journal = journal };
        }
    }
}
