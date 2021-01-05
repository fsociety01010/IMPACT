using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class changeScene : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void toMenu()
    {
        SceneManager.LoadScene("IMPACT");
    }
    public void toKNN()
    {
        SceneManager.LoadScene("leapScene");
    }
    public void toTestImpact()
    {
        SceneManager.LoadScene("impactTestScene");
    }
    public void quit()
    {
        Application.Quit();
    }

    // Update is called once per frame
    void Update()
    {
    }
}
