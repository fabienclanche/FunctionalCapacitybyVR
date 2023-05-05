using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Utils;
using System.IO;
using System.Collections;
using StudyStore;

namespace TestSuite.UI
{
    public class DataViewUI : View
    {
        private static readonly int ENTRY_HEIGHT = 22, HEADER_HEIGHT = 32;

        [Serializable]
        public abstract class Tab
        {
            public List<Category> formattedData;
            public int height;

            public abstract List<Test.TestData> Columns { get; }
            public abstract string TabName { get; }

            public void ComputeHeight(Predicate<Entry> filter)
            {
                height = 0;

                foreach (var cat in GetCategoriesRecursive())
                {
                    height += HEADER_HEIGHT + 1;
                    foreach (var entry in cat.entries) if (filter(entry)) height += ENTRY_HEIGHT + 1;
                }
            }

            public List<Category> GetCategoriesRecursive()
            {
                if (formattedData == null) return new List<Category>();

                List<Category> categories = new List<Category>(formattedData);
                for (int i = 0; i < categories.Count; i++) if (categories[i].children != null) categories.AddRange(categories[i].children);
                return categories;
            }
        }

        [Serializable]
        public class TestTab : Tab
        {
            public Test.TestData data;

            public List<Test.TestData> steps = new List<Test.TestData>();

            public override List<Test.TestData> Columns => steps;
            public override string TabName => data.metadata?.label ?? data.step;
        }

        [Serializable]
        public class OverviewTab : Tab
        {
            public List<Test.TestData> dataList;

            public override List<Test.TestData> Columns => dataList;
            public override string TabName => "$ui:dataOverview";
        }

        [Serializable]
        public class Entry
        {
            public int importance = int.MinValue;
            public string label;
            public string aggregation;
            public List<Test.TestData> steps = new List<Test.TestData>();
            public List<IndicatorField> fields = new List<IndicatorField>();
            public List<string> values;

            public string CategoryID => (fields.Count > 0) ? fields[0].CategoryID : null;
        }

        [Serializable]
        public class Category
        {
            public Category parent = null;
            public string label, id;
            public List<Category> children = new List<Category>();
            public List<Entry> entries = new List<Entry>();

            public Entry GetEntry(string label)
            {
                return entries.Find(e => e.label == label);
            }
        }

        public ReplayUI replayUI;

        public string defaultDirectory = "";

        public List<TestTab> tabs;
        public OverviewTab overviewTab;

        private Vector2 testTabScroll, overviewScroll;

        public int selectedTab = -1;
        public int overviewLOD = 1, lod = 1;
        public HashSet<string> collapsedCategories = new HashSet<string>();

        public override bool HidesCameraView => true;
        public override bool ShowBackgroundScreen => true;

        public void Start()
        {
            if (defaultDirectory != null && defaultDirectory.Length > 0)
            {
                defaultDirectory = JSONSerializer.Path(defaultDirectory);

                if (Directory.Exists(defaultDirectory))
                {
                    string indexPath = defaultDirectory + "/index.json";
                    ExperimentIndex eIndex = JSONSerializer.FromJSONFile<ExperimentIndex>(indexPath);
                    SetData(eIndex, true);
                }
            }
        }

        public void SetData(ExperimentIndex eIndex, bool fromLocal)
        {
            StartCoroutine(LoadDataCoroutine(eIndex, fromLocal));

            // replay UI data
            if (replayUI)
            {
                if (fromLocal) replayUI.InitFromLocalFiles(eIndex);
                else replayUI.InitRemote(eIndex);
            }
        }

        private IEnumerator LoadDataCoroutine(ExperimentIndex eIndex, bool fromLocal)
        {
            List<Test.TestData> testData = new List<Test.TestData>();

            for (int i = 0; i < eIndex.contents.Count; i++)
            {
                if (eIndex.contents[i] == null) continue;

                if (fromLocal)
                {
                    var data = JSONSerializer.FromJSONFile<Test.TestData>(eIndex.RootDirectory + eIndex.contents[i].indicatorsFile);
                    testData.Add(data);
                }
                else
                {
                    bool cont = false;
                    StreamReader reader = null;

                    API.Instance.ReadFile(eIndex, eIndex.contents[i].indicatorsFile, sr => { reader = sr; cont = true; }, err => { cont = true; });
                    yield return new WaitUntil(() => cont);

                    if (reader == null) break;

                    var data = JSONSerializer.FromJSON<Test.TestData>(reader.ReadToEnd());
                    testData.Add(data);
                }

                if(!fromLocal) yield return new WaitForSecondsRealtime(0.1f);
            }

            SetData(testData, eIndex.subjectId == API.Instance.CurrentSubject?.uuid ? API.Instance.CurrentSubject?.anonymizationId : "", eIndex.CreationDate());
        }

