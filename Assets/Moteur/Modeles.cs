using System.Collections.Generic;
using System.Text;

namespace ProjetGame.Moteur
{
    // Une compétence d'un personnage.
    // Type : "1" = 1 ennemi, "A" = tous les ennemis, "H1" = soin d'un allié, "HA" = soin de tous les alliés.
    public class Competence
    {
        public string Nom;
        public int Puissance;   // en % de l'ATQ (pour un soin : en % des PV max du soigneur)
        public int Coups;       // nombre de coups
        public int Recharge;    // recharge en tours (0 = aucune)
        public string Type;

        public Competence(string nom, int puissance, int coups, int recharge, string type)
        {
            Nom = nom; Puissance = puissance; Coups = coups; Recharge = recharge; Type = type;
        }

        public bool EstSoin { get { return Type[0] == 'H'; } }
    }

    // Passif de meneur : un bonus fixe pour toute l'équipe si le perso est désigné meneur.
    // Genre : "atq", "atqdps", "def", "pv", "spd", "heal" ou "crit".
    public class Passif
    {
        public string Genre;
        public int Valeur;
        public string Texte;

        public Passif(string genre, int valeur, string texte)
        {
            Genre = genre; Valeur = valeur; Texte = texte;
        }
    }

    // La fiche d'un personnage (ses données de départ).
    public class Perso
    {
        public string Nom;
        public string Role;
        public string Rarete;
        public int Spd;
        public int Crit;                 // taux de critique en %
        public Competence[] Competences;
        public Passif Meneur;            // null si le perso n'a pas de passif de meneur
        public int[] StatsFixes;         // {PV, ATQ, DEF} réglées à la main, ou null

        public Perso(string nom, string role, string rarete, int spd, int crit,
                     Competence[] competences, Passif meneur, int[] statsFixes)
        {
            Nom = nom; Role = role; Rarete = rarete; Spd = spd; Crit = crit;
            Competences = competences; Meneur = meneur; StatsFixes = statsFixes;
        }
    }

    // Un personnage engagé dans un combat, avec ses stats du moment.
    public class Unite
    {
        public string Equipe;      // "A" ou "B"
        public int Position;       // 0 à 4 (0 = premier de la ligne)
        public int IndexPerso;
        public string Nom;         // par exemple "A1 Valdis"
        public string Role;
        public string Rarete;
        public int Max;
        public int Pv;
        public int Atq;
        public int Def;
        public double Spd;
        public double Crit;        // taux de critique en %
        public double DegCrit;     // dégâts critiques en plus de la base (+50 %)
        public double Soin;        // bonus de puissance de soin en %
        public Competence[] Competences;
        public int[] Recharges;    // recharge restante de chaque compétence
    }

    // Ce qui arrive à une cible lors d'une action.
    public class EffetCible
    {
        public string Nom;
        public int Valeur;         // dégâts ou soin
        public int Critiques;
        public bool Mort;
        public string Equipe;      // équipe de la cible ("A" ou "B")
        public int Position;       // position de la cible (0 à 4)
        public int PvApres;        // PV de la cible après l'action
    }

    // Un événement du combat. Le client rejouera cette liste en animation.
    public class Evenement
    {
        public string Type;        // "Round", "Meneur" ou "Action"
        public int Round;
        public string Equipe;
        public string Acteur;
        public int ActeurPosition;     // position (0 à 4) de celui qui joue
        public int IndexCompetence;    // 0, 1 ou 2 : la compétence utilisée
        public int[] RechargesAvant;   // recharges de ses compétences juste avant l'action
        public string Competence;
        public bool EstSoin;
        public string Texte;
        public List<EffetCible> Cibles = new List<EffetCible>();

        public string EnTexte()
        {
            if (Type == "Round") return "— Round " + Round + " —";
            if (Type == "Meneur") return Texte;
            var sb = new StringBuilder();
            sb.Append(Acteur).Append(" — ").Append(Competence).Append(" → ");
            for (int i = 0; i < Cibles.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                EffetCible c = Cibles[i];
                sb.Append(c.Nom).Append(EstSoin ? " +" : " −").Append(c.Valeur);
                if (c.Critiques > 0) sb.Append(" (").Append(c.Critiques).Append(" crit)");
                if (c.Mort) sb.Append(" ✝");
            }
            return sb.ToString();
        }
    }

    // Le résultat d'un combat.
    public class Resultat
    {
        public string Gagnant;             // "A" ou "B"
        public int Rounds;
        public bool LimiteAtteinte;        // vrai si la limite de 100 rounds est atteinte (défaite de A)
        public List<Unite> Unites;         // état final des 10 persos
        public List<Evenement> Journal;    // vide si le journal n'a pas été demandé
    }
}
