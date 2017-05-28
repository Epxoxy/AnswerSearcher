using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.OleDb;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string subjectPattern = "<div.+?><span.+?class=\"lb.+?>.+?\\.([\\s\\S]+?)<.+?<table.+?>([\\s\\S]+?.+?)<\\/table>";
        private string optionPattern = "<input.+?id=\"(.+?)\".+?value=\"(.+?)\"><label.+?>(.+?)<";
        private string rightPattern = "<div.+?><span.+?class=\"lb.+?>.+?\\.([\\s\\S]+?)<.+?<table.+?>([\\s\\S]+?.+?)<\\/table>[\\s\\S]+?正确答案为:(.+?)<";
        private string jsFormat = "var {0} = document.getElementById(\"{1}\");{0}.checked = true;";
        private string jsFormat1 = "var found = new Array({0});\nfor(int i=0;i<found.length;i++){{\nfound[i].checked = true;\n}}";
        private string store = Environment.CurrentDirectory + "\\subjects.txt";
        private string subjectBankText;
        private List<SubjectPlus> subjectSrc;
        private List<SubjectPlus> lastFound;
        private List<Subject> subjectBank;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += onThisLoaded;
        }

        private void onThisLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= onThisLoaded;
            subjectPtnBox.Text = subjectPattern;
            optionPtnBox.Text = optionPattern;
            loadDefBtnClick(this, null);
        }

        private void openBtnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                subjectBankText = new StreamReader(dialog.FileName).ReadToEnd();
                subjectBank = readSubjects(subjectBankText);
                bankCount.Text = subjectBank.Count.ToString();
            }
        }

        private void loadDefBtnClick(object sender, RoutedEventArgs e)
        {
            subjectBank = readSubjects(Properties.Resources.subjects);
            subjectBankText = Properties.Resources.subjects;
            FileInfo info = new FileInfo(store);
            if (info.Exists)
            {
                string othersBankText = info.OpenText().ReadToEnd();
                subjectBankText += othersBankText;
                var otherBank = readSubjects(othersBankText);
                subjectBank.AddRange(otherBank);
            }
            bankCount.Text = subjectBank.Count.ToString();
        }

        private void rInputBtnClick(object sender, RoutedEventArgs e)
        {
            subjectSrc = readRegexInputText();
            subjCount.Text  = subjectSrc.Count.ToString();
        }

        private void runBtnClick(object sender, RoutedEventArgs e)
        {
            string js = fitAnswerGenJS(subjectSrc, subjectBank, out lastFound);
            rs.Text = js;
            System.Diagnostics.Debug.WriteLine(js);
        }

        private double findSimilarity(List<Subject> src, Subject dst)
        {
            var computer = new SimilarityComputer();
            double max = 0d;
            foreach (var subject in src)
            {
                double similarity = 0;
                computer.SimilarText(dst.HoleText, subject.HoleText, out similarity);
                if (similarity > max)
                    max = similarity;
            }
            return max;
        }

        private void addBankBtnClick(object sender, RoutedEventArgs e)
        {
            var newSubjects = readRightSubject(correctBox.Text);
            var remainSubjects = new List<Subject>();
            var computer = new SimilarityComputer();
            if(subjectSrc != null)
            {
                for (int i = 0; i < subjectSrc.Count; i++)
                {
                    var subj = subjectSrc[i];
                    if (lastFound.Contains(subj)) continue;
                    double max = findSimilarity(newSubjects, subj);
                    if (max >= 70) continue;
                    max = findSimilarity(subjectBank, subj);
                    if (max >= 70) continue;
                    subj.HoleText += "A";
                    subj.Answer = "A";
                    remainSubjects.Add(subj);
                }
            }
            System.Diagnostics.Debug.WriteLine("Remain "+remainSubjects.Count);
            System.Diagnostics.Debug.WriteLine("New "+newSubjects.Count);
            var builder = new StringBuilder();
            if (newSubjects.Count > 0)
                for (int i = 0; i < newSubjects.Count; i++)
                    builder.AppendLine(newSubjects[i].HoleText);
            if (remainSubjects.Count > 0)
                for (int i = 0; i < newSubjects.Count; i++)
                    builder.AppendLine(newSubjects[i].HoleText);
            if(builder.Length > 0)
                saveToFile(store, builder.ToString());
        }


        private List<Subject> readRightSubject(string input)
        {
            var subjects = new List<Subject>();
            var rRight = new Regex(rightPattern);
            var rOption = new Regex(optionPattern);
            var matches = rRight.Matches(input);
            char[] options = { 'A', 'B', 'C', 'D', 'E', 'F', 'G' };
            char sp = ':';
            foreach(Match match in matches)
            {
                string question = match.Groups[1].Value;
                string answer = match.Groups[3].Value;
                var matches2 = rOption.Matches(match.Groups[2].Value);
                var builder = new StringBuilder();
                for (int i = 0; i< matches2.Count; i++)
                {
                    string value = matches2[i].Groups[3].Value.Replace(" ", "");
                    if(value[0] != options[i])
                        builder.Append(options[i]);
                    if (value[1] != sp)
                        builder.Append(sp);
                    builder.Append(value);
                    builder.Append(",");
                }
                builder.Replace("&nbsp;", "").Replace(" ", "");
                subjects.Add(new Subject(question, builder.ToString(), answer)
                {
                    HoleText = question + builder.ToString() + answer
                });
            }
            return subjects;
        }

        private List<Subject> readSubjects(string input)
        {
            var r = new Regex("(.+?)[\u3002\uff1f?].+?([A-Za-z].+)");
            var matches = r.Matches(input);
            var subjects = new List<Subject>();
            foreach (Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    string question = match.Groups[1].Value;
                    string opans = match.Groups[2].Value
                        .Replace("\r", "")
                        .Replace("\n", "")
                        .Replace("\t", "");
                    string answer = opans[opans.Length - 1].ToString();
                    string option = opans.Remove(opans.Length - 1, 1);
                    subjects.Add(new Subject(question, option, answer)
                    {
                        HoleText = match.Groups[0].Value
                    });
                }
            }
            return subjects;
        }

        private List<SubjectPlus> readRegexInputText()
        {
            var subjectPlusList = new List<SubjectPlus>();
            string input = inputBox.Text;
            var subjectRegex = new Regex(subjectPtnBox.Text);
            var optionRegex = new Regex(optionPtnBox.Text);
            var matches = subjectRegex.Matches(input);
            foreach (Match match in matches)
            {
                var builder = new StringBuilder();
                string question = match.Groups[1].Value;
                string options = match.Groups[2].Value;
                //
                builder.Append(question);
                //
                var subPlus = new SubjectPlus(question, options)
                {
                    OptionsList = new List<Option>()
                };
                var subMatches = optionRegex.Matches(options);
                foreach (Match subMatch in subMatches)
                {
                    subPlus.OptionsList.Add(new Option
                    {
                        Mark = subMatch.Groups[2].Value,
                        Id = subMatch.Groups[1].Value,
                        Text = subMatch.Groups[3].Value
                    });
                    builder.Append(subMatch.Groups[2].Value);
                    builder.Append(",");
                    builder.Append(subMatch.Groups[3].Value);
                }
                subPlus.HoleText = builder.ToString();
                subjectPlusList.Add(subPlus);
            }
            return subjectPlusList;
        }

        private string fitAnswerGenJS(List<SubjectPlus> questionSrc, List<Subject> answerSrc, out List<SubjectPlus> found)
        {
            found = null;
            //TODO Alert message
            if (questionSrc == null || answerSrc == null)
                return string.Empty;
            if (questionSrc.Count == 0 || answerSrc.Count == 0)
                return string.Empty;
            var builder = new StringBuilder();
            var computer = new SimilarityComputer();
            string question = string.Empty;
            string holeText = string.Empty;
            found = new List<SubjectPlus>();
            for (int i = 0; i < questionSrc.Count; i++)
            {
                var plus = questionSrc[i];
                question = plus.Question;
                holeText = plus.HoleText;
                int index = subjectBankText.IndexOf(question);
                string answer = string.Empty;
                if (index > 0)
                {
                    for(int x = index; x < subjectBankText.Length; x++)
                    {
                        if(subjectBankText[x] == '\r' || subjectBankText[x] == '\n')
                        {
                            answer = subjectBankText[x - 1].ToString();
                            break;
                        }
                    }
                }else
                {
                    int foundIndex = -1;
                    double maxSimilarity = 70;
                    for (int j = 0; j < answerSrc.Count; j++)
                    {
                        if (holeText.Length - answerSrc[j].HoleText.Length > 10) continue;
                        double similarity = 0;
                        computer.SimilarText(holeText, answerSrc[j].HoleText, out similarity);
                        if (similarity > maxSimilarity)
                        {
                            foundIndex = j;
                            maxSimilarity = similarity;
                        }
                    }
                    if (foundIndex == -1) continue;
                    answer = answerSrc[foundIndex].Answer;
                }

                if (!string.IsNullOrEmpty(answer))
                {
                    for (int k = 0; k < plus.OptionsList.Count; k++)
                    {
                        if (plus.OptionsList[k].Mark == answer)
                        {
                            var option = plus.OptionsList[k];
                            //builder.AppendLine($"\"{option.Id}\",");
                            builder.AppendLine(string.Format(jsFormat, "radio_" + i, option.Id));
                            found.Add(plus);
                            break;
                        }
                    }
                }
            }
            string objs = builder.ToString();
            return objs;
            //return string.Format(jsFormat1, objs.Remove(objs.Length - 1));
        }

        private void saveToFile(string path, string text)
        {
            StreamWriter writer = null;
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                writer = new StreamWriter(fileInfo.Open(FileMode.OpenOrCreate));
                writer.BaseStream.Seek(0, SeekOrigin.End);
                writer.WriteLine(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                if (writer != null)
                    writer.Dispose();
            }
        }

    }

    struct Option
    {
        public string Mark { get; set; }
        public string Text { get; set; }
        public string Id { get; set; }
    }

    class SubjectPlus : Subject
    {
        public List<Option> OptionsList { get; set; }

        public SubjectPlus(string question, string option)
        {
            Question = question;
            OptionsText = option;
        }
    }

    class Subject
    {
        public string Question { get; set; }
        public string OptionsText { get; set; }
        public string Answer { get; set; }
        public string HoleText { get; set; }

        public Subject() { }

        public Subject(string question, string option, string answer)
        {
            Question = question;
            OptionsText = option;
            Answer = answer;
        }
    }
}
