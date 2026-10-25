using System.Collections;
using UnityEngine;
using ProjetGame.Moteur;

// Un personnage à l'écran. Tout est dessiné par le code (ombre, corps, tête, barre de vie) :
// il n'y a aucun prefab à construire. Plus tard, ces formes seront remplacées par les vrais dessins.
public class UniteVue : MonoBehaviour
{
    public Unite Fiche;              // les infos du moteur (nom, rôle, PV max...)
    public Vector3 PositionBase;     // sa place de départ sur le plateau
    public bool CorpsACorps;         // vrai = il court vers l'ennemi pour frapper (le tank)

    CombatScene scene;
    float echelle;
    Transform visuel;
    SpriteRenderer anneau, ombre, corps, tete, barreFond, barreRempl;
    Color couleurCorps, couleurTete, couleurEquipe;
    float barreEchelleX;
    float pvAffiches, pvCible, secousse, flash, alpha = 1f, decalage;
    bool mort;

    static Sprite spCercle, spCapsule, spCarre;

    // ---- Formes de base, fabriquées par le code ----

    static Sprite Cercle()
    {
        if (spCercle != null) return spCercle;
        int n = 64;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
            }
        t.Apply();
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;
        spCercle = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return spCercle;
    }

    static Sprite Capsule()
    {
        if (spCapsule != null) return spCapsule;
        int l = 64, h = 128;
        var t = new Texture2D(l, h, TextureFormat.RGBA32, false);
        float r = l / 2f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < l; x++)
            {
                float cy = Mathf.Clamp(y + 0.5f, r, h - r);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, cy));
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
            }
        t.Apply();
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;
        spCapsule = Sprite.Create(t, new Rect(0, 0, l, h), new Vector2(0.5f, 0f), l);   // pivot en bas
        return spCapsule;
    }

    public static Sprite Carre()
    {
        if (spCarre != null) return spCarre;
        var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++) t.SetPixel(x, y, Color.white);
        t.Apply();
        spCarre = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4f);    // pivot à gauche, 1 unité de large
        return spCarre;
    }

    static Color CouleurRole(string role)
    {
        if (role == "Tank") return new Color(0.56f, 0.64f, 0.75f);
        if (role == "DPS") return new Color(1f, 0.64f, 0.36f);
        if (role == "Support") return new Color(0.78f, 0.61f, 1f);
        return new Color(0.44f, 0.86f, 0.64f);
    }

    SpriteRenderer Creer(string nom, Sprite sp, Vector2 pos, Vector2 taille, Color c)
    {
        var go = new GameObject(nom);
        go.transform.SetParent(visuel, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.color = c;
        Vector2 b = sp.bounds.size;
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(taille.x / b.x, taille.y / b.y, 1f);
        return sr;
    }

    // ---- Mise en place ----

    public void Init(CombatScene s, Unite fiche, Vector3 pos, float e, Color equipe)
    {
        scene = s;
        Fiche = fiche;
        PositionBase = pos;
        transform.position = pos;
        echelle = e;
        couleurEquipe = equipe;
        CorpsACorps = fiche.Role == "Tank";
        pvAffiches = pvCible = fiche.Max;
        decalage = Random.value * 10f;

        visuel = new GameObject("Visuel").transform;
        visuel.SetParent(transform, false);

        couleurCorps = CouleurRole(fiche.Role);
        couleurTete = new Color(0.95f, 0.82f, 0.69f);
        float largeur = 0.72f * e * (CorpsACorps ? 1.2f : 1f);

        anneau = Creer("Anneau", Cercle(), Vector2.zero, new Vector2(1.3f * e, 0.46f * e), new Color(equipe.r, equipe.g, equipe.b, 0.6f));
        ombre = Creer("Ombre", Cercle(), Vector2.zero, new Vector2(1.02f * e, 0.36f * e), new Color(0f, 0f, 0f, 0.45f));
        corps = Creer("Corps", Capsule(), new Vector2(0f, 0.06f * e), new Vector2(largeur, 1.02f * e), couleurCorps);
        tete = Creer("Tete", Cercle(), new Vector2(0f, 1.32f * e), new Vector2(0.54f * e, 0.54f * e), couleurTete);
        barreFond = Creer("BarreFond", Carre(), new Vector2(-0.45f * e, 1.95f * e), new Vector2(0.9f * e, 0.13f * e), new Color(0f, 0f, 0f, 0.65f));
        barreRempl = Creer("BarrePV", Carre(), new Vector2(-0.45f * e, 1.95f * e), new Vector2(0.9f * e, 0.13f * e), equipe);
        barreEchelleX = barreRempl.transform.localScale.x;
    }

    // ---- Chaque image ----

    void Update()
    {
        if (scene == null || Fiche == null) return;
        float dt = Time.deltaTime * scene.Vitesse;

        pvAffiches = Mathf.MoveTowards(pvAffiches, pvCible, Mathf.Max(1f, Fiche.Max) * 2.5f * dt);
        float ratio = Mathf.Clamp01(pvAffiches / Mathf.Max(1f, Fiche.Max));
        var sc = barreRempl.transform.localScale;
        barreRempl.transform.localScale = new Vector3(barreEchelleX * ratio, sc.y, 1f);

        secousse = Mathf.Max(0f, secousse - dt * 0.6f);
        flash = Mathf.Max(0f, flash - dt * 4f);
        float bob = Mathf.Sin(Time.time * 4f + decalage) * 0.03f * echelle;
        visuel.localPosition = new Vector3(Mathf.Sin(Time.time * 80f) * secousse, bob, 0f);

        if (mort) alpha = Mathf.MoveTowards(alpha, 0f, dt * 2f);
        Teinte(anneau, new Color(couleurEquipe.r, couleurEquipe.g, couleurEquipe.b, 0.6f));
        Teinte(ombre, new Color(0f, 0f, 0f, 0.45f));
        Teinte(corps, Color.Lerp(couleurCorps, Color.white, flash));
        Teinte(tete, Color.Lerp(couleurTete, Color.white, flash));
        Teinte(barreFond, new Color(0f, 0f, 0f, 0.65f));
        Teinte(barreRempl, couleurEquipe);
    }

    void LateUpdate()
    {
        // Le plus bas à l'écran passe devant : tri selon la hauteur.
        int baseOrdre = 10000 - Mathf.RoundToInt(transform.position.y * 100f) * 10;
        anneau.sortingOrder = baseOrdre;
        ombre.sortingOrder = baseOrdre + 1;
        corps.sortingOrder = baseOrdre + 2;
        tete.sortingOrder = baseOrdre + 3;
        barreFond.sortingOrder = baseOrdre + 4;
        barreRempl.sortingOrder = baseOrdre + 5;
    }

    void Teinte(SpriteRenderer sr, Color c)
    {
        c.a *= alpha;
        sr.color = c;
    }

    // ---- Ce que le combat demande à un personnage ----

    public IEnumerator Courir(Vector3 cible, float duree)
    {
        Vector3 depart = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * scene.Vitesse / duree;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2f);
            transform.position = Vector3.Lerp(depart, cible, k);
            yield return null;
        }
        transform.position = cible;
    }

    public void Impact(int pvApres)
    {
        pvCible = pvApres;
        flash = 1f;
        secousse = 0.12f * echelle;
    }

    public void Soin(int pvApres)
    {
        pvCible = pvApres;
    }

    public void Mourir()
    {
        mort = true;
    }

    public Vector3 Haut()
    {
        return transform.position + new Vector3(0f, 1.8f * echelle, 0f);
    }
}
