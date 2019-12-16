using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;

public class BlinkstopScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMSelectable[] buttons;
    //public KMColorblindMode Colorblind;
    //private bool colorblindActive = false;
    //public GameObject cblindtext;

    public GameObject statuslightoff;
    public GameObject statuslighton;
    public GameObject statuslightstrike;
    public GameObject statuslightp;
    public GameObject statuslightc;
    public GameObject statuslighty;
    public GameObject statuslightm;
    public GameObject statuslightmlight1;
    public GameObject statuslightmlight2;

    private Coroutine ledcycling;
    private Coroutine rainbowcycling;

    private string numSequence = "";
    private string correctNumSequence;

    private char[] prevledcols;
    private char[] ledcols;
    private int prevpurplect;
    private int prevcyanct;
    private int prevyellowct;
    private int prevmultict;
    private int purplect;
    private int cyanct;
    private int yellowct;
    private int multict;

    private bool pausephase;
    private bool startingphase = true;
    private bool struck = false;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        //colorblindActive = Colorblind.ColorblindModeActive;
        foreach (KMSelectable obj in buttons){
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        //cblindtext.GetComponent<TextMesh>().text = "";
        GetComponent<KMBombModule>().OnActivate = OnActivate;
        Debug.LogFormat("[Blinkstop #{0}] Due to the constantly changing sequences, this module will not log any information about solutions until a submission is made", moduleId);
    }

    void Start () {
        setLEDCols();
        calcAnswer();
    }

    void OnActivate()
    {
        ledcycling = StartCoroutine(cycleLEDColors());
        rainbowcycling = StartCoroutine(cycleRainbowLED());
        //Debug.LogFormat("[Codenames #{0}] Colorblind mode: {1}", moduleId, colorblindActive);
    }

    void PressButton(KMSelectable pressed)
    {
        if(startingphase == true)
        {
            Debug.LogFormat("[Blinkstop #{0}] The module cannot be interacted with while the first sequence is flashing (to avoid bugs)!", moduleId);
        }
        else if (moduleSolved != true)
        {
            pressed.AddInteractionPunch(0.25f);
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            if(buttons[0] == pressed)
            {
                numSequence += "3";
            }
            else if (buttons[1] == pressed)
            {
                numSequence += "2";
            }
            else if (buttons[2] == pressed)
            {
                numSequence += "1";
            }
            else if (buttons[3] == pressed)
            {
                if (pausephase)
                {
                    Debug.LogFormat("[Blinkstop #{0}] Blank button has been pressed while a long pause is happening, resetting inputs!", moduleId);
                    numSequence = "";
                }
                else
                {
                    string flashes = getPrevLEDCols();
                    Debug.LogFormat("[Blinkstop #{0}] The previous sequence of flashes at the time of submission was: {1} (totalling {2} flashes), where P=Purple, C=Cyan, Y=Yellow, and M=Mulicolored", moduleId, flashes, prevledcols.Length);
                    Debug.LogFormat("[Blinkstop #{0}] The number of purple flashes in the sequence at the time of submission was: {1}", moduleId, prevpurplect);
                    Debug.LogFormat("[Blinkstop #{0}] The number of cyan flashes in the sequence at the time of submission was: {1}", moduleId, prevcyanct);
                    Debug.LogFormat("[Blinkstop #{0}] The number of yellow flashes in the sequence at the time of submission was: {1}", moduleId, prevyellowct);
                    Debug.LogFormat("[Blinkstop #{0}] The number of multicolored flashes in the sequence at the time of submission was: {1}", moduleId, prevmultict);
                    Debug.LogFormat("[Blinkstop #{0}] This makes the correct input sequence for the previous sequence of flashes: {1}", moduleId, correctNumSequence);
                    if (correctNumSequence.Equals(numSequence))
                    {
                        Debug.LogFormat("[Blinkstop #{0}] You entered an input sequence for the previous sequence of flashes of: {1}, which is correct. Module Disarmed.", moduleId, numSequence);
                        moduleSolved = true;
                        StopCoroutine(ledcycling);
                        StopCoroutine(rainbowcycling);
                        statuslightp.SetActive(false);
                        statuslightc.SetActive(false);
                        statuslighty.SetActive(false);
                        statuslightm.SetActive(false);
                        statuslightoff.SetActive(false);
                        statuslighton.SetActive(true);
                        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                        GetComponent<KMBombModule>().HandlePass();
                    }
                    else
                    {
                        if (numSequence.Equals(""))
                        {
                            Debug.LogFormat("[Blinkstop #{0}] You entered an input sequence for the previous sequence of flashes of: nothing, which is incorrect. Strike!", moduleId);
                        }
                        else
                        {
                            Debug.LogFormat("[Blinkstop #{0}] You entered an input sequence for the previous sequence of flashes of: {1}, which is incorrect. Strike!", moduleId, numSequence);
                        }
                        statuslightp.SetActive(false);
                        statuslightc.SetActive(false);
                        statuslighty.SetActive(false);
                        statuslightm.SetActive(false);
                        statuslightoff.SetActive(false);
                        StartCoroutine(striker());
                        GetComponent<KMBombModule>().HandleStrike();
                    }
                }
            }
        }
    }

    private void setLEDCols()
    {
        prevledcols = ledcols;
        prevpurplect = purplect;
        prevcyanct = cyanct;
        prevyellowct = yellowct;
        prevmultict = multict;
        purplect = 0;
        cyanct = 0;
        yellowct = 0;
        multict = 0;
        int[] possibles = { 30,33,37,39,42,44,47,51,55,59 };
        int rando = UnityEngine.Random.Range(0, possibles.Length);
        ledcols = new char[possibles[rando]];
        while (multipleSmalls(new int[] { purplect, cyanct, yellowct, multict }))
        {
            purplect = 0;
            cyanct = 0;
            yellowct = 0;
            multict = 0;
            for (int i = 0; i < ledcols.Length; i++)
            {
                int rando2 = UnityEngine.Random.Range(0, 4);
                if (rando2 == 0)
                {
                    ledcols[i] = 'P';
                    purplect++;
                }
                else if (rando2 == 1)
                {
                    ledcols[i] = 'C';
                    cyanct++;
                }
                else if (rando2 == 2)
                {
                    ledcols[i] = 'Y';
                    yellowct++;
                }
                else
                {
                    ledcols[i] = 'M';
                    multict++;
                }
            }
        }
    }

    private void calcAnswer()
    {
        correctNumSequence = "";
        if(prevcyanct != 0 && prevpurplect != 0 && prevyellowct != 0 && prevmultict != 0)
        {
            if(prevledcols.Length == 30)
            {
                if(isNumSmallest(prevpurplect, prevcyanct, prevyellowct, prevmultict))
                {
                    correctNumSequence = "1321231221";
                }
                else if (isNumSmallest(prevcyanct, prevpurplect, prevyellowct, prevmultict))
                {
                    correctNumSequence = "3231223221";
                }
                else if (isNumSmallest(prevyellowct, prevpurplect, prevcyanct, prevmultict))
                {
                    correctNumSequence = "3132132232";
                }
                else if (isNumSmallest(prevmultict, prevpurplect, prevcyanct, prevyellowct))
                {
                    correctNumSequence = "1213132111";
                }
            }
            else if (prevledcols.Length == 33)
            {
                if (isNumSmallest(prevpurplect, prevcyanct, prevyellowct, prevmultict))
                {
                    correctNumSequence = "3212123121";
                }
                else if (isNumSmallest(prevcyanct, prevpurplect, prevyellowct, prevmultict))
                {
                    correctNumSequence = "3212312323";
                }
                else if (isNumSmallest(prevyellowct, prevpurplect, prevcyanct, prevmultict))
                {
                    correctNumSequence = "1213111132";
                }
                else if (isNumSmallest(prevmultict, prevpurplect, prevcyanct, prevyellowct))
                {
                    correctNumSequence = "2233112332";
                }
            }
            else if (prevledcols.Length == 37)
            {
                if (isNumSmallest(prevpurplect, prevcyanct, prevyellowct, prevmultict))
                {
                    correctNumSequence = "1232321213";
                }
                else if (isNumSmallest(prevcyanct, prevpurplect, prevyellowct, prevmultict))
                {
                    correctNumSequence = "1212221223";
                }
                else if (isNumSmallest(prevyellowct, prevpurplect, prevcyanct, prevmultict))
                {
                    correctNumSequence = "2332332312";
                }
                else if (isNumSmallest(prevmultict, prevpurplect, prevcyanct, prevyellowct))
                {
                    correctNumSequence = "2321313122";
                }
            }
            else if (prevledcols.Length == 39)
            {
                if (isNumSmallest(prevpurplect, prevcyanct, prevyellowct, prevmultict))
                {
                    correctNumSequence = "3231212322";
                }
                else if (isNumSmallest(prevcyanct, prevpurplect, prevyellowct, prevmultict))
                {
                    correctNumSequence = "3213231223";
                }
                else if (isNumSmallest(prevyellowct, prevpurplect, prevcyanct, prevmultict))
                {
                    correctNumSequence = "2321123213";
                }
                else if (isNumSmallest(prevmultict, prevpurplect, prevcyanct, prevyellowct))
                {
                    correctNumSequence = "2132233231";
                }
            }
            else if (prevledcols.Length == 42)
            {
                if (isNumSmallest(prevpurplect, prevcyanct, prevyellowct, prevmultict))
                {
                    correctNumSequence = "3221212121";
                }
                else if (isNumSmallest(prevcyanct, prevpurplect, prevyellowct, prevmultict))
                {
                    correctNumSequence = "2221212112";
                }
                else if (isNumSmallest(prevyellowct, prevpurplect, prevcyanct, prevmultict))
                {
                    correctNumSequence = "1232321121";
                }
                else if (isNumSmallest(prevmultict, prevpurplect, prevcyanct, prevyellowct))
                {
                    correctNumSequence = "3231231221";
                }
            }
            else if (prevledcols.Length == 44)
            {
                if (isNumSmallest(prevpurplect, prevcyanct, prevyellowct, prevmultict))
                {
                    correctNumSequence = "2323131231";
                }
                else if (isNumSmallest(prevcyanct, prevpurplect, prevyellowct, prevmultict))
                {
                    correctNumSequence = "1211212332";
                }
                else if (isNumSmallest(prevyellowct, prevpurplect, prevcyanct, prevmultict))
                {
                    correctNumSequence = "1322121211";
                }
                else if (isNumSmallest(prevmultict, prevpurplect, prevcyanct, prevyellowct))
                {
                    correctNumSequence = "3212312122";
                }
            }
            else if (prevledcols.Length == 47)
            {
                if (isNumSmallest(prevpurplect, prevcyanct, prevyellowct, prevmultict))
                {
                    correctNumSequence = "1212132232";
                }
                else if (isNumSmallest(prevcyanct, prevpurplect, prevyellowct, prevmultict))
                {
                    correctNumSequence = "1122321123";
                }
                else if (isNumSmallest(prevyellowct, prevpurplect, prevcyanct, prevmultict))
                {
                    correctNumSequence = "3212123232";
                }
                else if (isNumSmallest(prevmultict, prevpurplect, prevcyanct, prevyellowct))
                {
                    correctNumSequence = "3222122321";
                }
            }
            else if (prevledcols.Length == 51)
            {
                if (isNumSmallest(prevpurplect, prevcyanct, prevyellowct, prevmultict))
                {
                    correctNumSequence = "2123232231";
                }
                else if (isNumSmallest(prevcyanct, prevpurplect, prevyellowct, prevmultict))
                {
                    correctNumSequence = "3232313312";
                }
                else if (isNumSmallest(prevyellowct, prevpurplect, prevcyanct, prevmultict))
                {
                    correctNumSequence = "1213222113";
                }
                else if (isNumSmallest(prevmultict, prevpurplect, prevcyanct, prevyellowct))
                {
                    correctNumSequence = "3212112322";
                }
            }
            else if (prevledcols.Length == 55)
            {
                if (isNumSmallest(prevpurplect, prevcyanct, prevyellowct, prevmultict))
                {
                    correctNumSequence = "2323122223";
                }
                else if (isNumSmallest(prevcyanct, prevpurplect, prevyellowct, prevmultict))
                {
                    correctNumSequence = "1313313122";
                }
                else if (isNumSmallest(prevyellowct, prevpurplect, prevcyanct, prevmultict))
                {
                    correctNumSequence = "3231213231";
                }
                else if (isNumSmallest(prevmultict, prevpurplect, prevcyanct, prevyellowct))
                {
                    correctNumSequence = "2312312312";
                }
            }
            else if (prevledcols.Length == 59)
            {
                if (isNumSmallest(prevpurplect, prevcyanct, prevyellowct, prevmultict))
                {
                    correctNumSequence = "1232213223";
                }
                else if (isNumSmallest(prevcyanct, prevpurplect, prevyellowct, prevmultict))
                {
                    correctNumSequence = "3232123211";
                }
                else if (isNumSmallest(prevyellowct, prevpurplect, prevcyanct, prevmultict))
                {
                    correctNumSequence = "2231232333";
                }
                else if (isNumSmallest(prevmultict, prevpurplect, prevcyanct, prevyellowct))
                {
                    correctNumSequence = "3212333123";
                }
            }
        }
    }

    private bool isNumSmallest(int small, int other1, int other2, int other3)
    {
        if(small < other1 && small < other2 && small < other3)
        {
            return true;
        }
        return false;
    }

    private bool multipleSmalls(int[] nums)
    {
        int small = nums[0];
        for(int i = 1; i < 4; i++)
        {
            if(nums[i] < small)
            {
                small = nums[i];
            }
        }
        int counter = 0;
        for(int i = 0; i < 4; i++)
        {
            if (nums[i] == small)
            {
                counter++;
            }
        }
        if(counter > 1)
        {
            return true;
        }
        return false;
    }

    private string getPrevLEDCols()
    {
        string temp = "";
        for(int i = 0; i < prevledcols.Length; i++)
        {
            if(i == 0)
            {
                temp += ""+prevledcols[i];
            }
            else
            {
                temp += ", " + prevledcols[i];
            }
        }
        return temp;
    }

    private IEnumerator cycleLEDColors()
    {
        pausephase = true;
        yield return new WaitForSeconds(5.0f);
        pausephase = false;
        for(int i = 0; i < ledcols.Length; i++)
        {
            if(struck != true)
            {
                statuslightoff.SetActive(false);
                if (ledcols[i].Equals('P'))
                {
                    statuslightp.SetActive(true);
                    /**if (colorblindActive)
                    {
                        cblindtext.GetComponent<TextMesh>().text = "P";
                    }*/
                }
                else if (ledcols[i].Equals('C'))
                {
                    statuslightc.SetActive(true);
                    /**if (colorblindActive)
                    {
                        cblindtext.GetComponent<TextMesh>().text = "C";
                    }*/
                }
                else if (ledcols[i].Equals('Y'))
                {
                    statuslighty.SetActive(true);
                    /**if (colorblindActive)
                    {
                        cblindtext.GetComponent<TextMesh>().text = "Y";
                    }*/
                }
                else if (ledcols[i].Equals('M'))
                {
                    statuslightm.SetActive(true);
                    /**if (colorblindActive)
                    {
                        cblindtext.GetComponent<TextMesh>().text = "M";
                    }*/
                }
            }
            yield return new WaitForSeconds(0.6f);
            if(struck != true)
            {
                if (ledcols[i].Equals('P'))
                {
                    statuslightp.SetActive(false);
                }
                else if (ledcols[i].Equals('C'))
                {
                    statuslightc.SetActive(false);
                }
                else if (ledcols[i].Equals('Y'))
                {
                    statuslighty.SetActive(false);
                }
                else if (ledcols[i].Equals('M'))
                {
                    statuslightm.SetActive(false);
                }
                /**if (colorblindActive)
                {
                    cblindtext.GetComponent<TextMesh>().text = "";
                }*/
                statuslightoff.SetActive(true);
            }
            yield return new WaitForSeconds(0.25f);
        }
        Start();
        startingphase = false;
        ledcycling = StartCoroutine(cycleLEDColors());
    }

    private IEnumerator cycleRainbowLED()
    {
        float fadeOutTime = 0.25f;
        Color originalColor = statuslightm.GetComponent<Renderer>().material.color;
        for (float t = 0.01f; t < fadeOutTime; t += Time.deltaTime)
        {
            statuslightm.GetComponent<Renderer>().material.color = Color.Lerp(originalColor, Color.red, Mathf.Min(1, t / fadeOutTime));
            statuslightmlight1.GetComponent<Light>().color = Color.Lerp(originalColor, Color.red, Mathf.Min(1, t / fadeOutTime));
            statuslightmlight2.GetComponent<Light>().color = Color.Lerp(originalColor, Color.red, Mathf.Min(1, t / fadeOutTime));
            yield return null;
        }
        originalColor = statuslightm.GetComponent<Renderer>().material.color;
        for (float t = 0.01f; t < fadeOutTime; t += Time.deltaTime)
        {
            statuslightm.GetComponent<Renderer>().material.color = Color.Lerp(originalColor, Color.yellow, Mathf.Min(1, t / fadeOutTime));
            statuslightmlight1.GetComponent<Light>().color = Color.Lerp(originalColor, Color.yellow, Mathf.Min(1, t / fadeOutTime));
            statuslightmlight2.GetComponent<Light>().color = Color.Lerp(originalColor, Color.yellow, Mathf.Min(1, t / fadeOutTime));
            yield return null;
        }
        originalColor = statuslightm.GetComponent<Renderer>().material.color;
        for (float t = 0.01f; t < fadeOutTime; t += Time.deltaTime)
        {
            statuslightm.GetComponent<Renderer>().material.color = Color.Lerp(originalColor, Color.green, Mathf.Min(1, t / fadeOutTime));
            statuslightmlight1.GetComponent<Light>().color = Color.Lerp(originalColor, Color.green, Mathf.Min(1, t / fadeOutTime));
            statuslightmlight2.GetComponent<Light>().color = Color.Lerp(originalColor, Color.green, Mathf.Min(1, t / fadeOutTime));
            yield return null;
        }
        originalColor = statuslightm.GetComponent<Renderer>().material.color;
        for (float t = 0.01f; t < fadeOutTime; t += Time.deltaTime)
        {
            statuslightm.GetComponent<Renderer>().material.color = Color.Lerp(originalColor, Color.blue, Mathf.Min(1, t / fadeOutTime));
            statuslightmlight1.GetComponent<Light>().color = Color.Lerp(originalColor, Color.blue, Mathf.Min(1, t / fadeOutTime));
            statuslightmlight2.GetComponent<Light>().color = Color.Lerp(originalColor, Color.blue, Mathf.Min(1, t / fadeOutTime));
            yield return null;
        }
        originalColor = statuslightm.GetComponent<Renderer>().material.color;
        for (float t = 0.01f; t < fadeOutTime; t += Time.deltaTime)
        {
            statuslightm.GetComponent<Renderer>().material.color = Color.Lerp(originalColor, Color.magenta, Mathf.Min(1, t / fadeOutTime));
            statuslightmlight1.GetComponent<Light>().color = Color.Lerp(originalColor, Color.magenta, Mathf.Min(1, t / fadeOutTime));
            statuslightmlight2.GetComponent<Light>().color = Color.Lerp(originalColor, Color.magenta, Mathf.Min(1, t / fadeOutTime));
            yield return null;
        }
        rainbowcycling = StartCoroutine(cycleRainbowLED());
    }

    private IEnumerator striker()
    {
        struck = true;
        statuslightstrike.SetActive(true);
        yield return new WaitForSeconds(1.0f);
        statuslightstrike.SetActive(false);
        statuslightoff.SetActive(true);
        struck = false;
    }

    //twitch plays
    private bool paramsValid(string prms)
    {
        char[] valids = { '1', '2', '3' };
        for(int i = 0; i < prms.Length; i++)
        {
            if (!valids.Contains(prms.ElementAt(i)))
            {
                return false;
            }
        }
        return true;
    }

