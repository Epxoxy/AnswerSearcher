using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Timers;
using System.Windows.Shapes;

namespace AnswerSearcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string subjectPAT = "<div.+?><span.+?class=\"lb.+?>.+?\\.([\\s\\S]+?)<.+?<table.+?>([\\s\\S]+?.+?)<\\/table>";
        private const string optionPAT = "<input.+?id=\"(.+?)\".+?value=\"(.+?)\"><label.+?>(.+?)<";
        private const string optionAnswerPAT = "<div.+?><span.+?class=\"lb.+?>.+?\\.([\\s\\S]+?)<.+?<table.+?>([\\s\\S]+?.+?)<\\/table>[\\s\\S]+?正确答案为:(.+?)<";
        private const string jsFormat = "var {0} = document.getElementById(\"{1}\");{0}.checked = true;";
        private const string jsFormatLite = "var found = new Array({0});\nfor(var i=0;i<found.length;i++){{\nfound[i].checked = true;\n}}";
        private const string jsCheckAll = "//for(var i=0;i<50;i++){document.getElementById(\"rbtl\"+i+\"_0\").checked=true;}\n";
        private string storeFileName = Environment.CurrentDirectory + "\\subjects.txt";
        private List<SubjectPlus> inputList;
        private List<SubjectPlus> foundList;
        private List<Subject> repertory;
        private string repertoryText;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += onThisLoaded;
        }


        private void onThisLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= onThisLoaded;
            subjectPATBox.Text = subjectPAT;
            optionPATBox.Text = optionPAT;
            loadRepertoryBtnClick(this, null);
        }

        private void openFileBtnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                repertoryText = new StreamReader(dialog.FileName).ReadToEnd();
                repertory = readSubjects(repertoryText);
                repertoryCount.Text = repertory.Count.ToString();
            }
        }

        private void loadRepertoryBtnClick(object sender, RoutedEventArgs e)
        {
            asyncDoing(() =>
            {
                repertory = readSubjects(Properties.Resources.subjects);
                repertoryText = Properties.Resources.subjects;
                FileInfo info = new FileInfo(storeFileName);
                if (info.Exists)
                {
                    string othersBankText = info.OpenText().ReadToEnd();
                    repertoryText += othersBankText;
                    var otherBank = readSubjects(othersBankText);
                    if (repertory == null)
                        repertory = otherBank;
                    else repertory.AddRange(otherBank);
                }
                Dispatcher.Invoke(() =>
                {
                    repertoryCount.Text = repertory.Count.ToString();
                });
            });
        }
        
        private void readInputBtnClick(object sender, RoutedEventArgs e)
        {
            asyncDoing(() =>
            {
                string subjectPAT = null, optionPAT = null, input = null;
                Dispatcher.Invoke(() =>
                {
                    subjectPAT = subjectPATBox.Text;
                    optionPAT = optionPATBox.Text;
                    input = inputBox.Text;
                });
                inputList = readInputText(subjectPAT, optionPAT, input);
                Dispatcher.Invoke(() =>
                {
                    inputCount.Text = inputList.Count.ToString();
                });
            });
        }

        private void runBtnClick(object sender, RoutedEventArgs e)
        {
            asyncDoing(() =>
            {
                string js = fitAnswerGenJS(inputList, repertory, out foundList);
                Dispatcher.Invoke(() =>
                {
                    rsBox.Text = jsCheckAll + js;
                });
            });
        }

        private void checkRepertoryBtnClick(object sender, RoutedEventArgs e)
        {
            var computer = new SimilarityComputer();
            char[] chars = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'a', 'b', 'c', 'd', 'e', 'f' };
            string holeText = string.Empty;
            var builder = new StringBuilder(repertoryText);
            for (int i = 0;i < repertory.Count; i++)
            {
                int index = 0;
                holeText = repertory[i].HoleText;
                //Check if anwer is not in option
                if (holeText.IndexOfAny(chars) > 0
                    && (index = holeText.IndexOf(repertory[i].Answer)) > 0)
                {
                    if (holeText.IndexOf(repertory[i].Answer, index + 1, holeText.Length - index - 1) < 0)
                    {
                        builder.Replace(holeText, "");
                        System.Diagnostics.Debug.WriteLine("******************");
                        System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine(repertory[i].HoleText);
                        //System.Diagnostics.Debug.WriteLine(subjectBank[j].HoleText);
                    }
                }
                /*for (int j = 0; j < subjectBank.Count; j++)
                {
                    if (i == j) continue;
                    double similarity = 0;
                    computer.SimilarText(subjectBank[i].HoleText, subjectBank[j].HoleText, out similarity);
                    if (similarity > 70)
                    {
                        System.Diagnostics.Debug.WriteLine("******************");
                        System.Diagnostics.Debug.WriteLine("");
                        System.Diagnostics.Debug.WriteLine(subjectBank[i].HoleText);
                        System.Diagnostics.Debug.WriteLine(subjectBank[j].HoleText);
                    }
                }*/
            }
            //saveToFile(Environment.CurrentDirectory + "\\subjectsCopy.txt", builder.ToString());
        }
        
        private void addRepertoryBtnClick(object sender, RoutedEventArgs e)
        {
            string mistakeText = mistakeBox.Text;
            asyncDoing(() =>
            {
                var newSubjects = readMistakesSubject(mistakeText);
                var remainSubjects = new List<Subject>();
                if (inputList != null)
                {
                    for (int i = 0; i < inputList.Count; i++)
                    {
                        Subject maxObj;
                        var subj = inputList[i];
                        if (foundList.Contains(subj)) continue;
                        double max = findSimilarity(newSubjects, subj, out maxObj);
                        if (max >= 70) { continue; }
                        max = findSimilarity(repertory, subj, out maxObj);
                        if (max >= 70) continue;
                        subj.HoleText += "A";
                        subj.Answer = "A";
                        remainSubjects.Add(subj);
                    }
                }
                System.Diagnostics.Debug.WriteLine("Remain " + remainSubjects.Count);
                System.Diagnostics.Debug.WriteLine("New " + newSubjects.Count);
                var builder = new StringBuilder();
                if (newSubjects.Count > 0)
                    for (int i = 0; i < newSubjects.Count; i++)
                        builder.AppendLine(newSubjects[i].HoleText);
                if (remainSubjects.Count > 0)
                    for (int i = 0; i < newSubjects.Count; i++)
                        builder.AppendLine(newSubjects[i].HoleText);
                if (builder.Length > 0)
                    saveToFile(storeFileName, builder.ToString());
            }, () =>
            {
                loadRepertoryBtnClick(this, null);
            });
        }
        
        private void clearBtnClick(object sender, RoutedEventArgs e)
        {
            inputBox.Text = string.Empty;
            rsBox.Text = string.Empty;
            mistakeBox.Text = string.Empty;
            inputCount.Text = "0";
            if (inputList != null)
                inputList.Clear();
            if(foundList != null)
                foundList.Clear();
        }


        private async void asyncDoing(Action action, Action continueAtion = null)
        {
            processBar.Visibility = Visibility.Visible;
            processBar.IsIndeterminate = true;
            var rectangle = new Rectangle
            {
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#11f4f4f4"))
            };
            using (var overlay = OverlayAdorner<UIElement>.Overlay(this.Content as UIElement, rectangle))
            {
                Keyboard.Focus(rectangle);
                await Task.Run(action);
                Keyboard.ClearFocus();
            }
            processBar.IsIndeterminate = false;
            processBar.Visibility = Visibility.Collapsed;
            continueAtion?.Invoke();
        }

        private double findSimilarity(List<Subject> src, Subject dst, out Subject maxObj)
        {
            maxObj = null;
            var computer = new SimilarityComputer();
            double max = 0d;
            foreach (var subject in src)
            {
                double similarity = 0;
                computer.SimilarText(dst.HoleText, subject.HoleText, out similarity);
                if (similarity > max)
                {
                    max = similarity;
                    maxObj = subject;
                }
            }
            return max;
        }

        private List<Subject> readMistakesSubject(string input)
        {
            var subjects = new List<Subject>();
            var rAnswer = new Regex(optionAnswerPAT);
            var rOption = new Regex(optionPAT);
            var matches = rAnswer.Matches(input);
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

        private List<SubjectPlus> readInputText(string subjectPAT, string optionPAT, string input)
        {
            var subjectPlusList = new List<SubjectPlus>();
            var subjectRegex = new Regex(subjectPAT);
            var optionRegex = new Regex(optionPAT);
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
                string answer = string.Empty;
                //
                int index = repertoryText.IndexOf(question);
                if(index > 0)
                {
                    var answers = new Dictionary<string, int>();
                    do
                    {
                        int answerIndex = repertoryText.IndexOfAny(new char[] { '\r', '\n' }, index);
                        if(answerIndex > 0)
                        {
                            answer = repertoryText[answerIndex - 1].ToString();
                            if (!answers.ContainsKey(answer))
                                answers.Add(answer, 0);
                            answers[answer] += 1;
                        }
                    } while ((index = repertoryText.IndexOf(question, index + 1)) >= 0);
                    //Find max for the same question of different answer
                    int max = 0;
                    foreach (var ans in answers)
                    {
                        if (ans.Value > max)
                        {
                            max = ans.Value;
                            answer = ans.Key;
                        }
                    }
                }
                else
                {
                    //Find the max similar if same question not found
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
