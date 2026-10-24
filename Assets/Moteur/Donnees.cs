using System.Collections.Generic;

namespace ProjetGame.Moteur
{
    // Bonus apportés par les Reliques d'un personnage.
    public class Bonus
    {
        public double AtqFixe, DefFixe, PvFixe;
        public double AtqPct, DefPct, PvPct;
        public double Spd, Crit, DegCrit;
    }

    // Toutes les données du jeu pour le prototype : persos, rareté, Reliques.
    // Plus tard, ces données iront dans des fichiers séparés pour modifier l'équilibrage sans toucher au code.
    public static class Donnees
    {
        // PV, ATQ, DEF de base à la rareté "Commun" pour chaque rôle.
        public static readonly Dictionary<string, double[]> Base = new Dictionary<string, double[]>
        {
            { "Tank", new double[] { 1800, 120, 130 } },
            { "DPS", new double[] { 1000, 220, 60 } },
            { "Support", new double[] { 1200, 140, 80 } },
            { "Soigneur", new double[] { 1300, 100, 80 } },
        };

        // Multiplicateur de la rareté, en %.
        public static readonly Dictionary<string, int> MultRarete = new Dictionary<string, int>
        {
            { "Commun", 100 }, { "Atypique", 115 }, { "Rare", 130 }, { "Épique", 150 }, { "Légendaire", 175 },
        };

        // Les 15 personnages du prototype, dans l'ordre du doc (le numéro 1 est à l'index 0).
        public static readonly Perso[] Persos = new Perso[]
        {
            new Perso("Garrick", "Tank", "Commun", 25, 5,
                new Competence[] {
                    new Competence("Coup de bouclier", 100, 1, 0, "1"),
                    new Competence("Porte close", 140, 1, 3, "1"),
                    new Competence("Cri du rempart", 80, 1, 5, "A")
                },
                null, null),
            new Perso("Tamsin", "DPS", "Commun", 53, 32,
                new Competence[] {
                    new Competence("Flèche des marais", 100, 1, 0, "1"),
                    new Competence("Triple tir", 65, 3, 3, "1")
                },
                null, null),
            new Perso("Pip", "Support", "Commun", 49, 12,
                new Competence[] {
                    new Competence("Note aiguë", 100, 1, 0, "1"),
                    new Competence("Refrain entêtant", 130, 1, 3, "1")
                },
                null, null),
            new Perso("Maëlle", "Soigneur", "Commun", 39, 5,
                new Competence[] {
                    new Competence("Coup de louche", 100, 1, 0, "1"),
                    new Competence("Cataplasme", 25, 1, 3, "H1")
                },
                null, null),
            new Perso("Hakon", "Tank", "Atypique", 31, 12,
                new Competence[] {
                    new Competence("Coup de coque", 100, 1, 0, "1"),
                    new Competence("Abordage", 150, 1, 4, "1"),
                    new Competence("Vague de bord", 85, 1, 5, "A")
                },
                new Passif("spd", 10, "Équipage soudé : +10 % SPD"), null),
            new Perso("Kenzo", "DPS", "Atypique", 51, 28,
                new Competence[] {
                    new Competence("Coupe-vent", 100, 1, 0, "1"),
                    new Competence("Double lame", 95, 2, 3, "1"),
                    new Competence("Danse des lames", 105, 1, 5, "A")
                },
                null, null),
            new Perso("Zora", "Support", "Atypique", 47, 22,
                new Competence[] {
                    new Competence("Fiole vive", 100, 1, 0, "1"),
                    new Competence("Flacon explosif", 140, 1, 3, "1"),
                    new Competence("Brouillard acide", 85, 1, 4, "A")
                },
                null, null),
            new Perso("Anselm", "Soigneur", "Atypique", 36, 5,
                new Competence[] {
                    new Competence("Coup de bâton", 100, 1, 0, "1"),
                    new Competence("Tisane apaisante", 25, 1, 3, "H1"),
                    new Competence("Jardin du monastère", 18, 1, 6, "HA")
                },
                null, null),
            new Perso("Isaure", "Tank", "Rare", 28, 8,
                new Competence[] {
                    new Competence("Estoc", 100, 1, 0, "1"),
                    new Competence("Charge du Rempart", 140, 1, 3, "1"),
                    new Competence("Bastion", 75, 1, 4, "A")
                },
                new Passif("def", 25, "Rempart blanc : +25 % DEF"), null),
            new Perso("Ren", "DPS", "Rare", 54, 33,
                new Competence[] {
                    new Competence("Lame courte", 100, 1, 0, "1"),
                    new Competence("Coup dans le dos", 190, 1, 4, "1"),
                    new Competence("Pluie de lames", 105, 1, 5, "A")
                },
                null, new int[] { 1160, 258, 78 }),
            new Perso("Aurélien", "Support", "Rare", 45, 10,
                new Competence[] {
                    new Competence("Coup de hampe", 100, 1, 0, "1"),
                    new Competence("Bannière tranchante", 130, 1, 3, "1"),
                    new Competence("Charge des couleurs", 90, 1, 4, "A")
                },
                new Passif("atq", 20, "Bannière dorée : +20 % ATQ"), null),
            new Perso("Naïa", "Soigneur", "Rare", 42, 10,
                new Competence[] {
                    new Competence("Éclat de source", 100, 1, 0, "1"),
                    new Competence("Goutte claire", 28, 1, 3, "H1"),
                    new Competence("Marée de visions", 15, 1, 4, "HA")
                },
                new Passif("heal", 30, "Visions des sources : +30 % de soin"), null),
            new Perso("Valdis", "Tank", "Épique", 33, 15,
                new Competence[] {
                    new Competence("Poing enchaîné", 100, 1, 0, "1"),
                    new Competence("Chaînes brisées", 160, 1, 3, "1"),
                    new Competence("Rugissement de l’arène", 90, 1, 5, "A")
                },
                new Passif("pv", 30, "Champion de l’arène : +30 % PV max"), null),
            new Perso("Vesper", "Support", "Épique", 55, 18,
                new Competence[] {
                    new Competence("Plume acérée", 100, 1, 0, "1"),
                    new Competence("Ordre de feu", 135, 1, 3, "1"),
                    new Competence("Plan de bataille", 95, 1, 4, "A")
                },
                new Passif("atqdps", 40, "Stratège : +40 % ATQ aux DPS"), null),
            new Perso("Valen", "DPS", "Légendaire", 60, 40,
                new Competence[] {
                    new Competence("Lame sans ombre", 100, 1, 0, "1"),
                    new Competence("Fente fantôme", 200, 1, 3, "1"),
                    new Competence("Éclipse", 120, 1, 5, "A")
                },
                new Passif("crit", 15, "Premier réveillé : +15 points de critique"), new int[] { 1230, 290, 98 })
        };

