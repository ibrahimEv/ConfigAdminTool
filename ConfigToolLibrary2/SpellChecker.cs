using NHunspell;
using System.Collections.Generic;

namespace ConfigToolLibrary2
{
    public class SpellChecker
    {
        public Hunspell HunspellObj { get; }
        public SpellChecker()
        {
            HunspellObj = new Hunspell("en_US.aff", "en_US.dic");
        }

        public void AddWordsToDictionary(List<string> wordList)
        {
            foreach (var word in wordList)
            {
                HunspellObj.Add(word);
            }
        }

        public void RemoveWordFromDictionary(string word)
        {
            HunspellObj.Remove(word);
        }

        public string CheckAndGetSpelling(string word)
        {
            List<string> suggestedWordList = HunspellObj.Suggest(word);

            if (suggestedWordList.Count > 0) return suggestedWordList[0];
            return string.Empty;
        }
    }
}
