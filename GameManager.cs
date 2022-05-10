using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using TMPro;

/*Attributions, etc:
 * Quest Design Helpers: Charles, Alejandro, Sean, Alex, Anthony, Ted
 * 
 * Soccer goal by Poly by Google: https://polygone.art/#page=model&guid=7t4hmicGHV4 //couldn't get this to show ;\
 * Barn by Poly by Google: https://polygone.art/#page=model&guid=dSsUaUlaxHk
 * Tree stump by Poly by Google: https://polygone.art/#page=model&guid=esFOngb0uwl
 * Bar by Poly by Google: https://polygone.art/#page=model&guid=cr_gfObT2-R
 * Skeleton Key by sirkitree: https://polygone.art/#page=model&guid=dR-kItm5ihP
 * Steak by Poly by Google: https://polygone.art/#page=model&guid=6Kmmf2ViaNB
 * Drumstick Chicken by Poly by Google: https://polygone.art/#page=model&guid=5qyQzU0o9TR
 * beef steak by Dario Demi (D911C): https://polygone.art/#page=model&guid=bahvpV8htLb
 * BruBurger ext railing fence by BJP Studio: https://polygone.art/#page=model&guid=9q_blP1v6UZ
 * chain link fence by Jeremy Eyring: https://polygone.art/#page=model&guid=5JOX9wrmsTs
 * Fountain by Poly by Google: https://polygone.art/#page=model&guid=7AydBrjR2Ss
 * Red Couch by Seventy4: https://polygone.art/#page=model&guid=dp_qvs6aiP3
 * old lamp by Justin Randall: https://polygone.art/#page=model&guid=73r4EQM-Z8e
 * Mario block by Shizvayne (Shizvayne) : https://polygone.art/#page=model&guid=3qcRbHeXEm1
 * mario flag by Shizvayne (Shizvayne): https://polygone.art/#page=model&guid=03bTj_ZgzJ7
 * mario pipe by Shizvayne (Shizvayne): https://polygone.art/#page=model&guid=4foLRNiprUa
 * Mushroom by Aphikhun Wiwatthanaphirak: https://polygone.art/#page=model&guid=7S0F1NIxztY
 * Red Flower by Julian: https://polygone.art/#page=model&guid=8tZ75emMtR3
 * Car model: https://opengameart.org/content/car-vw-corradon
 * */
//this will hold overall information and some useful functions/coroutines
//can be called by using GameManager.GM. [insert public var or function here]
public class GameManager : MonoBehaviour
{
    public static GameManager GM;
    public bool resetQuestProgression;
    [Tooltip("0=title; 1=ingame; 2=endscreen")]
    public int gameState = 0;
    public List<Quest> QuestLog; //this will keep track of all quests for showing what's been completed.
    public List<QuestObject> TimedQuests; //this will run through and start all of the time-based quests.
    public List<Quest> CarQuests; //quests that the car will unlock without interacting with another obj?
    public GameObject car;
    public MeteorSpawner mSpawner;
    [Tooltip("Synced with gameState")]
    public AudioClip[] bgms;
    public AudioClip questComplete, questCounter, applause, impactSFX, meteorExplode, ballKick, gooooal;
    AudioSource aud;
    public AudioMixer audSettings;
    public AudioMixerGroup audMGroup;
    public GameObject genericMeteorCollPF; //generic explosion prefab that all meteors will drop when they hit their destination.
    public GameObject meteorPointOfImpact; //where will the big boy hit at the end of the run?

    [Header("Run options")]
    public float totalRunTime = 300; //5 minute default
    [HideInInspector]
    public float timeLeft;
    public List<Transform> respawnLocations; //using transforms so we can also set the car's rotation upon respawn if we want to.

    [Header("UI Stuff")]
    [Tooltip("How long should the achievement popup stay on screen?")]
    public float achNotDel;
    //public Text ;
    public TextMeshProUGUI pLostText, endGameCompletionTxt, timeLeftT, speedT;
    //public Text achNameT, achProgressT;
    public AchievementScript achScriptInGame;
    public GameObject achNotif, endGamePanel, inGamePanel, cinematicPanel; //turn this on and it'll play its OnEnter animation.  Remember to set the name text before.
    public GameObject checklistParentTitle, checkListParentEndRun, achChecklistPrefab, chkListParentInGame; //don't like that we're using different checklists but whatever
    public Button endGameRestartButt, pauseScreenRestartButt;
    public Scrollbar titleChecklistSB, endGameSB, inRunSB;
    Coroutine achNotifCR; //keep a reference of the last one to turn it off if need be.
    public List<Quest> achOnDeck; //for delay, we'll add new ones to a list and when the old one is done, check the list and play the next.
    public GameObject pauseMenu, speedometer;
    public Slider[] volumeSliders;