#pragma warning disable 414
    //private readonly string TwitchHelpMessage = @"!{0} submit <nums> [Submits the specified number sequence] | !{0} reset [Waits for a long pause and then presses the blank button] | !{0} colorblind [Toggles colorblind mode] | Valid numbers are 1-3";
    private readonly string TwitchHelpMessage = @"!{0} submit <nums> [Submits the specified number sequence] | !{0} reset [Waits for a long pause and then presses the blank button] | Valid numbers are 1-3";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        /**
        if (Regex.IsMatch(command, @"^\s*colorblind\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            Debug.LogFormat("[Blinkstop #{0}] Colorblind mode toggled! (TP)", moduleId);
            if (colorblindActive)
            {
                colorblindActive = false;
                cblindtext.GetComponent<TextMesh>().text = "";
            }
            else
            {
                colorblindActive = true;
            }
            yield break;
        }*/
        if (startingphase)
        {
            yield return "sendtochaterror You may not press any buttons while the first sequence is flashing (to avoid bugs)!";
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            while(pausephase != true)
            {
                yield return "trycancel Waiting for the long pause to reset has been halted by a request to cancel.";
                yield return new WaitForSeconds(0.1f);
            }
            buttons[3].OnInteract();
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify which numbered buttons to press!";
            }
            else
            {
                string seq = "";
                for(int i = 1; i < parameters.Length; i++)
                {
                    seq += parameters[i];
                }
                if (paramsValid(seq))
                {
                    yield return null;
                    if (!seq.Equals(correctNumSequence))
                    {
                        yield return "strike";
                    }
                    else if (seq.Equals(correctNumSequence))
                    {
                        yield return "solve";
                    }
                    for(int i = 0; i < seq.Length; i++)
                    {
                        if (seq.ElementAt(i).Equals('3'))
                        {
                            buttons[0].OnInteract();
                        }
                        else if (seq.ElementAt(i).Equals('2'))
                        {
                            buttons[1].OnInteract();
                        }
                        else if (seq.ElementAt(i).Equals('1'))
                        {
                            buttons[2].OnInteract();
                        }
                        yield return new WaitForSeconds(0.1f);
                    }
                    buttons[3].OnInteract();
                }
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (pausephase || startingphase)
        {
            yield return new WaitForSeconds(0.1f);
        }
        yield return ProcessTwitchCommand("submit " + correctNumSequence);
    }
}
