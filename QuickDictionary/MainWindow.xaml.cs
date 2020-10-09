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
using System.Text.RegularExpressions;
using System.Net;
using MaterialDesignThemes.Wpf;
using HtmlAgilityPack;
using System.Security.Cryptography.X509Certificates;
using System.Collections.ObjectModel;
using Tesseract;
using System.IO;
using System.Drawing;
using System.Windows.Media.Animation;
using Squirrel;
using CefSharp.Handler;
using CefSharp;
using System.Threading;
using System.Xml.Serialization;
using MoreLinq.Extensions;
using System.Globalization;
using CefSharp.Wpf;

namespace QuickDictionary
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private KeyboardHook keyHook = new KeyboardHook();

        TesseractEngine engine = new TesseractEngine("data/tessdata", "eng", EngineMode.LstmOnly);
        bool engineBusy = true;

        string title;

        int updateProgress = 0;
        public int UpdateProgress
        {
            get
            {
                return updateProgress;
            }
            set
            {
                updateProgress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateProgress)));
            }
        }

        bool showNewWordPanel = false;
        public bool ShowNewWordPanel
        {
            get
            {
                return showNewWordPanel;
            }
            set
            {
                showNewWordPanel = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowNewWordPanel)));
            }
        }

        string newListName;
        public string NewListName
        {
            get
            {
                return newListName;
            }
            set
            {
                newListName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewListName)));
            }
        }

        PathWordListPair selectedWordList = null;
        public PathWordListPair SelectedWordList
        {
            get
            {
                return selectedWordList;
            }
            set
            {
                selectedWordList = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedWordList)));
            }
        }

        private string persistentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary");

        private bool stopSelectionUpdate = false;

        SemaphoreSlim updateFinished = new SemaphoreSlim(0, 1);

        public MainWindow()
        {
            InitializeComponent();
            Helper.HideBoundingBox(root);
        }

        List<OCRWord> OCRWords = new List<OCRWord>();
        Task ocrTask;

        private void startOCR()
        {
            if (engineBusy)
            {
                Storyboard sb = FindResource("shakeStoryboard") as Storyboard;
                sb.Begin(this, true);
                return;
            }
            OCROverlay overlay = new OCROverlay();
            overlay.WordSelected += Overlay_WordSelected;
            OCRWords.Clear();
            var screenshot = ScreenCapture.GetScreenshot();
            overlay.OCRWords = OCRWords;
            ocrTask = Task.Run(() =>
            {
                engineBusy = true;
                Pix tessImg;
                using (MemoryStream ms = new MemoryStream())
                {
                    screenshot.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    tessImg = Pix.LoadFromMemory(ms.ToArray());

                    ms.Seek(0, SeekOrigin.Begin);

                    Dispatcher.Invoke(() =>
                    {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = ms;
                        bitmapImage.EndInit();
                        overlay.SetBg(bitmapImage);
                    });
                }

                using (var page = engine.Process(tessImg, PageSegMode.SparseText))
                {
                    using (var iter = page.GetIterator())
                    {
                        iter.Begin();
                        OCRWords.Clear();
                        do
                        {
                            string word = iter.GetText(PageIteratorLevel.Word);

                            if (!string.IsNullOrWhiteSpace(word))
                                if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out Tesseract.Rect rect))
                                {
                                    if (Regex.IsMatch(word, "[a-zA-Z]"))
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            System.Windows.Point p1 = Helper.RealPixelsToWpf(this, new System.Windows.Point(rect.X1, rect.Y1));
                                            System.Windows.Point p2 = Helper.RealPixelsToWpf(this, new System.Windows.Point(rect.X2, rect.Y2));
                                            OCRWords.Add(new OCRWord()
                                            {
                                                Rect = new RectangleF((float)p1.X, (float)p1.Y, (float)(p2.X - p1.X), (float)(p2.Y - p1.Y)),
                                                Word = word,
                                            });
                                        });
                                    }
                                }
                        } while (iter.Next(PageIteratorLevel.Word));
                    }
                }
                engineBusy = false;
            });
            overlay.ShowDialog();
            Dispatcher.Invoke(() => progressLoading.Visibility = Visibility.Visible);
        }

        private void KeyHook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (e.Key == System.Windows.Forms.Keys.F)
            {
                this.Activate();
                Keyboard.Focus(txtWord);
                txtWord.Focus();
                txtWord.SelectAll();
            }
            else
            {
                startOCR();
            }
        }

        private async void Overlay_WordSelected(object sender, System.Windows.Point position)
        {
            await Task.WhenAll(ocrTask);
            if (position.X < 0 && position.Y < 0)
            {
                Dispatcher.Invoke(() => progressLoading.Visibility = Visibility.Hidden);
                return;
            }
            OCRWord word = OCROverlay.FindClosest(OCRWords, (float)position.X, (float)position.Y);
            if (word != null)
            {
                search(word.Word.Trim());
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<Dictionary> dictionaries = new ObservableCollection<Dictionary>();
        public static List<string> Adhosts = new List<string>();

        public Config Config;

        private void saveConfig()
        {
            using (FileStream fs = new FileStream(Path.Combine(persistentPath, "config.xml"), FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Config));
                serializer.Serialize(fs, Config);
            }
        }

        private void loadConfig()
        {
            string path = Path.Combine(persistentPath, "config.xml");
            if (File.Exists(path))
            {
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Config));
                    Config = (Config)serializer.Deserialize(fs);
                }
            }
            else
            {
                Config = new Config();
                Config.SelectedDictionaries = dictionaries.Select(x => x.Name).ToList();
            }
        }

        private async void Window_SourceInitialized(object sender, EventArgs e)
        {
            stopSelectionUpdate = true;

            loadConfig();

            await WordListManager.LoadAllLists();

            dictionaries.Add(Dictionary.CambridgeCE);
            dictionaries.Add(Dictionary.MedicalDictionary);
            dictionaries.Add(Dictionary.OxfordLearnersDict);
            dictionaries.Add(Dictionary.DictionaryCom);
            dictionaries.Add(Dictionary.GoogleDefinitions);
            listDictionaries.ItemsSource = dictionaries;
            listDictionaries.SelectedItems.Clear();
            foreach (string dict in Config.SelectedDictionaries)
            {
                Dictionary d = dictionaries.FirstOrDefault(x => x.Name == dict);
                if (d != null)
                    listDictionaries.SelectedItems.Add(d);
            }
            foreach (Dictionary dict in dictionaries)
            {
                dict.Precedence = listDictionaries.SelectedItems.IndexOf(dict) + 1;
            }
            SelectedWordList = WordListManager.WordLists.FirstOrDefault(x => x.WordList.Name == Config.LastWordListName);
            listWordListSelector.ItemsSource = WordListManager.WordLists;

            stopSelectionUpdate = false;

            checkTopmost.IsChecked = Config.AlwaysOnTop;
            checkPause.IsChecked = Config.PauseClipboard;

            keyHook.RegisterHotKey(ModifierKeys.Alt, System.Windows.Forms.Keys.F);
            keyHook.RegisterHotKey(ModifierKeys.Alt, System.Windows.Forms.Keys.G);
            keyHook.KeyPressed += KeyHook_KeyPressed;

            engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-' ");
            engine.SetVariable("tessedit_char_blacklist", "¢§+~»~`!@#$%^&*()_+={}[]|\\:\";<>?,./");

            if (!Directory.Exists(persistentPath))
            {
                Directory.CreateDirectory(persistentPath);
            }

            engineBusy = false;

            Title = "Quick Dictionary v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            title = Title;

            browser.RequestHandler = new AdBlockRequestHandler();

            await Helper.WaitUntil(() => browser.IsBrowserInitialized);

            // Initialize the clipboard now that we have a window source to use
            var windowClipboardManager = new ClipboardManager(this);
            windowClipboardManager.ClipboardChanged += ClipboardChanged;

            WebClient client = new WebClient();
            Stream stream = client.OpenRead("https://raw.githubusercontent.com/anudeepND/blacklist/master/adservers.txt");
            StreamReader reader = new StreamReader(stream);
            string content = reader.ReadToEnd();
            var matches = Regex.Matches(content, @"0\.0\.0\.0 (.+)");
            foreach (Match match in matches)
            {
                Adhosts.Add(match.Groups[1].Value);
            }

            try
            {
                using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/Henry-YSLin/QuickDictionary"))
                {
                    var updateInfo = await mgr.CheckForUpdate(false, (progress) =>
                    {
                        UpdateProgress = progress;
                        Dispatcher.Invoke(() => Title = title + $" - Checking {progress}%");
                    });
                    if (updateInfo.ReleasesToApply.Any())
                    {
                        var result = await mgr.UpdateApp((progress) =>
                        {
                            UpdateProgress = progress;
                            Dispatcher.Invoke(() => Title = title + $" - Updating {progress}%");
                        });
                        await Task.Delay(500);
                        Dispatcher.Invoke(() => Title = title + " - Restart app to update");
                    }
                    else
                    {
                        Dispatcher.Invoke(() => Title = title);
                    }
                }
            }
            catch (Exception ex)
            {
                App.LogException(ex, ex.Source);
            }
            updateFinished.Release();
        }

        private void ClipboardChanged(object sender, EventArgs e)
        {
            if (checkPause.IsChecked.GetValueOrDefault()) return;
            // Handle your clipboard update here, debug logging example:
            if (Clipboard.ContainsText())
            {
                string word = Clipboard.GetText().Trim();
                progressLoading.Visibility = Visibility.Visible;
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
                    if (validations.Count > 0)
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
                progressLoading.Visibility = Visibility.Visible;
                search(txtWord.Text);
            }
        }

        private void listDictionaries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (stopSelectionUpdate) return;
            foreach (Dictionary dict in dictionaries)
            {
                dict.Precedence = listDictionaries.SelectedItems.IndexOf(dict) + 1;
            }
            Config.SelectedDictionaries = listDictionaries.SelectedItems.Cast<Dictionary>().Select(x => x.Name).ToList();
            saveConfig();
        }

        private void browser_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            Dispatcher.Invoke(() => progressLoading.Visibility = e.IsLoading ? Visibility.Visible : Visibility.Hidden);
        }

        private void btnOCR_Click(object sender, RoutedEventArgs e)
        {
            startOCR();
        }

        public class AdBlockRequestHandler : RequestHandler
        {
            protected override bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
            {
                bool block = Adhosts.Exists(x => request.Url.Contains(x));
                if (block) Console.WriteLine("BLOCKED: " + request.Url);
                return block;
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
                scrollToolbar.LineRight();
            else
                scrollToolbar.LineLeft();

            e.Handled = true;
        }

        bool canExit = false;

        private async void mainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (canExit) return;
            if (updateFinished.CurrentCount <= 0)
            {
                // Still updating
                e.Cancel = true;
                gridApp.Visibility = Visibility.Collapsed;
                gridUpdate.Visibility = Visibility.Visible;
                await updateFinished.WaitAsync();
                canExit = true;
                Close();
            }
        }

        private void check_Checked(object sender, RoutedEventArgs e)
        {
            Config.PauseClipboard = checkPause.IsChecked.GetValueOrDefault();
            Config.AlwaysOnTop = checkTopmost.IsChecked.GetValueOrDefault();
            saveConfig();
        }

        string lastWordUrl = null;

        private async void btnNewWordPanel_Checked(object sender, RoutedEventArgs e)
        {
            //ShowNewWordPanel = btnNewWordPanel.IsChecked.GetValueOrDefault();
            if (!ShowNewWordPanel) return;
            if (!browser.IsBrowserInitialized)
            {
                Dispatcher.Invoke(() =>
                {
                    gridNewWordDetails.Show();
                    gridNewWordLoading.Hide();
                });
                return;
            }
            if (browser.Address == lastWordUrl)
            {
                Dispatcher.Invoke(() =>
                {
                    txtNewLink.Text = browser.Address;
                    gridNewWordDetails.Show();
                    gridNewWordLoading.Hide();
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    gridNewWordLoading.Show();
                    gridNewWordDetails.Hide();
                    btnNewWordPanel.IsEnabled = false;
                });

                var dict = dictionaries.FirstOrDefault(x => new Uri(x.Url).Host.Trim().ToLower() == new Uri(browser.Address).Host.Trim().ToLower());
                if (dict != null)
                {
                    string headword = null;
                    try
                    {
                        headword = await dict.GetWord(browser);
                    }
                    catch (Exception ex)
                    {
                        App.LogException(ex, ex.Source);
                    }
                    if (string.IsNullOrWhiteSpace(headword)) headword = txtWord.Text;
                    string desc = "";
                    try
                    {
                        desc = await dict.GetDescription(browser);
                    }
                    catch (Exception ex)
                    {
                        App.LogException(ex, ex.Source);
                    }
                    Dispatcher.Invoke(() =>
                    {
                        txtNewWord.Text = headword;
                        txtNewDesc.Text = desc;
                    });
                }

                Dispatcher.Invoke(() =>
                {
                    txtNewLink.Text = browser.Address;
                    gridNewWordDetails.Show();
                    gridNewWordLoading.Hide();
                    btnNewWordPanel.IsEnabled = true;
                });

            }
            lastWordUrl = browser.Address;
        }

        private void btnWordLists_Click(object sender, RoutedEventArgs e)
        {
            snackbarMain.MessageQueue.Enqueue("Coming soon!");
        }

        private void txtNewWord_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool valid = txtNewWord.Text.Length > 0 && SelectedWordList != null;
            btnAddWord.IsEnabled = valid;
            if (valid)
            {
                if (SelectedWordList.WordList.Entries.Select(x => x.Word.Trim().ToLower()).Contains(txtNewWord.Text.Trim().ToLower()))
                {
                    lblDuplicateWord.Show();
                }
                else
                {
                    lblDuplicateWord.Hide(false);
                }
            }
            else
            {
                lblDuplicateWord.Hide(false);
            }
        }

        private void btnNewListSave_Click(object sender, RoutedEventArgs e)
        {
            if (!WordlistNameValidationRule.ValidateWordlistName(txtNewListName.Text, CultureInfo.InvariantCulture).IsValid)
            {
                txtNewListName.Focus();
                Keyboard.Focus(txtNewListName);
                txtNewListName.SelectAll();
                return;
            }
            string listname = txtNewListName.Text;
            PathWordListPair wordlistPair = new PathWordListPair(Path.Combine(persistentPath, $"Word Lists\\{listname}.xml"), new WordList() { Name = listname, Created = DateTime.Now });
            WordListManager.WordLists.Add(wordlistPair);
            WordListManager.SaveList(wordlistPair);
            dialogHost.IsOpen = false;
        }

        private void btnNewList_Click(object sender, RoutedEventArgs e)
        {
            drawerHost.IsBottomDrawerOpen = true;
        }

        private void btnAddWord_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedWordList == null || string.IsNullOrWhiteSpace(txtNewWord.Text)) return;
            SelectedWordList.WordList.Entries.Add(new WordEntry() { Word = txtNewWord.Text, Description = txtNewDesc.Text, Created = DateTime.Now, LastModified = DateTime.Now, Url = txtNewLink.Text });
            WordListManager.SaveList(SelectedWordList);
            snackbarMain.MessageQueue.Enqueue($"{txtNewWord.Text} saved");
            ShowNewWordPanel = false;
            txtNewWord.Clear();
            txtNewLink.Clear();
            txtNewDesc.Clear();
        }

        private void listWordListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool valid = txtNewWord.Text.Length > 0 && SelectedWordList != null;
            btnAddWord.IsEnabled = valid;
            if (valid)
            {
                if (SelectedWordList.WordList.Entries.Select(x => x.Word.Trim().ToLower()).Contains(txtNewWord.Text.Trim().ToLower()))
                {
                    lblDuplicateWord.Show();
                }
                else
                {
                    lblDuplicateWord.Hide(false);
                }
            }
            else
            {
                lblDuplicateWord.Hide(false);
            }
            if (SelectedWordList != null)
                drawerHost.IsBottomDrawerOpen = false;
        }
    }

    public class WordlistNameValidationRule : ValidationRule
    {
        public static ValidationResult ValidateWordlistName(object value, CultureInfo cultureInfo)
        {
            string val = value as string;
            val = val.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(val))
                return new ValidationResult(false, "Name cannot be empty");
            if (WordListManager.WordLists.Select(x => x.WordList.Name.ToLower()).Contains(val))
                return new ValidationResult(false, "This list exists already");
            if (val.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return new ValidationResult(false, "Invalid character");
            return ValidationResult.ValidResult;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return ValidateWordlistName(value, cultureInfo);
        }
    }

    public class OCRWord
    {
        public RectangleF Rect;
        public string Word;
    }

    public class Dictionary : INotifyPropertyChanged
    {
        // Url to the dictionary with %s in place of the query
        public string Url { get; set; }

        // Function to validate a query given the query url
        public Func<string, Task<bool>> ValidateQuery { get; set; }

        public Func<ChromiumWebBrowser, Task<string>> GetWord { get; set; }

        public Func<ChromiumWebBrowser, Task<string>> GetDescription { get; set; }

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
            GetWord = async (browser) =>
            {
                var headword = await browser.GetInnerTextByXPath(@"//span[contains(@class,""headword"")]");
                if (!string.IsNullOrWhiteSpace(headword))
                    return headword;
                headword = Regex.Match(browser.Address, @"dictionary\.cambridge\.org\/dictionary\/[\w-_]+\/([\w_-]+)").Groups[1].Value;
                return headword;
            },
            GetDescription = async (browser) =>
            {
                return await browser.GetInnerTextByXPath(@"//div[contains(@class,""def ddef_d"")]");
            },
            Icon = PackIconKind.LetterCBox,
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
            GetWord = async (browser) =>
            {
                var headword = await browser.GetInnerTextByXPath(@"//h1[contains(@class,""hword"")]");
                if (!string.IsNullOrWhiteSpace(headword))
                    return headword;
                headword = Regex.Match(browser.Address, @"www\.merriam-webster\.com\/[\w-_]+\/([\w_-]+)").Groups[1].Value;
                return headword;
            },
            GetDescription = async (browser) =>
            {
                return await browser.GetInnerTextByXPath(@"//span[@class=""dtText""]");
            },
            Icon = PackIconKind.MedicalBag,
            Name = "Merriam-Webster Medical Dictionary",
        };

        public static Dictionary OxfordLearnersDict = new Dictionary()
        {
            Url = "https://www.oxfordlearnersdictionaries.com/search/english/?q=%s",
            ValidateQuery = async (url) =>
            {
                return !(await Helper.GetFinalRedirectAsync(url)).Contains("spellcheck");
            },
            GetWord = async (browser) =>
            {
                var headword = await browser.GetInnerTextByXPath(@"//h1[@class=""headword""]");
                if (!string.IsNullOrWhiteSpace(headword))
                    return headword;
                headword = await browser.GetInnerTextByXPath(@"//h2[@class=""h""]");
                if (!string.IsNullOrWhiteSpace(headword))
                    return headword;
                headword = Regex.Match(browser.Address, @"www\.oxfordlearnersdictionaries\.com\/definition\/[\w-_]+\/([\w_-]+)").Groups[1].Value;
                return headword;
            },
            GetDescription = async (browser) =>
            {
                return await browser.GetInnerTextByXPath(@"//span[@class=""def""]");
            },
            Icon = PackIconKind.LetterOBox,
            Name = "Oxford Advanced Learner's Dictionary",
        };

        public static Dictionary DictionaryCom = new Dictionary()
        {
            Url = "https://www.dictionary.com/browse/%s",
            ValidateQuery = async (url) =>
            {
                return !(await Helper.GetFinalRedirectAsync(url)).Contains("misspelling");
            },
            GetWord = async (browser) =>
            {
                var headword = await browser.GetInnerTextByXPath(@"//h1[@class=""css-1jzk4d9 e1rg2mtf8""]");
                if (!string.IsNullOrWhiteSpace(headword))
                    return headword;
                headword = Regex.Match(browser.Address, @"www\.dictionary\.com\/definition\/[\w-_]+\/([\w_-]+)").Groups[1].Value;
                return headword;
            },
            GetDescription = async (browser) =>
            {
                return await browser.GetInnerTextByXPath(@"//div[@class=""css-1ghs5zt e1q3nk1v3""]");
            },
            Icon = PackIconKind.LetterDBox,
            Name = "Dictionary.com",
        };

        public static Dictionary GoogleDefinitions = new Dictionary()
        {
            Url = "https://www.google.com/search?q=define+%s",
            ValidateQuery = async (url) =>
            {
                return true;
            },
            GetWord = async (browser) =>
            {
                return null;
            },
            GetDescription = async (browser) =>
            {
                return null;
            },
            Icon = PackIconKind.Google,
            Name = "Google Dictionary",
        };
    }

    public class DictionaryDragAndDropListBox : DragAndDropListBox<Dictionary> { }
}