        public void SetData(List<Test.TestData> testData, string subjectId, string dateString)
        {
            overviewTab = new OverviewTab();
            overviewTab.dataList = new List<Test.TestData>();

            this.tabs = new List<TestTab>();
            for (int i = 0; i < testData.Count; i++)
            {
                TestTab tab = new TestTab();
                tab.data = testData[i];
                this.tabs.Add(tab);

                QuerySingleTestData(tab);
            }

            QueryOverviewData(overviewTab, subjectId, dateString);
        }

        private Tab GetTab(int i)
        {
            return (i == -1) ? (Tab)overviewTab : tabs[i];
        }

        public void ComputeAllTabsHeight()
        {
            for (int i = -1; i < tabs.Count; i++)
            {
                var tab = GetTab(i);
                tab.ComputeHeight(e => EntryFilter(tab, e));
            }
        }

        private bool EntryFilter(Tab tab, Entry entry)
        {
            int lod = tab is OverviewTab ? this.overviewLOD : this.lod;

            return (entry.importance >= lod) && !collapsedCategories.Contains(entry.CategoryID);
        }

        private void OnGUICategory(Tab tab, Category category, ref Vector2 position, float maxWidth, int depth = 0)
        {
            float offset = 5 * (1 + depth);
            GUI.Label(new Rect(position + Vector2.right * offset, new Vector2(maxWidth / 2 - 10 - offset, HEADER_HEIGHT)), Localization.Format("<b>" + category.label + "</b>"));

            position.y += HEADER_HEIGHT + 1;

            foreach (var entry in category.entries) if (EntryFilter(tab, entry)) OnGUIEntry(entry, ref position, maxWidth, depth);
            foreach (var childCategory in category.children) OnGUICategory(tab, childCategory, ref position, maxWidth, depth + 1);
        }

        private void OnGUIEntry(Entry entry, ref Vector2 position, float maxWidth, int depth = 0)
        {
            float offset = 5 * (1 + depth);
            GUI.Box(new Rect(position + Vector2.right * offset, new Vector2(maxWidth / 2 - 10 - offset, ENTRY_HEIGHT)), Localization.Format(entry.label));

            float valWidth = (maxWidth / 2 - 10) / entry.values.Count;

            for (int i = 0; i < entry.values.Count; i++)
            {
                var value = Localization.Format(entry.values[i]);

                if (i == entry.values.Count - 1)
                    GUI.Label(new Rect(position + (maxWidth / 2 + valWidth * i) * Vector2.right, new Vector2(valWidth, ENTRY_HEIGHT)), "<b>" + value + "</b>");
                else
                    GUI.Label(new Rect(position + (maxWidth / 2 + valWidth * i) * Vector2.right, new Vector2(valWidth, ENTRY_HEIGHT)), value);
            }

            position.y += ENTRY_HEIGHT + 1;
        }

        private void OnGUITabHeader(Tab tab, Vector2 position, float maxWidth)
        {
            float valWidth = (maxWidth / 2 - 10) / tab.Columns.Count;

            GUI.Box(new Rect(position, new Vector2(maxWidth, ENTRY_HEIGHT)), "");
            GUI.Box(new Rect(position, new Vector2(maxWidth, ENTRY_HEIGHT)), "");
            GUI.Label(new Rect(position, new Vector2(maxWidth, ENTRY_HEIGHT)), "\t<b>" + Localization.Format("$ui:indicators") + "</b>");

            for (int i = 0; i < tab.Columns.Count; i++)
            {
                string label = tab.Columns[i].metadata?.label ?? tab.Columns[i].step;

                if (tab is TestTab && i == tab.Columns.Count - 1)
                {
                    label = "$ui:aggregatedIndicators";
                    if (tab.Columns.Count == 1) label = "";
                }

                GUI.Label(new Rect(position + (maxWidth / 2 + valWidth * i) * Vector2.right, new Vector2(valWidth, ENTRY_HEIGHT)), Localization.LocalizeDefault("<b>" + label + "</b>", @short: true));
            }
        }

        public override void OnMadeVisible()
        {
            base.OnMadeVisible();
            this.selectedTab = -1;
        }