    public GameObject cinemaCam;
    public float cineCamDelay = 8f;
    Coroutine cinematicCR;

    float timeToOneHundred;

    bool noAchievement, noNEWAchievement;
    public ArcadeCar playerCar;

    [TextArea(2,5)]
    public string[] paradiseLostQuotes;

    //weird quest stuff:
    float quietTimer, vol;
    Quest quiet;

    private void Awake()
    {
        GM = this;
        GetRespawnLocs();
    }
    // Start is called before the first frame update
    void Start()
    {
        playerCar = car.GetComponent<ArcadeCar>();
        achOnDeck = new List<Quest>();
        Time.timeScale = 1;
        aud = GetComponent<AudioSource>();
        timeLeft = totalRunTime;
        if(resetQuestProgression)
        {
            ResetQuests();
        }
        //find all the one-time quests and turn them off
        //!! might need a few frames of opaque canvas at the start of the game to hide this:
        foreach(QuestObject qo in FindObjectsOfType<QuestObject>())
        {
            if(qo.questLine!=null && qo.questLine.isTimeDependent)
            {
                TimedQuests.Add(qo);
                qo.gameObject.SetActive(false);
            }
        }

        quiet = QuestSearch("A Quiet Race");

        noAchievement = true;
        noNEWAchievement = true;

        //first clear the checklist:
        ClearChecklist(checklistParentTitle);
        PopulateChecklist(checklistParentTitle.transform);
        StartCoroutine(ScrollBarSet(titleChecklistSB));

        //load stuff:
        timeToOneHundred = PlayerPrefs.GetFloat("timeToOneHundred");
        //not saving this in PP yet but starting it low should be fine enough atm
        foreach (Slider s in volumeSliders)
        {
            s.value = -10f;
        }
    }

    public void ClearChecklist(GameObject contentParent)
    {
        foreach(AchievementScript a in contentParent.GetComponentsInChildren<AchievementScript>())
        {
            Destroy(a.gameObject);
        }
    }

    //this is dumb but you have to wait a frame for this to actually register:
    public IEnumerator ScrollBarSet(Scrollbar sb)
    {
        yield return new WaitForEndOfFrame();
        sb.value = 1; //set the scrollbar value to 1 so it's at the top of the scrollRect after populating
    }

