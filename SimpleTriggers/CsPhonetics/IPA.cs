using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace SimpleTriggers.Phonetics {
    public class IPA : IDisposable
    {
        private Dictionary<string, string> dictionary;

        public IPA (string dictionaryPath) {
            dictionary = new Dictionary<string, string>(130000); // cheating a bit here
            using(var stream = File.Open(dictionaryPath, FileMode.Open))
            using(var reader = new StreamReader(stream)) {
                string? line;
                while ((line = reader.ReadLine()) != null) {
                    var parts = line.Split('\t');
                    if (parts.Length >= 2)
                    {
                        // This cleans up some things that Kokoro doesn't support that may be part of the IPA standard.
                        // I don't really have a good way of finding what is and isn't supported other than I find
                        // them as they pop up. e.g. if no sound plays when you try to test a phrase, check /xllog
                        var second = parts[1].Split(',')[0]; // ugly, only store the first pronunciation 
                        second = second.Replace("/", "");
                        second = second.Replace("ɫ", "l");
                        second = second.Replace("ɝ", "ɜː");
                        if(second[0]=='\u02c8') second = second.Remove(0,1); // creates some 'iy' sound
                        dictionary[parts[0].Trim()] = second;
                    }
                }
            }
        }

        public string EnglishToIPA(string text) {
            var builder = new StringBuilder();
            string[] words = Regex.Split(text, @"([\s\p{P}])"); // Split on spaces or punctuation

            foreach (var match in words) {
                var lower = match.ToLower();
                string? append;
                if(dictionary.ContainsKey(lower)) {
                    append = dictionary[lower];
                } else {
                    append = lower;
                }
                builder.Append(append);
                //if(match.Trim().Length>0) Log.Debug($"word={match} ;; match={append}");
            }
            return builder.ToString();
        }

        public void Dispose()
        {
            dictionary.Clear();
        }
    }
}
