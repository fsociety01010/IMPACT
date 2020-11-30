using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour
{
    AudioManager audioManager;

    public string audioName;
    // Start is called before the first frame update
    void Start()
    {
        audioManager = AudioManager.instance;
        audioManager.PlaySound("Verre-fin-1");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
