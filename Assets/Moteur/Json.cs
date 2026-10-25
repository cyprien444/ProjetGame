using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ProjetGame.Moteur
{
    // Petit lecteur de JSON, sans aucune bibliothèque à installer.
    // Lire(texte) renvoie : Dictionary<string, object> (un objet { }), List<object> (une liste [ ]),
    // string, double, bool, ou null.
    public static class Json
    {
        public static object Lire(string texte)
        {
            var l = new Lecteur(texte);
            object v = l.Valeur();
            l.IgnorerEspaces();
            if (!l.Fin) throw l.Erreur("du texte reste après la fin du JSON");
            return v;
        }

        // ---- Petites aides pour lire les données ----

        public static Dictionary<string, object> Objet(object o, string ou)
        {
            var d = o as Dictionary<string, object>;
            if (d == null) throw new Exception("JSON : un objet { } était attendu pour « " + ou + " ».");
            return d;
        }

        public static List<object> Liste(object o, string ou)
        {
            var l = o as List<object>;
            if (l == null) throw new Exception("JSON : une liste [ ] était attendue pour « " + ou + " ».");
            return l;
        }

        public static object Cle(Dictionary<string, object> d, string cle, string ou)
        {
            object v;
            if (!d.TryGetValue(cle, out v)) throw new Exception("JSON : la clé « " + cle + " » est manquante dans « " + ou + " ».");
            return v;
        }

        public static string Texte(object o, string ou)
        {
            var s = o as string;
            if (s == null) throw new Exception("JSON : un texte entre guillemets était attendu pour « " + ou + " ».");
            return s;
        }

        public static double Nombre(object o, string ou)
        {
            if (!(o is double)) throw new Exception("JSON : un nombre était attendu pour « " + ou + " ».");
            return (double)o;
        }

        public static int Entier(object o, string ou)
        {
            return (int)Math.Round(Nombre(o, ou));
        }

        public static double[] Nombres(object o, string ou)
        {
            List<object> l = Liste(o, ou);
            var r = new double[l.Count];
            for (int i = 0; i < r.Length; i++) r[i] = Nombre(l[i], ou);
            return r;
        }

        public static string[] Textes(object o, string ou)
        {
            List<object> l = Liste(o, ou);
            var r = new string[l.Count];
            for (int i = 0; i < r.Length; i++) r[i] = Texte(l[i], ou);
            return r;
        }

        // ---- Le lecteur lui-même ----

        class Lecteur
        {
            readonly string s;
            int i;

            public Lecteur(string texte)
            {
                s = texte ?? "";
                i = (s.Length > 0 && s[0] == '\uFEFF') ? 1 : 0;
            }

            public bool Fin { get { return i >= s.Length; } }

            public Exception Erreur(string message)
            {
                int ligne = 1, colonne = 1;
                for (int k = 0; k < i && k < s.Length; k++)
                {
                    if (s[k] == '\n') { ligne++; colonne = 1; } else colonne++;
                }
                return new Exception("JSON invalide à la ligne " + ligne + ", colonne " + colonne + " : " + message + ".");
            }

            public void IgnorerEspaces()
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }

            public object Valeur()
            {
                IgnorerEspaces();
                if (Fin) throw Erreur("le texte se termine trop tôt");
                char c = s[i];
                if (c == '{') return Objet();
                if (c == '[') return Liste();
                if (c == '"') return Chaine();
                if (c == 't') { Mot("true"); return true; }
                if (c == 'f') { Mot("false"); return false; }
                if (c == 'n') { Mot("null"); return null; }
                return Nb();
            }

            void Mot(string mot)
            {
                if (string.CompareOrdinal(s, i, mot, 0, mot.Length) != 0) throw Erreur("« " + mot + " » était attendu");
                i += mot.Length;
            }

            object Nb()
            {
                int debut = i;
                while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                if (i == debut) throw Erreur("valeur inattendue « " + s[i] + " » (virgule oubliée, guillemet manquant ?)");
                double d;
                if (!double.TryParse(s.Substring(debut, i - debut), NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                    throw Erreur("nombre invalide");
                return d;
            }

            string Chaine()
            {
                i++; // le guillemet ouvrant
                var sb = new StringBuilder();
                while (true)
                {
                    if (Fin) throw Erreur("guillemet fermant manquant");
                    char c = s[i++];
                    if (c == '"') break;
                    if (c != '\\') { sb.Append(c); continue; }
                    if (Fin) throw Erreur("fin de texte inattendue");
                    char e = s[i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            if (i + 4 > s.Length) throw Erreur("code \\u incomplet");
                            sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                            i += 4;
                            break;
                        default: throw Erreur("séquence d'échappement inconnue \\" + e);
                    }
                }
                return sb.ToString();
            }

            List<object> Liste()
            {
                i++; // [
                var r = new List<object>();
                IgnorerEspaces();
                if (!Fin && s[i] == ']') { i++; return r; }
                while (true)
                {
                    r.Add(Valeur());
                    IgnorerEspaces();
                    if (Fin) throw Erreur("« ] » manquant");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; return r; }
                    throw Erreur("« , » ou « ] » était attendu");
                }
            }

            Dictionary<string, object> Objet()
            {
                i++; // {
                var r = new Dictionary<string, object>();
                IgnorerEspaces();
                if (!Fin && s[i] == '}') { i++; return r; }
                while (true)
                {
                    IgnorerEspaces();
                    if (Fin || s[i] != '"') throw Erreur("un nom entre guillemets était attendu");
                    string cle = Chaine();
                    IgnorerEspaces();
                    if (Fin || s[i] != ':') throw Erreur("« : » était attendu après « " + cle + " »");
                    i++;
                    r[cle] = Valeur();
                    IgnorerEspaces();
                    if (Fin) throw Erreur("« } » manquant");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; return r; }
                    throw Erreur("« , » ou « } » était attendu");
                }
            }
        }
    }
}
