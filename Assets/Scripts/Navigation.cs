
using UnityEngine;
using UnityEngine.SceneManagement;

public class Navigation : MonoBehaviour
{
    public void OuvrirCollection()
    {
        SceneManager.LoadScene("Collection");
    }

    public void OuvrirEquipe()
    {
        SceneManager.LoadScene("Equipe");
    }

    public void OuvrirCombat()
    {
        SceneManager.LoadScene("Combat");
    }

    public void RetourMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
