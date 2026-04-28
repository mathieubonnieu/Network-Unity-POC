using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapScene : MonoBehaviour
{
    
    public IEnumerator Start()
    {
        
        SceneManager.LoadScene("MainMenu");
        yield return null;
    }
}
