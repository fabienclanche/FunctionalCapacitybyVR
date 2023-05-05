using System.Runtime.Serialization;
using TestSuite.UI.Form;
using Utils;

namespace TestSuite
{
    /// <summary>
    /// Stores the configuration of forms to display in a study.
    /// </summary>
    [DataContract]
    public class StudyConfig
    {
        // instance members  
        [DataMember] private GenericForm[] forms;
        [DataMember] private Localization[] localizations;

        public StudyConfig()
        {

        }

        /// <summary>
        /// Returns a copy of an array of generic forms, created from the study configuration.
        /// </summary>
        /// <returns> a copy of an array of generic forms, created from the study configuration</returns>
        public GenericForm[] MakeForms()
        {
            GenericForm[] clone = new GenericForm[forms?.Length ?? 0];
            for (int i = 0; i < clone.Length; i++) clone[i] = forms[i].Clone();
            return clone;
        }

        public Localization GetCustomLocalizationData(string lang)
        {
            if (this.localizations == null) return null;

            foreach (var loc in this.localizations)
            {
                if (lang == loc.ShortLabel)
                {
                    return loc;
                }
            }

            return null;
        }
    }
}