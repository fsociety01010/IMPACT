using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using UnityEngine.UI;
using System.IO;
using System;
using System.Linq;

/// <summary>
/// Performs KNN
/// </summary>
public class KNN : MonoBehaviour
{

    //The amount of neighbours to compare the hand to.
    public int k = 3;

    //The LeapMotion
    private Controller controller;

    //Checks if we need to perform the KNN
    private bool isTracking = true;

    //The handmodels
    public HandModel left;
    public HandModel right;

    //The class of both hands
    string classLeft;
    string classRight;

    //The zones where to display the class
    public Text textLeft;
    public Text textRight;

    //The path to the data
    public string path;

    //The path to the test data
    public string pathTest;

    //The lightsource of the scene
    public Light sun;

    //The range of the LeapMotion
    float rangeLeap = 430;

    //Les données du fichier
    private List<Tuple<string, float[]>> data = new List<Tuple<string, float[]>>();

    void Start()
    {
        controller = new Controller();
        readDataFile();
    }
    /// <summary>
    /// Loads the data used to perform the KNN
    /// </summary>
    private void readDataFile()
    {
        StreamReader read = new StreamReader(path);
        while (!read.EndOfStream)
        {
            string[] line = read.ReadLine().Split(' ');
            string classe = line[line.Length - 1];
            float[] vals = Array.ConvertAll(line.Slice(0, line.Length - 1).ToArray(), float.Parse);
            data.Add(new Tuple<string, float[]>(classe, vals));
        }
    }
    /// <summary>
    /// Gets the class of the hand touching the glass surface
    /// </summary>
    public string getClassOfContactHand(string hand)
    {
        return hand.Equals("right") ? classRight : classLeft;
    }

    /// <summary>
    /// Enable tracking
    /// </summary>
    public void enableTracking()
    {
        isTracking = true;
        readDataFile();
    }
    /// <summary>
    /// Disable tracking
    /// </summary>
    public void disableTracking()
    {
        isTracking = false;
    }
    /// <summary>
    /// Computes a hand's class
    /// </summary>
    string getClassOfHand(Tuple<string, float>[] kNearest)
    {
        List<string> label = new List<string>();
        List<int> vals= new List<int>();
        foreach (Tuple<string, float> tuple in kNearest)
        {
            if (label.Contains(tuple.Item1))
            {
                vals[label.IndexOf(tuple.Item1)] += 1;
            }
            else
            {
                label.Add(tuple.Item1);
                vals.Add(1);
            }
        }
        return label[vals.IndexOf(vals.Max())];
    }
    /// <summary>
    /// Adjusts the sun's height
    /// </summary>
    public void adjustSunHeight(Hand hand,string classe)
    {
        if (classe == "sun")
        {
            sun.intensity = Math.Max((float)0.4,Math.Min((float)1.5,hand.PalmPosition.y / rangeLeap));
        }
    }

    /// <summary>
    /// Tests the KNN's accuracy
    /// </summary>
    public void testKNN()
    {
        StreamReader read = new StreamReader(pathTest);
        int nbTestData = 0;
        int nbCorrectGuesses = 0;
        List<string> listClasse = new List<string>(new string[]{"closed", "open", "poke", "sun"});
        int[,] confusionMatrix =new int[4, 4] { { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 } };
        while (!read.EndOfStream)
        {
            string[] line = read.ReadLine().Split(' ');
            string classe = line[line.Length - 1];
            float[] vals = Array.ConvertAll(line.Slice(0, line.Length - 1).ToArray(), float.Parse);

            List<Tuple<string, float>> dists = new List<Tuple<string, float>>();
            foreach (Tuple<string, float[]> tuple in data)
            {
                float d = 0;
                for (int i = 0; i < tuple.Item2.Length; i++)
                {
                    d += Mathf.Pow(tuple.Item2[i] - vals[i], 2);
                }
                dists.Add(new Tuple<string, float>(tuple.Item1, Mathf.Sqrt(d)));
            }
            dists.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
            string predictedClasse = getClassOfHand(dists.Slice(0, k).ToArray());
            nbTestData++;
            confusionMatrix[listClasse.IndexOf(classe), listClasse.IndexOf(predictedClasse)]++;
            if (predictedClasse.Equals(classe))
            {
                nbCorrectGuesses++;
            }
        }
        print("accuracy : "+ (nbCorrectGuesses*100)/nbTestData+"%");
        string disp = "";
        for(int i = 0; i < listClasse.Count; i++)
        {
            disp+="[";
            for (int j = 0; j < listClasse.Count; j++)
            {
                disp += confusionMatrix[i,j]+" ";
            }
            disp += "]\n";
        }
        print(disp);
    }
    /// <summary>
    /// Performs the KNN every frame
    /// </summary>
    void Update()
    {
        //Récupère les deux mains
        Frame frame = controller.Frame();
        List<Hand> hands = frame.Hands;
        if (hands.Count != 0 && isTracking && data.Count != 0)
        {

            //Pour les deux mains
            foreach (Hand hand in hands)
            {

                //Calcule la distance de la paume avec le bout des doigts
                List<float> handData = new List<float>();
                Vector palm = hand.PalmPosition;
                foreach (Finger finger in hand.Fingers)
                {
                    Vector posFinger = finger.TipPosition;
                    float dist = posFinger.DistanceTo(palm);
                    handData.Add(dist);
                }
                handData.Add(hand.Rotation.x);
                handData.Add(hand.Rotation.y);
                handData.Add(hand.Rotation.z);
                handData.Add(hand.Rotation.w);
                //Calcule du KNN

                List<Tuple<string, float>> dists = new List<Tuple<string, float>>();
                foreach(Tuple<string,float[]> tuple in data)
                {
                    float d = 0;
                    for(int i = 0; i < tuple.Item2.Length; i++)
                    {
                        d += Mathf.Pow(tuple.Item2[i] - handData[i],2);
                    }
                    dists.Add(new Tuple<string, float>(tuple.Item1, Mathf.Sqrt(d)));
                }
                dists.Sort((t1,t2)=>t1.Item2.CompareTo(t2.Item2));
                string classe = getClassOfHand(dists.Slice(0, k).ToArray());

                //Change le texte
                if (hand.IsLeft)
                {
                    classLeft = classe;
                    textLeft.text = "Left Hand : " +classe;
                }else if (hand.IsRight)
                {
                    classRight = classe;
                    textRight.text = "Right Hand : " + classe;
                    adjustSunHeight(hand, classe);
                }
            }
        }
    }
}

