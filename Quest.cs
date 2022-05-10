using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Quest : ScriptableObject
{
    public string questName;
    public string questDescription;
    public bool isOneTime, isTimeDependent, carQuest, requiresFire;
    [Tooltip("If true, object doesn't destroy when hit")]
    public bool isPersistent, destroyCar, destroyObject;
    public int maxToCollect, currentProgress;
    [Tooltip("This plays each time you collect, ex. monkey screech")]
    public AudioClip sfxToPlay;
    public bool completeThisRun, completeOverall;
    [Tooltip("How many seconds INTO the run should this quest 'Turn on'?")]
    public float timedStart;
    [Tooltip("How long should the quest stay 'on'?")]
    public float timedLength;
    [Tooltip("The exact name of the obj to turn off")]
    public string turnThisOff;
    public string turnThisON;
    [Tooltip("If domino effect turning on, set override order here (0 = first)")]
    public float multiOnOrder;
}
