using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// Allows switching between scenes
/// </summary>
public class changeScene : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    /// <summary>
    /// Switches to the main menu
    /// </summary>
    public void toMenu()
    {
        SceneManager.LoadScene("IMPACT");
    }
    /// <summary>
    /// Switches to the leap motion scene
    /// </summary>
    public void toKNN()
    {
        SceneManager.LoadScene("leapScene");
    }
    /// <summary>
    /// Switches to the impact test scene
    /// </summary>
    public void toTestImpact()
    {
        SceneManager.LoadScene("impactTestScene");
    }
    /// <summary>
    /// Quits the application
    /// </summary>
    public void quit()
    {
        Application.Quit();
    }

    // Update is called once per frame
    void Update()
    {
    }
}
