using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ProjetGame.Moteur;

// L'écran de combat : il demande le combat au moteur, puis REJOUE le journal en animation.
// Tout est construit par le code (décor, personnages, interface) : il suffit d'ajouter ce script
// à un objet vide de la scène, avec un Canvas (et son EventSystem) dans la scène.
public class CombatScene : MonoBehaviour
{
    [Header("Le combat (réglages modifiables dans l'Inspector)")]
    public int[] equipeA = { 12, 0, 9, 10, 11 };   // numéros de persos (le n°1 du doc est le 0)
    public int[] equipeB = { 8, 4, 5, 13, 7 };
    public int meneurA = 3;                        // position du meneur (0 à 4), -1 = aucun
    public int meneurB = 3;
    public int niveauReliques = 12;                // 0, 6 ou 12
    public uint graine = 1;
    public float Vitesse = 2f;                     // 1, 2 ou 3

    Camera cam;
    Canvas canvas;
    RectTransform racine;
    readonly Dictionary<string, UniteVue> vues = new Dictionary<string, UniteVue>();
    readonly List<GameObject> decorEtEffets = new List<GameObject>();

    TMP_Text texteRound, texteBanniere, texteActeur;
    readonly TMP_Text[] texteComp = new TMP_Text[3];
    readonly Image[] fondComp = new Image[3];
    Button boutonRejouer;
    bool rejouer;
    float banniereTemps;

    static readonly Color bleu = new Color(0.30f, 0.55f, 1f);
    static readonly Color rouge = new Color(1f, 0.35f, 0.35f);
    static readonly Color vert = new Color(0.30f, 0.80f, 0.55f);
    static readonly Color jaune = new Color(1f, 0.82f, 0.25f);

    // ------------------------------------------------------------ démarrage

    void Start()
    {
        ChargeurDonnees.Charger();
        if (!Donnees.Charge) return;
        ConfigurerCamera();
        CreerDecor();
        CreerInterface();
        StartCoroutine(Boucle());
    }

    void Update()
    {
        if (banniereTemps > 0f)
        {
            banniereTemps -= Time.deltaTime * Vitesse;
            if (banniereTemps <= 0f && texteBanniere != null) texteBanniere.text = "";
        }
    }

    public void ChangerVitesse(int v)
    {
        Vitesse = v;
    }

    // ------------------------------------------------------------ plateau en perspective

    // u = profondeur (négatif = côté joueur, positif = côté ennemi), v = largeur.
    static Vector3 Projeter(float u, float v, out float echelle)
    {
        float ypx = 290f - u * 90f;
        float sc = 0.8f + ypx / 640f * 0.4f;
        float xpx = 180f + v * 95f * sc;
        echelle = sc * 1.3f;
        return new Vector3((xpx - 180f) * 0.03f, (320f - ypx) * 0.03f, 0f);
    }

    static readonly float[,] places = { { 0.9f, -0.55f }, { 0.9f, 0.55f }, { 2f, -1.05f }, { 2.1f, 0f }, { 2f, 1.05f } };

    static Vector3 Position(string equipe, int pos, out float echelle)
    {
        float u = places[pos, 0] * (equipe == "A" ? -1f : 1f);
        return Projeter(u, places[pos, 1], out echelle);
    }

