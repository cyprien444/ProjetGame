using System;
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

    // Toutes les données du jeu : persos, rareté, Reliques.
    // Elles viennent des fichiers persos.json et reliques.json (dossier Assets/Resources/Donnees).
    // Pour changer l'équilibrage, on modifie ces fichiers, pas le code.
    public static class Donnees
    {
        public static bool Charge { get; private set; }

        // PV, ATQ, DEF de base à la rareté "Commun" pour chaque rôle.
        public static Dictionary<string, double[]> Base;
        // Multiplicateur de la rareté, en %.
        public static Dictionary<string, int> MultRarete;
        // Les personnages, dans l'ordre du fichier (le numéro 1 du doc est à l'index 0).
        public static Perso[] Persos;

        // Reliques
        static Dictionary<string, double[]> valeurs;
        static Dictionary<string, string[]> principaux;
        static Dictionary<string, string[]> sets;
        static Dictionary<string, string> setGenre;
        static Dictionary<string, double> setValeur;

        public static void Charger(string persosJson, string reliquesJson)
        {
            // ---- persos.json ----
            var racine = Json.Objet(Json.Lire(persosJson), "persos.json");

            Base = new Dictionary<string, double[]>();
            foreach (var kv in Json.Objet(Json.Cle(racine, "base", "persos.json"), "base"))
                Base[kv.Key] = Json.Nombres(kv.Value, "base/" + kv.Key);

            MultRarete = new Dictionary<string, int>();
            foreach (var kv in Json.Objet(Json.Cle(racine, "multRarete", "persos.json"), "multRarete"))
                MultRarete[kv.Key] = Json.Entier(kv.Value, "multRarete/" + kv.Key);

            var liste = Json.Liste(Json.Cle(racine, "persos", "persos.json"), "persos");
            var persos = new Perso[liste.Count];
            for (int i = 0; i < persos.Length; i++)
            {
                var d = Json.Objet(liste[i], "persos[" + i + "]");
                string nom = Json.Texte(Json.Cle(d, "nom", "persos[" + i + "]"), "nom");
                string ou = "perso « " + nom + " »";
                string role = Json.Texte(Json.Cle(d, "role", ou), "role de " + nom);
                string rarete = Json.Texte(Json.Cle(d, "rarete", ou), "rarete de " + nom);
                if (!Base.ContainsKey(role)) throw new Exception("Données : le rôle « " + role + " » de " + nom + " n'existe pas dans « base ».");
                if (!MultRarete.ContainsKey(rarete)) throw new Exception("Données : la rareté « " + rarete + " » de " + nom + " n'existe pas dans « multRarete ».");

                var cl = Json.Liste(Json.Cle(d, "competences", ou), "competences de " + nom);
                var comps = new Competence[cl.Count];
                for (int j = 0; j < comps.Length; j++)
                {
                    var c = Json.Objet(cl[j], "competences de " + nom);
                    comps[j] = new Competence(
                        Json.Texte(Json.Cle(c, "nom", ou), "nom"),
                        Json.Entier(Json.Cle(c, "puissance", ou), "puissance"),
                        Json.Entier(Json.Cle(c, "coups", ou), "coups"),
                        Json.Entier(Json.Cle(c, "recharge", ou), "recharge"),
                        Json.Texte(Json.Cle(c, "type", ou), "type"));
                }

                Passif meneur = null;
                object m = Json.Cle(d, "meneur", ou);
                if (m != null)
                {
                    var md = Json.Objet(m, "meneur de " + nom);
                    meneur = new Passif(
                        Json.Texte(Json.Cle(md, "genre", ou), "genre"),
                        Json.Entier(Json.Cle(md, "valeur", ou), "valeur"),
                        Json.Texte(Json.Cle(md, "texte", ou), "texte"));
                }

                int[] fixes = null;
                object sf = Json.Cle(d, "statsFixes", ou);
                if (sf != null)
                {
                    double[] t = Json.Nombres(sf, "statsFixes de " + nom);
                    if (t.Length != 3) throw new Exception("Données : « statsFixes » de " + nom + " doit contenir 3 nombres (PV, ATQ, DEF).");
                    fixes = new int[] { (int)t[0], (int)t[1], (int)t[2] };
                }

                persos[i] = new Perso(nom, role, rarete,
                    Json.Entier(Json.Cle(d, "spd", ou), "spd de " + nom),
                    Json.Entier(Json.Cle(d, "crit", ou), "crit de " + nom),
                    comps, meneur, fixes);
            }
            Persos = persos;

            // ---- reliques.json ----
            var rr = Json.Objet(Json.Lire(reliquesJson), "reliques.json");
            valeurs = new Dictionary<string, double[]>();
            foreach (var kv in Json.Objet(Json.Cle(rr, "valeurs", "reliques.json"), "valeurs"))
                valeurs[kv.Key] = Json.Nombres(kv.Value, "valeurs/" + kv.Key);

            principaux = new Dictionary<string, string[]>();
            sets = new Dictionary<string, string[]>();
            foreach (var kv in Json.Objet(Json.Cle(rr, "builds", "reliques.json"), "builds"))
            {
                var b = Json.Objet(kv.Value, "builds/" + kv.Key);
                principaux[kv.Key] = Json.Textes(Json.Cle(b, "principaux", "builds/" + kv.Key), "principaux");
                sets[kv.Key] = Json.Textes(Json.Cle(b, "sets", "builds/" + kv.Key), "sets");
            }

            setGenre = new Dictionary<string, string>();
            setValeur = new Dictionary<string, double>();
            foreach (var kv in Json.Objet(Json.Cle(rr, "sets", "reliques.json"), "sets"))
            {
                var s = Json.Objet(kv.Value, "sets/" + kv.Key);
                setGenre[kv.Key] = Json.Texte(Json.Cle(s, "genre", "sets/" + kv.Key), "genre");
                setValeur[kv.Key] = Json.Nombre(Json.Cle(s, "valeur", "sets/" + kv.Key), "valeur");
            }

            Charge = true;
        }

        static double Niv(string cle, int n)
        {
            double[] v = valeurs[cle];
            return v[0] + (v[1] - v[0]) * n / 12.0;
        }

        // niveau = 0 (sans Reliques), 6 ou 12.
        public static Bonus BonusReliques(string role, int niveau)
        {
            var o = new Bonus();
            if (niveau == 0) return o;
            o.AtqFixe = Niv("atqFixe", niveau);
            o.DefFixe = Niv("defFixe", niveau);
            o.PvFixe = Niv("pvFixe", niveau);
            foreach (string k in principaux[role])
            {
                if (k == "atq") o.AtqPct += Niv("atqPct", niveau);
                if (k == "def") o.DefPct += Niv("defPct", niveau);
                if (k == "pv") o.PvPct += Niv("pvPct", niveau);
                if (k == "spd") o.Spd += Niv("spd", niveau);
                if (k == "crit") o.Crit += Niv("crit", niveau);
                if (k == "dcrit") o.DegCrit += Niv("degCrit", niveau);
            }
            int paires = niveau >= 12 ? 3 : 2;
            for (int i = 0; i < paires; i++)
            {
                string s = sets[role][i];
                string genre = setGenre[s];
                double v = setValeur[s];
                if (genre == "atqPct") o.AtqPct += v;
                if (genre == "pvPct") o.PvPct += v;
                if (genre == "defPct") o.DefPct += v;
                if (genre == "degCrit") o.DegCrit += v;
            }
            return o;
        }
    }
}
