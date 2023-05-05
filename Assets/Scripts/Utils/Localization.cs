using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using TestSuite;
using UnityEngine;

namespace Utils
{
    [DataContract, Serializable]
    public struct LocalizationEntry
    {
        [DataMember(Order = 0)] public string key;
        [DataMember(Order = 1)] public string value;
        [DataMember(Order = 2, EmitDefaultValue = false)] public string @short;
    }

    [DataContract]
    public class Localization
    {
        public static Regex keyRegex = new Regex(@"(?<![\\])[$][a-zA-Z0-9_:]+");
        private static Localization DEFAULT, PATCH;

        [DataMember] string label, shortlabel;
        private Dictionary<string, LocalizationEntry> dictionary;
        [DataMember] public List<LocalizationEntry> entries;

        public string ShortLabel => shortlabel;

        public static string CurrentLang => DEFAULT?.shortlabel;

        private Localization()
        {
            label = "default";
            shortlabel = "def";
            dictionary = new Dictionary<string, LocalizationEntry>();
            entries = new List<LocalizationEntry>();
        }

        public static void PatchFromConfig(StudyConfig patch)
        {
            InitDefault();

            var data = patch.GetCustomLocalizationData(DEFAULT.ShortLabel);

            if (data != null)
            {
                DEFAULT.entries.AddRange(data.entries);
                data.InitDictionary();
            }

            PATCH = data;
        }

        public static void InitDefault(bool force = false)
        {
            if (DEFAULT == null || force)
            {
                try
                {
                    DEFAULT = JSONSerializer.FromJSONFile<Localization>("./localization.json");
                }
                catch { DEFAULT = new Localization(); }

                DEFAULT.InitDictionary();
            }
        }

        public static bool LocalizedAudioDefault(string key, Action<AudioClip> callback)
        {
            InitDefault();

            return DEFAULT.LocalizedAudio(key, callback);
        }

        public static string LocalizeDefault(string input, bool @short = false)
        {
            InitDefault();

            return DEFAULT.Localize(input, @short);
        }

        public static string Format(string input, params string[] values)
        {
            InitDefault();

            if (values.Length > 0)
                try
                {
                    return string.Format(DEFAULT.Localize(input, false), values);
                }
                catch { };

            return DEFAULT.Localize(input, false);
        }

        public bool LocalizedAudio(string key, Action<AudioClip> callback)
        {
            key = Localize(key);

            ResourceRequest resourceRequest = Resources.LoadAsync<AudioClip>(key);

            resourceRequest.completed += (asyncOp) => callback((AudioClip)resourceRequest.asset);

            return resourceRequest != null;
        }

        public string Localize(string input, bool @short = false)
        {
            if (input == null) return null;

            MatchCollection matches = keyRegex.Matches(input);
            StringBuilder output = new StringBuilder();
            int i = 0;

            foreach (Match match in matches)
            {
                if (i < match.Index) output.Append(input.Substring(i, match.Index - i));

                string key = input.Substring(match.Index + 1, match.Length - 1);

                LocalizationEntry entry;

                if ((PATCH != null && PATCH.dictionary.TryGetValue(key, out entry)) || dictionary.TryGetValue(key, out entry))
                {
                    output.Append(@short && entry.@short != null ? entry.@short : entry.value);
                }
                else
                {
                    output.Append(key);
                }

                i = match.Index + match.Length;
            }
            if (i < input.Length) output.Append(input.Substring(i, input.Length - i));

            return output.ToString();
        }

        private void InitDictionary()
        {
            if (this.dictionary == null) this.dictionary = new Dictionary<string, LocalizationEntry>();
            foreach (var entry in entries)
            {
                this.dictionary[entry.key] = entry;
            }
        }
    }
}