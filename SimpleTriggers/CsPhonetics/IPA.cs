using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace SimpleTriggers.Phonetics {
    public class IPA {
        private Dictionary<string, string> dictionary;

        public IPA (string assemblyPath, string ipaDictionary) {
            this.dictionary = new Dictionary<string, string>();
            var resource = Path.Join(assemblyPath, ipaDictionary);

            using(var stream = File.Open(resource, FileMode.Open))
            using(var reader = new StreamReader(stream)) {
                string? line;
                while ((line = reader.ReadLine()) != null) {
                    var parts = line.Split('\t');
                    if (parts.Length >= 2)
                    {
                        var second = parts[1].Split(',')[0]; // ugly, only store the first pronunciation 
                        second = second.Replace("/", "");
                        second = second.Replace("\u02c8", ""); // creates some 'iy' sound
                        second = second.Replace("ɫ", "l");
                        dictionary[parts[0].Trim()] = second;
                        
                    }
                }
            }
        }

        public IPA (Dictionary<string, string> dictionary) {
            this.dictionary = dictionary;
        }

        public string EnglishToIPA(string text) {
            var builder = new StringBuilder();
            string[] words = Regex.Split(text, @"([\s\p{P}])"); // Split on spaces or punctuation

            foreach (var match in words) {
                var lower = match.ToLower();
                string? append;
                if (dictionary.ContainsKey(lower)) {
                    //builder.Append(dictionary[lower]);
                    append = dictionary[lower];
                } else {
                    //builder.Append(lower);
                    append = lower;
                }
                builder.Append(append);
                //if(match.Trim().Length>0) Log.Debug($"word={match} ;; match={append}");
            }
            return builder.ToString();
        }

    }
}
