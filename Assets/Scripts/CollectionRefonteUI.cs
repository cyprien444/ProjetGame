using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ProjetGame.Moteur;

// Collection personnelle + Encyclopedie pour Projet Game (Unity 6, UGUI et TextMeshPro).
// Ajouter sur le Canvas de la scene Collection. Aucune modification du moteur de combat.
// La collection personnelle lit DonneesJoueur ; l'encyclopedie lit les 15 entrees de persos.json.
public class CollectionRefonteUI : MonoBehaviour
{
    private enum Mode { Personnelle, Galerie, FicheEncyclopedie, InfoPersonnage }
    private enum SousInfo { Avis, Utilisations, Equipements }
    private enum Onglet { Competences, Infos, Equipements, Runes, Utilisations }

    private static readonly Color Or = new Color(0.96f, 0.76f, 0.39f);
    private static readonly Color TexteClair = new Color(0.98f, 0.96f, 0.91f);
    private static readonly Color TexteSecondaire = new Color(0.72f, 0.81f, 0.92f);
    private static readonly Color AccentBleu = new Color(0.52f, 0.79f, 1f);
    private static readonly Color Sombre = new Color(0.035f, 0.067f, 0.12f, 0.93f);
    private static readonly Color Card = new Color(0.075f, 0.12f, 0.21f, 0.95f);

    private Mode mode = Mode.Personnelle;
    private Onglet onglet = Onglet.Equipements;
    private int indexSelectionne = -1;
    private int indexCompetence = 0;
    private SousInfo sousInfo = SousInfo.Avis;
    private RectTransform racine;
    private ScrollRect listePortraits;
    private TMP_Text texteInfo;
    private float positionVerticale = 1f;
    private float positionHorizontaleMesHeros = 0f;
    private bool pret;

    private void Awake()
    {
        // Cet ecran remplace visuellement l'ancienne collection, sans supprimer ses objets.
        GestionCollection ancien = GetComponent<GestionCollection>();
        if (ancien != null) ancien.enabled = false;
        MasquerAncienEnfant("Scroll View");
        MasquerAncienEnfant("BoutonRetour");
        MasquerAncienEnfant("FichePersonnage");
    }

    private void MasquerAncienEnfant(string nom)
    {
        Transform enfant = transform.Find(nom);
        if (enfant != null) enfant.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (GetComponent<Canvas>() == null)
        {
            Debug.LogError("CollectionRefonteUI doit etre sur le Canvas de Collection.");
            return;
        }
        try { ChargeurDonnees.Charger(); }
        catch (Exception e)
        {
            Debug.LogError("CollectionRefonteUI : echec du chargement JSON : " + e.Message);
            return;
        }
        if (!Donnees.Charge || Donnees.Persos == null || Donnees.Persos.Length == 0)
        {
            Debug.LogError("CollectionRefonteUI : donnees des personnages introuvables.");
            return;
        }

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        indexSelectionne = DonneesJoueur.PersonnagesPossedes.Count > 0
            ? DonneesJoueur.PersonnagesPossedes[0] : -1;
        pret = true;
        Construire();
    }

    private Sprite SpriteUI(string nom)
    {
        return Resources.Load<Sprite>("CollectionUI/" + nom);
    }

    private Sprite Portrait(int index)
    {
        return Resources.Load<Sprite>("CollectionUI/Portraits/" + index);
    }

