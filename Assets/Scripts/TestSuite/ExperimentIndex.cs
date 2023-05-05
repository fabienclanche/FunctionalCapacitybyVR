using StudyStore;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Utils;

namespace TestSuite
{
    /// <summary>
    /// Contains metadata about an experiment (including: study, subject & experiment IDs, date, list of test data files)
    /// </summary>
	[DataContract]
    public class ExperimentIndex
    { 
        /// <summary>
        /// Contains metadata about a single test in an experiment (including: recorded duration, location of indicators and mocap files)
        /// </summary>
        [DataContract]
        public class Entry
        {
            [DataMember(Order = 0)] public string name;
            [DataMember(Order = 1)] public string indicatorsFile;
            [DataMember(Order = 2)] public string mocapFile;
            [DataMember(Order = 3)] public float mocapLength;
        }

        [DataMember(Order = 0)] public string studyId;
        [DataMember(Order = 1)] public string subjectId;
        [DataMember(Order = 2)] public string experimentId;
        [DataMember(Order = 3)] public long timestamp = DateTime.Now.ToFileTime();
        [DataMember(Order = 4)] public bool readyForQuestionnaire;
        [DataMember(Order = 5)] public List<Entry> contents = new List<Entry>();

        /// <summary>
        /// The root directory for this experiment files
        /// </summary>
		public string RootDirectory => Config.OutputDirectory + "\\" + studyId + "\\" + subjectId + "\\" + experimentId + "\\";

        /// <summary>
        /// Returns a localized string representing this object
        /// </summary>
        /// <returns> a string representing this object</returns>
        public override string ToString()
        {
            return Localization.Format("$experimentIndex:timeAgo_creationDate::2", TimeAgo(), CreationDate());
        }

        /// <summary>
        /// Returns a localized string representing the argument experiment, in a manner similar to calling <code>ToString</code> on an <code>ExperimentIndex</code> object
        /// </summary>
        /// <param name="exp">an experiment to represent as a string</param>
        /// <returns>a localized string representing the argument experiment</returns>
        public static string ToString(Experiment exp)
        {
            return Localization.Format("$experimentIndex:timeAgo_creationDate::1", exp.timestamp);
        }

        /// <summary>
        /// Checks wether the argument object is an <code>ExperimentIndex</code> and that its experiment, study and subject IDs are equals to this object's.
        /// </summary>
        /// <param name="obj">an object to compare to this  <code>ExperimentIndex</code></param>
        /// <returns>true iff the argument object is an <code>ExperimentIndex</code> and that its experiment, study and subject IDs are equals to this object's</returns>
        public override bool Equals(object obj)
        {
            if (obj is ExperimentIndex)
            {
                var other = (ExperimentIndex)obj;
                return other.experimentId == this.experimentId && other.studyId == this.studyId && other.subjectId == this.subjectId;
            }
            return base.Equals(obj);
        }

        /// <summary>
        /// Returns a hashcode based on this experiments' id, the subject id and the study id
        /// </summary>
        /// <returns>a hashcode based on this experiments' id, the subject id and the study id</returns>
        public override int GetHashCode()
        {
            return this.experimentId.GetHashCode() ^ this.subjectId.GetHashCode() ^ this.studyId.GetHashCode();
        }

        /// <summary>
        /// Returns a localized string with the date and hour of creation of this experiment
        /// </summary>
        /// <returns>a localized string with the date and hour of creation of this experiment</returns>
        public string CreationDate()
        {
            var creationDate = DateTime.FromFileTime(timestamp);
            return Localization.Format("$unit:YMDHMdate::5", creationDate.Year + "", (creationDate.Month + "").PadLeft(2, '0'), (creationDate.Day + "").PadLeft(2, '0'), (creationDate.Hour + "").PadLeft(2, '0'), (creationDate.Minute + "").PadLeft(2, '0'));
        }

        /// <summary>
        /// Returns a localized string representing the time elapsed since the creation of this experiment. 
        /// The time elapsed will be displayed in seconds, minutes, hours or days, whichever is more fitting to the amount of time elapsed.
        /// </summary>
        /// <returns>a localized string representing the time elapsed since the creation of this experiment</returns>
        public string TimeAgo()
        {
            var creationDate = DateTime.FromFileTime(timestamp);
            var timeSpan = DateTime.Now - creationDate;

            long t = (long)timeSpan.TotalSeconds;
            if (t < 60) return Localization.Format(t + " $unit:seconds");

            t = (long)timeSpan.TotalMinutes;
            if (t < 60) return Localization.Format(t + " $unit:minutes");

            t = (long)timeSpan.TotalHours;
            if (t < 24) return Localization.Format(t + " $unit:hours");

            t = (long)timeSpan.TotalDays;
            return Localization.Format(t + " $unit:days");
        }

        /// <summary>
        /// Returns a deep copy of this experiment index, creating a copy for each test entry
        /// </summary>
        /// <returns>a deep copy of this experiment index</returns>
        public ExperimentIndex Clone()
        {
            var original = this;

            var copy = new ExperimentIndex()
            {
                experimentId = original.experimentId,
                studyId = original.studyId,
                subjectId = original.subjectId,
                timestamp = original.timestamp,
                contents = new List<Entry>()
            };


            for (int i = 0; i < original.contents.Count; i++)
            {
                if (original.contents[i] == null) copy.contents.Add(null);
                else
                {
                    copy.contents.Add(new Entry()
                    {
                        name = original.contents[i].name,
                        mocapLength = original.contents[i].mocapLength,
                        mocapFile = original.contents[i].mocapFile,
                        indicatorsFile = original.contents[i].indicatorsFile
                    });
                }
            }

            return copy;
        }
    }
}