    void ConfigurerCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 9.6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.07f, 0.12f);
        cam.transform.position = new Vector3(0f, 0f, -10f);
    }

    GameObject Rectangle(string nom, Vector2 centre, Vector2 taille, Color c, int ordre)
    {
        var go = new GameObject(nom);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = UniteVue.Carre();
        sr.color = c;
        sr.sortingOrder = ordre;
        go.transform.position = new Vector3(centre.x - taille.x / 2f, centre.y, 0f);   // pivot à gauche
        go.transform.localScale = new Vector3(taille.x, taille.y, 1f);
        decorEtEffets.Add(go);
        return go;
    }

    void Ligne(Vector3 a, Vector3 b, float epaisseur, Color c)
    {
        var go = new GameObject("Ligne");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = UniteVue.Carre();
        sr.color = c;
        sr.sortingOrder = -900;
        go.transform.position = a;
        Vector3 d = b - a;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        go.transform.localScale = new Vector3(d.magnitude, epaisseur, 1f);
        decorEtEffets.Add(go);
    }

    void CreerDecor()
    {
        float e;
        float yMilieu = Projeter(0f, 0f, out e).y;
        Rectangle("SolEnnemi", new Vector2(0f, (yMilieu + 9.6f) / 2f), new Vector2(11f, 9.6f - yMilieu), new Color(0.23f, 0.10f, 0.14f), -1000);
        Rectangle("SolJoueur", new Vector2(0f, (yMilieu - 9.6f) / 2f), new Vector2(11f, yMilieu + 9.6f), new Color(0.08f, 0.14f, 0.27f), -1000);
        var trait = new Color(1f, 1f, 1f, 0.07f);
        for (float u = -2.5f; u <= 2.51f; u += 0.5f)
            Ligne(Projeter(u, -1.7f, out e), Projeter(u, 1.7f, out e), 0.03f, trait);
        for (float v = -1.5f; v <= 1.51f; v += 0.5f)
            Ligne(Projeter(-2.8f, v, out e), Projeter(2.8f, v, out e), 0.03f, trait);
    }

    // ------------------------------------------------------------ interface (créée par le code)

    RectTransform Zone(string nom, Transform parent, Vector2 ancreMin, Vector2 ancreMax, Vector2 decMin, Vector2 decMax)
    {
        var go = new GameObject(nom, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = ancreMin;
        r.anchorMax = ancreMax;
        r.offsetMin = decMin;
        r.offsetMax = decMax;
        return r;
    }

    TMP_Text CreerTexte(string nom, Transform parent, string texte, float taille, TextAlignmentOptions align, Color c)
    {
        var go = new GameObject(nom, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = texte;
        t.fontSize = taille;
        t.alignment = align;
        t.color = c;
        t.raycastTarget = false;
        return t;
    }

    void Etirer(RectTransform r, float marge)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(marge, marge);
        r.offsetMax = new Vector2(-marge, -marge);
    }

    Button CreerBouton(string nom, Transform parent, string texte, float taille, Color fond, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(nom, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = fond;
        var b = go.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(action);
        var t = CreerTexte("Texte", go.transform, texte, taille, TextAlignmentOptions.Center, Color.white);
        Etirer(t.rectTransform, 0f);
        return b;
    }

    void CreerInterface()
    {
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }
        var cs = canvas.GetComponent<CanvasScaler>();
        if (cs != null)
        {
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080f, 1920f);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0.5f;
        }
        racine = (RectTransform)canvas.transform;
        if (FindFirstObjectByType<EventSystem>() == null)
            Debug.LogWarning("Il manque un EventSystem : les boutons ne répondront pas. " +
                             "Dans Hierarchy : clic droit > UI > Event System.");

        // Barre du haut
        CreerTexte("Chapitre", racine, "Chapitre 1 · combat 10/50", 38f, TextAlignmentOptions.TopLeft, new Color(0.6f, 0.65f, 0.75f));
        var rect = (RectTransform)racine.Find("Chapitre");
        rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(0.6f, 1f);
        rect.offsetMin = new Vector2(30f, -90f); rect.offsetMax = new Vector2(0f, -30f);

        texteRound = CreerTexte("Round", racine, "", 38f, TextAlignmentOptions.TopRight, new Color(0.6f, 0.65f, 0.75f));
        var rr = texteRound.rectTransform;
        rr.anchorMin = new Vector2(0.4f, 1f); rr.anchorMax = new Vector2(1f, 1f);
        rr.offsetMin = new Vector2(0f, -90f); rr.offsetMax = new Vector2(-30f, -30f);

        // Boutons de vitesse
        for (int i = 0; i < 3; i++)
        {
            int vit = i + 1;
            Button b = CreerBouton("Vitesse" + vit, racine, "x" + vit, 44f, new Color(0.15f, 0.18f, 0.3f, 0.95f), () => ChangerVitesse(vit));
            var r = (RectTransform)b.transform;
            r.anchorMin = r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(1f, 1f);
            r.sizeDelta = new Vector2(150f, 80f);
            r.anchoredPosition = new Vector2(-30f - (2 - i) * 165f, -110f);
        }

        // Bannière au centre
        texteBanniere = CreerTexte("Banniere", racine, "", 96f, TextAlignmentOptions.Center, Color.white);
        var rb = texteBanniere.rectTransform;
        rb.anchorMin = new Vector2(0f, 0.5f); rb.anchorMax = new Vector2(1f, 0.5f);
        rb.offsetMin = new Vector2(0f, -80f); rb.offsetMax = new Vector2(0f, 80f);

        // Panneau du bas : les compétences de celui qui joue
        var panneau = Zone("PanneauBas", racine, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 360f));
        var fond = panneau.gameObject.AddComponent<Image>();
        fond.color = new Color(0.04f, 0.05f, 0.09f, 0.92f);
        fond.raycastTarget = false;

        texteActeur = CreerTexte("Acteur", panneau, "", 46f, TextAlignmentOptions.TopLeft, Color.white);
        var ra = texteActeur.rectTransform;
        ra.anchorMin = new Vector2(0f, 1f); ra.anchorMax = new Vector2(1f, 1f);
        ra.offsetMin = new Vector2(30f, -90f); ra.offsetMax = new Vector2(-30f, -20f);

        for (int i = 0; i < 3; i++)
        {
            var slot = Zone("Slot" + i, panneau, new Vector2(i / 3f, 0f), new Vector2((i + 1) / 3f, 1f), new Vector2(12f, 20f), new Vector2(-12f, -100f));
            fondComp[i] = slot.gameObject.AddComponent<Image>();
            fondComp[i].color = new Color(0.10f, 0.12f, 0.20f);
            fondComp[i].raycastTarget = false;
            texteComp[i] = CreerTexte("Texte", slot, "", 34f, TextAlignmentOptions.Center, Color.white);
            Etirer(texteComp[i].rectTransform, 8f);
        }

        // Bouton Rejouer (caché jusqu'à la fin du combat)
        boutonRejouer = CreerBouton("Rejouer", racine, "Rejouer", 56f, new Color(0.2f, 0.45f, 0.9f), () => rejouer = true);
        var rj = (RectTransform)boutonRejouer.transform;
        rj.anchorMin = rj.anchorMax = new Vector2(0.5f, 0f);
        rj.pivot = new Vector2(0.5f, 0f);
        rj.sizeDelta = new Vector2(440f, 130f);
        rj.anchoredPosition = new Vector2(0f, 400f);
        boutonRejouer.gameObject.SetActive(false);
    }

    void Banniere(string texte, Color c, float duree)
    {
        texteBanniere.text = texte;
        texteBanniere.color = c;
        banniereTemps = duree;
    }

    void AfficherCompetences(UniteVue acteur, int utilisee, int[] recharges)
    {
        Unite u = acteur.Fiche;
        texteActeur.text = (u.Equipe == "A" ? "Ton équipe" : "Ennemi") + " · " + u.Nom + " (" + u.Role + ")";
        texteActeur.color = u.Equipe == "A" ? bleu : rouge;
        for (int i = 0; i < 3; i++)
        {
            if (i >= u.Competences.Length)
            {
                texteComp[i].text = "—";
                fondComp[i].color = new Color(0.07f, 0.08f, 0.13f);
                continue;
            }
            string etat = i == 0 ? "sans recharge" : (recharges != null && recharges[i] > 0 ? "recharge " + recharges[i] : "prête");
            texteComp[i].text = (i + 1) + ". " + u.Competences[i].Nom + "\n<size=70%>" + etat + "</size>";
            fondComp[i].color = i == utilisee ? new Color(0.25f, 0.22f, 0.08f) : new Color(0.10f, 0.12f, 0.20f);
        }
    }

    // ------------------------------------------------------------ le déroulement

    IEnumerator Attendre(float secondes)
    {
        float t = 0f;
        while (t < secondes)
        {
            t += Time.deltaTime * Vitesse;
            yield return null;
        }
    }

    IEnumerator Boucle()
    {
        while (true)
        {
            yield return StartCoroutine(Combat());
            boutonRejouer.gameObject.SetActive(true);
            rejouer = false;
            while (!rejouer) yield return null;
            boutonRejouer.gameObject.SetActive(false);
            graine++;
        }
    }

    void Nettoyer()
    {
        foreach (UniteVue v in vues.Values) if (v != null) Destroy(v.gameObject);
        vues.Clear();
        texteBanniere.text = "";
    }

    UniteVue Trouver(string equipe, int position)
    {
        return vues[equipe + position];
    }

    IEnumerator Combat()
    {
        Nettoyer();
        Resultat r = Simulateur.Combattre(equipeA, meneurA, equipeB, meneurB, graine, niveauReliques, true, true);

        foreach (Unite u in r.Unites)
        {
            float e;
            Vector3 p = Position(u.Equipe, u.Position, out e);
            var go = new GameObject("Unite " + u.Nom);
            var v = go.AddComponent<UniteVue>();
            v.Init(this, u, p, e, u.Equipe == "A" ? bleu : rouge);
            vues[u.Equipe + u.Position] = v;
        }
        texteRound.text = "";
        texteActeur.text = "";
        for (int i = 0; i < 3; i++) texteComp[i].text = "";
        yield return StartCoroutine(Attendre(0.8f));

        foreach (Evenement ev in r.Journal)
        {
            if (ev.Type == "Round") { texteRound.text = "Round " + ev.Round + " · Auto"; continue; }
            if (ev.Type == "Meneur") { Debug.Log(ev.Texte); continue; }
            yield return StartCoroutine(Action(ev));
        }

        yield return StartCoroutine(Attendre(0.5f));
        string gagnant = r.Gagnant == "A" ? "Victoire !" : "Défaite";
        if (r.LimiteAtteinte) gagnant += " (limite de rounds)";
        Banniere(gagnant, r.Gagnant == "A" ? jaune : rouge, 9999f);
    }

    IEnumerator Action(Evenement ev)
    {
        UniteVue acteur = Trouver(ev.Equipe, ev.ActeurPosition);
        AfficherCompetences(acteur, ev.IndexCompetence, ev.RechargesAvant);
        if (ev.IndexCompetence > 0) Banniere(ev.Competence, ev.Equipe == "A" ? bleu : rouge, 0.9f);
        yield return StartCoroutine(Attendre(0.35f));

        EffetCible premier = ev.Cibles[0];
        UniteVue cible = Trouver(premier.Equipe, premier.Position);

        if (acteur.CorpsACorps && !ev.EstSoin)
        {
            Vector3 devant = acteur.PositionBase + (cible.PositionBase - acteur.PositionBase) * 0.78f;
            yield return StartCoroutine(acteur.Courir(devant, 0.26f));
            AppliquerEffets(ev);
            yield return StartCoroutine(Attendre(0.2f));
            yield return StartCoroutine(acteur.Courir(acteur.PositionBase, 0.24f));
        }
        else
        {
            yield return StartCoroutine(Projectile(acteur, cible, ev.EstSoin ? vert : (ev.Equipe == "A" ? bleu : rouge)));
            AppliquerEffets(ev);
            yield return StartCoroutine(Attendre(0.3f));
        }
    }

    void AppliquerEffets(Evenement ev)
    {
        foreach (EffetCible c in ev.Cibles)
        {
            UniteVue v = Trouver(c.Equipe, c.Position);
            if (ev.EstSoin)
            {
                v.Soin(c.PvApres);
                StartCoroutine(Flottant(v.Haut(), "+" + c.Valeur, vert, 60f));
            }
            else
            {
                v.Impact(c.PvApres);
                bool crit = c.Critiques > 0;
                StartCoroutine(Flottant(v.Haut(), "-" + c.Valeur + (crit ? "!" : ""), crit ? jaune : Color.white, crit ? 84f : 60f));
                if (c.Mort) v.Mourir();
            }
        }
    }

    IEnumerator Projectile(UniteVue de, UniteVue vers, Color c)
    {
        var go = new GameObject("Projectile");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = UniteVue.Carre();
        sr.color = c;
        sr.sortingOrder = 20000;
        go.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
        Vector3 a = de.transform.position + new Vector3(0f, 0.8f, 0f);
        Vector3 b = vers.transform.position + new Vector3(0f, 0.8f, 0f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * Vitesse / 0.32f;
            float k = Mathf.Clamp01(t);
            Vector3 p = Vector3.Lerp(a, b, k) + new Vector3(0f, Mathf.Sin(k * Mathf.PI) * 1.0f, 0f);
            go.transform.position = p - new Vector3(0.225f, 0f, 0f);   // le carré a son pivot à gauche
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator Flottant(Vector3 monde, string texte, Color c, float taille)
    {
        TMP_Text t = CreerTexte("Flottant", racine, texte, taille, TextAlignmentOptions.Center, c);
        var r = t.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(400f, 120f);
        Vector2 ecran = cam.WorldToScreenPoint(monde);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(racine, ecran, null, out local);
        float k = 0f;
        while (k < 1f)
        {
            k += Time.deltaTime * Vitesse / 0.9f;
            r.anchoredPosition = local + new Vector2(0f, 160f * Mathf.Clamp01(k));
            var col = c;
            col.a = 1f - Mathf.Clamp01(k) * Mathf.Clamp01(k);
            t.color = col;
            yield return null;
        }
        Destroy(t.gameObject);
    }
}
