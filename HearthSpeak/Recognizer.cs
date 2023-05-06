﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text.RegularExpressions;
using HearthSpeak.menus;
using System.Text;
using System.Threading.Tasks;

namespace HearthSpeak
{
    class Recognizer
    {

        GameManager gameManager;
        public SpeechRecognitionEngine Engine;

        public Recognizer()
        {
            gameManager = new GameManager();
            Engine = new SpeechRecognitionEngine();
            Engine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);
            Engine.SetInputToDefaultAudioDevice();
            AddGrammars(Engine);
            Engine.RecognizeAsync(RecognizeMode.Multiple);
        }
        Grammar BuildGrammar()
        {
            var hearthDictionary = new List<string> {
                // Main Menu
                 "hearthstone", "battlegrounds", "mercenaries", "modes", "shop for cards", "journal", "open packs", "my collection",

                // Modes Menu
                 "solo adventures","arena", "duels", "tavern brawl",  "choose",

                // Journal
                "quests", "quest log", "rewards", "rewards track", "achievements", "complete achievement", "profile", "close journal",

                // Playing Game
                "face",  "concede game",  "blue button", "end turn", "power", "champion",

                // Emotes
                "thank you", "sorry",  "oops", "threaten", "greetings", "good game",  "well played",

                // Miscellaenous
                "cancel", "position", "click", "go back",  "escape", "cancel search", "casual", "ranked",  "naxxramas",
                "x marks the spot", "buy arena admission",

                // Mouse Movement
                "center mouse", "hide mouse",

                // Collections
                 "flip next", "flip back",  "scroll up", "scroll down",
                "crafting", "confirm disenchant", "cancel disenchant", "create card", "disenchant card", "buy pack",
                "show only golden cards", "include uncraftable cards"

            };
            foreach (string desc in new string[] { "friendly", "enemy", "card", "deck", "play", "choose", "select",
                                                   "filter", "toggle" })
            {
                for (int i = 0; i < 11; i++)
                {
                    hearthDictionary.Add(desc + " " + i.ToString());
                }
            }
            Choices choices = new Choices(hearthDictionary.ToArray());
            GrammarBuilder gb = new GrammarBuilder(choices, 1, 99);
            Grammar grammar = new Grammar(gb);
            return grammar;
        }
        void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result == null || e.Result.Confidence <= .9) return;
            List<string> words = e.Result.Text.Split(' ').ToList();
            System.Console.WriteLine("Got input: " + String.Join(" ", words));
            while (words.Count > 0)
            {
                string wordsText = String.Join(" ", words.ToArray());
                string matchedText = "";
                foreach (var item in gameManager.ActionMap)
                {
                    Match match = item.Key.Match(wordsText);
                    if (!match.Success) continue;
                    matchedText = match.Groups[0].Value;
                    item.Value(matchedText.Split(' ').ToList());
                    words = wordsText.Remove(0, matchedText.Length).Split(' ').ToList();
                    break;
                }
                if (matchedText == "")
                {
                    words.RemoveAt(0);
                }
            }
        }

        void AddGrammars(SpeechRecognitionEngine recognizer)
        {
            Grammar dictationGrammar = BuildGrammar();
            Grammar mulliganGrammer = MakeRepeatedGrammar(new string[] { "mulligan" }, new string[] { "1", "2", "3", "4", "confirm" }, 99);
            Grammar moveGrammar = MakeMoveGrammar();
            Grammar removeGrammar = RemoveCardGrammar();
            recognizer.LoadGrammar(dictationGrammar);
            recognizer.LoadGrammar(mulliganGrammer);
            recognizer.LoadGrammar(moveGrammar);
            recognizer.LoadGrammar(removeGrammar);
        }

        Grammar MakeRepeatedGrammar(string[] firstWords, string[] choicesArr, int choicesMax = 99)
        {
            var gb = new GrammarBuilder(new Choices(firstWords));
            var choices = new Choices(choicesArr);
            gb.Append(new GrammarBuilder(choices, 1, choicesMax));
            return new Grammar(gb);
        }

        Grammar MakeMoveGrammar()
        {
            var directions = new string[] { "up", "right", "down", "left" };
            var numberList = Enumerable.Range(0, 9).Select(i => i.ToString()).ToArray();
            var numberChoices = new Choices(numberList);
            var min1Number = new GrammarBuilder(numberChoices, 1, 4);
            var numInFront = new GrammarBuilder();
            numInFront.Append(min1Number);
            numInFront.Append(new GrammarBuilder(PointGrammar(numberChoices), 0, 1));
            var gb = new GrammarBuilder(new Choices(directions));
            gb.Append(new Choices(new GrammarBuilder[] { numInFront, PointGrammar(numberChoices) }));
            return new Grammar(gb);
        }

        Grammar RemoveCardGrammar()
        {
            var numberListSmall = Enumerable.Range(1, 3).Select(i => i.ToString()).ToArray();
            var numberList = Enumerable.Range(0, 10).Select(i => i.ToString()).ToArray();
            var gb = new GrammarBuilder("remove");
            gb.Append(new GrammarBuilder(new Choices(numberListSmall), 0, 1));
            gb.Append(new GrammarBuilder(new Choices(numberList)));
            return new Grammar(gb);
        }

        GrammarBuilder PointGrammar(Choices numberChoices)
        {
            var pointGb = new GrammarBuilder("point");
            pointGb.Append(new GrammarBuilder(numberChoices), 1, 4);
            return pointGb;
        }

        public void ListenIO()
        {
            System.Console.WriteLine("Listening for input. Press Enter to stop listening.");
            Console.ReadLine();
            Engine.RecognizeAsyncCancel();
            new MainMenu();
        }

    }
}
