namespace ProjetGame.Moteur
{
    // Générateur de nombres pseudo-aléatoires "Mulberry32".
    // C'est exactement le même que celui du simulateur web : avec la même graine,
    // on obtient les mêmes tirages (critiques) et donc les mêmes combats.
    public class Mulberry32
    {
        private uint etat;

        public Mulberry32(uint graine)
        {
            etat = graine;
        }

        // Renvoie un nombre décimal entre 0 (inclus) et 1 (exclu).
        public double Suivant()
        {
            unchecked
            {
                etat += 0x6D2B79F5u;
                uint t = (etat ^ (etat >> 15)) * (1u | etat);
                t = (t + ((t ^ (t >> 7)) * (61u | t))) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
    }
}