    //i'm 99% sure we don't want to do much in Update with the GameManager:
    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.M))
        //{
        //    mSpawner.MeteorsSpawn();
        //}
        
        if (gameState == 1)
        {
            timeLeft -= Time.deltaTime;
            
            if (timeLeft <= 0)
            {
                EndRun();
            }

            if(CompletionPercentage()<100)
            {
                timeToOneHundred += Time.deltaTime;
            }

            if (Input.GetKeyDown(KeyCode.Minus))
            {
                EndRun();
            }
           if(Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                Time.timeScale++;
            }
           if(Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                if (Time.timeScale > 1)
                    Time.timeScale--;
            }
            //if (Input.GetKeyDown(KeyCode.Equals))
            //{
            //    AchievementNotif(QuestLog[Random.Range(0, QuestLog.Count - 1)]);
            //}

            //A Quiet Race
            
            audSettings.GetFloat("Volume", out vol);
            //print(vol);
            if (vol<=-50)
            {
                quietTimer += Time.deltaTime;
                if(!quiet.completeThisRun && quietTimer>=60)
                {
                    AchievementNotif(quiet);
                }
            }

            if (Input.GetButtonDown("Pause"))
            {
                SwapPauseScreen();
            }

            //print(FormatTime(timeLeft));
            timeLeftT.text = FormatTime(timeLeft);
        }
        if (gameState == 0)
        {
            if (cinematicCR != null && Input.GetButtonDown("Cancel"))
            {
                StopCoroutine(cinematicCR);
                SetGameState(1);
            }
        }
    }

    public void AchievementNotif(Quest q)
    {
        //A delay system if the player is achieving multiple quests at once.
        //or if the player is getting an achievement before the UI element has left the screen.
        if (achNotifCR != null)
        {
            if (!achOnDeck.Contains(q)) //you can't get the same achievement twice, so this should be fine.
            {
                achOnDeck.Add(q);

                if (q.currentProgress >= q.maxToCollect)
                {

                    //pity party checks:
                    noAchievement = false;
                    if (!q.completeOverall) //if it's not complete already, that means it's new
                    {
                        noNEWAchievement = false;
                    }

                    //mark it as complete so we can show it at the end of the run:
                    q.completeThisRun = true;
                    q.completeOverall = true; //do i need to store this in PP for subsequent plays??
                }
            }
            return;
            //StopCoroutine(achNotifCR);
        }

        //achNameT.text = q.questName;
        achScriptInGame.achievementName.text = q.questName;
        //if it's a collectible quest, also show progress:
        if(q.maxToCollect>0)
        {
            //achProgressT.text = q.currentProgress + "/" + q.maxToCollect;
            achScriptInGame.currentCounter = q.currentProgress;
            achScriptInGame.totalCounter = q.maxToCollect;
            achScriptInGame.UpdateCounter();
            achScriptInGame.counter.enabled = true;

            //show it if you've completed it in prior runs?
            if(q.completeOverall)
            {
                achScriptInGame.description.text = q.questDescription;
                achScriptInGame.description.enabled = true;
            }
            else
            {
                achScriptInGame.description.enabled = false;
            }
            if (achNotifCR != null)
            {
                StopCoroutine(achNotifCR);
            }
            achNotifCR = StartCoroutine(AchievementCounter(GetClipTime(achNotif.GetComponent<Animator>(), "Achievement Counter Update")));
            //PlayClip(questCounter); //moving this to the animation itself and AchievementScript.cs
        }
        else
        {
            achScriptInGame.counter.enabled = false;
        }

        //this should probably even work for quests that aren't collectible
        //(0 = 0, so pop the quest complete)
        if (q.currentProgress>=q.maxToCollect)
        {
            //turn it off first in case you're gettinga  bunch in a row:
            if (achNotif.activeSelf)
            {
                achNotif.SetActive(false);
            }
            //achProgressT.text = q.questDescription; //if you've completed it, show the description of what it is?
            achScriptInGame.description.text = q.questDescription;
            achScriptInGame.description.enabled = true;

            //pity party checks:
            noAchievement = false;
            if(!q.completeOverall) //if it's not complete already, that means it's new
            {
                noNEWAchievement = false;
            }

            //mark it as complete so we can show it at the end of the run:
            q.completeThisRun = true;
            //print("Marking this run " + q.questName);
            q.completeOverall = true; //do i need to store this in PP for subsequent plays??

            
            achNotifCR = StartCoroutine(AchievementDelay(GetClipTime(achNotif.GetComponent<Animator>(), "Achievement Earn")));
            //PlayClip(questComplete);
        }
        
    }

    public IEnumerator AchievementDelay(float d)
    {
        print("achievement earned: " + achScriptInGame.achievementName.text);
        //just turning on and off right now.  could add Animator parameters instead if we want.
        achNotif.SetActive(true);
        achScriptInGame.GetComponent<Animator>().SetTrigger("Earn");
        FindObjectOfType<WackyCarBehaviour>().cheevosThisRun++;
        yield return new WaitForSeconds(d);
        //achNotif.SetActive(false);
        achScriptInGame.GetComponent<Animator>().ResetTrigger("Earn");
        //if there's an achievement waiting, go back and fire it:
        achNotifCR = null;
        if (achOnDeck.Count > 0)
        {
            AchievementNotif(achOnDeck[0]);
            achOnDeck.RemoveAt(0);
            
        }
    }

    public IEnumerator AchievementCounter(float d)
    {
        //just turning on and off right now.  could add Animator parameters instead if we want.
        achNotif.SetActive(true);
        achScriptInGame.GetComponent<Animator>().SetTrigger("Add Counter");
        yield return new WaitForSeconds(d);
        //achNotif.SetActive(false);
        achScriptInGame.GetComponent<Animator>().ResetTrigger("Add Counter");
        achNotifCR = null;

        if (achOnDeck.Count > 0)
        {
            achNotifCR = null;
            AchievementNotif(achOnDeck[0]);
            achOnDeck.RemoveAt(0);

        }

    }

    public IEnumerator StartTimedQuest(QuestObject q, float start, float end)
    {
        yield return new WaitForSeconds(start);
        //turn it on
        q.gameObject.SetActive(true); //presumably there's a boxcollider/trigger attached to this GO that allows it to be interacted with
        if(q.GetComponent<AudioSource>()!=null)
        {
            q.GetComponent<AudioSource>().Play();
        }
        print("Turning on " + q.questLine.questName);
        yield return new WaitForSeconds(end);
        if(q!=null && q.gameObject!=null)
        {
            q.gameObject.SetActive(false);
            print("Turning off " + q.questLine.questName);
        }


    }
    /// <summary>
    /// send in the EXACT questname (not the name in assets folder)
    /// </summary>
    /// <param name="nameOQuest"></param>
    /// <returns></returns>
    public Quest QuestSearch(string nameOQuest)
    {
        Quest q = null;
        foreach(Quest qu in QuestLog)
        {
            if(qu.questName==nameOQuest)
            {
                q= qu; //AMAZING 10/10
            }
        }
        return q;
    }

    public void CompleteCarQuest(Quest qu)
    {
        if (qu.sfxToPlay != null)
        {
            PlayClip(qu.sfxToPlay);
            //AudioSource.PlayClipAtPoint(questLine.sfxToPlay, Camera.main.transform.position);

        }
        if (!qu.completeThisRun)
        {
            //show a snazzy visual notification:
            AchievementNotif(qu);

            //play a quest complete sfx?


        }
    }
    public void CompleteCarQuest(Quest qu, GameObject getDestroyed)
    {
        if (qu.sfxToPlay != null)
        {
            PlayClip(qu.sfxToPlay);
            //AudioSource.PlayClipAtPoint(questLine.sfxToPlay, Camera.main.transform.position);

        }
        if (!qu.completeThisRun)
        {
            //show a snazzy visual notification:
            AchievementNotif(qu);
           
            //play a quest complete sfx?


        }
        if (qu.destroyObject)
        {
            Destroy(getDestroyed);
        }
    }
    public void ResetQuests()
    {
        foreach (Quest q in QuestLog)
        {
            q.completeOverall = false;
        }
        ClearChecklist(checklistParentTitle);
        PopulateChecklist(checklistParentTitle.transform);
        timeToOneHundred = 0;
    }
    public string FormatTime(float timeToFormat) //doing this enough that i should make it a function.  call it to format a time.
    {
        int fminutes = Mathf.FloorToInt(timeToFormat / 60f);
        int fseconds = Mathf.FloorToInt(timeToFormat - fminutes * 60);
        string formattedTime = string.Format("{0:00}:{1:00}", fminutes, fseconds);

        return formattedTime;
    }

    public float GetClipTime(Animator anim, string aName)
    {
        float time = 0;
        foreach (AnimationClip c in anim.runtimeAnimatorController.animationClips)
        {
            if (c.name == aName)
            {
                time = c.length;
            }
        }
        return time;
    }

    public void SetGameState(int w)
    {
        gameState = w;
        aud.clip = bgms[w];
        aud.Play();
        switch(w)
        {
            case 1:
                playerCar.controllable = true;
                cinemaCam.SetActive(false);
                cinematicPanel.SetActive(false);
                timeLeftT.transform.parent.gameObject.SetActive(true);
                break;

                //pause:
            case 3:
               
                break;
        }
    }
    public void SwapPauseScreen()
    {
        if (gameState == 1)
        {


            if (pauseMenu.activeSelf)
            {
                //should probably do an exit anim instead here:
                pauseMenu.SetActive(false);
                Time.timeScale = 1;

            }
            else
            {
                //time not quite stopped:
                Time.timeScale = 0.1f;
                pauseMenu.SetActive(true);
                LoadRunAchievementList(chkListParentInGame,inGamePanel, inRunSB);
                pauseScreenRestartButt.Select();
            }
        }
    }
    public void PlayImpact()
    {
        AudioSource a = Camera.main.gameObject.AddComponent<AudioSource>();
        a.clip = impactSFX;
        a.outputAudioMixerGroup = audMGroup;
        a.Play();
        Destroy(a, a.clip.length);

    }
    public void PlayClip(AudioClip c)
    {
        //this is adding an audiosource to the main camera instead of
        //instantiating in world space because the main camera
        //is moving, because it's a car.
        AudioSource a = Camera.main.gameObject.AddComponent<AudioSource>();
        a.clip = c;
        a.outputAudioMixerGroup = audMGroup;
        a.Play();
        Destroy(a, a.clip.length);
        //AudioSource.PlayClipAtPoint(c, Camera.main.transform.position);
    }
    public void PlayClip(AudioClip c, float vol)
    {

        AudioSource a = Camera.main.gameObject.AddComponent<AudioSource>();
        a.clip = c;
        a.volume = vol;
        a.outputAudioMixerGroup = audMGroup;
        a.Play();
        Destroy(a, a.clip.length);
        //AudioSource.PlayClipAtPoint(c, Camera.main.transform.position);
    }

    public void StartGame()
    {
        //turn on cinemachine camera (and off)
        cinematicCR = StartCoroutine(CinemaCamOnAndOff(cineCamDelay));

        //start timer
        
        //check for timed quests and turn turn them on on delay:
        foreach (QuestObject qo in TimedQuests)
        {
            if (qo.questLine.isTimeDependent)
            {
                StartCoroutine(StartTimedQuest(qo, qo.questLine.timedStart, qo.questLine.timedLength));
                print(qo.questLine.questName + " will turn on in " + qo.questLine.timedStart + "s");
            }
        }

    }
    public IEnumerator CinemaCamOnAndOff(float delay)
    {
        float uiAnim = GetClipTime(cinematicPanel.GetComponent<Animator>(), "cinematicHereWeGo");
        cinemaCam.SetActive(true);
        mSpawner.MeteorsSpawn(); //spawn meteors if you're starting the run.
        playerCar.controllable = false;
        cinematicPanel.SetActive(true);
        
        yield return new WaitForSeconds(delay-uiAnim);
        //cinematicPanel.SetActive(true);
        cinematicPanel.GetComponent<Animator>().SetTrigger("HereWeGo");
        yield return new WaitForSeconds(uiAnim); //after the ui is done, get in the game zone (GZ);
        cinemaCam.SetActive(false);
        cinematicPanel.SetActive(false);
        SetGameState(1);

    }

    public void EndRun()
    {
        //print("ending run");
        //!!This is where we endgame
        car.GetComponent<AudioSource>().Stop();
        
        //print(CompletionPercentage() + " vs " + (((float)(QuestLog.Count - 1) / (float)QuestLog.Count) * 100));
        if(NumCompleted() == QuestLog.Count-1)
        {
            AchievementNotif(QuestSearch("Overachiever"));
        }

        //the pity parties might fire at the same time.
        //might finally need to employ a delay on achievement notificationnns... sigh.
        if(noAchievement)
        {

            AchievementNotif(QuestSearch("Pity Party II"));
        }

        if(noNEWAchievement)
        {
            AchievementNotif(QuestSearch("Pity Party"));
        }

        timeLeft = 0;
        string pLQ = string.Empty;
        if(CompletionPercentage()==100)
        {
            pLQ = "“Yet he who reigns within himself, and rules \nPassions, desires, and fears, is more a king.”\n-John Milton, Paradise Regained";
        }
        else
        {
            pLQ = paradiseLostQuotes[Random.Range(0, paradiseLostQuotes.Length)] + "\n" + "-John Milton, Paradise Lost";
        }
        PlayerPrefs.SetFloat("timeToOneHundred", timeToOneHundred);
        pLostText.text = pLQ;

        //added a slider to show completion so taking it out of this text element:
        endGameCompletionTxt.text = /*"Complete: " + CompletionPercentage().ToString("F0") + "%" + "\n */"Achievements This Run: " + NumCompletedThisRun() + "\nTime to 100%: " + FormatTime(timeToOneHundred); ;
        endGamePanel.SetActive(true);
        endGameRestartButt.Select();

        //hacky turning off then on the achievement slider:
        GameObject egSlider = endGamePanel.GetComponentInChildren<SliderSetTextToPercent>().gameObject;
        egSlider.SetActive(false);
        egSlider.SetActive(true);

        //load checklist with achievements earned this run:
        LoadRunAchievementList(checkListParentEndRun, endGamePanel, endGameSB);
        //FindObjectOfType<ChecklistScript>().GetComponent<Animator>().SetBool("On", true);
        //!!probably don't just stop time:
        Time.timeScale = 0;
        gameState = 2;
        //reload the game after a minute of being on the end screen.
        StartCoroutine(LoadOnDelay(60f, 0));
        
    }
    public void PopulateChecklist(Transform t)
    {
        foreach (Quest q in QuestLog)
        {
            q.completeThisRun = false; //turn them all off (collectibles are doing it in QuestObject.cs) //i think this is false EG 4/18/22
            if (q.carQuest)
            {
                CarQuests.Add(q);
            }
            //also populate the checklist:

            AchievementScript ass = Instantiate(achChecklistPrefab, t.transform).GetComponent<AchievementScript>();
            ass.achievementName.text = q.questName;
            if (q.completeOverall)
            {
                ass.description.text = q.questDescription;
                ass.checkmark.enabled = true;
            }
            else
            {
                ass.description.text = "???";
            }
            ass.counter.gameObject.SetActive(false);
            //turn off all the GOs, which will be turned on by the SwapAnim function in ChecklistScript.cs
            //ass.gameObject.SetActive(false)
        }
    }
    public void LoadRunAchievementList(GameObject clParent, GameObject panel, Scrollbar sb)
    {
        ClearChecklist(clParent);
        foreach (Quest q in QuestLog)
        {
            if (q.completeThisRun)
            {
                AchievementScript ass = Instantiate(achChecklistPrefab, clParent.transform).GetComponent<AchievementScript>();
                ass.achievementName.text = q.questName;
                ass.description.text = q.questDescription;
                ass.checkmark.enabled = true;
                if (!q.isOneTime && q.currentProgress > 0)
                {
                    ass.currentCounter = q.currentProgress;
                    ass.totalCounter = q.maxToCollect;
                    ass.UpdateCounter();
                }
                else
                {
                    ass.counter.gameObject.SetActive(false);
                }
            }



            StartCoroutine(ScrollBarSet(sb));
            //get the checklist anim and turn it on at end of run:
            panel.GetComponentInChildren<ChecklistScript>().GetComponent<Animator>().SetBool("On", true);
        }
    }
    public int NumCompletedThisRun()
    {
        int nctr = 0;
        foreach (Quest q in QuestLog)
        {
            if (q.completeThisRun)
            {
                nctr++;
            }
        }
        return nctr;
    }
    public float NumCompleted()
    {
        float numCompleted = 0;
        foreach (Quest q in QuestLog)
        {
            if (q.completeOverall)
            {
                numCompleted++;
            }
        }
        return numCompleted;
    }

    public float CompletionPercentage()
    {
        
        float total = QuestLog.Count;
        //print("Complete = " + numCompleted + "// Total = " + QuestLog.Count);
        //print((float)(numCompleted / total) * 100);

        return ((NumCompleted() / total)*100);
    }
    public IEnumerator LoadOnDelay(float del, int s)
    {
        yield return new WaitForSecondsRealtime(del);
        SceneManager.LoadScene(s);
    }

    public void LoadScene(int s)
    {
        SceneManager.LoadScene(0);
    }

    public IEnumerator RespawnNearby(float del, Transform pos)
    {
        //print("turning off. closest = " + pos);
        car.SetActive(false);
        yield return new WaitForSeconds(del);
        //print("turning on. at " + pos);

        car.transform.position = pos.position;
        car.transform.rotation = pos.rotation;
        car.SetActive(true);

    }

    public void GetRespawnLocs()
    {
        foreach(GameObject go in GameObject.FindGameObjectsWithTag("RespawnPoint"))
        {
            respawnLocations.Add(go.transform);
        }
    }

    public void SetMasterVolume(Slider s)
    {
        audSettings.SetFloat("Volume", s.value);
        //update other slider:
        foreach (Slider sl in volumeSliders)
        {
            if(sl!=s)
            {
                sl.SetValueWithoutNotify(s.value);
            }
        }
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.SetFloat("timeToOneHundred", timeToOneHundred);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