    private RectTransform Zone(string nom, Transform parent, float x1, float y1, float x2, float y2)
    {
        var obj = new GameObject(nom, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x1, y1);
        rt.anchorMax = new Vector2(x2, y2);
        rt.pivot = new Vector2(.5f, .5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private Image ImageUI(RectTransform rt, Color couleur, string sprite = null, bool raycast = false)
    {
        var i = rt.gameObject.AddComponent<Image>();
        Sprite s = sprite == null ? null : SpriteUI(sprite);
        if (s != null)
        {
            i.sprite = s;
            if (s.border.sqrMagnitude > .5f) i.type = Image.Type.Sliced;
        }
        i.color = couleur;
        i.raycastTarget = raycast;
        return i;
    }

    private TMP_Text Texte(string nom, Transform parent, string contenu, int taille,
        Color couleur, float x1, float y1, float x2, float y2,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var rt = Zone(nom, parent, x1, y1, x2, y2);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = contenu ?? "";
        t.fontSize = taille;
        t.color = couleur;
        t.alignment = align;
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return t;
    }

    private Button Bouton(string nom, Transform parent, string libelle,
        Action action, float x1, float y1, float x2, float y2,
        bool actif = false, int taille = 28, bool disponible = true)
    {
        var rt = Zone(nom, parent, x1, y1, x2, y2);
        string texture = actif ? "bouton_dore" : "bouton_sombre";
        var fond = ImageUI(rt, actif ? Color.white : new Color(.95f,.98f,1f), texture, true);
        var bouton = rt.gameObject.AddComponent<Button>();
        bouton.targetGraphic = fond;
        bouton.interactable = disponible;
        var cols = bouton.colors;
        cols.normalColor = Color.white;
        cols.highlightedColor = new Color(1f, .93f, .76f);
        cols.pressedColor = new Color(.75f, .83f, .94f);
        cols.disabledColor = new Color(.57f, .6f, .68f, .83f);
        bouton.colors = cols;
        if (action != null) bouton.onClick.AddListener(() => action());
        Texte("Libelle", rt, libelle, taille, actif ? new Color(.15f,.12f,.09f) : TexteClair,
            .03f, .04f, .97f, .96f);
        return bouton;
    }

    private RectTransform Panneau(string nom, Transform parent,
        float x1, float y1, float x2, float y2)
    {
        var z = Zone(nom, parent, x1, y1, x2, y2);
        ImageUI(z, Color.white, "panneau_codex");
        return z;
    }

    private List<int> ListePossedee()
    {
        var l = new List<int>();
        foreach (int i in DonneesJoueur.PersonnagesPossedes)
            if (i >= 0 && i < Donnees.Persos.Length && !l.Contains(i)) l.Add(i);
        return l;
    }

    private void RetenirDefilement()
    {
        if (listePortraits == null) return;
        // Seule la bande « Mes héros » est défilante.
        if (mode == Mode.Personnelle)
            positionHorizontaleMesHeros = listePortraits.horizontalNormalizedPosition;
    }

    private void Selection(int index)
    {
        if (index < 0 || index >= Donnees.Persos.Length) return;
        RetenirDefilement();
        indexSelectionne = index;
        indexCompetence = 0;
        // Conserver l'onglet actif lorsque l'on change de personnage.
        // Depuis la galerie de l'encyclopedie, ouvrir la nouvelle fiche sur INFOS.
        if (mode == Mode.Galerie)
        {
            mode = Mode.FicheEncyclopedie;
            onglet = Onglet.Infos;
        }
        Construire();
    }

    private void BasculerPersonnelle()
    {
        RetenirDefilement();
        mode = Mode.Personnelle;
        onglet = Onglet.Equipements;
        var possedes = ListePossedee();
        if (!possedes.Contains(indexSelectionne))
            indexSelectionne = possedes.Count > 0 ? possedes[0] : -1;
        Construire();
    }

    private void BasculerEncyclopedie()
    {
        mode = Mode.Galerie;
        onglet = Onglet.Infos;
        Construire();
    }

    private void OuvrirInfoPersonnage()
    {
        mode = Mode.InfoPersonnage;
        sousInfo = SousInfo.Avis;
        Construire();
    }

    private void RetourFichePersonnelle()
    {
        mode = Mode.Personnelle;
        Construire();
    }

    private void ChangerSousInfo(SousInfo nouveau)
    {
        sousInfo = nouveau;
        Construire();
    }

    private void ChangerOnglet(Onglet nouveau)
    {
        RetenirDefilement();
        onglet = nouveau;
        Construire();
    }

    private void RetourGalerie()
    {
        mode = Mode.Galerie;
        Construire();
    }

    private void Construire()
    {
        if (!pret) return;
        if (racine != null) Destroy(racine.gameObject);
        racine = Zone("CollectionRefonte_Generee", transform, 0f, 0f, 1f, 1f);
        ImageUI(racine, Color.white, "fond_codex");
        if (mode == Mode.Personnelle) ConstruirePersonnelle();
        else if (mode == Mode.Galerie) ConstruireGalerie();
        else if (mode == Mode.InfoPersonnage) ConstruireInfoPersonnage();
        else ConstruireFicheEncyclopedie();
    }

    private void Entete(string titre, string sousTitre, Action retour = null)
    {
        var b = Panneau("Entete", racine, .014f, .921f, .986f, .993f);
        Bouton("Retour", b, "‹", retour ?? (() => SceneManager.LoadScene("Menu")),
            .012f, .12f, .135f, .91f, false, 59);
        Texte("Titre", b, titre, 47, Or, .16f, .40f, .83f, .98f);
        Texte("SousTitre", b, sousTitre, 24, TexteSecondaire, .16f, .05f, .83f, .44f);
        Bouton("Menu", b, "⌂", () => SceneManager.LoadScene("Menu"),
            .855f, .12f, .988f, .91f, false, 45);
    }

    private void DessinerPortrait(RectTransform parent, int index, bool selectionne,
        bool etiquette = false)
    {
        var bord = ImageUI(parent, selectionne ? Or : Color.white, "cadre_portrait", true);
        var btn = parent.gameObject.AddComponent<Button>();
        btn.targetGraphic = bord;
        btn.onClick.AddListener(() => Selection(index));
        var imgRect = Zone("Portrait", parent, .065f, etiquette ? .24f : .065f,
            .935f, .935f);
        var i = ImageUI(imgRect, Color.white);
        i.sprite = Portrait(index);
        i.preserveAspect = true;
        // Les personnages non possedes conservent leur image d'origine en plus sombre.
        i.color = DonneesJoueur.Possede(index)
            ? Color.white : new Color(.65f, .65f, .65f, 1f);
        if (i.sprite == null)
            Texte("Initiale", imgRect, Donnees.Persos[index].Nom.Substring(0,1),
                50, Or, 0f, 0f, 1f, 1f);
        if (etiquette)
            Texte("Nom", parent, Donnees.Persos[index].Nom, 21, TexteClair,
                0f, .015f, 1f, .23f);
    }

    // Barre permanente de portraits possedes, en bas. Bouton Encyclopedie a droite.
    private void BandeMesHerosEnBas(Transform parent)
    {
        var indices = ListePossedee();
        var panneau = Panneau("BandeMesHeros", parent, .025f, .050f, .975f, .253f);
        Texte("TitreBande", panneau, "MES HÉROS · " + indices.Count + "/" + Donnees.Persos.Length,
            23, Or, .025f, .865f, .785f, .995f, TextAlignmentOptions.MidlineLeft);
        var scroll = Zone("ScrollPossedes", panneau, .012f, .075f, .808f, .868f);
        var sr = scroll.gameObject.AddComponent<ScrollRect>();
        sr.horizontal = true;
        sr.vertical = false;
        sr.scrollSensitivity = 55f;
        sr.movementType = ScrollRect.MovementType.Clamped;
        var vp = Zone("Viewport", scroll, 0f, 0f, 1f, 1f);
        ImageUI(vp, new Color(.01f, .02f, .04f, .05f), null, true);
        vp.gameObject.AddComponent<RectMask2D>();
        var content = Zone("Content", vp, 0f, 0f, 0f, 1f);
        content.pivot = new Vector2(0f, .5f);
        content.sizeDelta = new Vector2(Mathf.Max(1, indices.Count) * 174f + 24f, 0f);
        var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 12f;
        layout.padding = new RectOffset(10, 10, 0, 0);
        foreach (int index in indices)
        {
            var cadre = Zone("PortraitPossede_" + index, content, 0f, 0f, 0f, 0f);
            cadre.sizeDelta = new Vector2(162f, 168f);
            DessinerPortrait(cadre, index, index == indexSelectionne);
        }
        sr.content = content;
        sr.viewport = vp;
        listePortraits = sr;
        Canvas.ForceUpdateCanvases();
        sr.horizontalNormalizedPosition = positionHorizontaleMesHeros;
        Bouton("BoutonEncyclopedieBas", panneau, "ENCYCLO-\nPÉDIE",
            BasculerEncyclopedie, .825f, .11f, .988f, .84f, true, 24);
    }

    // Ecran personnel inspire de la reference: art au centre, competences a droite,
    // statistiques en bas a gauche, liste de heros en bas.
    private void ConstruirePersonnelle()
    {
        var possedes = ListePossedee();
        if (!possedes.Contains(indexSelectionne))
            indexSelectionne = possedes.Count > 0 ? possedes[0] : -1;

        string titre = indexSelectionne < 0 ? "COLLECTION" : Donnees.Persos[indexSelectionne].Nom.ToUpperInvariant();
        string sousTitre = indexSelectionne < 0 ? "Aucun héros possédé" :
            Donnees.Persos[indexSelectionne].Role + " · " + Donnees.Persos[indexSelectionne].Rarete;
        Entete(titre, sousTitre);
        BandeMesHerosEnBas(racine);

        if (indexSelectionne < 0)
        {
            var vide = Panneau("SansHeros", racine, .035f, .32f, .969f, .87f);
            Texte("AucunHero", vide, "Aucun personnage possédé pour le moment.\n\nOuvre l'Encyclopédie pour voir tous les héros.",
                36, TexteClair, .05f, .18f, .95f, .82f);
            return;
        }

        Perso p = Donnees.Persos[indexSelectionne];
        var scene = Panneau("ScenePersonnage", racine, .025f, .267f, .746f, .910f);
        var zoneArt = Zone("HeroCentral", scene, .035f, .195f, .972f, .965f);
        var art = ImageUI(zoneArt, Color.white);
        art.sprite = SpriteUI("HeroArt/" + indexSelectionne);
        if (art.sprite == null) art.sprite = Portrait(indexSelectionne);
        art.preserveAspect = true;

        Bouton("Informations", scene, "ⓘ INFOS", OuvrirInfoPersonnage,
            .027f, .875f, .365f, .978f, true, 26);

        var stats = Panneau("StatsBase", scene, .030f, .047f, .57f, .405f);
        Texte("TitreStats", stats, "STATISTIQUES DE BASE", 25, Or,
            .035f, .83f, .96f, .98f, TextAlignmentOptions.MidlineLeft);
        string[] lignes = {
            "PV       " + Stat(p, 0).ToString("N0"),
            "ATQ      " + Stat(p, 1).ToString("N0"),
            "DEF      " + Stat(p, 2).ToString("N0"),
            "SPD      " + p.Spd,
            "CRIT     " + p.Crit + " %"
        };
        for (int k = 0; k < lignes.Length; k++)
        {
            float yy = .765f - k * .142f;
            Texte("Stat" + k, stats, lignes[k], 26, TexteClair,
                .06f, yy - .10f, .95f, yy + .02f, TextAlignmentOptions.MidlineLeft);
        }
        int n = p.Competences == null ? 0 : p.Competences.Length;
        if (n > 0)
        {
            indexCompetence = Mathf.Clamp(indexCompetence, 0, n - 1);
            var selected = p.Competences[indexCompetence];
            var apercu = Panneau("DescriptionCompetence", scene, .59f, .047f, .972f, .405f);
            Texte("TitreCompetence", apercu, selected.Nom, 25, Or, .045f, .70f, .95f, .97f);
            Texte("EffetCompetence", apercu, Description(selected), 23, TexteClair,
                .055f, .13f, .945f, .74f);
        }

        var competences = Panneau("EchelleCompetences", racine, .752f, .267f, .975f, .910f);
        Texte("CompetencesTitre", competences, "COMPÉTENCES", 25, Or,
            .02f, .908f, .98f, .989f);
        for (int j = 0; j < 3; j++)
        {
            int pos = j;
            bool existe = p.Competences != null && j < p.Competences.Length;
            float y = .69f - j * .215f;
            Bouton("Competence" + j, competences, (j + 1).ToString(),
                existe ? (Action)(() => { indexCompetence = pos; Construire(); }) : null,
                .20f, y, .80f, y + .158f, existe && indexCompetence == j, 45, existe);
            Texte("CompetenceNom" + j, competences,
                existe ? p.Competences[j].Nom : "À DÉBLOQUER", 20,
                existe ? TexteClair : TexteSecondaire,
                .015f, y - .067f, .985f, y + .005f);
        }
        Bouton("Eveil", competences, "ÉVEIL", null,
            .115f, .025f, .885f, .139f, false, 24, false);
        Texte("EveilNote", competences, "À VENIR", 18, TexteSecondaire,
            .025f, .148f, .975f, .187f);
    }

    // La page d'information est preparee. Les avis et tendances demanderont plus tard
    // un compte joueur, un etat d'eveil et des donnees communautaires reelles.
    private void ConstruireInfoPersonnage()
    {
        if (indexSelectionne < 0 || indexSelectionne >= Donnees.Persos.Length)
        {
            RetourFichePersonnelle();
            return;
        }
        Perso p = Donnees.Persos[indexSelectionne];
        Entete("INFOS DU HÉROS", p.Nom, RetourFichePersonnelle);
        Bouton("RetourVersFiche", racine, "‹ FICHE DU HÉROS", RetourFichePersonnelle,
            .033f, .849f, .553f, .906f, false, 25);
        var identite = Panneau("IdentificationHeros", racine, .035f, .690f, .965f, .841f);
        var cadre = Zone("MiniPortrait", identite, .035f, .085f, .235f, .915f);
        var img = ImageUI(cadre, Color.white);
        img.sprite = Portrait(indexSelectionne);
        img.preserveAspect = true;
        Texte("NomHero", identite, p.Nom, 44, Or, .275f, .56f, .955f, .90f,
            TextAlignmentOptions.MidlineLeft);
        Texte("RoleHero", identite, p.Role + " · " + p.Rarete, 27, TexteClair,
            .275f, .17f, .955f, .54f, TextAlignmentOptions.MidlineLeft);

        var tabs = Zone("OngletsInfo", racine, .035f, .616f, .965f, .678f);
        Bouton("Avis", tabs, "COMMUNAUTÉ", () => ChangerSousInfo(SousInfo.Avis),
            .00f, .03f, .328f, .98f, sousInfo == SousInfo.Avis, 23);
        Bouton("Utilisations", tabs, "UTILISATIONS", () => ChangerSousInfo(SousInfo.Utilisations),
            .336f, .03f, .662f, .98f, sousInfo == SousInfo.Utilisations, 23);
        Bouton("ReliquesPopulaires", tabs, "ÉQUIPEMENTS", () => ChangerSousInfo(SousInfo.Equipements),
            .670f, .03f, 1f, .98f, sousInfo == SousInfo.Equipements, 23);
        var contenu = Panneau("ContenuInfos", racine, .035f, .15f, .965f, .604f);
        if (sousInfo == SousInfo.Avis)
        {
            Texte("Titre", contenu, "AVIS DE LA COMMUNAUTÉ", 35, Or,
                .05f, .82f, .95f, .965f);
            Texte("Explication", contenu,
                "Plus tard, un compte joueur pourra donner un seul avis sur ce personnage, après son éveil.\n\n" +
                "Aucun avis communautaire n'est disponible dans ce prototype.",
                28, TexteClair, .065f, .34f, .935f, .78f);
            Bouton("DonnerAvis", contenu, "DONNER MON AVIS · À VENIR", null,
                .10f, .08f, .90f, .20f, false, 25, false);
        }
        else if (sousInfo == SousInfo.Utilisations)
        {
            Texte("Titre", contenu, "OÙ CE HÉROS EST-IL UTILISÉ ?", 33, Or,
                .05f, .83f, .95f, .97f);
            Texte("Explication", contenu,
                "À terme : répartition des utilisations par mode de jeu, basée sur les équipes réelles de la communauté.\n\n" +
                "Les données ne sont pas encore collectées. Aucun pourcentage n'est inventé.",
                27, TexteClair, .065f, .18f, .935f, .79f);
        }
        else
        {
            Texte("Titre", contenu, "ÉQUIPEMENTS LES PLUS UTILISÉS", 33, Or,
                .05f, .83f, .95f, .97f);
            Texte("Explication", contenu,
                "À terme : sets de Reliques et statistiques d'équipement les plus utilisés sur ce héros.\n\n" +
                "Aucune donnée communautaire disponible pour le moment.",
                28, TexteClair, .065f, .18f, .935f, .79f);
        }
    }

    private void Tab(RectTransform parent, string nom, Onglet cible, float x1, float x2)
    {
        Bouton("Tab_" + nom, parent, nom, () => ChangerOnglet(cible),
            x1, .05f, x2, .97f, onglet == cible, 22);
    }

    private void VueEquipements(RectTransform zone, Perso perso)
    {
        Texte("TitreEquipements", zone, "MES 6 EMPLACEMENTS DE RELIQUES", 28, Or,
            .025f, .914f, .585f, .99f, TextAlignmentOptions.MidlineLeft);
        var emplacement = Zone("PanneauSixCases", zone, .023f, .205f, .598f, .895f);
        ImageUI(emplacement, new Color(.05f,.09f,.16f,.62f));
        Texte("Centre", emplacement, "✧\nRELIQUES", 30, Or,
            .355f,.38f,.645f,.65f);
        string[] noms = { "Arme", "Bottes", "Armure", "Gants", "Casque", "Amulette" };
        string[] icons = { "arme", "bottes", "armure", "gants", "casque", "amulette" };
        for (int k = 0; k < 6; k++)
        {
            int j=k;
            int col=k%2, row=k/2;
            float x1=col==0?.03f:.68f;
            float y2=.98f-row*.315f;
            var slot = Zone("Relique_" + noms[k], emplacement,x1,y2-.285f,x1+.285f,y2);
            var frame=ImageUI(slot,Color.white,"emplacement_relique",true);
            var button=slot.gameObject.AddComponent<Button>();
            button.targetGraphic=frame;
            button.onClick.AddListener(() => Message("Relique " + noms[j] + " : disponible à l'étape 10."));
            var ic=Zone("Icon",slot,.20f,.24f,.80f,.89f);
            var img=ImageUI(ic,Color.white);
            img.sprite=SpriteUI("Icons/"+icons[k]);
            img.preserveAspect=true;
            Texte("Nom",slot,noms[k].ToUpperInvariant(),18,TexteClair,.01f,.01f,.99f,.23f);
        }
        var stats = Panneau("StatsEquipements",zone,.618f,.205f,.984f,.897f);
        Texte("TitreStats",stats,"STATS · NIV. 1",30,Or,.04f,.89f,.96f,.99f);
        int pv=Stat(perso,0), atk=Stat(perso,1), def=Stat(perso,2);
        string[] lignes={ "PV      "+pv.ToString("N0"),"ATQ     "+atk.ToString("N0"),
            "DEF     "+def.ToString("N0"),"SPD     "+perso.Spd,
            "Crit     "+perso.Crit+" %","Dég crit   50 %","ACC     0 %","RES     0 %"};
        for(int k=0;k<lignes.Length;k++)
        {
            float yy=.83f-k*.083f;
            Texte("Stat_"+k,stats,lignes[k],25, k%2==0?TexteClair:TexteSecondaire,
                .065f, yy-.07f, .95f, yy+.01f, TextAlignmentOptions.MidlineLeft);
        }
        Texte("Ensemble",zone,"BONUS DE SET : aucun (non équipé)",23,TexteSecondaire,
            .03f,.127f,.97f,.197f,TextAlignmentOptions.Center);
        Bouton("AutoEquip",zone,"AUTO-ÉQUIPER",null,.045f,.021f,.48f,.109f,false,24,false);
        Bouton("Retirer",zone,"RETIRER TOUT",null,.515f,.021f,.955f,.109f,false,24,false);
    }

    private static int Stat(Perso p, int colonne)
    {
        if (p.StatsFixes != null && p.StatsFixes.Length > colonne)
            return p.StatsFixes[colonne];
        if (Donnees.Base == null || Donnees.MultRarete == null ||
            !Donnees.Base.ContainsKey(p.Role) || !Donnees.MultRarete.ContainsKey(p.Rarete)) return 0;
        return Mathf.FloorToInt((float)(Donnees.Base[p.Role][colonne]
            * Donnees.MultRarete[p.Rarete] / 100.0));
    }

    private void VueInfos(RectTransform zone, Perso p)
    {
        Texte("Titre",zone,"STATISTIQUES · NIVEAU 1",36,Or,.04f,.87f,.96f,.975f);
        string[] labels={"PV","ATQ","DEF","SPD","TAUX CRIT","DÉG. CRIT","PRÉCISION","RÉSISTANCE"};
        string[] valeurs={Stat(p,0).ToString("N0"), Stat(p,1).ToString("N0"),
            Stat(p,2).ToString("N0"),p.Spd.ToString(),p.Crit+" %","50 %","0 %","0 %"};
        for(int k=0;k<labels.Length;k++)
        {
            int col=k/4, row=k%4;
            float xx=col==0?.04f:.52f;
            float yy=.74f-row*.14f;
            Texte("Label"+k,zone,labels[k],28,TexteSecondaire,xx,yy,xx+.23f,yy+.08f,
                TextAlignmentOptions.MidlineLeft);
            Texte("Valeur"+k,zone,valeurs[k],30,TexteClair,xx+.23f,yy,xx+.42f,yy+.08f,
                TextAlignmentOptions.MidlineRight);
        }
        Texte("Passif",zone,"PASSIF DE MENEUR\n"+
            (p.Meneur == null?"Aucun":p.Meneur.Texte),27,Or,
            .05f,.06f,.95f,.27f,TextAlignmentOptions.Center);
    }

    private void VueCompetences(RectTransform zone, Perso p)
    {
        Texte("Titre",zone,"COMPÉTENCES DE "+p.Nom.ToUpperInvariant(),33,Or,
            .04f,.87f,.96f,.98f);
        int count=p.Competences==null?0:p.Competences.Length;
        if(count==0)
        {
            Texte("Vide",zone,"Aucune compétence renseignée.",30,TexteClair,.05f,.2f,.95f,.8f);
            return;
        }
        for(int i=0;i<count;i++)
        {
            Competence c=p.Competences[i];
            float h=.74f/count, top=.85f-i*h;
            var slot=Panneau("Competence"+i,zone,.035f,top-h+.017f,.965f,top);
            Texte("Nom",slot,(i+1)+". "+c.Nom,29,Or,.045f,.54f,.95f,.95f,
                TextAlignmentOptions.MidlineLeft);
            Texte("Description",slot,Description(c),25,TexteClair,.045f,.045f,.95f,.56f,
                TextAlignmentOptions.MidlineLeft);
        }
        Texte("Note",zone,"Compétences disponibles dès le niveau 1.",23,TexteSecondaire,
            .03f,.015f,.97f,.095f);
    }

    private static string Description(Competence c)
    {
        string cible=c.Type=="A"?"tous les ennemis":
            c.Type=="H1"?"un allié":c.Type=="HA"?"tous les alliés":"un ennemi";
        string effet=c.EstSoin?"Soin":"Dégâts";
        string puissance=c.Coups>1?c.Coups+" × "+c.Puissance+" %":c.Puissance+" %";
        return effet+" : "+puissance+" · "+cible+" · RC "+c.Recharge;
    }

    private void VueRunes(RectTransform zone)
    {
        Texte("Titre",zone,"RUNES",39,Or,.07f,.79f,.93f,.97f);
        Texte("Explication",zone,
            "Cet onglet n'a pas encore de fonction distincte dans le prototype.\n\n"+
            "Les six emplacements de Reliques sont maintenant dans ÉQUIPEMENTS.\n\n"+
            "Aucune Rune supplémentaire n'est définie pour le moment.",
            30,TexteClair,.07f,.23f,.93f,.77f,TextAlignmentOptions.Center);
        Bouton("RetourEquipements",zone,"VOIR MES ÉQUIPEMENTS",()=>ChangerOnglet(Onglet.Equipements),
            .21f,.065f,.79f,.16f,true,27);
    }

    private void Message(string message)
    {
        if (texteInfo != null) texteInfo.text = message;
        Debug.Log("CollectionRefonteUI : " + message);
    }

    // Encyclopedie : page galerie complete, sans barre de defilement.
    // Les 15 portraits sont visibles directement. Clic => fiche dediee.
    private void ConstruireGalerie()
    {
        Entete("ENCYCLOPÉDIE", "Toutes les légendes · " + Donnees.Persos.Length + " personnages",
            BasculerPersonnelle);
        var panneau = Panneau("GaleriePortraits", racine, .032f, .065f, .968f, .910f);
        Texte("Instruction", panneau, "CHOISIS UN PERSONNAGE POUR OUVRIR SA FICHE", 29, Or,
            .045f, .916f, .955f, .982f);

        // Grille fixe de 4 colonnes, sans ScrollRect ni bande de portraits.
        var grille = Zone("GrillePortraits", panneau, .03f, .055f, .97f, .906f);
        var layout = grille.gameObject.AddComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        layout.cellSize = new Vector2(200f, 224f);
        layout.spacing = new Vector2(20f, 26f);
        layout.padding = new RectOffset(16, 16, 18, 18);
        layout.childAlignment = TextAnchor.UpperCenter;
        for (int k = 0; k < Donnees.Persos.Length; k++)
        {
            var frame = Zone("Hero" + k, grille, 0f, 0f, 0f, 0f);
            DessinerPortrait(frame, k, k == indexSelectionne);
        }
        Texte("Bas", racine,
            "Portraits assombris : héros non possédés · Tous les personnages sont consultables.",
            24, TexteSecondaire, .05f, .014f, .95f, .062f);
    }

    private void ConstruireFicheEncyclopedie()
    {
        if (indexSelectionne<0 || indexSelectionne>=Donnees.Persos.Length)
            indexSelectionne=0;
        Perso p=Donnees.Persos[indexSelectionne];
        Entete("ENCYCLOPÉDIE", "Fiche de "+p.Nom, RetourGalerie);

        var art=Panneau("FicheIllustration",racine,.03f,.489f,.514f,.849f);
        var portrait=Zone("Illustration",art,.03f,.045f,.97f,.965f);
        var img=ImageUI(portrait,Color.white);
        img.sprite=SpriteUI("HeroArt/"+indexSelectionne);
        if(img.sprite==null) img.sprite=Portrait(indexSelectionne);
        img.preserveAspect=true;
        var resume=Panneau("FicheStats",racine,.525f,.489f,.969f,.849f);
        Texte("Nom",resume,p.Nom,48,Or,.05f,.795f,.95f,.975f);
        Texte("Role",resume,p.Role+" · "+p.Rarete,28,TexteClair,
            .05f,.675f,.95f,.79f);
        string[] lignes={"PV    "+Stat(p,0).ToString("N0"),"ATQ   "+Stat(p,1).ToString("N0"),
            "DEF   "+Stat(p,2).ToString("N0"),"SPD   "+p.Spd,
            "CRIT   "+p.Crit+" %"};
        for(int i=0;i<lignes.Length;i++)
        {
            float yy=.62f-i*.108f;
            Texte("Stat"+i,resume,lignes[i],29,TexteClair,
                .075f,yy-.09f,.925f,yy+.01f,TextAlignmentOptions.MidlineLeft);
        }

        var tabs=Zone("TabsEncyclo",racine,.035f,.433f,.965f,.487f);
        TabEncyclo(tabs,"INFOS",Onglet.Infos,0f,.326f);
        TabEncyclo(tabs,"COMPÉTENCES",Onglet.Competences,.336f,.662f);
        TabEncyclo(tabs,"UTILISATIONS",Onglet.Utilisations,.672f,1f);
        // Page individuelle : aucun mini-portrait en bas. Retour => galerie des 15.
        // Le panneau agrandi utilise l'espace anciennement occupe par la bande de defilement.
        var contenu=Panneau("DetailsHeros",racine,.034f,.070f,.966f,.431f);
        if(onglet==Onglet.Infos) DetailEncycloInfos(contenu,p);
        else if(onglet==Onglet.Competences) DetailEncycloCompetences(contenu,p);
        else DetailEncycloUtilisations(contenu,p);
        Texte("Note",racine,"Statistiques de base au niveau 1 · Retour en haut à gauche pour choisir un autre héros.",
            22,TexteSecondaire,.05f,.018f,.95f,.063f);
    }

    private void TabEncyclo(RectTransform parent,string title,Onglet cible,float x1,float x2)
    {
        Bouton("Onglet"+title,parent,title,()=>ChangerOnglet(cible),
            x1,.02f,x2,.99f,onglet==cible,25);
    }

    private void DetailEncycloInfos(RectTransform z,Perso p)
    {
        Texte("Titre",z,"PROFIL DU PERSONNAGE",30,Or,.025f,.81f,.975f,.985f,
            TextAlignmentOptions.MidlineLeft);
        string role=p.Role=="Tank"?"Encaisse les attaques et sécurise l'avant-garde.":
            p.Role=="DPS"?"Se concentre sur les dégâts infligés à l'ennemi.":
            p.Role=="Soigneur"?"Restaure les PV de ses alliés.":
            "Soutien polyvalent pour la composition d'équipe.";
        Texte("Role",z,role,27,TexteClair,.045f,.45f,.955f,.81f,
            TextAlignmentOptions.MidlineLeft);
        Texte("Meneur",z,"PASSIF DE MENEUR : "+(p.Meneur==null?"Aucun":p.Meneur.Texte),
            24,Or,.045f,.08f,.955f,.45f,TextAlignmentOptions.MidlineLeft);
    }

    private void DetailEncycloCompetences(RectTransform z,Perso p)
    {
        int nb=p.Competences==null?0:p.Competences.Length;
        if(nb==0) { Texte("Vide",z,"Aucune compétence.",29,TexteClair,.02f,.2f,.98f,.8f); return; }
        for(int i=0;i<nb;i++)
        {
            Competence c=p.Competences[i];
            float h=.94f/nb;
            float top=.97f-i*h;
            Texte("Nom"+i,z,(i+1)+". "+c.Nom,28,Or,.045f,top-h*.46f,.955f,top,
                TextAlignmentOptions.MidlineLeft);
            Texte("Effet"+i,z,Description(c),24,TexteClair,.045f,top-h+.01f,.955f,top-h*.48f,
                TextAlignmentOptions.MidlineLeft);
        }
    }

    private void DetailEncycloUtilisations(RectTransform z,Perso p)
    {
        string conseil;
        switch(p.Role)
        {
            case "Tank": conseil="Histoire et donjons : utile pour encaisser les coups.";break;
            case "DPS": conseil="Histoire et arène : utile pour infliger des dégâts.";break;
            case "Soigneur": conseil="Donjons et combats longs : aide à maintenir l'équipe.";break;
            default: conseil="Équipes mixtes : soutien complémentaire selon les compétences.";break;
        }
        Texte("Titre",z,"UTILISATIONS PRINCIPALES",32,Or,.035f,.77f,.965f,.98f);
        Texte("Description",z,conseil,29,TexteClair,.045f,.34f,.955f,.78f);
        Texte("Prudence",z,"Conseil indicatif selon le rôle, à confirmer par les tests d'équilibrage.",
            23,TexteSecondaire,.045f,.05f,.955f,.34f);
    }


}
