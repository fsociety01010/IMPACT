using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

/// <summary>
/// Plays a sound
/// </summary>
public class playSound : MonoBehaviour
{
    //The audio manager to use
    public AudioManager audioManager;
    //The ibject performing the KNN
    public KNN knn;
    //The hand models
    public HandModel left;
    public HandModel right;

    //Plays a sound when something touches the surface
    private void OnCollisionEnter(Collision collision)
    {
        bool isLeft = (collision.transform.parent!=null)&&collision.transform.parent.name.ToLower().Contains("left");
        bool isRight = (collision.transform.parent != null) && collision.transform.parent.name.ToLower().Contains("right");
        if (isLeft || isRight)
        {
            print(knn.getClassOfContactHand(isRight ? "right" : "left"));
            audioManager.PlaySound(knn.getClassOfContactHand(isRight ? "right" : "left"));
        }
        else
        {
            audioManager.PlaySound("default");
        }
        
    }
}
