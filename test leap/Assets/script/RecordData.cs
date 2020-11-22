using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using System.IO;


public class RecordData : MonoBehaviour
{
    // Start is called before the first frame update
    private Controller controller;
    private bool isRecording = false;
    public HandModel left;
    public HandModel right;
    public string mode = "closed";
    public string path = "";
    void Start()
    {
        controller = new Controller();
    }

    public void enableRecording()
    {
        isRecording = true;
    }

    public void disableRecording()
    {
        isRecording = false;
    }

    public void toggle()
    {
        isRecording = !isRecording;
    }

    // Update is called once per frame
    void Update()
    {
        Frame frame = controller.Frame();
        List<Hand> hands = frame.Hands;
        if (hands.Count != 0 && isRecording)
        {
            foreach (Hand hand in hands)
            {
                string distance = "";
                Vector palm = hand.PalmPosition;
                foreach (Finger finger in hand.Fingers)
                {
                    Vector posFinger = finger.TipPosition;
                    float dist = posFinger.DistanceTo(palm);
                    distance += dist + " ";
                }
                //print(distance);
                File.AppendAllText(path, distance + mode + "\n");
            }
        }
    }
}