        public override void OnViewGUI(Vector2 screen)
        {
            GUI.Box(new Rect(Vector2.one * 5, screen - Vector2.one * 10), Localization.Format("<b>$ui:dataOverviewWindow</b>"), "window");

            // tabs
            for (int i = -1; i < tabs.Count; i++)
            {
                string label = GetTab(i).TabName;

                if (i == selectedTab) GUI.color = Color.cyan;
                else GUI.color = Color.gray;

                if (GUI.Button(new Rect(new Vector2(110 + 100 * i, 30), new Vector2(100, 20)), Localization.LocalizeDefault(label, @short: true)))
                    selectedTab = i;

                GUI.color = Color.white;
            }

            // form scroll view
            Rect viewportRect = new Rect(new Vector2(5, 50), screen - new Vector2(20, 100));
            MainUI.LightBox(viewportRect);

            Tab tab = GetTab(selectedTab) ?? overviewTab;
            if (tab.height == 0) ComputeAllTabsHeight();

            Rect contentRect = new Rect(0, 0, viewportRect.width - 15, Mathf.Max(viewportRect.height, tab.height + 32));

            if (tab is TestTab) testTabScroll = GUI.BeginScrollView(viewportRect, testTabScroll, contentRect);
            else overviewScroll = GUI.BeginScrollView(viewportRect, overviewScroll, contentRect);

            Vector2 position = new Vector2(0, 32);
            if (tab.formattedData != null) foreach (var cat in tab.formattedData) OnGUICategory(tab, cat, ref position, viewportRect.width - 15);

            GUI.EndScrollView();

            OnGUITabHeader(tab, new Vector2(5, 50), viewportRect.width - 15);

            // replay

            if (tabs.Count > 0 && replayUI)
                if (GUI.Button(new Rect(new Vector2(screen.x / 2 - 120, screen.y - 42), new Vector2(240, 30)),
                    new GUIContent(Localization.Format("$ui:replays"), replayUI.cameraIcon)))
                {
                    replayUI.SelectFile(0);
                    MainUI.PushView(replayUI);
                }
        }

        private void ListStepsWithFields(Test.TestData root, List<Test.TestData> list)
        {
            if (root.children != null) foreach (var child in root.children) ListStepsWithFields(child, list);
            if (root.fields != null && root.fields.Count > 0) list.Add(root);
        }

        private void ForEachField(Test.TestData data, Action<Test.TestData, IndicatorField> fieldConsumer)
        {
            if (data.children != null) foreach (var child in data.children) ForEachField(child, fieldConsumer);
            if (data.fields != null) foreach (var field in data.fields) fieldConsumer(data, field);
        }

        private string IndicatorValueToString(object value, Metadata fieldMetadata)
        {
            if (value is decimal) value = decimal.ToDouble((decimal)value);

            if (value == null) return null;
            else if (value is float || value is double)
            {
                double doubleValue = (double)value;
                string stringValue;

                if (fieldMetadata != null)
                {
                    if (fieldMetadata.decimalPlaces > 0) stringValue = doubleValue.ToString("F" + fieldMetadata.decimalPlaces);
                    else stringValue = doubleValue.ToString("F2");

                    if (fieldMetadata.unit != null) stringValue += fieldMetadata.unit;
                }
                else stringValue = doubleValue.ToString("F2");

                return stringValue;
            }
            else if (value is int || value is long)
            {
                if (fieldMetadata != null && fieldMetadata.unit != null) return value + fieldMetadata.unit;
                else return value.ToString();
            }
            else if (value is string) return (string)value;
            else if (value is bool) return (bool)value ? "$ui:checkmark" : "$ui:crossmark";
            else return value.ToString();
        }

        private Category GetOrCreateCategory(IndicatorField field, Dictionary<string, Category> categories)
        {
            string categoryId = "";
            Category parent = null;

            for (int i = 0; i < field.Categories.Length; i++)
            {
                if (categoryId == "") categoryId = field.Categories[i];
                else categoryId += "." + field.Categories[i];

                Category category;
                if (!categories.TryGetValue(categoryId, out category))
                {
                    category = new Category();
                    category.label = field.Categories[i];
                    category.id = field.CategoryID;
                    categories[categoryId] = category;

                    if (parent != null) parent?.children.Add(category);
                    category.parent = parent;
                }

                parent = category;
            }

            return parent;
        }

        private void MakeEntries(Test.TestData data, Dictionary<string, Entry> entries, Dictionary<string, Category> categories)
        {
            ForEachField(data, (step, field) =>
            {
                Entry entry;
                if (!entries.TryGetValue(field.FullName, out entry))
                {
                    entry = new Entry();
                    entries[field.FullName] = entry;
                    entry.label = Localization.LocalizeDefault(field.Name, @short: true);

                    Category category = GetOrCreateCategory(field, categories);
                    category.entries.Add(entry);
                }
                entry.steps.Add(step);
                entry.fields.Add(field);
                entry.importance = Mathf.Max(entry.importance, field.metadata?.importance ?? 0);
            });
        }

        /// <summary>
        /// Generates a test data tab
        /// </summary>
        /// <param name="tab"></param>
        private void QuerySingleTestData(TestTab tab)
        {
            tab.steps = new List<Test.TestData>();
            ListStepsWithFields(tab.data, tab.steps);

            Dictionary<string, Category> categories = new Dictionary<string, Category>();
            Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

            MakeEntries(tab.data, entries, categories);

            foreach (var entry in entries.Values) FormatEntry(entry, tab);

            tab.formattedData = new List<Category>(categories.Values.Where(cat => cat.parent == null));
        }

