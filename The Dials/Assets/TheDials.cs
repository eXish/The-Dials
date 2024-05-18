using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class TheDials : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;
   public KMSelectable[] Dials;
   public TextMesh Jon;
   public KMSelectable Submit;
   public VoltageMeterReader VMR;

   int[][] DialPositions = new int[4][] {
      new int[8] {1, 2, 3, 4, 5, 6, 7, 8,},
      new int[8] {1, 2, 3, 4, 5, 6, 7, 8,},
      new int[8] {1, 2, 3, 4, 5, 6, 7, 8,},
      new int[8] {1, 2, 3, 4, 5, 6, 7, 8,}
    };
   int[] EndingRotations = new int[4];
   int[] Rotations = new int[4];
   Queue<int> PotentialPathways = new Queue<int>();
   Dictionary<int, int> Pathways = new Dictionary<int, int>();
   List<int> Visited = new List<int>();
   List<int> AnswerPathway = new List<int>();

   string[] CardinalDirections = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
   string[] dirConvert = { "U", "UR", "R", "DR", "D", "DL", "L", "UL" };
   string[] dirConvertFuckYouNonexistentStringReverseFunction = { "U", "RU", "R", "RD", "D", "LD", "L", "LU" };
   string[] SelectedLetters = { "A", "B", "C", "D" };
   string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
   string[][] Maze = new string[][] { //Shows walls
      new string[] { "ULR ", "UL  ", "UR  ", "UL  ", "UD  ", "UD  ", "UD  ", "UR  "},
      new string[] { "L   ", "R   ", "LD  ", " D  ", "U   ", "UR  ", "UL  ", "R   "},
      new string[] { "LR  ", " LD ", "UD  ", "UR  ", "LR  ", "LR  ", "LR  ", "LRD "},
      new string[] { "L   ", "UR  ", "UL  ", "D   ", "RD  ", "LR  ", "LD  ", "UR  "},
      new string[] { "LR  ", "LR  ", "LD  ", "UR  ", "ULD ", "R   ", "UL  ", "R   "},
      new string[] { "LR  ", "LD  ", "UD  ", " D  ", "U R ", "LD  ", "RD  ", "LR  "},
      new string[] { "LR  ", "UL  ", "DU  ", "UR  ", "LD  ", "UR  ", "ULD ", "R   "},
      new string[] { "LD  ", "RD  ", "ULD ", "D   ", "UD  ", "D   ", "UD  ", "DR  "},
   };
   int[][] DebugMaze = new int[][] {
      new int[] { 0, 0, 0, 0, 0, 0, 0, 0},
      new int[] { 0, 0, 0, 0, 0, 0, 0, 0},
      new int[] { 0, 0, 0, 0, 0, 0, 0, 0},
      new int[] { 0, 0, 0, 0, 0, 0, 0, 0},
      new int[] { 0, 0, 0, 0, 0, 0, 0, 0},
      new int[] { 0, 0, 0, 0, 0, 0, 0, 0},
      new int[] { 0, 0, 0, 0, 0, 0, 0, 0},
      new int[] { 0, 0, 0, 0, 0, 0, 0, 0}
   };
   string[] Dial4Directions = { "LLL", "DUD", "LRD", "LLR", "UUL", "DLR", "LRL", "DDL", "RUU", "LRR", "UDR", "RRL", "LDU", "RDU", "LUR", "DRD" };

   int[] CurPosition = { 0, 0 };
   int[] GoalPosition = { 0, 0 };

   int LastDirection;
   int SecondToLastDirection;

   string[][] MazeLetters = new string[][] { //Shows walls
      new string[] { "Q", "", "", "", "U", "", "", ""},
      new string[] { "", "", "", "E", "", "X", "", ""},
      new string[] { "", "W", "", "I", "", "Y", "", "Z"},
      new string[] { "", "", "R", "", "K", "", "C", ""},
      new string[] { "", "F", "O", "", "T", "", "L", ""},
      new string[] { "", "", "", "J", "P", "", "", "V"},
      new string[] { "G", "M", "D", "", "", "B", "A", ""},
      new string[] { "", "", "H", "N", "S", "", "", ""},
   };


   string CheckForDuplicates;
   string Indicators;
   string SerialNumber;

   bool Duplicated;
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

      Jon.text = "";
      SerialNumber = Bomb.GetSerialNumberLetters().Join("");
      Indicators = Bomb.GetIndicators().Join("");
      //GenerateLetters();
      /*for (int i = 0; i < 4; i++) {
         DialPositions[i].Shuffle(); //Randomizes dial turning position
      }*/
      AnswerGenerator();
      Debug.LogFormat("[The Dials #{0}] The given letters are {1}, {2}, {3}, {4}.", moduleId, SelectedLetters[0], SelectedLetters[1], SelectedLetters[2], SelectedLetters[3]);
   }

   void ShowLetter (KMSelectable Dial) {
      for (int i = 0; i < 4; i++) {
         if (Dial == Dials[i]) {
            Jon.text = SelectedLetters[i];
         }
      }
   }

   void HideLetter (KMSelectable Dial) {
      Jon.text = "";
   }

   void DialTurn (KMSelectable Dial) {
      Audio.PlaySoundAtTransform("click", transform);
      //if (moduleSolved) {
      //   return;
      //}
      for (int i = 0; i < 4; i++) {
         if (Dial == Dials[i]) {
            Rotations[i]++;
            Rotations[i] %= 8;
            Jon.text = DialPositions[i][Rotations[i]].ToString();
            StartCoroutine(DialAnimation(Dial, i));
            //Dial.transform.localEulerAngles = new Vector3(0, (float) 45 * Rotations[i], 0);
         }
      }
   }

   IEnumerator DialAnimation (KMSelectable Dial, int i) {
      for (int j = 0; j < 90; j++) {
         Dial.transform.Rotate(0, .5f, 0, Space.Self);
         //yield return new WaitForSeconds(.01f);
      }
      yield return null;
   }

   void SubmitPress () {
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Submit.transform);
      Submit.AddInteractionPunch();
      if (moduleSolved) {
         return;
      }
      bool Validity = true;
      for (int i = 0; i < 4; i++) {
         if (DialPositions[i][Rotations[i]] != EndingRotations[i] + 1) {
            Validity = false;
         }
      }
      if (Validity) {
         GetComponent<KMBombModule>().HandlePass();
         moduleSolved = true;
      }
      else {
         GetComponent<KMBombModule>().HandleStrike();
      }
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
         SelectedLetters[i] = Alphabet[Rnd.Range(0, Alphabet.Length)].ToString();
         for (int j = 0; j < SerialNumber.Length; j++) {
            if (SelectedLetters[i] == SerialNumber[j].ToString()) {
               SharedLetterSerialNumberBitch = true;
            }
            if ((i == 1 || i == 3) && VowelCheck(SelectedLetters[i])) {
               Vowel = true;
            }
         }
         CheckForDuplicates += SelectedLetters[i];
      }
      for (int i = 0; i < 4; i++) {
         int CheckForTwo = 0;
         for (int j = 0; j < 4; j++) {
            if (SelectedLetters[i] == CheckForDuplicates[j].ToString()) {
               CheckForTwo++;
            }
         }
         if (CheckForTwo > 1) {
            Duplicated = true;
            break;
         }
      }
      for (int i = 0; i < 4; i++) {
         for (int j = 0; j < Indicators.Length; j++) {
            if (SelectedLetters[i] == Indicators[j].ToString()) {
               IndicatorCheck = true;
               break;
            }
         }
      }
      if ((SharedLetterSerialNumberBitch && Vowel && !IndicatorCheck && !Duplicated) || (!SharedLetterSerialNumberBitch && !Vowel && IndicatorCheck && Duplicated) || (!SharedLetterSerialNumberBitch && !Vowel && !IndicatorCheck && !Duplicated)) {
         //Checks if the Venn Diagram will have an answer
         goto Restart;
      }
   }

   bool VowelCheck (string SN) {
      return SN.Any(x => "AEIOU".Contains(x));
   }

   void AnswerGenerator () {

      Reset();
      GenerateLetters();
      DialOne();
      DialTwo();
      DialThree();
   }

   void Reset () {
      PotentialPathways.Clear();
      Pathways.Clear();
      Visited.Clear();
      Visited.Clear();
      AnswerPathway.Clear();
      for (int i = 0; i < 64; i++) {
         DebugMaze[i / 8][i % 8] = 0;
      }
   }

   void DialOne () {
      int NumberForAnswer = 0; //Venn diagram dial 1
      if (SharedLetterSerialNumberBitch) {
         NumberForAnswer++;
      }
      if (Duplicated) {
         NumberForAnswer += 2;
      }
      if (Vowel) {
         NumberForAnswer += 4;
      }
      if (IndicatorCheck) {
         NumberForAnswer += 8;
      }
      switch (NumberForAnswer) {
         case 1:
            EndingRotations[0] = 1;
            break;
         case 2:
            EndingRotations[0] = Bomb.GetPorts().Count() % 8;
            break;
         case 3:
            EndingRotations[0] = 5;
            break;
         case 4:
            EndingRotations[0] = 6;
            break;
         case 6:
            EndingRotations[0] = VMR.GetVoltageMeterInt() == -1 ? 0 : VMR.GetVoltageMeterInt() % 8;
            break;
         case 7:
            EndingRotations[0] = 7;
            break;
         case 8:
            EndingRotations[0] = Bomb.GetIndicators().Count() % 8;
            break;
         case 9:
            EndingRotations[0] = Bomb.GetSerialNumberNumbers().Last() % 8;
            break;
         case 11:
            EndingRotations[0] = 0;
            break;
         case 12:
            EndingRotations[0] = Bomb.GetBatteryCount() % 8;
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
      }
   }

   void DialTwo () {
      if (IndicatorCheck) {
         //Debug.LogFormat("[The Dials #{0}] There is an indicator that shares a letter with the module, going down the yes route.", moduleId);
         if (Bomb.GetBatteryCount(Battery.AA) > Bomb.GetBatteryCount(Battery.D)) {
            //Debug.LogFormat("[The Dials #{0}] There are more AA batteries than D batteries, going down the yes route.", moduleId);
            if (Bomb.IsIndicatorPresent(Indicator.FRK) || Bomb.IsIndicatorPresent(Indicator.FRQ) || Bomb.IsIndicatorPresent(Indicator.BOB)) {
               //Debug.LogFormat("[The Dials #{0}] There is an FRK, FRQ, BOB.", moduleId);
               EndingRotations[1] = 4;
            }
            else {
               //Debug.LogFormat("[The Dials #{0}] There is not an FRK, FRQ, BOB.", moduleId);
               EndingRotations[1] = 0;
            }
         }
         else {
            //Debug.LogFormat("[The Dials #{0}] There are less AA batteries than D batteries, going down the no route.", moduleId);
            if (Bomb.IsPortPresent(Port.StereoRCA)) {
               //Debug.LogFormat("[The Dials #{0}] There is a Stereo RCA port.", moduleId);
               EndingRotations[1] = 6;
            }
            else {
               //Debug.LogFormat("[The Dials #{0}] There is not a Stereo RCA port.", moduleId);
               EndingRotations[1] = 1;
            }
         }
      }
      else {
         //Debug.LogFormat("[The Dials #{0}] There is not an indicator that shares a letter with the module, going down the no route.", moduleId);
         if (Bomb.IsPortPresent(Port.RJ45)) {
            //Debug.LogFormat("[The Dials #{0}] There is a RJ-45 in the serial number, going down the yes route.", moduleId);
            if (VowelCheck(SerialNumber)) {
               //Debug.LogFormat("[The Dials #{0}] There is a vowel in the serial number.", moduleId);
               EndingRotations[1] = 3;
            }
            else {
               //Debug.LogFormat("[The Dials #{0}] There is not a vowel in the serial number.", moduleId);
               EndingRotations[1] = 5;
            }
         }
         else {
            //Debug.LogFormat("[The Dials #{0}] There is not a RJ-45 in the serial number, going down the no route.", moduleId);
            if (int.Parse(Bomb.GetSerialNumber()[5].ToString()) % 2 == 0) {
               //Debug.LogFormat("[The Dials #{0}] The last digit of the serial number is even.", moduleId);
               EndingRotations[1] = 7;
            }
            else {
               //Debug.LogFormat("[The Dials #{0}] The last digit of the serial number is odd.", moduleId);
               EndingRotations[1] = 2;
            }
         }
      }
   }

   void DialThree () {
      GoalPosition[0] = EndingRotations[1];
      GoalPosition[1] = EndingRotations[0];
      for (int i = 0; i < 8; i++) {
         for (int j = 0; j < 8; j++) {
            if (MazeLetters[i][j] == SelectedLetters[2]) {
               CurPosition[0] = i;
               CurPosition[1] = j;
            }
         }
      }
      if (MultipleAnswers(CurPosition[0], CurPosition[1], GoalPosition[0], GoalPosition[1]))
      {
         AnswerGenerator();
         return;
      }
      BFS(CurPosition[0] * 10 + CurPosition[1], GoalPosition[0] * 10 + GoalPosition[1]);
   }

   void BFS (int Starting, int Ending) {
      PotentialPathways.Enqueue(Starting);
      int Test = PotentialPathways.Dequeue();
      //Visited.Add(Test[0] * 10 + Test[1]);
      //Debug.Log(Maze[Test[0]][Test[1]]);
      Debug.Log("Starting at " + Starting.ToString("00"));
      Debug.Log("Going to " + GoalPosition[0] + GoalPosition[1]);
      while (true) { //Doing this to make sure it doesn't check if the objects are the same
        // Debug.Log("Checking from: " + Test);
         if (!Maze[Test / 10][Test % 10].Contains("U") && !Visited.Contains(Test - 10) && !PotentialPathways.Contains(Test - 10)) {
            //Debug.Log("Adding to queue: " + (Test - 10));
            PotentialPathways.Enqueue(Test - 10);
            Pathways.Add(Test - 10, Test);
         }

         if (!Maze[Test / 10][Test % 10].Contains("R") && !Visited.Contains(Test + 1) && !PotentialPathways.Contains(Test + 1)) {
            //Debug.Log("Adding to queue: " + (Test + 1));
            PotentialPathways.Enqueue(Test + 1);
            Pathways.Add(Test + 1, Test);
         }
         if (!Maze[Test / 10][Test % 10].Contains("D") && !Visited.Contains(Test + 10) && !PotentialPathways.Contains(Test + 10)) {
            //Debug.Log("Adding to queue: " + (Test + 10));
            PotentialPathways.Enqueue(Test + 10);
            Pathways.Add(Test + 10, Test);
         }
         if (!Maze[Test / 10][Test % 10].Contains("L") && !Visited.Contains(Test - 1) && !PotentialPathways.Contains(Test - 1)) {
            //Debug.Log("Adding to queue: " + (Test - 1));
            PotentialPathways.Enqueue(Test - 1);
            Pathways.Add(Test - 1, Test);
         }

         DebugMaze[Test / 10][Test % 10]++;

         Visited.Add(Test);
         string log = "";
         for (int i = 0; i < Visited.Count(); i++) {
            log += Visited[i] + " ";
         }
         //Debug.Log(log);
         if (Test == GoalPosition[0] * 10 + GoalPosition[1]) {
            break;
         }
         Test = PotentialPathways.Dequeue();

      }
      while (Visited.Last() != GoalPosition[0] * 10 + GoalPosition[1]) {
         Visited.Remove(Visited.Last());
      }
      AnswerPathway.Add(Visited.Last());
      int value = 0;
      while (AnswerPathway.Last() != Starting) {
         if (Pathways.TryGetValue(AnswerPathway.Last(), out value)) {
            AnswerPathway.Add(value);
         }
      }
      //Debug.Log(AnswerPathway.Count());
      AnswerPathway.Reverse();
      if (AnswerPathway.Count() < 3) {
         AnswerGenerator();
         return;
      }

      string w = "";

      for (int i = 0; i < 2; i++) {
         if (AnswerPathway[AnswerPathway.Count() - (i + 2)] / 10 > AnswerPathway[AnswerPathway.Count() - (i + 1)] / 10) {
            w += "U";
            continue;
         }
         if (AnswerPathway[AnswerPathway.Count() - (i + 2)] / 10 < AnswerPathway[AnswerPathway.Count() - (i + 1)] / 10) {
            w += "D";
            continue;
         }
         if (AnswerPathway[AnswerPathway.Count() - (i + 2)] % 10 < AnswerPathway[AnswerPathway.Count() - (i + 1)] % 10) {
            w += "R";
            continue;
         }
         if (AnswerPathway[AnswerPathway.Count() - (i + 2)] % 10 > AnswerPathway[AnswerPathway.Count() - (i + 1)] % 10) {
            w += "L";
            continue;
         }
      }
      if (w[0] == w[1]) {
         w = w[0].ToString();
      }
      //Debug.Log(w);
      EndingRotations[2] = Array.IndexOf(dirConvert, w) == -1 ? Array.IndexOf(dirConvertFuckYouNonexistentStringReverseFunction, w) : Array.IndexOf(dirConvert, w);

      //Debug.LogFormat("[The Dials #{0}] The third dial's rotation is {1}.", moduleId, EndingRotations[2]);
      DialFour();
   }

   void DialFour () {
      
      List<int> DirAnswers = new List<int>() { };
      for (int i = 0; i < dirConvert[EndingRotations[0]].Length; i++) {
         for (int j = 0; j < dirConvert[EndingRotations[1]].Length; j++) {
            for (int k = 0; k < dirConvert[EndingRotations[2]].Length; k++) {
               if (Dial4Directions.Contains(dirConvert[EndingRotations[0]][i].ToString() + dirConvert[EndingRotations[1]][j].ToString() + dirConvert[EndingRotations[2]][k].ToString())) {
                  DirAnswers.Add(Array.IndexOf(Dial4Directions, dirConvert[EndingRotations[0]][i].ToString() + dirConvert[EndingRotations[1]][j].ToString() + dirConvert[EndingRotations[2]][k].ToString()));
               }
            }
         }
      }
      if (DirAnswers.Count() != 1) {
         AnswerGenerator();
         return;
      }
      EndingRotations[3] = DirAnswers[0] / 2;
      LogAnswer();
   }

   void LogAnswer () {
      string log = "";
      for (int i = 0; i < AnswerPathway.Count(); i++) {
         log += (AnswerPathway[i] / 10 + 1) + "," + (AnswerPathway[i] % 10 + 1) + " ";
      }
      if (SharedLetterSerialNumberBitch) {
         Debug.LogFormat("[The Dials #{0}] The serial number shares a letter with the module.", moduleId);
      }
      if (Duplicated) {
         Debug.LogFormat("[The Dials #{0}] There is a duplicated letter on the module.", moduleId);
      }
      if (Vowel) {
         Debug.LogFormat("[The Dials #{0}] There is a vowel on the module in either the second or fourth position.", moduleId);
      }
      if (IndicatorCheck) {
         Debug.LogFormat("[The Dials #{0}] There is an indicator that shares a letter with the module.", moduleId);
      }
      Debug.LogFormat("[The Dials #{0}] The first dial's final rotation is {1}.", moduleId, CardinalDirections[EndingRotations[0]]);
      Debug.LogFormat("[The Dials #{0}] The second dial's final rotation is {1}.", moduleId, CardinalDirections[EndingRotations[1]]);
      Debug.LogFormat("[The Dials #{0}] The shortest path from {1},{2} to {3},{4} is {5}", moduleId, CurPosition[0] + 1, CurPosition[1] + 1, EndingRotations[1] + 1, EndingRotations[0] + 1, log);
      Debug.LogFormat("[The Dials #{0}] The third dial's final rotation is {1}.", moduleId, CardinalDirections[EndingRotations[2]]);
      Debug.LogFormat("[The Dials #{0}] The fourth dial's final rotation is {1}.", moduleId, CardinalDirections[EndingRotations[3]]);
   }

    bool MultipleAnswers(int startX, int startY, int endX, int endY)
    {
        var q = new Queue<int[]>();
        var allMoves = new List<Movement>();
        var startPoint = new int[] { startX, startY, 0 };
        var target = new int[] { endX, endY };
        q.Enqueue(startPoint);
        int targetLength = -1;
        int ct = 0;
        while (q.Count > 0)
        {
            var next = q.Dequeue();
            if (next[0] == target[0] && next[1] == target[1])
            {
                if (targetLength == -1)
                    targetLength = next[2];
                ct++;
            }
            if (targetLength != -1 && q.All(x => x[2] != targetLength))
                goto foundSols;
            string paths = Maze[next[0]][next[1]];
            var cell = paths.Replace(" ", "");
            var allDirections = "ULRD";
            var offsets = new int[,] { { -1, 0 }, { 0, -1 }, { 0, 1 }, { 1, 0 } };
            for (int i = 0; i < 4; i++)
            {
                var check = new int[] { next[0] + offsets[i, 0], next[1] + offsets[i, 1] };
                if (!cell.Contains(allDirections[i]) && !allMoves.Any(x => x.start[0] == check[0] && x.start[1] == check[1]))
                {
                    q.Enqueue(new int[] { next[0] + offsets[i, 0], next[1] + offsets[i, 1], next[2] + 1 });
                    allMoves.Add(new Movement { start = next, end = new int[] { next[0] + offsets[i, 0], next[1] + offsets[i, 1], next[2] + 1 } });
                }
            }
        }
        foundSols:
        if (ct > 1)
            return true;
        return false;
    }

    class Movement
    {
        public int[] start;
        public int[] end;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} cycle to cycle the letters. Use !{0} 1234 to set the dials positions (1-8 from N to NW) in their respective order and then submit.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      Command = Command.Trim().ToUpper();
      yield return null;
      if (Command == "CYCLE") {
         for (int i = 0; i < 4; i++) {
            Dials[i].OnHighlight();
            yield return new WaitForSeconds(.5f);
            Dials[i].OnHighlightEnded();
            yield return new WaitForSeconds(.5f);
         }
      }
      else if (Command.Length != 4 || !Command.All(x => "12345678".Contains(x))) {
         yield return "sendtochaterror I don't understand!";
      }
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
      for (int i = 0; i < 4; i++) {
         while (DialPositions[i][Rotations[i]] != EndingRotations[i] + 1) {
            Dials[i].OnInteract();
            yield return new WaitForSeconds(.1f);
         }
      }
      Submit.OnInteract();
   }
}
