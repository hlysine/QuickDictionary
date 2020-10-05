using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Text.RegularExpressions;
using System.Net;
using MaterialDesignThemes.Wpf;
using HtmlAgilityPack;
using System.Security.Cryptography.X509Certificates;
using System.Collections.ObjectModel;

namespace QuickDictionary
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            dictionaries.Add(Dictionary.CambridgeCE);
            dictionaries.Add(Dictionary.MedicalDictionary);
            dictionaries.Add(Dictionary.GoogleDefinitions);
            listDictionaries.ItemsSource = dictionaries;
            listDictionaries.SelectAll();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<Dictionary> dictionaries = new ObservableCollection<Dictionary>();

        private async void Window_SourceInitialized(object sender, EventArgs e)
        {
            await Helper.WaitUntil(() => browser.IsBrowserInitialized);

            // Initialize the clipboard now that we have a window soruce to use
            var windowClipboardManager = new ClipboardManager(this);
            windowClipboardManager.ClipboardChanged += ClipboardChanged;
        }

        private void ClipboardChanged(object sender, EventArgs e)
        {
            if (checkPause.IsChecked.GetValueOrDefault()) return;
            // Handle your clipboard update here, debug logging example:
            if (Clipboard.ContainsText())
            {
                string word = Clipboard.GetText().Trim();
                search(word);
            }
        }

        async void search(string word)
        {
            if (word.Length < 100)
            {
                if (Regex.IsMatch(word, "[a-zA-Z]"))
                {
                    txtWord.Text = word;
                    List<Dictionary> dicts = listDictionaries.SelectedItems.Cast<Dictionary>().ToList();
                    List<Task<bool>> validations = new List<Task<bool>>();
                    foreach (Dictionary dict in dicts)
                    {
                        var task = dict.ValidateQuery(dict.Url.Replace("%s", WebUtility.UrlEncode(word)));
                        validations.Add(task);
                    }
                    await Task.WhenAny(validations.ToArray());
                    for (int i = 0; i < validations.Count; i++)
                    {
                        await Task.WhenAny(validations[i]);
                        if (validations[i].Result)
                        {
                            browser.Load(dicts[i].Url.Replace("%s", WebUtility.UrlEncode(word)));
                            return;
                        }
                    }
                    browser.Load("data:text/plain;base64,Tm8gcmVzdWx0cyBmb3VuZC4NClRyeSBlbmFibGluZyBtb3JlIGRpY3Rpb25hcmllcy4=");
                }
            }
        }

        private void txtWord_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                search(txtWord.Text);
            }
        }

        private void listDictionaries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (Dictionary dict in dictionaries)
            {
                dict.Precedence = listDictionaries.SelectedItems.IndexOf(dict) + 1;
            }
        }
    }

    public class Dictionary : INotifyPropertyChanged
    {
        // Url to the dictionary with %s in place of the query
        public string Url { get; set; }

        // Function to validate a query given the query url
        public Func<string, Task<bool>> ValidateQuery { get; set; }

        // Pack icon in toolbar
        public PackIconKind Icon { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }

        private string precedenceString;
        public string PrecedenceString
        {
            get
            {
                return precedence > 0 ? precedence.ToString() : "";
            }
        }

        private int precedence;
        public int Precedence
        {
            get
            {
                return precedence;
            }
            set
            {
                precedence = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Precedence)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PrecedenceString)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static Dictionary CambridgeCE = new Dictionary()
        {
            Url = "https://dictionary.cambridge.org/search/english-chinese-traditional/direct/?source=gadgets&q=%s",
            ValidateQuery = async (url) =>
            {
                return !(await Helper.GetFinalRedirectAsync(url)).Contains("spellcheck");
            },
            Icon = PackIconKind.Dictionary,
            Name = "Cambridge English-Chinese Dictionary",
        };

        public static Dictionary MedicalDictionary = new Dictionary()
        {
            Url = "https://www.merriam-webster.com/medical/%s",
            ValidateQuery = async (url) =>
            {
                var web = new HtmlWeb();
                var doc = await web.LoadFromWebAsync(url);
                if (web.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return false;
                }
                var failNode1 = doc.DocumentNode.SelectSingleNode(@"//p[contains(@class,""missing-query"")]");
                if (failNode1 != null) return false;
                var failNode2 = doc.DocumentNode.SelectSingleNode(@"//div[contains(@class,""no-spelling-suggestions"")]");
                if (failNode2 != null) return false;
                return true;
            },
            Icon = PackIconKind.MedicalBag,
            Name = "Merriam-Webster Medical Dictionary",
        };

        public static Dictionary GoogleDefinitions = new Dictionary()
        {
            Url = "https://www.google.com/search?q=define+%s",
            ValidateQuery = async (url) =>
            {
                return true;
            },
            Icon = PackIconKind.Google,
            Name = "Google Dictionary",
        };
    }

    public class DictionaryDragAndDropListBox : DragAndDropListBox<Dictionary> { }
}