        /// <summary>
        /// Generates the overview tab
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="expSubjectId">Subject id</param>
        /// <param name="expDateString">Experiment date (as a string)</param>
        private void QueryOverviewData(OverviewTab tab, string expSubjectId, string expDateString)
        {
            // categories found in each tab; grouped by id
            Dictionary<string, List<Category>> catsInAllTabs = new Dictionary<string, List<Category>>();

            int nonFormTabCount = 0;


            foreach (var t in this.tabs.Where(tb => !tb.data.isForm))
            {
                tab.dataList.Add(t.data);

                foreach (var cat in t.GetCategoriesRecursive())
                {
                    List<Category> cats;
                    if (!catsInAllTabs.TryGetValue(cat.id, out cats)) catsInAllTabs[cat.id] = cats = new List<Category>();
                    cats.Add(cat);
                }

                nonFormTabCount++;
            }

            // experiment metadata
            tab.formattedData = new List<Category>();
            var expData = new Category();
            expData.label = "";
            expData.id = "overview.experimentMetadata";

            var expSubject = new Entry();
            expSubject.importance = 1;
            expSubject.label = "$ui:expSubjectLabel : " + expSubjectId;
            expSubject.values = new List<string>();

            var expDate = new Entry();
            expDate.importance = 1;
            expDate.label = "$ui:expDateLabel : " + expDateString;
            expDate.values = new List<string>();

            expData.entries.Add(expSubject);
            expData.entries.Add(expDate);

            tab.formattedData.Add(expData);

            // find categories common to all tests
            if (nonFormTabCount == 0) return;

            foreach (var tabsCat in catsInAllTabs.Values)
            {
                if (tabsCat.Count == nonFormTabCount)
                {
                    var cat = tabsCat[0];
                }
            }
        }

        private void FormatEntry(Entry entry, Tab tab)
        {
            entry.values = new List<string>();
            for (int i = 0; i < tab.Columns.Count; i++) entry.values.Add(null);

            string aggregationMode = null;
            List<double> valuesToAggregate = new List<double>();
            List<double> aggregationWeights = new List<double>();

            for (int j = 0; j < entry.steps.Count; j++)
            {
                var step = entry.steps[j];
                var field = entry.fields[j];

                int i = tab.Columns.IndexOf(step);
                entry.values[i] = IndicatorValueToString(field.Value, field.metadata);

                if (field.metadata?.aggregation != null)
                {
                    aggregationMode = field.metadata.aggregation;

                    double? val = field.DoubleValue;
                    if (val != null)
                    {
                        valuesToAggregate.Add((double)val);
                        aggregationWeights.Add(1); // set default weight

                        if (field.metadata.aggregationWeightAttribute != null)
                        {
                            string weightFullName = field.metadata.aggregationWeightAttribute;
                            var weightField = step.Find(weightFullName);
                            aggregationWeights[aggregationWeights.Count - 1] = weightField?.DoubleValue ?? 1;
                        }
                    }
                }
            }

            // only aggregate for test tabs
            TestTab testTab = tab as TestTab;
            if (testTab == null) return;

            int rootIndex = testTab.steps.IndexOf(testTab.data);

            if (rootIndex != -1 && aggregationMode != null && aggregationMode != "none" && valuesToAggregate.Count > 0 && entry.values[rootIndex] == null)
            {
                double? aggregated = null;

                switch (aggregationMode)
                {
                    case "min":
                        aggregated = valuesToAggregate.Min();
                        break;
                    case "max":
                        aggregated = valuesToAggregate.Max();
                        break;
                    case "sum":
                        aggregated = valuesToAggregate.Sum();
                        Debug.LogWarning("ssuuum");
                        break;
                    case "avg":
                    case "average":
                        double sum = 0, weightSum = 0;
                        for (int i = 0; i < valuesToAggregate.Count; i++)
                        {
                            if (aggregationWeights[i] == 0) continue;
                            sum += valuesToAggregate[i] * aggregationWeights[i];
                            weightSum += aggregationWeights[i];
                        }
                        aggregated = weightSum != 0 ? sum / weightSum : 0;
                        break;
                }

                // check if value is an integer for proper display
                if (aggregated != null && (Math.Round((double)aggregated) == (double)aggregated))
                    entry.values[rootIndex] = IndicatorValueToString((long)Math.Round((double)aggregated), entry.fields[0].metadata);
                else
                    entry.values[rootIndex] = IndicatorValueToString(aggregated, entry.fields[0].metadata);

            }
        }
    }
}