        // ---- Reliques : valeurs [à +0, à +12] ----
        static readonly double[] AtqFixe = { 10, 50 };
        static readonly double[] DefFixe = { 6, 30 };
        static readonly double[] PvFixe = { 100, 300 };
        static readonly double[] AtqPct = { 8, 40 };
        static readonly double[] DefPct = { 8, 35 };
        static readonly double[] PvPct = { 8, 25 };
        static readonly double[] SpdRel = { 2, 10 };
        static readonly double[] CritRel = { 4, 20 };
        static readonly double[] DegCritRel = { 6, 40 };

        static double Niv(double[] v, int n) { return v[0] + (v[1] - v[0]) * n / 12.0; }

        // Build de référence par rôle : attributs principaux des bottes, gants et amulette, puis les 3 sets.
        static readonly Dictionary<string, string[]> Principaux = new Dictionary<string, string[]>
        {
            { "Tank", new[] { "pv", "def", "pv" } },
            { "DPS", new[] { "atq", "dcrit", "atq" } },
            { "Support", new[] { "spd", "atq", "pv" } },
            { "Soigneur", new[] { "pv", "pv", "pv" } },
        };
        static readonly Dictionary<string, string[]> Sets = new Dictionary<string, string[]>
        {
            { "Tank", new[] { "Basalte", "Plume", "Basalte" } },
            { "DPS", new[] { "Braise", "Encre", "Braise" } },
            { "Support", new[] { "Braise", "Basalte", "Braise" } },
            { "Soigneur", new[] { "Basalte", "Plume", "Basalte" } },
        };

        // niveau = 0 (sans Reliques), 6 ou 12.
        public static Bonus BonusReliques(string role, int niveau)
        {
            var o = new Bonus();
            if (niveau == 0) return o;
            o.AtqFixe = Niv(AtqFixe, niveau);
            o.DefFixe = Niv(DefFixe, niveau);
            o.PvFixe = Niv(PvFixe, niveau);
            foreach (string k in Principaux[role])
            {
                if (k == "atq") o.AtqPct += Niv(AtqPct, niveau);
                if (k == "def") o.DefPct += Niv(DefPct, niveau);
                if (k == "pv") o.PvPct += Niv(PvPct, niveau);
                if (k == "spd") o.Spd += Niv(SpdRel, niveau);
                if (k == "crit") o.Crit += Niv(CritRel, niveau);
                if (k == "dcrit") o.DegCrit += Niv(DegCritRel, niveau);
            }
            int paires = niveau >= 12 ? 3 : 2;
            for (int i = 0; i < paires; i++)
            {
                string s = Sets[role][i];
                if (s == "Braise") o.AtqPct += 15;
                if (s == "Basalte") o.PvPct += 10;
                if (s == "Plume") o.DefPct += 10;
                if (s == "Encre") o.DegCrit += 30;
            }
            return o;
        }
    }
}
