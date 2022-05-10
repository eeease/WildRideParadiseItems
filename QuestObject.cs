using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Place this on an object that will be collected or trigger a quest completion.
[RequireComponent(typeof(Collider))]
public class QuestObject : MonoBehaviour
{
    //drag in the quest that this script will control?
    public Quest questLine;
    public Quest[] multiQuest; //trees 10, 20, all, ex;
    int multiIndex;
    GameObject objToTurnOn;
    // Start is called before the first frame update
    private void Awake()
    {
        if(multiQuest.Length>0)
        {
            questLine = multiQuest[0];
        }
        if(questLine==null)
        {
            print("Forgot to put a quest in " + gameObject.name);
            Destroy(this);
        }
    }
    void Start()
    {
        //!!this is causing a problem with quest objects that are on spawned GOs (ex boulders, each time one spawns, it turns this false)
        //questLine.completeThisRun = false;
        questLine.currentProgress = 0; //have to reset this cause scriptable objects 'remember' when they're changed.
        for (int i = 0; i < multiQuest.Length; i++)
        {
            multiQuest[i].completeThisRun = false;
            multiQuest[i].currentProgress = 0;
        }

        if (!string.IsNullOrEmpty(questLine.turnThisON))
        {
            StartCoroutine(TurnObjOnDelay(questLine.multiOnOrder));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator TurnObjOnDelay(float del)
    {
        yield return new WaitForSecondsRealtime(del);
        //find it and turn it off and store it in a reference var
        objToTurnOn = GameObject.Find(questLine.turnThisON);
        objToTurnOn.SetActive(false);

    }

    public void UpdateQuest(WackyCarBehaviour wcb)
    {
        bool fireCheck = (questLine.requiresFire && wcb.onFire|| !questLine.requiresFire);
        if (questLine.sfxToPlay != null)
        {
            GameManager.GM.PlayClip(questLine.sfxToPlay);
            //AudioSource.PlayClipAtPoint(questLine.sfxToPlay, Camera.main.transform.position);

        }
        if (questLine.isOneTime)
        {
            if(fireCheck)
            {
                CompleteQuest();
                if (objToTurnOn != null)
                {
                    objToTurnOn.SetActive(true);
                }
            }
        }
        else
        {
            if (multiQuest.Length > 0)
            {
                //check if the early one(s) are completed and increment my index:
                for (int i = multiIndex; i < multiQuest.Length; i++)
                {
                    if(multiQuest[i].completeThisRun)
                    {
                        multiIndex++;
                    }
                }
                questLine = multiQuest[multiIndex];
                //increment the others in the array (not the current):
                for (int i = multiIndex+1; i < multiQuest.Length; i++)
                {
                    multiQuest[i].currentProgress++;
                }
            }
            //implement the current:
            questLine.currentProgress++;
            
            if (questLine.currentProgress>=questLine.maxToCollect)
            {
                //you completed the quest!
                CompleteQuest();
            }
            else
            {
                GameManager.GM.AchievementNotif(questLine);
            }
        }

        //drop particle or other cool effect:

        //die:
        if(questLine.requiresFire)
        {
            //instantiating first then assigning parent so it keeps its original size.
            GameObject g = Instantiate(wcb.fireParticles, transform.position, Quaternion.identity);
            g.transform.SetParent(gameObject.transform);
        }
        if (!questLine.isPersistent)
        {
            if (fireCheck)
            Destroy(gameObject);
        }
        if(questLine.destroyCar)
        {
            wcb.Explode();
        }
        if(!string.IsNullOrEmpty(questLine.turnThisOff))
        {
            GameObject.Find(questLine.turnThisOff).SetActive(false);
        }
        

    }

    void CompleteQuest()
    {
        //show a snazzy visual notification:
        if(!questLine.completeThisRun)
        {
            //it already plays audio if it's not one time, so for one times we have to play it here:
            if(questLine.isOneTime&&questLine.sfxToPlay!=null)
            {
                GameManager.GM.PlayClip(questLine.sfxToPlay);
            }
            GameManager.GM.AchievementNotif(questLine);
            //print("about to check");
            if (multiQuest.Length > 0)
            {
                //print("incrementing multiquest");
                multiIndex++;
                if(multiIndex<multiQuest.Length)
                questLine = multiQuest[multiIndex];
            }
        }
       
        //play a quest complete sfx?

        //mark it as complete so we can show it at the end of the run:
        //questLine.completeThisRun = true;
        //questLine.completeOverall = true; //do i need to store this in PP for subsequent plays??


    }
}
