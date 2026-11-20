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
    // Fiche encyclopedie : un panneau d'explication n'apparait qu'au clic.
    private bool afficherDescriptionCompetence = false;
    private bool afficherDescriptionMeneur = false;
    private SousInfo sousInfo = SousInfo.Avis;
    private Mode retourDepuisInfos = Mode.Personnelle;
    private RectTransform racine;
    private ScrollRect listePortraits;
    private TMP_Text texteInfo;
    private float positionVerticale = 1f;
    private float positionHorizontaleMesHeros = 0f;
    private bool afficherApercuPromotionMesHeros = false;
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
        afficherApercuPromotionMesHeros = false;
        indexCompetence = 0;
        afficherDescriptionCompetence = false;
        afficherDescriptionMeneur = false;
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
        retourDepuisInfos = mode;
        mode = Mode.InfoPersonnage;
        sousInfo = SousInfo.Avis;
        Construire();
    }

    // Ces deux raccourcis sont reserves a la fiche de l'Encyclopedie.
    // Ils reutilisent la page d'infos deja presente dans la V3.4.
    private void OuvrirInfoDepuisEncyclopedie(SousInfo section)
    {
        retourDepuisInfos = Mode.FicheEncyclopedie;
        sousInfo = section;
        mode = Mode.InfoPersonnage;
        Construire();
    }

    private void SelectionnerCompetenceEncyclopedie(int numero)
    {
        bool etaitOuverte = afficherDescriptionCompetence && indexCompetence == numero;
        indexCompetence = numero;
        afficherDescriptionCompetence = !etaitOuverte;
        afficherDescriptionMeneur = false;
        Construire();
    }

    private void SelectionnerMeneurEncyclopedie()
    {
        afficherDescriptionMeneur = !afficherDescriptionMeneur;
        afficherDescriptionCompetence = false;
        Construire();
    }

    // Images decoratives : des decoupes de la maquette choisie par l'utilisateur.
    // Tous les textes propres aux heros et leurs statistiques restent dynamiques.
    private RectTransform IllustrationFiche(string nom, Transform parent, string ressource,
        float x1, float y1, float x2, float y2)
    {
        var zone = Zone(nom, parent, x1, y1, x2, y2);
        var image = ImageUI(zone, Color.white);

        // V3.5.6 : cadres 100 % Unity UI avec des Image standard.
        // Les anciens PNG et ContourDoreUI ne sont plus utilises.
        // Le dessin utilise des bandes UI standard (4 cotes a chaque couche),
        // sans mesh personnalise, sans decoupage d'images et sans losange.
        bool estCadre = ressource == "panneau_stats" ||
                        ressource == "icone_eveil" ||
                        ressource == "icone_lead" ||
                        ressource == "icone_vide" ||
                        ressource == "icone_skill_1" ||
                        ressource == "icone_skill_2" ||
                        ressource == "icone_skill_3";
        if (estCadre)
        {
            // Graphic invisible conservé pour les boutons et leur zone de clic.
            image.sprite = null;
            image.color = Color.clear;
            DessinerCadreAvecImagesUnity(zone);
        }
        else
        {
            // Aucun changement : fond, bannieres, Communaute et Utilisation
            // conservent leurs images, texte, fond bleu et boutons d'origine.
            image.sprite = SpriteUI("Encyclopedie/" + ressource);
            if (image.sprite == null)
            {
                image.sprite = SpriteUI("panneau_codex");
                Debug.LogWarning("Ressource de la fiche manquante : " + ressource +
                    ". Lance Projet Game > Collections > Configurer images fiche V3.5.");
            }
        }
        return zone;
    }

    // V3.5.7 : mêmes emplacements qu'en V3.5.6, mais coins biseautés.
    // Bordures réalisées avec des Image UGUI transparentes au centre,
    // sans PNG, sans shader et sans modifier les boutons ni leurs positions.
    private void DessinerCadreAvecImagesUnity(RectTransform parent)
    {
        AjouterCoucheCadreBiseaute(parent, "Ombre", 2f, 10f, 22f,
            new Color(0.22f, 0.13f, 0.055f, 0.97f));
        AjouterCoucheCadreBiseaute(parent, "Or", 3.5f, 6f, 20f,
            new Color(0.83f, 0.57f, 0.22f, 1f));
        AjouterCoucheCadreBiseaute(parent, "Reflet", 4.5f, 1.8f, 19f,
            new Color(1f, 0.88f, 0.58f, 1f));
        AjouterCoucheCadreBiseaute(parent, "LisereInterieur", 10f, 1.5f, 24f,
            new Color(0.58f, 0.36f, 0.13f, 0.90f));
    }

    // Les tailles sont en pixels du Canvas (pas en coordonnées 0..1).
    // Les 4 côtés sont ancrés, les 4 diagonales sont ancrées aux coins :
    // l'ensemble s'adapte sans lire parent.rect, même au premier rendu.
    private void AjouterCoucheCadreBiseaute(RectTransform parent, string nom,
        float marge, float epaisseur, float chanfrein, Color teinte)
    {
        AjouterBandeCadre(parent, nom + "_Haut",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(.5f, .5f), new Vector2(-2f * chanfrein, epaisseur),
            new Vector2(0f, -marge), teinte);
        AjouterBandeCadre(parent, nom + "_Bas",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(.5f, .5f), new Vector2(-2f * chanfrein, epaisseur),
            new Vector2(0f, marge), teinte);
        AjouterBandeCadre(parent, nom + "_Gauche",
            new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(.5f, .5f), new Vector2(epaisseur, -2f * chanfrein),
            new Vector2(marge, 0f), teinte);
        AjouterBandeCadre(parent, nom + "_Droite",
            new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(.5f, .5f), new Vector2(epaisseur, -2f * chanfrein),
            new Vector2(-marge, 0f), teinte);

        float centreCoin = (marge + chanfrein) * .5f;
        float longueur = (chanfrein - marge) * Mathf.Sqrt(2f);
        AjouterDiagonaleCadre(parent, nom + "_CoinHG",
            new Vector2(0f, 1f), new Vector2(centreCoin, -centreCoin),
            longueur, epaisseur, 45f, teinte);
        AjouterDiagonaleCadre(parent, nom + "_CoinHD",
            new Vector2(1f, 1f), new Vector2(-centreCoin, -centreCoin),
            longueur, epaisseur, -45f, teinte);
        AjouterDiagonaleCadre(parent, nom + "_CoinBG",
            new Vector2(0f, 0f), new Vector2(centreCoin, centreCoin),
            longueur, epaisseur, -45f, teinte);
        AjouterDiagonaleCadre(parent, nom + "_CoinBD",
            new Vector2(1f, 0f), new Vector2(-centreCoin, centreCoin),
            longueur, epaisseur, 45f, teinte);
    }

    private void AjouterDiagonaleCadre(RectTransform parent, string nom,
        Vector2 coinAncre, Vector2 decalage, float longueur,
        float epaisseur, float angle, Color teinte)
    {
        var objet = new GameObject(nom, typeof(RectTransform));
        objet.transform.SetParent(parent, false);
        var rt = objet.GetComponent<RectTransform>();
        rt.anchorMin = coinAncre;
        rt.anchorMax = coinAncre;
        rt.pivot = new Vector2(.5f, .5f);
        rt.sizeDelta = new Vector2(longueur, epaisseur);
        rt.anchoredPosition = decalage;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        var image = objet.AddComponent<Image>();
        image.color = teinte;
        image.raycastTarget = false;
    }

    private void AjouterBandeCadre(RectTransform parent, string nom,
        Vector2 ancreMin, Vector2 ancreMax, Vector2 pivot,
        Vector2 taille, Vector2 position, Color teinte)
    {
        var objet = new GameObject(nom, typeof(RectTransform));
        objet.transform.SetParent(parent, false);
        var rt = objet.GetComponent<RectTransform>();
        rt.anchorMin = ancreMin;
        rt.anchorMax = ancreMax;
        rt.pivot = pivot;
        rt.sizeDelta = taille;
        rt.anchoredPosition = position;
        var trait = objet.AddComponent<Image>();
        trait.color = teinte;
        trait.raycastTarget = false;
    }

    private RectTransform BoutonFicheImage(string nom, Transform parent, string ressource,
        Action action, float x1, float y1, float x2, float y2, bool disponible = true)
    {
        var zone = IllustrationFiche(nom, parent, ressource, x1, y1, x2, y2);
        var image = zone.GetComponent<Image>();
        image.raycastTarget = true;
        var bouton = zone.gameObject.AddComponent<Button>();
        bouton.targetGraphic = image;
        bouton.interactable = disponible;
        if (action != null) bouton.onClick.AddListener(() => action());
        return zone;
    }

    // V3.6.2 : icones independantes des cadres UGUI, centrees avec marge.
    // Les sprites ne sont pas incrustes dans les bordures : aucun debordement.
    // RawImage accepte les PNG du dossier Resources sans configuration Sprite.
    private void AjouterIconeDansCadre(RectTransform cadre, string fichier,
        bool disponible = true)
    {
        const string prefixe = "CollectionUI/Encyclopedie/Capacites/";
        Texture2D image = Resources.Load<Texture2D>(prefixe + fichier);
        if (image == null)
        {
            // Egalement compatible avec une texture importee en Sprite (2D et UI).
            Sprite sprite = Resources.Load<Sprite>(prefixe + fichier);
            if (sprite != null) image = sprite.texture;
        }
        if (image == null)
        {
            Debug.LogWarning("Icone de competence introuvable : " + prefixe + fichier);
            return;
        }

        // 12 % de marge de chaque cote, meme pour le cadre Eveil plus grand.
        // Les 5 positions d'origine et tous leurs clics sont preserves.
        var contenu = Zone("IconeInterieure", cadre, .12f, .12f, .88f, .88f);
        var dessin = contenu.gameObject.AddComponent<RawImage>();
        dessin.texture = image;
        dessin.color = disponible ? Color.white : new Color(.82f, .85f, .90f, .90f);
        dessin.raycastTarget = false;
    }

    private void RetourFichePersonnelle()
    {
        mode = retourDepuisInfos;
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

    // V8.8 : bande compacte de portraits, sans compteur de collection.
    // L'Encyclopedie reste independante du defilement des heros possedes.
    private void BandeMesHerosEnBas(Transform parent)
    {
        List<int> indices = ListePossedee();
        RectTransform bande = Panneau("BandeCollectionV88", parent, .025f, .025f, .975f, .159f);

        // Toute la hauteur de la bande sert aux portraits, sans titre redondant.
        RectTransform zoneScroll = Zone("DefilementDesHeros", bande, .017f, .070f, .796f, .942f);
        ScrollRect scroll = zoneScroll.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.scrollSensitivity = 46f;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = Zone("FenetreDesPortraits", zoneScroll, 0f, 0f, 1f, 1f);
        ImageUI(viewport, new Color(.02f, .05f, .11f, .11f), null, true);
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform contenu = Zone("PortraitsPossedes", viewport, 0f, 0f, 0f, 1f);
        contenu.pivot = new Vector2(0f, .5f);
        const float largeurCase = 175f;
        const float hauteurCase = 197f;
        const float ecart = 12f;
        contenu.sizeDelta = new Vector2(Mathf.Max(1, indices.Count) * (largeurCase + ecart) + 22f, 0f);
        HorizontalLayoutGroup layout = contenu.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = ecart;
        layout.padding = new RectOffset(10, 10, 0, 0);
        foreach (int index in indices)
        {
            RectTransform casePortrait = Zone("Hero_" + index, contenu, 0f, 0f, 0f, 0f);
            casePortrait.sizeDelta = new Vector2(largeurCase, hauteurCase);
            DessinerPortrait(casePortrait, index, index == indexSelectionne, true);
        }
        scroll.content = contenu;
        scroll.viewport = viewport;
        listePortraits = scroll;
        Canvas.ForceUpdateCanvases();
        scroll.horizontalNormalizedPosition = positionHorizontaleMesHeros;

        // Bouton fixe, jamais inclus dans la zone defilante.
        Bouton("AccesEncyclopedie", bande, "ENCYCLO-\nPÉDIE",
            BasculerEncyclopedie, .813f, .110f, .982f, .897f, true, 22);
    }

    // V8.7 : page Mes héros complète. La fiche encyclopédie n'est pas modifiée.
    private void ConstruirePersonnelle()
    {
        List<int> possedes = ListePossedee();
        if (!possedes.Contains(indexSelectionne))
            indexSelectionne = possedes.Count > 0 ? possedes[0] : -1;

        RectTransform haut = Panneau("EnteteMesHerosV87", racine, .025f, .924f, .975f, .994f);
        Bouton("RetourAuMenu", haut, "RETOUR",
            () => SceneManager.LoadScene("Menu"), .015f, .145f, .246f, .870f, false, 25);
        Texte("TitreMesHeros", haut, "MES HÉROS", 46, Or,
            .263f, .438f, .765f, .950f);
        Texte("SousTitreMesHeros", haut, "COLLECTION PERSONNELLE", 22, TexteSecondaire,
            .280f, .095f, .745f, .450f);
        Bouton("AccesEquipe", haut, "ÉQUIPE",
            () => SceneManager.LoadScene("Equipe"), .790f, .145f, .985f, .870f, false, 25);

        BandeMesHerosEnBas(racine);
        if (indexSelectionne < 0)
        {
            var sansHero = Panneau("AucunHeroPossede", racine, .04f, .280f, .96f, .890f);
            Texte("MessageAucunHero", sansHero,
                "Aucun héros possédé.\n\nDécouvre les personnages dans l'Encyclopédie.",
                37, TexteClair, .09f, .27f, .91f, .74f);
            return;
        }

        Perso perso = Donnees.Persos[indexSelectionne];
        EtatProgressionHero progression = ProgressionHeros.Lire(indexSelectionne);

        // Bandeau identitaire : personnage, classe et rareté.
        RectTransform identite = Panneau("IdentiteDuHero", racine, .025f, .846f, .975f, .918f);
        RectTransform vignette = Zone("PortraitIdentite", identite, .016f, .10f, .152f, .91f);
        ImageUI(vignette, Color.white, "cadre_portrait");
        RectTransform photo = Zone("ImageIdentite", vignette, .09f, .09f, .91f, .91f);
        Image imgPhoto = ImageUI(photo, Color.white);
        imgPhoto.sprite = Portrait(indexSelectionne);
        imgPhoto.preserveAspect = true;
        Texte("NomSelectionne", identite, perso.Nom, 42, Or,
            .172f, .46f, .865f, .94f, TextAlignmentOptions.MidlineLeft);
        Texte("RoleEtRarete", identite, perso.Role + "  ·  " + perso.Rarete, 25, TexteClair,
            .172f, .12f, .865f, .50f, TextAlignmentOptions.MidlineLeft);
        Texte("CollectionTag", identite, "POSSÉDÉ", 18, Or,
            .798f, .15f, .960f, .45f, TextAlignmentOptions.MidlineRight);

        // Illustration mise en avant : on réutilise l'art du héros, sans le modifier.
        RectTransform sceneHeros = Panneau("PresentationDuHero", racine, .025f, .457f, .658f, .839f);
        RectTransform art = Zone("IllustrationCentrale", sceneHeros, .045f, .083f, .955f, .956f);
        Image dessin = ImageUI(art, Color.white);
        dessin.sprite = SpriteUI("HeroArt/" + indexSelectionne);
        if (dessin.sprite == null) dessin.sprite = Portrait(indexSelectionne);
        dessin.preserveAspect = true;
        dessin.raycastTarget = false;
        Bouton("VoirLesInformations", sceneHeros, "INFOS",
            OuvrirInfoPersonnage, .055f, .872f, .350f, .974f, true, 26);
        Texte("LegendeScene", sceneHeros, "APERÇU DU HÉROS", 21, TexteSecondaire,
            .17f, .025f, .83f, .097f);

        // Colonne de gestion : quatre onglets actifs et les six reliques à venir.
        RectTransform gestion = Panneau("GestionDuHero", racine, .670f, .457f, .975f, .839f);
        Texte("TitreGestion", gestion, "GESTION", 27, Or,
            .04f, .925f, .96f, .990f);
        Bouton("OngletReliques", gestion, "RELIQUES",
            () => ChangerOnglet(Onglet.Equipements),
            .035f, .835f, .496f, .921f, onglet == Onglet.Equipements, 20);
        Bouton("OngletSkills", gestion, "SKILLS",
            () => ChangerOnglet(Onglet.Competences),
            .505f, .835f, .965f, .921f, onglet == Onglet.Competences, 20);
        Bouton("OngletFiche", gestion, "FICHE",
            () => ChangerOnglet(Onglet.Infos),
            .035f, .742f, .496f, .825f, onglet == Onglet.Infos, 20);
        Bouton("OngletRunes", gestion, "RUNES",
            () => ChangerOnglet(Onglet.Runes),
            .505f, .742f, .965f, .825f, onglet == Onglet.Runes, 20);
        RectTransform contenuGestion = Zone("ContenuGestion", gestion, .052f, .042f, .948f, .725f);
        if (onglet == Onglet.Competences)
            AfficherCompetencesMesHeros(contenuGestion, perso);
        else if (onglet == Onglet.Infos)
            AfficherInfosMesHeros(contenuGestion, perso);
        else if (onglet == Onglet.Runes)
            AfficherRunesMesHeros(contenuGestion);
        else
            AfficherReliquesMesHeros(contenuGestion);

        // Carte XP : aucun gain ni achat ne se produit sur cet écran.
        AfficherProgressionMesHeros(racine, progression);

        // Deux cartes de consultation rapide : valeurs de base + skill choisie.
        RectTransform stats = Panneau("StatistiquesMesHeros", racine, .025f, .167f, .506f, .313f);
        Texte("TitreStatistiques", stats, "STATS DE BASE", 24, Or,
            .055f, .800f, .945f, .975f, TextAlignmentOptions.MidlineLeft);
        string[] labels = { "PV", "ATQ", "DEF", "SPD", "CRIT" };
        string[] valeurs = {
            Stat(perso, 0).ToString("N0"), Stat(perso, 1).ToString("N0"),
            Stat(perso, 2).ToString("N0"), perso.Spd.ToString(), perso.Crit + " %"
        };
        for (int i = 0; i < labels.Length; i++)
        {
            // Hauteur suffisante pour le texte TextMeshPro : evite les champs vides
            // lorsqu'une ligne est plus petite que la taille du caractere.
            float yHaut = .795f - i * .141f;
            var nomStat = Texte("StatNom" + i, stats, labels[i], 25, Or,
                .067f, yHaut - .130f, .370f, yHaut,
                TextAlignmentOptions.MidlineLeft);
            var valeurStat = Texte("StatValeur" + i, stats, valeurs[i], 25, TexteClair,
                .41f, yHaut - .130f, .923f, yHaut,
                TextAlignmentOptions.MidlineRight);
            // Les libelles sont sur une ligne et ne dependront plus du glyphe Ellipsis.
            nomStat.enableWordWrapping = false;
            valeurStat.enableWordWrapping = false;
            nomStat.enableAutoSizing = true;
            valeurStat.enableAutoSizing = true;
            nomStat.fontSizeMin = 16f;
            valeurStat.fontSizeMin = 16f;
            nomStat.fontSizeMax = 25f;
            valeurStat.fontSizeMax = 25f;
            nomStat.overflowMode = TextOverflowModes.Overflow;
            valeurStat.overflowMode = TextOverflowModes.Overflow;
            if (i < 4)
            {
                var separateur = Zone("SeparateurStat" + i, stats,
                    .07f, yHaut - .135f, .93f, yHaut - .131f);
                ImageUI(separateur, new Color(.75f, .57f, .30f, .35f));
            }
        }

        RectTransform details = Panneau("DetailsCompetenceMesHeros", racine, .519f, .167f, .975f, .313f);
        int total = perso.Competences == null ? 0 : perso.Competences.Length;
        if (total > 0)
        {
            indexCompetence = Mathf.Clamp(indexCompetence, 0, total - 1);
            Competence selection = perso.Competences[indexCompetence];
            Texte("TitreSkill", details, "COMPÉTENCE " + (indexCompetence + 1), 23, Or,
                .06f, .785f, .93f, .970f, TextAlignmentOptions.MidlineLeft);
            Texte("NomSkill", details, selection.Nom, 24, TexteClair,
                .06f, .524f, .95f, .805f, TextAlignmentOptions.MidlineLeft);
            Texte("EffetSkill", details, Description(selection), 19, TexteSecondaire,
                .06f, .045f, .94f, .540f, TextAlignmentOptions.MidlineLeft);
        }
        else
            Texte("AucuneCompetence", details, "Aucune compétence renseignée.", 24,
                TexteClair, .07f, .18f, .93f, .82f);

        texteInfo = Texte("AideMesHeros", racine,
            "Reliques et monnaie de promotion : fonctionnalités en préparation.",
            18, TexteSecondaire, .028f, .002f, .973f, .025f);
        if (afficherApercuPromotionMesHeros)
            AfficherApercuPromotionMesHeros(perso, progression);
    }

    // Etape 8.8 : consultation des conditions de promotion, SANS achat ni mutation de sauvegarde.
    // Cette interface reste dans Mes heros et n'affecte ni les combats ni l'Encyclopedie.
    private void AfficherApercuPromotionMesHeros(Perso perso, EtatProgressionHero etat)
    {
        RectTransform voile = Zone("VoilePromotion", racine, 0f, 0f, 1f, 1f);
        Image voileImage = ImageUI(voile, new Color(.005f, .014f, .035f, .78f), null, true);
        voileImage.raycastTarget = true;
        RectTransform fenetre = Panneau("FenetrePromotion", voile,
            .090f, .345f, .910f, .654f);
        Texte("TitrePromotion", fenetre, "PROMOTION DU HÉROS", 35, Or,
            .08f, .835f, .92f, .955f);
        Texte("NomPromotion", fenetre, perso.Nom + "  ·  " + etat.Etoiles + " / 5 étoiles",
            28, TexteClair, .08f, .707f, .92f, .835f);

        if (etat.Etoiles >= 5)
        {
            Texte("EtatPromotion", fenetre, "Rang maximum atteint.", 28,
                Or, .075f, .48f, .925f, .685f);
        }
        else
        {
            int requis = ProgressionHeros.PlafondNiveau(etat.Etoiles);
            bool possible = ProgressionHeros.PeutEtrePromu(indexSelectionne);
            Texte("ConditionPromotion", fenetre,
                "Niveau requis : " + requis + "   ·   Actuel : " + etat.Niveau,
                26, TexteClair, .075f, .56f, .925f, .705f);
            Texte("CoutPromotion", fenetre,
                "Coût indicatif : " + ProgressionHeros.CoutPromotionPropose(etat.Etoiles).ToString("N0") +
                " (monnaie à définir)",
                23, TexteSecondaire, .075f, .405f, .925f, .557f);
            Texte("EtatPromotion", fenetre,
                possible ? "Niveau atteint · paiement non activé" :
                "Atteins d'abord le niveau " + requis + ".",
                25, possible ? Or : TexteSecondaire,
                .075f, .245f, .925f, .412f);
        }
        Bouton("FermerPromotion", fenetre, "FERMER",
            () => { afficherApercuPromotionMesHeros = false; Construire(); },
            .315f, .065f, .685f, .222f, true, 22);
    }

    // Nouvel affichage de progression ; données lues dans ProgressionHeros (déjà validé).
    private void AfficherProgressionMesHeros(RectTransform parent, EtatProgressionHero etat)
    {
        RectTransform carte = Panneau("ProgressionMesHerosV87", parent, .025f, .319f, .975f, .449f);
        if (etat == null)
        {
            Texte("ProgressionIndisponible", carte, "Progression indisponible", 28,
                TexteClair, .07f, .20f, .93f, .80f);
            return;
        }
        int plafond = ProgressionHeros.PlafondNiveau(etat.Etoiles);
        bool bloque = etat.Niveau >= plafond;
        int xpMax = bloque ? 0 : ProgressionHeros.XpPourNiveauSuivant(etat.Niveau);
        float ratio = (!bloque && xpMax > 0)
            ? Mathf.Clamp01((float)etat.Experience / xpMax) : 0f;

        Texte("TitreProgression", carte, "PROGRESSION DU HÉROS", 23, Or,
            .045f, .795f, .670f, .965f, TextAlignmentOptions.MidlineLeft);
        // Etape suivante : apercu de promotion uniquement, aucune depense de monnaie.
        Bouton("VoirPromotion", carte, etat.Etoiles >= 5 ? "MAX" : "PROMOTION",
            () => { afficherApercuPromotionMesHeros = true; Construire(); },
            .713f, .788f, .958f, .972f, false, 17, etat.Etoiles < 5);
        Texte("NiveauEnGrand", carte, "Lv. " + etat.Niveau, 43, Or,
            .052f, .425f, .315f, .785f, TextAlignmentOptions.MidlineLeft);
        Texte("PlafondDeNiveau", carte, "MAX " + plafond, 22, TexteSecondaire,
            .052f, .252f, .315f, .485f, TextAlignmentOptions.MidlineLeft);
        Texte("RangEtoile", carte, "RANG", 19, TexteSecondaire,
            .415f, .673f, .945f, .825f, TextAlignmentOptions.MidlineLeft);

        // 5 étoiles sous forme d'images : pas de glyphes Unicode manquants dans TMP.
        for (int i = 0; i < 5; i++)
        {
            float x = .425f + i * .106f;
            RectTransform etoile = Zone("Etoile_" + (i + 1), carte,
                x, .430f, x + .083f, .720f);
            Image image = ImageUI(etoile, i < etat.Etoiles
                ? new Color(1f, .91f, .63f, 1f)
                : new Color(.33f, .42f, .58f, .82f));
            image.sprite = EtoileMesHerosSprite();
            image.preserveAspect = true;
        }
        string texteXP = bloque
            ? (etat.Etoiles == 5 ? "NIVEAU MAXIMUM ATTEINT" : "PALIER ATTEINT - PROMOTION À VENIR")
            : "XP  " + etat.Experience.ToString("N0") + " / " + xpMax.ToString("N0");
        var legende = Texte("XpCourante", carte, texteXP, 20,
            bloque ? Or : TexteClair, .340f, .258f, .955f, .448f,
            TextAlignmentOptions.MidlineRight);
        legende.enableAutoSizing = true;
        legende.fontSizeMin = 15;
        legende.fontSizeMax = 20;
        legende.overflowMode = TextOverflowModes.Truncate;

        RectTransform rail = Zone("RailXP", carte, .055f, .105f, .945f, .200f);
        ImageUI(rail, new Color(.050f, .115f, .208f, 1f), "bouton_sombre");
        RectTransform railInterieur = Zone("RailInterieur", rail, .012f, .24f, .988f, .76f);
        ImageUI(railInterieur, new Color(.16f, .28f, .43f, .88f));
        if (ratio > .001f)
        {
            var remplissage = Zone("XPRemplie", railInterieur, 0f, 0f, ratio, 1f);
            ImageUI(remplissage, new Color(.27f, .69f, .96f, 1f));
            var reflet = Zone("RefletXP", remplissage, 0f, .62f, 1f, 1f);
            ImageUI(reflet, new Color(.75f, .94f, 1f, .65f));
        }
    }

    // Texture d'une étoile à cinq branches dessinée en code, une fois pour toutes.
    private static Sprite etoileMesHeros;
    private static Sprite EtoileMesHerosSprite()
    {
        if (etoileMesHeros != null) return etoileMesHeros;
        const int taille = 80;
        Texture2D image = new Texture2D(taille, taille, TextureFormat.RGBA32, false);
        image.name = "EtoileProgressionUI";
        image.wrapMode = TextureWrapMode.Clamp;
        Vector2[] sommets = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float a = Mathf.PI * .5f + i * Mathf.PI / 5f;
            float r = (i % 2 == 0) ? 34f : 15f;
            sommets[i] = new Vector2(40f + Mathf.Cos(a) * r, 40f + Mathf.Sin(a) * r);
        }
        for (int y = 0; y < taille; y++)
            for (int x = 0; x < taille; x++)
            {
                bool interieur = false;
                for (int i = 0, j = sommets.Length - 1; i < sommets.Length; j = i++)
                {
                    Vector2 a = sommets[i], b = sommets[j];
                    if ((a.y > y) != (b.y > y) &&
                        x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                        interieur = !interieur;
                }
                Color c = Color.clear;
                if (interieur)
                {
                    float v = (float)y / (taille - 1);
                    c = Color.Lerp(new Color(.71f, .37f, .08f, 1f),
                        new Color(1f, .92f, .48f, 1f), v);
                }
                image.SetPixel(x, y, c);
            }
        image.Apply();
        etoileMesHeros = Sprite.Create(image,
            new Rect(0f, 0f, taille, taille), new Vector2(.5f, .5f), 100f);
        return etoileMesHeros;
    }

    private void AfficherReliquesMesHeros(RectTransform parent)
    {
        Texte("LegendeReliques", parent, "6 EMPLACEMENTS · À ÉQUIPER", 19, Or,
            .015f, .923f, .985f, 1f);
        string[] noms = { "ARME", "BOTTES", "ARMURE", "GANTS", "CASQUE", "AMULETTE" };
        string[] icones = { "arme", "bottes", "armure", "gants", "casque", "amulette" };
        for (int i = 0; i < 6; i++)
        {
            int pos = i;
            int col = i % 2;
            int ligne = i / 2;
            float gauche = col == 0 ? .005f : .511f;
            float haut = .911f - ligne * .300f;
            RectTransform cadre = Zone("Emplacement_" + noms[i], parent,
                gauche, haut - .274f, gauche + .483f, haut);
            Image fond = ImageUI(cadre, Color.white, "emplacement_relique", true);
            Button bouton = cadre.gameObject.AddComponent<Button>();
            bouton.targetGraphic = fond;
            bouton.onClick.AddListener(() => Message("Relique " + noms[pos] +
                " : équipement prévu à l'étape 10."));
            RectTransform imageSlot = Zone("Pictogramme", cadre, .215f, .275f, .785f, .862f);
            Image ico = ImageUI(imageSlot, new Color(.77f, .83f, .94f, .88f));
            ico.sprite = SpriteUI("Icons/" + icones[i]);
            ico.preserveAspect = true;
            Texte("Nom", cadre, noms[i], 16, TexteSecondaire,
                .01f, .020f, .99f, .235f);
        }
    }

    private void AfficherCompetencesMesHeros(RectTransform parent, Perso p)
    {
        Texte("LegendeCompetences", parent, "COMPÉTENCES DU HÉROS", 19, Or,
            .01f, .905f, .99f, .995f);
        for (int i = 0; i < 3; i++)
        {
            int position = i;
            bool active = p.Competences != null && i < p.Competences.Length;
            string libelle = active ? (i + 1) + ". " + p.Competences[i].Nom : "À DÉBLOQUER";
            float haut = .875f - i * .285f;
            Bouton("AccesCompetence" + i, parent, libelle,
                active ? (Action)(() => {
                    RetenirDefilement();
                    indexCompetence = position;
                    Construire();
                }) : null,
                .015f, haut - .239f, .985f, haut,
                active && indexCompetence == i, 21, active);
        }
    }

    private void AfficherInfosMesHeros(RectTransform parent, Perso p)
    {
        Texte("TitreFicheRapide", parent, "FICHE RAPIDE", 23, Or,
            .035f, .889f, .965f, .985f);
        Texte("TypeEtClasse", parent,
            "RÔLE\n" + p.Role + "\n\nRARETÉ\n" + p.Rarete,
            24, TexteClair, .08f, .400f, .92f, .855f);
        Texte("MeneurRapide", parent,
            p.Meneur == null ? "MENEUR : AUCUN" : "MENEUR DISPONIBLE",
            20, p.Meneur == null ? TexteSecondaire : Or,
            .06f, .230f, .94f, .390f);
        Bouton("InfosDetaillees", parent, "COMMUNAUTÉ / INFOS",
            OuvrirInfoPersonnage, .025f, .025f, .975f, .205f, false, 18);
    }

    private void AfficherRunesMesHeros(RectTransform parent)
    {
        Texte("TitreRunes", parent, "RUNES", 28, Or,
            .06f, .770f, .94f, .980f);
        Texte("RunesAvenir", parent,
            "Cet onglet reste réservé à une future mécanique.\n\n" +
            "Les six Reliques sont accessibles depuis RELIQUES.",
            21, TexteClair, .08f, .300f, .92f, .755f);
        Bouton("AccesReliques", parent, "VOIR LES RELIQUES",
            () => ChangerOnglet(Onglet.Equipements),
            .06f, .055f, .94f, .215f, false, 20);
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

    // Un seul Sprite procedurale reutilise pour tous les separateurs.
    // On garde le panneau et ses textes independants pour une future evolution.
    private static Sprite separateurStatsDegrade;

    private static Sprite ObtenirSeparateurStatsDegrade()
    {
        if (separateurStatsDegrade != null) return separateurStatsDegrade;

        const int largeur = 256;
        var texture = new Texture2D(largeur, 2, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.name = "SeparateurStatsOrDegrade";

        for (int x = 0; x < largeur; x++)
        {
            float t = x / (float)(largeur - 1);
            float proximiteCentre = 1f - Mathf.Abs(2f * t - 1f);
            float eclat = Mathf.Pow(proximiteCentre, 1.7f);
            var teinte = new Color(
                1f,
                Mathf.Lerp(.58f, .89f, eclat),
                Mathf.Lerp(.24f, .60f, eclat),
                Mathf.Lerp(.035f, .93f, eclat));
            texture.SetPixel(x, 0, teinte);
            texture.SetPixel(x, 1, teinte);
        }
        texture.Apply();
        separateurStatsDegrade = Sprite.Create(texture,
            new Rect(0f, 0f, largeur, 2f), new Vector2(.5f, .5f), 100f);
        return separateurStatsDegrade;
    }

    private void AjouterSeparateurStats(RectTransform parent, string nom, float hauteur)
    {
        // Halo large et discret, puis trait fin; toute la largeur est en degrade.
        var halo = Zone(nom + "_Halo", parent,
            .105f, hauteur - .007f, .895f, hauteur + .007f);
        var haloImage = ImageUI(halo, new Color(1f, 1f, 1f, .22f));
        haloImage.sprite = ObtenirSeparateurStatsDegrade();
        haloImage.raycastTarget = false;

        var trait = Zone(nom, parent,
            .105f, hauteur - .0023f, .895f, hauteur + .0023f);
        var image = ImageUI(trait, Color.white);
        image.sprite = ObtenirSeparateurStatsDegrade();
        image.raycastTarget = false;
    }

    // Fermeture de la fiche : fond bleu nuit propre, bordure doree biseautee,
    // deux diagonales UGUI pour la croix. Aucun caractère spécial / sprite requis.
    private void ConstruireCroixRetourEncyclopedie(RectTransform banniere)
    {
        // Décale la croix d'environ 12 pixels Canvas (référence 1080).
        // Son image cliquable est transparente : le bleu de la bannière reste visible.
        var zone = Zone("RetourGalerie", banniere,
            .873f, .218f, .964f, .782f);
        var fond = ImageUI(zone, Color.clear, null, true);
        var bouton = zone.gameObject.AddComponent<Button>();
        bouton.targetGraphic = fond;
        bouton.onClick.AddListener(RetourGalerie);
        var teintes = bouton.colors;
        teintes.normalColor = Color.white;
        teintes.highlightedColor = new Color(1f, .98f, .89f, 1f);
        teintes.pressedColor = new Color(.78f, .83f, .94f, 1f);
        bouton.colors = teintes;

        // Réutilise exactement le même dessin de contour que les autres cadres.
        DessinerCadreAvecImagesUnity(zone);

        // Ombre discrète puis deux branches dorées, dessinées directement en UI.
        var centre = new Vector2(.5f, .5f);
        var ombre = new Color(.20f, .105f, .025f, .95f);
        var lumiere = new Color(1f, .88f, .56f, 1f);
        AjouterDiagonaleCadre(zone, "CroixOmbre1", centre,
            new Vector2(0f, -2f), 43f, 11f, 45f, ombre);
        AjouterDiagonaleCadre(zone, "CroixOmbre2", centre,
            new Vector2(0f, -2f), 43f, 11f, -45f, ombre);
        AjouterDiagonaleCadre(zone, "CroixOr1", centre,
            Vector2.zero, 43f, 7.5f, 45f, lumiere);
        AjouterDiagonaleCadre(zone, "CroixOr2", centre,
            Vector2.zero, 43f, 7.5f, -45f, lumiere);
    }

    // ---- V3.6.1 : DEUX BOUTONS LATERAUX ENTIÈREMENT DYNAMIQUES ----
    // Fond bleu lumineux + contour dore COMPLET + icone + texte independants.
    // La forme asymetrique est differente des cases Skills / Eveil / Lead.
    // Aucune image decoupee, aucune nouvelle ressource externe necessaire.
    private static Sprite visuelBoutonLateral;
    private static Sprite iconeCommunaute;
    private static Sprite iconeUtilisation;

    private void ConstruireBoutonLateralEncyclo(string nom, RectTransform parent,
        string libelle, bool estCommunaute, Action action,
        float xMin, float yMin, float xMax, float yMax)
    {
        var zone = Zone(nom, parent, xMin, yMin, xMax, yMax);
        var fond = ImageUI(zone, Color.white);
        fond.sprite = ObtenirVisuelBoutonLateral();
        fond.type = Image.Type.Simple;
        fond.preserveAspect = false;
        fond.raycastTarget = true;

        var bouton = zone.gameObject.AddComponent<Button>();
        bouton.targetGraphic = fond;
        if (action != null) bouton.onClick.AddListener(() => action());

        // Effet au survol / pression : le sprite ne change jamais de taille.
        var couleurs = bouton.colors;
        couleurs.normalColor = Color.white;
        couleurs.highlightedColor = new Color(1f, .98f, .90f, 1f);
        couleurs.pressedColor = new Color(.76f, .85f, 1f, 1f);
        couleurs.fadeDuration = .12f;
        bouton.colors = couleurs;

        // Pictogramme dore (sans fond) dans la partie superieure.
        var zoneIcone = Zone("Pictogramme", zone, .34f, .47f, .66f, .88f);
        var imageIcone = ImageUI(zoneIcone, Color.white);
        imageIcone.sprite = ObtenirIconeBoutonLateral(estCommunaute);
        imageIcone.preserveAspect = true;
        imageIcone.raycastTarget = false;

        // Texte reel, modifiable, independant du contour et du pictogramme.
        var texte = Texte("Libelle", zone, libelle, 24, Or,
            .035f, .105f, .965f, .47f, TextAlignmentOptions.Center);
        texte.fontStyle = FontStyles.Bold;
        texte.enableAutoSizing = true;
        texte.fontSizeMin = 16;
        texte.fontSizeMax = 24;
        texte.overflowMode = TextOverflowModes.Overflow;
        texte.margin = Vector4.zero;
        texte.raycastTarget = false;
    }

    // Sprite procedural : coins biseautes asymetriques, fond bleu conserve,
    // leger eclat en haut a gauche, or metallique, pixels EXTERIEURS transparents.
    // Le sprite est genere UNE fois puis reutilise par les deux boutons.
    private static Sprite ObtenirVisuelBoutonLateral()
    {
        if (visuelBoutonLateral != null) return visuelBoutonLateral;

        const int largeur = 384;
        const int hauteur = 208;
        var tex = new Texture2D(largeur, hauteur, TextureFormat.RGBA32, false);
        tex.name = "BoutonLateralEncyclopedie_BleuOr";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        // Forme propre : coins differents des cadres de competences.
        // Tous les points sont A L'INTERIEUR du PNG, y compris le cote droit.
        Vector2[] polygone = {
            new Vector2(.014f, .16f),
            new Vector2(.077f, .025f),
            new Vector2(.927f, .025f),
            new Vector2(.986f, .13f),
            new Vector2(.986f, .845f),
            new Vector2(.916f, .975f),
            new Vector2(.108f, .975f),
            new Vector2(.014f, .855f)
        };
        var pixels = new Color[largeur * hauteur];
        for (int y = 0; y < hauteur; y++)
        {
            for (int x = 0; x < largeur; x++)
            {
                var uv = new Vector2((x + .5f) / largeur, (y + .5f) / hauteur);
                if (!DansPolygone(uv, polygone))
                {
                    pixels[y * largeur + x] = Color.clear;
                    continue;
                }

                float bord = DistanceBordPolygone(uv, polygone, largeur, hauteur);
                float illumination = Mathf.Exp(-Mathf.Pow((uv.x - .22f) / .40f, 2f)
                    - Mathf.Pow((uv.y - .88f) / .35f, 2f));
                float melangeBleu = Mathf.Clamp01(.22f + uv.y * .45f + illumination * .40f);
                Color teinte = Color.Lerp(
                    new Color(.026f, .055f, .122f, 1f),
                    new Color(.16f, .30f, .49f, 1f), melangeBleu);

                // Or a quatre niveaux : bronze exterieur / reflet / liseré /
                // ombre interieure. Aucun bord n'est emprunte a une photo.
                if (bord < 1.8f)
                    teinte = new Color(.37f, .21f, .065f, 1f);
                else if (bord < 4.8f)
                    teinte = Color.Lerp(new Color(.77f, .48f, .17f, 1f),
                        new Color(1f, .88f, .55f, 1f),
                        Mathf.Clamp01(.33f + uv.y * .50f + illumination * .25f));
                else if (bord < 6.8f)
                    teinte = new Color(.30f, .175f, .070f, 1f);
                else if (bord < 8.5f)
                    teinte = new Color(.92f, .70f, .37f, 1f);
                else if (bord < 11.2f)
                    teinte = Color.Lerp(teinte, new Color(.24f, .19f, .14f, 1f), .45f);

                // Contour antialiasé, transparent UNIQUEMENT en dehors.
                if (bord < 1.05f)
                    teinte.a = Mathf.Clamp01((bord + .15f) / 1.20f);
                pixels[y * largeur + x] = teinte;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        visuelBoutonLateral = Sprite.Create(tex,
            new Rect(0f, 0f, largeur, hauteur), new Vector2(.5f, .5f), 100f);
        visuelBoutonLateral.name = "BoutonLateralBleuOr";
        return visuelBoutonLateral;
    }

    // Petits pictogrammes dorés dessines localement, sans police speciale.
    // « Communauté » = trois silhouettes ; « Utilisation » = livre ouvert.
    private static Sprite ObtenirIconeBoutonLateral(bool estCommunaute)
    {
        if (estCommunaute && iconeCommunaute != null) return iconeCommunaute;
        if (!estCommunaute && iconeUtilisation != null) return iconeUtilisation;

        const int largeur = 120;
        const int hauteur = 74;
        var tex = new Texture2D(largeur, hauteur, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.name = estCommunaute ? "IconeCommunauteOr" : "IconeLivreOr";
        // Forme du livre initialisee une fois, et non pour chaque pixel.
        var pageGauche = new Vector2[] {
            new Vector2(.10f, .17f), new Vector2(.45f, .24f),
            new Vector2(.49f, .20f), new Vector2(.49f, .80f),
            new Vector2(.29f, .88f), new Vector2(.10f, .80f)
        };
        var pageDroite = new Vector2[] {
            new Vector2(.51f, .20f), new Vector2(.55f, .24f),
            new Vector2(.90f, .17f), new Vector2(.90f, .80f),
            new Vector2(.71f, .88f), new Vector2(.51f, .80f)
        };
        var pixels = new Color[largeur * hauteur];
        for (int y = 0; y < hauteur; y++)
        {
            for (int x = 0; x < largeur; x++)
            {
                float u = (x + .5f) / largeur;
                float v = (y + .5f) / hauteur;
                bool dessiner;
                if (estCommunaute)
                {
                    bool tetes = DansEllipse(u, v, .26f, .67f, .105f, .18f)
                        || DansEllipse(u, v, .50f, .76f, .12f, .19f)
                        || DansEllipse(u, v, .74f, .67f, .105f, .18f);
                    bool silhouettes = DansEllipse(u, v, .26f, .19f, .19f, .24f)
                        || DansEllipse(u, v, .74f, .19f, .19f, .24f)
                        || DansEllipse(u, v, .50f, .20f, .235f, .285f);
                    dessiner = tetes || silhouettes;
                }
                else
                {
                    // Pages du livre, bordure d'or, interieur bleu fonce.
                    bool dansGauche = DansPolygone(new Vector2(u, v), pageGauche);
                    bool dansDroite = DansPolygone(new Vector2(u, v), pageDroite);
                    float distanceGauche = DistanceBordPolygone(new Vector2(u,v), pageGauche,
                        largeur, hauteur);
                    float distanceDroite = DistanceBordPolygone(new Vector2(u,v), pageDroite,
                        largeur, hauteur);
                    bool cadreLivre = (dansGauche && distanceGauche < 5.0f)
                        || (dansDroite && distanceDroite < 5.0f);
                    bool tranche = (Mathf.Abs(u - .50f) < .022f && v > .11f && v < .83f);
                    dessiner = cadreLivre || tranche;
                }
                pixels[y * largeur + x] = dessiner
                    ? new Color(.98f, Mathf.Lerp(.62f, .85f, v), .33f, 1f)
                    : Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        var sprite = Sprite.Create(tex, new Rect(0f, 0f, largeur, hauteur),
            new Vector2(.5f, .5f), 100f);
        sprite.name = tex.name;
        if (estCommunaute) iconeCommunaute = sprite;
        else iconeUtilisation = sprite;
        return sprite;
    }

    private static bool DansEllipse(float x, float y, float centreX, float centreY,
        float rayonX, float rayonY)
    {
        float dx = (x - centreX) / rayonX;
        float dy = (y - centreY) / rayonY;
        return dx * dx + dy * dy <= 1f;
    }

    private static bool DansPolygone(Vector2 position, Vector2[] polygone)
    {
        bool dedans = false;
        for (int i = 0, j = polygone.Length - 1; i < polygone.Length; j = i++)
        {
            Vector2 a = polygone[i];
            Vector2 b = polygone[j];
            if ((a.y > position.y) != (b.y > position.y) &&
                position.x < (b.x - a.x) * (position.y - a.y) / (b.y - a.y) + a.x)
                dedans = !dedans;
        }
        return dedans;
    }

    private static float DistanceBordPolygone(Vector2 position, Vector2[] polygone,
        int largeur, int hauteur)
    {
        Vector2 point = new Vector2(position.x * largeur, position.y * hauteur);
        float minimum = float.MaxValue;
        for (int i = 0; i < polygone.Length; i++)
        {
            Vector2 a = new Vector2(polygone[i].x * largeur,
                polygone[i].y * hauteur);
            Vector2 b = new Vector2(polygone[(i + 1) % polygone.Length].x * largeur,
                polygone[(i + 1) % polygone.Length].y * hauteur);
            Vector2 ab = b - a;
            float projection = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
            float distance = Vector2.Distance(point, a + ab * projection);
            if (distance < minimum) minimum = distance;
        }
        return minimum;
    }

    // Fiche immersive V3.5, inspiree exactement de la maquette bleue/or.
    // Reservee a l'encyclopedie. La collection personnelle reste 100% V3.2.
    // Pour le futur rendu 3D, remplacer le composant Image "HeroIllustration2D"
    // dans "EmplacementModele3D" par une RawImage alimentee par une RenderTexture.
    private void ConstruireFicheEncyclopedie()
    {
        if (indexSelectionne < 0 || indexSelectionne >= Donnees.Persos.Length)
            indexSelectionne = 0;

        Perso p = Donnees.Persos[indexSelectionne];

        // 1 - Fond bleu nuit, angles dores et banniere EXACTE de la reference.
        IllustrationFiche("FondBleuEtOr", racine, "fond_fiche", 0f, 0f, 1f, 1f);
        IllustrationFiche("EnteteEncyclopedie", racine, "banniere_titre",
            0f, .910f, 1f, 1f);
        var panneauIdentite = IllustrationFiche("BannierePersonnage", racine,
            "banniere_hero", .019f, .816f, .977f, .917f);

        // Informations variables : jamais du texte incruste dans la photo.
        var titre = Texte("NomPersonnage", panneauIdentite, p.Nom, 48, Or,
            .085f, .45f, .855f, .88f);
        titre.enableAutoSizing = true;
        titre.fontSizeMin = 26;
        titre.fontSizeMax = 48;
        Texte("RoleEtRarete", panneauIdentite, p.Role + "  ·  " + p.Rarete, 30,
            TexteClair, .13f, .12f, .82f, .46f);

        // Bouton de fermeture entierement construit en UGUI.
        // La banniere PNG livree ne contient plus l'ancienne croix incrustee.
        ConstruireCroixRetourEncyclopedie(panneauIdentite);

        // 2 - Deux boutons a gauche. Ils ouvrent les vraies pages deja existantes.
        // V3.6.1 : boutons entièrement construits dans Unity UI :
        // même placement, aucun bord découpé, aucun fond parasite.
        ConstruireBoutonLateralEncyclo("Communauté", racine, "COMMUNAUTÉ", true,
            () => OuvrirInfoDepuisEncyclopedie(SousInfo.Avis),
            .017f, .739f, .241f, .807f);
        ConstruireBoutonLateralEncyclo("Utilisation", racine, "UTILISATION", false,
            () => OuvrirInfoDepuisEncyclopedie(SousInfo.Utilisations),
            .017f, .670f, .241f, .737f);

        // 3 - Illustration de ce personnage, en conservant son sprite d'origine.
        // Ce noeud accueillera plus tard le modele 3D (camera -> RenderTexture).
        var emplacement3D = Zone("EmplacementModele3D", racine,
            .178f, .236f, .794f, .821f);
        var imageHero = Zone("HeroIllustration2D", emplacement3D,
            0f, 0f, 1f, 1f);
        var art = ImageUI(imageHero, Color.white);
        Sprite illustration = SpriteUI("HeroArt/" + indexSelectionne);
        art.sprite = illustration != null ? illustration : Portrait(indexSelectionne);
        art.preserveAspect = true;
        art.raycastTarget = false;
        if (art.sprite == null)
            Texte("IllustrationManquante", emplacement3D, p.Nom, 38, TexteClair,
                .1f, .3f, .9f, .7f);

        // 4 - Les 5 cadres existants, remplis par des icones INDEPENDANTES.
        // Le modele 3D central, les bordures, les coordonnees et les clics
        // d'origine ne sont pas modifies. Icones provisoires, pas definitives.
        RectTransform cadreEveil = BoutonFicheImage(
            "EveilVerrouille", racine, "icone_eveil", null,
            .771f, .691f, .966f, .796f, false);
        AjouterIconeDansCadre(cadreEveil, "Eveil", false);

        string[] visuels = { "Competence1", "Competence2", "Competence3" };
        float[] bas = { .574f, .471f, .367f };
        const float hauteurCompetence = .096f;
        for (int j = 0; j < 3; j++)
        {
            int numero = j;
            bool existe = p.Competences != null && j < p.Competences.Length;
            RectTransform cadre = BoutonFicheImage("Competence_" + (j + 1), racine,
                existe ? "icone_skill_" + (j + 1) : "icone_vide",
                existe ? (Action)(() => SelectionnerCompetenceEncyclopedie(numero)) : null,
                .786f, bas[j], .954f, bas[j] + hauteurCompetence, existe);
            // Les soins sont correctement identifies par les donnees JSON.
            string icone = !existe ? "Verrouille" :
                (p.Competences[j].EstSoin ? "Soin" : visuels[j]);
            AjouterIconeDansCadre(cadre, icone, existe);
        }

        bool meneurExiste = p.Meneur != null;
        RectTransform cadreLead = BoutonFicheImage("Lead", racine,
            meneurExiste ? "icone_lead" : "icone_vide",
            meneurExiste ? (Action)SelectionnerMeneurEncyclopedie : null,
            .786f, .263f, .954f, .359f, meneurExiste);
        AjouterIconeDansCadre(cadreLead, meneurExiste ? "Lead" : "LeadAbsent",
            meneurExiste);

        // 5 - Veritable panneau dore de la maquette : les chiffres originaux
        // ont ete effaces de l'asset, puis remplis avec les stats JSON du heros.
        var statistiques = IllustrationFiche("PanneauStats", racine, "panneau_stats",
            .022f, .052f, .486f, .229f);
        string[] valeurs =
        {
            Stat(p, 0).ToString("N0"),
            Stat(p, 1).ToString("N0"),
            Stat(p, 2).ToString("N0"),
            p.Spd.ToString(),
            p.Crit + " %"
        };
        // Titre et lignes entierement dynamiques (plus de valeurs sans legendes).
        var titreStatistiques = Texte("TitreStatistiques", statistiques,
            "STATISTIQUES", 28, Or,
            .09f, .837f, .91f, .970f, TextAlignmentOptions.Center);
        titreStatistiques.fontStyle = FontStyles.Bold;
        // Reflet dore : centre lumineux, extremites douces et assombries.
        AjouterSeparateurStats(statistiques, "SeparateurTitre", .820f);

        string[] etiquettes = { "PV", "ATQ", "DEF", "SPD", "CRIT" };
        float[] centre = { .735f, .582f, .430f, .278f, .126f };
        for (int i = 0; i < valeurs.Length; i++)
        {
            // V3.5.9 : lignes assez hautes pour la police TMP.
            // Sans cela, TextMeshPro masque le texte (mode Ellipsis) et
            // affiche des avertissements sur la police LiberationSans SDF.
            var label = Texte("LibelleStat_" + i, statistiques,
                etiquettes[i], 30, Or,
                .125f, centre[i] - .072f, .52f, centre[i] + .072f,
                TextAlignmentOptions.MidlineLeft);
            label.fontStyle = FontStyles.Bold;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.margin = Vector4.zero;

            var valeur = Texte("Valeur" + i, statistiques,
                valeurs[i], 31, TexteClair,
                .54f, centre[i] - .072f, .935f, centre[i] + .072f,
                TextAlignmentOptions.MidlineRight);
            valeur.enableWordWrapping = false;
            valeur.overflowMode = TextOverflowModes.Overflow;
            valeur.margin = Vector4.zero;

            // Cinq traits sous les cinq lignes : y compris sous CRIT.
            AjouterSeparateurStats(statistiques,
                "SeparateurStat_" + i, centre[i] - .077f);
        }

        // 6 - Pop-up de details : absente par defaut comme sur la reference.
        if (afficherDescriptionCompetence && p.Competences != null &&
            indexCompetence >= 0 && indexCompetence < p.Competences.Length)
        {
            Competence c = p.Competences[indexCompetence];
            var popup = Panneau("DetailsCompetence", racine,
                .275f, .292f, .765f, .500f);
            Texte("NomCompetence", popup, c.Nom, 29, Or,
                .05f, .64f, .95f, .91f);
            Texte("DescriptionCompetence", popup, Description(c), 24, TexteClair,
                .07f, .20f, .93f, .66f);
            Bouton("FermerDetails", popup, "FERMER",
                () => { afficherDescriptionCompetence = false; Construire(); },
                .30f, .035f, .70f, .19f, false, 20);
        }
        else if (afficherDescriptionMeneur && p.Meneur != null)
        {
            var popup = Panneau("DetailsMeneur", racine,
                .275f, .292f, .765f, .500f);
            Texte("TitreMeneur", popup, "TALENT DE MENEUR", 29, Or,
                .05f, .64f, .95f, .91f);
            Texte("EffetMeneur", popup, p.Meneur.Texte, 23, TexteClair,
                .07f, .19f, .93f, .68f);
            Bouton("FermerDetails", popup, "FERMER",
                () => { afficherDescriptionMeneur = false; Construire(); },
                .30f, .035f, .70f, .19f, false, 20);
        }
    }






}
