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
        private string subjectPattern = "<div.+?><span.+?class=\"lb.+?>.+?\\.([\\s\\S]+?)<br.+?<table.+?>([\\s\\S]+?.+?)<\\/table>";
        private string optionPattern = "<input.+?id=\"(.+?)\".+?value=\"(.+?)\"><label.+?>(.+?)<";
        private string jsFormat = "var {0} = document.getElementById(\"{1}\");{0}.checked = true;";
        private string jsFormat1 = "var found = new Array({0});\nfor(int i=0;i<found.length;i++){{\nfound[i].checked = true;\n}}";
        private string subjectBankText;
        private List<SubjectPlus> subjectSrc;
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
            bankCount.Text = subjectBank.Count.ToString();
        }

        private void rInputBtnClick(object sender, RoutedEventArgs e)
        {
            subjectSrc = readRegexInputText();
            subjCount.Text  = subjectSrc.Count.ToString();
        }

        private void runBtnClick(object sender, RoutedEventArgs e)
        {
            string js = fitAnswerGenJS(subjectSrc, subjectBank);
            rs.Text = js;
            System.Diagnostics.Debug.WriteLine(js);
        }

        
        private List<Subject> readSubjects(string input)
        {
            var r = new Regex("(.+?)\u3002.+?([A-Za-z].+)");
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

        private string fitAnswerGenJS(List<SubjectPlus> questionSrc, List<Subject> answerSrc)
        {
            //TODO Alert message
            if (questionSrc == null || answerSrc == null)
                return string.Empty;
            if (questionSrc.Count == 0 || answerSrc.Count == 0)
                return string.Empty;
            var builder = new StringBuilder();
            var computer = new SimilarityComputer();
            string question = string.Empty;
            string holeText = string.Empty;
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
                            break;
                        }
                    }
                }
            }
            string objs = builder.ToString();
            return objs;
            //return string.Format(jsFormat1, objs.Remove(objs.Length - 1));
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
                    builder.Append(subMatch.Groups[3].Value);
                }
                subPlus.HoleText = builder.ToString();
                subjectPlusList.Add(subPlus);
            }
            return subjectPlusList;
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
