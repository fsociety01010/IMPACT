using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

public class playSound : MonoBehaviour
{
    public AudioManager audioManager;
    private bool wait;
    public KNN knn;
    public HandModel left;
    public HandModel right;

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
