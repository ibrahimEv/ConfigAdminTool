using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHunspell;

namespace ConfigToolLibrary2
{
    public class SpellChecker
    {
        public Hunspell HunspellObj { get; }
        public SpellChecker()
        {
            HunspellObj = new Hunspell("en_US.aff", "en_US.dic");
        }
        //public static void CheckSpelling()
        //{
        //    string res = CheckAndGetSpelling("ActivityWith");
        //    res = CheckAndGetSpelling("Active");
        //    res = CheckAndGetSpelling("Is");
        //    res = CheckAndGetSpelling("dhajs");
        //    using (var hunspell = new Hunspell("en_US.aff", "en_US.dic"))
        //    {
        //        bool correct = hunspell.Spell("ActivityWith");
        //        List<string> suggestions = hunspell.Suggest("ActionTypeKey");
        //        hunspell.Add("IsActive");
        //        correct = hunspell.Spell("IsActive");
        //        correct = hunspell.Spell("Active");
        //    }
        //}

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
