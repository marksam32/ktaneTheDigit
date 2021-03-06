﻿using KModkit;
using System.Collections;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;

public class TheDigitScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo Info;
    public KMAudio Audio;

    public KMSelectable UpBtn;
    public KMSelectable DownBtn;
    public KMSelectable ScreenBtn;

    public TextMesh ScreenText;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool isSolved;
    private bool solveAnimationDone;

    private int DisplayedNumber;
    private int CalculatedNumber;

    private bool interactable = true;

    private static readonly Regex SetRegEx = new Regex("^submit (\\d)$");

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "To submit an answer, do: !{0} submit #";
#pragma warning restore 414


    // Use this for initialization
    void Start()
    {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += Activate;
    }

    void Activate()
    {
        this.DisplayedNumber = Random.Range(0, 10);
        this.ScreenText.text = this.DisplayedNumber.ToString();
        GetAnswer();

        UpBtn.OnInteract += delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, UpBtn.transform);
            UpBtn.AddInteractionPunch();
            if (this.isSolved | !interactable)
            {
                return false;
            }

            if (this.DisplayedNumber == 9)
            {
                DisplayedNumber = 0;
                this.ScreenText.text = this.DisplayedNumber.ToString();
            }
            else
            {
                DisplayedNumber += 1;
                this.ScreenText.text = this.DisplayedNumber.ToString();
            }

            return false;
        };

        DownBtn.OnInteract += delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, UpBtn.transform);
            UpBtn.AddInteractionPunch();
            if (this.isSolved || !interactable)
            {
                return false;
            }

            if (this.DisplayedNumber == 0)
            {
                DisplayedNumber = 9;
                this.ScreenText.text = this.DisplayedNumber.ToString();
            }
            else
            {
                DisplayedNumber -= 1;
                this.ScreenText.text = this.DisplayedNumber.ToString();
            }

            return false;
        };

        ScreenBtn.OnInteract += delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, UpBtn.transform);
            UpBtn.AddInteractionPunch();
            if (this.isSolved || !interactable)
            {
                return false;
            }
            else if (this.DisplayedNumber == this.CalculatedNumber)
            {
                this.isSolved = true;
                StartCoroutine(Solve());
            }
            else
            {
                StartCoroutine(Solve());
            }

            return false;
        };
    }

    private void GetAnswer()
    {
        this.CalculatedNumber = this.Info.GetSerialNumberNumbers().Last() + this.Info.GetBatteryCount();
        Debug.LogFormat("[The Digit #{1}] Stage 1: The number is now {0}", CalculatedNumber, _moduleId);

        if (this.Info.IsPortPresent(Port.RJ45) || this.Info.IsPortPresent(Port.DVI))
        {
            CalculatedNumber += Info.GetSerialNumberLetters().Count();
        }

        Debug.LogFormat("[The Digit #{1}] Stage 2: The number is now {0}", CalculatedNumber, _moduleId);

        if (this.CalculatedNumber % 2 == 0)
        {
            this.CalculatedNumber /= 2;
        }

        else
        {
            this.CalculatedNumber *= 2;
        }

        Debug.LogFormat("[The Digit #{1}] Stage 3: The number is now {0}", CalculatedNumber, _moduleId);

        this.CalculatedNumber -= this.Info.GetPortPlateCount();
        Debug.LogFormat("[The Digit #{1}] Stage 4: The Number is now {0}", CalculatedNumber, _moduleId);
        if (Info.GetBatteryCount() * Info.GetBatteryHolderCount() > 6)
        {
            this.CalculatedNumber += 5;
        }

        Debug.LogFormat("[The Digit #{1}] Stage 5: The Number is now {0}", CalculatedNumber, _moduleId);

        this.CalculatedNumber -= 3;
        Debug.LogFormat("[The Digit #{1}] Stage 6: The Number is now {0}", CalculatedNumber, _moduleId);
        if (Info.GetPortCount() >= 1)
        {
            this.CalculatedNumber *= this.Info.GetPortCount();
        }

        Debug.LogFormat("[The Digit #{1}] Stage 7: The Number is now {0}", CalculatedNumber, _moduleId);
        this.CalculatedNumber += this.Info.GetOnIndicators().Count();
        Debug.LogFormat("[The Digit #{1}] Stage 8: The Number is now {0}", CalculatedNumber, _moduleId);
        if (this.CalculatedNumber < 0)
        {
            this.CalculatedNumber *= -1;
        }

        Debug.LogFormat("[The Digit #{1}] Stage 9: The Number is now {0}", CalculatedNumber, _moduleId);

        while (this.CalculatedNumber > 9)
        {
            this.CalculatedNumber -= 10;
        }

        Debug.LogFormat("[The Digit #{1}] The Final number is: {0}", CalculatedNumber, _moduleId);
    }

    public IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        var match = SetRegEx.Match(command);
        if (match.Success)
        {
            yield return null;
            while (DisplayedNumber != int.Parse(match.Groups[1].Value))
            {
                UpBtn.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            ScreenBtn.OnInteract();
            if (isSolved)
            {
                yield return "solve";
            }
            else
            {
                yield return "strike";
            }
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        Debug.LogFormat("[The Digit #{0}] Force solve requested by Twitch Plays.", _moduleId);
        while (CalculatedNumber != DisplayedNumber)
        {
            UpBtn.OnInteract();
            yield return new WaitForSeconds(.1f);
        }

        ScreenBtn.OnInteract();
        while (!solveAnimationDone)
        {
            yield return true;
        }
    }

    private IEnumerator Solve()
    {
        int numberOfCycles = 0;
        int initialScreenTxt = DisplayedNumber;
        interactable = false;
        while (numberOfCycles != 100)
        {
            yield return new WaitForSeconds(0.05f);
            retry:
            var screentxt = Random.Range(0, 10);
            if (screentxt.ToString() == ScreenText.text)
            {
                goto retry;
            }

            ScreenText.text = screentxt.ToString();
            numberOfCycles++;
        }

        if (this.isSolved)
        {
            ScreenText.text = initialScreenTxt.ToString();
            solveAnimationDone = true;
            HandleSolve();
        }
        else
        {
            this.interactable = true;
            HandleStrike();
        }
    }

    private void HandleSolve()
    {
        Debug.LogFormat("[The Digit #{0}] Submitted: {1}. That is correct. Module solved!", _moduleId,
            this.DisplayedNumber);
        Module.HandlePass();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, Module.transform);
        this.isSolved = true;
        ScreenText.color = new Color(0, 1.0f, 0);
    }

    private void HandleStrike()
    {
        Debug.LogFormat("[The Digit #{0}] Submitted: {1}. That is wrong. Strike!", _moduleId, this.DisplayedNumber);
        Module.HandleStrike();
        this.DisplayedNumber = Random.Range(0, 10);
        this.ScreenText.text = this.DisplayedNumber.ToString();
    }
}