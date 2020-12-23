using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playSound : MonoBehaviour
{
    public AudioClip audio;
    private bool wait;
    // Start is called before the first frame update
    void Start()
    {
        wait = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator timer()
    {
        wait = true;
        yield return new WaitForSeconds(0.8f);
        wait = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!wait)
        {
            StartCoroutine(timer());
            AudioSource.PlayClipAtPoint(audio, collision.GetContact(0).point);
        }
        
    }
}
