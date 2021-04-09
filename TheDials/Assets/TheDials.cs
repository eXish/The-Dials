using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class TheDials : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;
   public KMSelectable[] Dials;
   public TextMesh Jon;
   public KMSelectable Submit;

   int[][] DialPositions = new int[4][] {
      new int[8] {1, 2, 3, 4, 5, 6, 7, 8,},
      new int[8] {1, 2, 3, 4, 5, 6, 7, 8,},
      new int[8] {1, 2, 3, 4, 5, 6, 7, 8,},
      new int[8] {1, 2, 3, 4, 5, 6, 7, 8,}
    };
   int[] EndingRotations = new int[4];
   int[] Rotations = new int[4];

   string[] SelectedLetters = { "A", "B", "C", "D" };
   string AlphabetButSmol = "ACDEHILMNORSTU";
   string CheckForDuplicates;
   string Indicators;
   string SerialNumber;

   bool Duplicated;
   bool Error;
   bool IndicatorCheck;
   bool SharedLetterSerialNumberBitch;
   bool Unicorn;
   bool Vowel;

   static int moduleIdCounter = 1;
   int moduleId;
   private bool moduleSolved;

   void Awake () {
      moduleId = moduleIdCounter++;
      foreach (KMSelectable Dial in Dials) {
         Dial.OnInteract += delegate () { DialTurn(Dial); return false; };
         Dial.OnHighlight += delegate () { ShowLetter(Dial); };
         Dial.OnHighlightEnded += delegate () { HideLetter(Dial); };
      }
      Submit.OnInteract += delegate () { SubmitPress(); return false; };
   }

   void Start () {
      if (Bomb.GetBatteryCount() == 0 && Bomb.GetPortCount() == 0) {
         Unicorn = true;
         Debug.LogFormat("[The Dials #{0}] There are no batteries nor ports. All dials should be set to 0.", moduleId);
      }
      Jon.text = "";
      SerialNumber = Bomb.GetSerialNumberLetters().Join("");
      Indicators = Bomb.GetIndicators().Join("");
      GenerateLetters();
      Debug.LogFormat("[The Dials #{0}] The given letters are {1}, {2}, {3}, {4}.", moduleId, SelectedLetters[0], SelectedLetters[1], SelectedLetters[2], SelectedLetters[3]);
      for (int i = 0; i < 4; i++)
         DialPositions[i].Shuffle(); //Randomizes dial turning position
      AnswerGenerator();
      if (Unicorn)
         for (int i = 0; i < 4; i++)
            EndingRotations[i] = 1;
   }

   void ShowLetter (KMSelectable Dial) {
      for (int i = 0; i < 4; i++)
         if (Dial == Dials[i])
            Jon.text = SelectedLetters[i];
   }

   void HideLetter (KMSelectable Dial) {
      Jon.text = "";
   }

   void DialTurn (KMSelectable Dial) {
      Audio.PlaySoundAtTransform("Brrr", transform);
      if (moduleSolved)
         return;
      if (Error)
         GetComponent<KMBombModule>().HandlePass();
      for (int i = 0; i < 4; i++)
         if (Dial == Dials[i]) {
            Rotations[i]++;
            Rotations[i] %= 8;
            Jon.text = DialPositions[i][Rotations[i]].ToString();
            Dial.transform.localEulerAngles = new Vector3(0, (float) 45 * Rotations[i], 0);
         }
   }

   void SubmitPress () {
      if (moduleSolved)
         return;
      bool Validity = true;
      for (int i = 0; i < 4; i++)
         if (DialPositions[i][Rotations[i]] != EndingRotations[i])
            Validity = false;
      if (Validity) {
         GetComponent<KMBombModule>().HandlePass();
         moduleSolved = true;
      }
      else
         GetComponent<KMBombModule>().HandleStrike();
   }

   void GenerateLetters () {
      Restart:
      SerialNumber = Bomb.GetSerialNumberLetters().Join("");
      Indicators = Bomb.GetIndicators().Join("");
      SharedLetterSerialNumberBitch = false;
      Vowel = false;
      CheckForDuplicates = "";
      Duplicated = false;
      IndicatorCheck = false;
      for (int i = 0; i < 4; i++) {
         SelectedLetters[i] = AlphabetButSmol[UnityEngine.Random.Range(0, AlphabetButSmol.Length)].ToString();
         for (int j = 0; j < SerialNumber.Length; j++) {
            if (SelectedLetters[i] == SerialNumber[j].ToString())
               SharedLetterSerialNumberBitch = true;
            if ((i == 1 || i == 3) && VowelCheck(SelectedLetters[i]))
               Vowel = true;
         }
         CheckForDuplicates += SelectedLetters[i];
      }
      for (int i = 0; i < 4; i++) {
         int CheckForTwo = 0;
         for (int j = 0; j < 4; j++)
            if (SelectedLetters[i] == CheckForDuplicates[j].ToString())
               CheckForTwo++;
         if (CheckForTwo > 1) {
            Duplicated = true;
            goto Escape; //forgot how far breaks break
         }
      }
      Escape:
      for (int i = 0; i < 4; i++)
         for (int j = 0; j < Indicators.Length; j++)
            if (SelectedLetters[i] == Indicators[j].ToString()) {
               IndicatorCheck = true;
               goto Leave;
            }
      Leave:
      if ((SharedLetterSerialNumberBitch && Vowel && !IndicatorCheck && !Duplicated) || (!SharedLetterSerialNumberBitch && !Vowel && IndicatorCheck && Duplicated)
      || (!SharedLetterSerialNumberBitch && !Vowel && !IndicatorCheck && !Duplicated))//Checks if the Venn Diagram will have an answer
         goto Restart;
   }

   bool VowelCheck (string ThingToCheck) {
      if (ThingToCheck.Any(x => "AEIOU".Contains(x)))
         return true;
      else
         return false;
   }

   void AnswerGenerator () {
      int NumberForAnswer = 0; //Venn diagram dial 1
      if (SharedLetterSerialNumberBitch) {
         NumberForAnswer++;
         Debug.LogFormat("[The Dials #{0}] The serial number shares a letter with the module.", moduleId);
      }
      if (Duplicated) {
         NumberForAnswer += 2;
         Debug.LogFormat("[The Dials #{0}] There is a duplicated letter on the module.", moduleId);
      }
      if (Vowel) {
         NumberForAnswer += 4;
         Debug.LogFormat("[The Dials #{0}] There is a vowel on the module in either the second or fourth position.", moduleId);
      }
      if (IndicatorCheck) {
         NumberForAnswer += 8;
         Debug.LogFormat("[The Dials #{0}] There is an indicator that shares a letter with the module.", moduleId);
      }
      switch (NumberForAnswer) {
         case 1:
            EndingRotations[0] = 1;
            break;
         case 2:
            EndingRotations[0] = Bomb.GetPorts().Count() % 8 + 1;
            break;
         case 3:
            EndingRotations[0] = 5;
            break;
         case 4:
            EndingRotations[0] = 6;
            break;
         case 6:
            EndingRotations[0] = Bomb.GetSolvableModuleNames().Count() % 8 + 1;
            break;
         case 7:
            EndingRotations[0] = 7;
            break;
         case 8:
            EndingRotations[0] = Bomb.GetIndicators().Count() % 8 + 1;
            break;
         case 9:
            EndingRotations[0] = Bomb.GetSerialNumberNumbers().Last() % 8 + 1;
            break;
         case 11:
            EndingRotations[0] = 8;
            break;
         case 12:
            EndingRotations[0] = Bomb.GetBatteryCount() % 8 + 1;
            break;
         case 13:
            EndingRotations[0] = 4;
            break;
         case 14:
            EndingRotations[0] = 3;
            break;
         case 15:
            EndingRotations[0] = 2;
            break;
         default:
            Error = true;
            Debug.LogFormat("[The Dials #{0}] There has been a grave error. The module will now solve upon any input.", moduleId);
            return;
      }
      Debug.LogFormat("[The Dials #{0}] The first dial's rotation is {1}.", moduleId, EndingRotations[0]);
      //Flow chart, dial 2

      if (IndicatorCheck) {
         Debug.LogFormat("[The Dials #{0}] There is an indicator that shares a letter with the module, going down the yes route.", moduleId);
         goto BatteryCheckCountVenn;
      }
      Debug.LogFormat("[The Dials #{0}] There is not an indicator that shares a letter with the module, going down the no route.", moduleId);
      if (Bomb.IsPortPresent(Port.RJ45)) {
         Debug.LogFormat("[The Dials #{0}] There is a RJ-45 in the serial number, going down the yes route.", moduleId);
         goto VowelSerialNumberCheck;
      }
      Debug.LogFormat("[The Dials #{0}] There is not a RJ-45 in the serial number, going down the no route.", moduleId);
      if (int.Parse(Bomb.GetSerialNumber()[5].ToString()) % 2 == 0) {
         Debug.LogFormat("[The Dials #{0}] The last digit of the serial number is even.", moduleId);
         EndingRotations[1] = 7;
         goto End;
      }
      else {
         Debug.LogFormat("[The Dials #{0}] The last digit of the serial number is odd.", moduleId);
         EndingRotations[1] = 2;
         goto End;
      }

      VowelSerialNumberCheck:
      if (VowelCheck(SerialNumber)) {
         Debug.LogFormat("[The Dials #{0}] There is a vowel in the serial number.", moduleId);
         EndingRotations[1] = 3;
         goto End;
      }
      else {
         Debug.LogFormat("[The Dials #{0}] There is not a vowel in the serial number.", moduleId);
         EndingRotations[1] = 5;
         goto End;
      }

      BatteryCheckCountVenn:
      if (Bomb.GetBatteryCount(Battery.AA) > Bomb.GetBatteryCount(Battery.D)) {
         Debug.LogFormat("[The Dials #{0}] There are more AA batteries than D batteries, going down the yes route.", moduleId);
         goto IndicatorBullshit;
      }
      Debug.LogFormat("[The Dials #{0}] There are less AA batteries than D batteries, going down the no route.", moduleId);
      if (Bomb.IsPortPresent(Port.StereoRCA)) {
         Debug.LogFormat("[The Dials #{0}] There is a Stereo RCA port.", moduleId);
         EndingRotations[1] = 6;
         goto End;
      }
      else {
         Debug.LogFormat("[The Dials #{0}] There is not a Stereo RCA port.", moduleId);
         EndingRotations[1] = 1;
         goto End;
      }

      IndicatorBullshit:
      if (Bomb.IsIndicatorPresent(Indicator.FRK) || Bomb.IsIndicatorPresent(Indicator.FRQ) || Bomb.IsIndicatorPresent(Indicator.BOB)) {
         Debug.LogFormat("[The Dials #{0}] There is an FRK, FRQ, BOB.", moduleId);
         EndingRotations[1] = 4;
         goto End;
      }
      else {
         Debug.LogFormat("[The Dials #{0}] There is not an FRK, FRQ, BOB.", moduleId);
         EndingRotations[1] = 8;
         goto End;
      }

      End://Dial 3, aka just math
      Debug.LogFormat("[The Dials #{0}] The second dial's rotation is {1}.", moduleId, EndingRotations[1]);
      string Alphabet = ".ABCDEFGHIJKLMNOPQRSTUVWXYZ";
      int Everything = 0;
      Everything += Bomb.GetPortCount() + Bomb.GetBatteryCount() + Bomb.GetBatteryHolderCount() + Bomb.GetPortPlates().Count() + Bomb.GetIndicators().Count() + Bomb.GetSolvableModuleNames().Count() + 1;
      Debug.LogFormat("[The Dials #{0}] Adding all the necessary things gives {1}.", moduleId, Everything);
      Everything *= (Everything - 1) % 9 + 1;
      Everything++;
      Debug.LogFormat("[The Dials #{0}] Multiplying by the digital root and adding 1 gives {1}.", moduleId, Everything);
      int[] NumberedLetters = new int[4];
      for (int i = 0; i < 4; i++)
         for (int j = 0; j < Alphabet.Length; j++)
            if (SelectedLetters[i] == Alphabet[j].ToString()) {
               NumberedLetters[i] = j;
               Debug.LogFormat("[The Dials #{0}] Letter {1} is {2}.", moduleId, i + 1, j);
            }
      EndingRotations[2] = ((((Everything + NumberedLetters[0] - NumberedLetters[1]) * NumberedLetters[2]) / NumberedLetters[3]) % 8);
      while (EndingRotations[2] < 0)
         EndingRotations[2] += 8;
      EndingRotations[2] = ((((Everything + NumberedLetters[0] - NumberedLetters[1]) * NumberedLetters[2]) / NumberedLetters[3]) % 8);
      while (EndingRotations[2] < 0)
         EndingRotations[2] += 8;
      EndingRotations[2]++;
      Debug.LogFormat("[The Dials #{0}] The third dial's rotation is {1}.", moduleId, EndingRotations[2]);
      //Dial 4, Giant ass table why toast
      int[][] GiantAssTable = new int[14][] {
        new int[14] {1, 8, 7, 6, 7, 4, 5, 2, 3, 8, 1, 8, 7, 6},
        new int[14] {2, 7, 8, 5, 8, 3, 6, 1, 4, 7, 2, 7, 8, 5},
        new int[14] {3, 6, 1, 4, 7, 2, 7, 8, 5, 8, 3, 5, 1, 4},
        new int[14] {4, 5, 2, 3, 8, 1, 8, 7, 6, 7, 4, 6, 2, 3},
        new int[14] {5, 4, 3, 2, 1, 8, 7, 8, 7, 6, 5, 4, 3, 2},
        new int[14] {6, 3, 4, 1, 2, 7, 8, 7, 8, 5, 6, 3, 4, 1},
        new int[14] {7, 2, 5, 8, 3, 8, 1, 6, 7, 4, 7, 2, 5, 8},
        new int[14] {8, 1, 6, 7, 4, 7, 2, 5, 8, 3, 8, 1, 7, 7},
        new int[14] {7, 8, 7, 8, 5, 6, 3, 4, 1, 2, 7, 8, 6, 8},
        new int[14] {8, 7, 8, 7, 6, 5, 4, 3, 2, 1, 8, 7, 8, 7},
        new int[14] {1, 8, 7, 6, 7, 4, 5, 2, 3, 8, 1, 8, 7, 6},
        new int[14] {2, 7, 8, 5, 8, 3, 6, 1, 4, 7, 2, 7, 8, 5},
        new int[14] {3, 6, 1, 4, 7, 2, 7, 8, 5, 8, 3, 6, 1, 4},
        new int[14] {4, 5, 2, 3, 8, 1, 8, 7, 6, 7, 4, 5, 2, 3}
      };
      int[] IndexingForGiantAssTable = new int[2];
      for (int i = 0; i < AlphabetButSmol.Length; i++) {
         if (SelectedLetters[0] == AlphabetButSmol[i].ToString())
            IndexingForGiantAssTable[0] = i;
         if (SelectedLetters[2] == AlphabetButSmol[i].ToString())
            IndexingForGiantAssTable[1] = i;
      }
      EndingRotations[3] = GiantAssTable[IndexingForGiantAssTable[1]][IndexingForGiantAssTable[0]];
      Debug.LogFormat("[The Dials #{0}] The fourth dial's rotation is {1}.", moduleId, EndingRotations[3]);
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} cycle to cycle the letters. Use !{0} 1234 to set the dials to their respective positions in their respective order and then submit.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      Command = Command.Trim().ToUpper();
      yield return null;
      if (Command == "CYCLE")
         for (int i = 0; i < 4; i++) {
            Dials[i].OnHighlight();
            yield return new WaitForSeconds(.5f);
            Dials[i].OnHighlightEnded();
            yield return new WaitForSeconds(.5f);
         }
      else if (Command.Length != 4 || !Command.Any(x => "12345678".Contains(x)))
         yield return "sendtochaterror I don't understand!";
      else {
         for (int i = 0; i < 4; i++) {
            while (DialPositions[i][Rotations[i]] != int.Parse(Command[i].ToString())) {
               Dials[i].OnInteract();
               yield return new WaitForSeconds(.1f);
            }
         }
         Submit.OnInteract();
      }
   }

   IEnumerator TwitchHandleForcedSolve () {
      for (int i = 0; i < 4; i++)
         while (DialPositions[i][Rotations[i]] != EndingRotations[i]) {
            Dials[i].OnInteract();
            yield return new WaitForSeconds(.1f);
         }
      Submit.OnInteract();
   }
}
