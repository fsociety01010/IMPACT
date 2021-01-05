using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using System.IO;
using UnityEngine.UI;
/// <summary>
/// Allows us to record data
/// </summary>
public class RecordData : MonoBehaviour
{
    //The LeapMotion
    private Controller controller;
    //Checks if we're recording
    private bool isRecording = false;
    //The handmodels
    public HandModel left;
    public HandModel right;
    //The field were we type the gesture we want to record
    public InputField input;
    //The mode we're recording
    string mode = "closed";
    //The file to record to
    public string path = "";
    void Start()
    {
        controller = new Controller();
    }
    /// <summary>
    /// Enables recording
    /// </summary>
    public void enableRecording()
    {
        isRecording = true;
    }

    /// <summary>
    /// Disable recording
    /// </summary>
    public void disableRecording()
    {
        isRecording = false;
    }
    /// <summary>
    /// Switches between recording and not recording
    /// </summary>
    public void toggle()
    {
        mode = input.text;
        isRecording = !isRecording;
    }

    /// <summary>
    /// Records data
    /// </summary>
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
                string rotation = hand.Rotation.x + " " + hand.Rotation.y + " " + hand.Rotation.z + " " + hand.Rotation.w + " ";
                File.AppendAllText(path, distance+rotation+ mode + "\n");
            }
        }
    }
}
