using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// Reloads the current scene
/// </summary>
public class restartScene : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    /// <summary>
    /// Reloads the current scene
    /// </summary>
    public void reload()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
