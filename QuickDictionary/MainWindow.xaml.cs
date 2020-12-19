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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace QuickDictionary
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private KeyboardHook keyHook = new KeyboardHook();

        private WordLists wordListWindow = null;

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

        private PathWordListPair selectedWordList;
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


        private ObservableCollection<DictionaryResultPair> dictionaryResults = new ObservableCollection<DictionaryResultPair>();
        public ObservableCollection<DictionaryResultPair> DictionaryResults
        {
            get
            {
                return dictionaryResults;
            }
            set
            {
                dictionaryResults = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DictionaryResults)));
            }
        }


        private bool switchDictionaryExpanded;
        public bool SwitchDictionaryExpanded
        {
            get
            {
                return switchDictionaryExpanded;
            }
            set
            {
                switchDictionaryExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SwitchDictionaryExpanded)));
            }
        }


        public static string PersistentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary");

        private bool stopSelectionUpdate = false;

        SemaphoreSlim updateFinished = new SemaphoreSlim(0, 1);

        public MainWindow()
        {
            InitializeComponent();
            Helper.HideBoundingBox(root);

            browser.RequestHandler = new AdBlockRequestHandler();
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
            Dispatcher.Invoke(() => progressLoading.Visibility = Visibility.Visible);
            overlay.ShowDialog();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private void Unminimize()
        {
            WindowInteropHelper winInterop = new WindowInteropHelper(this);
            SendMessage(winInterop.Handle, 0x0112, 0xF120, 0);
        }

        private void KeyHook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (e.Key == System.Windows.Forms.Keys.F)
            {
                Unminimize();
                Activate();
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

        public static ObservableCollection<Dictionary> Dictionaries = new ObservableCollection<Dictionary>();
        public static List<string> Adhosts = new List<string>();

        public static Config Config = new Config();

        private async void Window_SourceInitialized(object sender, EventArgs e)
        {
            stopSelectionUpdate = true;

            Keyboard.Focus(txtWord);
            txtWord.Focus();

            Config.LoadConfig();

            await WordListManager.LoadAllLists();

            Dictionaries.Add(Dictionary.CambridgeCE);
            Dictionaries.Add(Dictionary.MedicalDictionary);
            Dictionaries.Add(Dictionary.OxfordLearnersDict);
            Dictionaries.Add(Dictionary.DictionaryCom);
            Dictionaries.Add(Dictionary.Wikipedia);
            Dictionaries.Add(Dictionary.GoogleTranslate);
            Dictionaries.Add(Dictionary.GoogleDefinitions);
            listDictionaries.ItemsSource = Dictionaries;
            listDictionaries.SelectedItems.Clear();
            foreach (string dict in Config.SelectedDictionaries)
            {
                Dictionary d = Dictionaries.FirstOrDefault(x => x.Name == dict);
                if (d != null)
                    listDictionaries.SelectedItems.Add(d);
            }
            foreach (Dictionary dict in Dictionaries)
            {
                dict.Precedence = listDictionaries.SelectedItems.IndexOf(dict) + 1;
            }
            SelectedWordList = WordListManager.WordLists.FirstOrDefault(x => x.WordList.Name == Config.LastWordListName);
            listWordListSelector.ItemsSource = WordListManager.WordLists;
            DictionaryResults.Clear();
            listDictionaries.SelectedItems.Cast<Dictionary>().Select(x => new DictionaryResultPair() { Dictionary = x, HasEntry = false, ToolTip = null }).ForEach(x => DictionaryResults.Add(x));
            listSwitchDictionaries.ItemsSource = DictionaryResults;

            stopSelectionUpdate = false;

            checkTopmost.IsSelected = Config.AlwaysOnTop;
            checkPause.IsSelected = Config.PauseClipboard;

            keyHook.RegisterHotKey(ModifierKeys.Alt, System.Windows.Forms.Keys.F);
            keyHook.RegisterHotKey(ModifierKeys.Alt, System.Windows.Forms.Keys.G);
            keyHook.KeyPressed += KeyHook_KeyPressed;

            engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-' ");
            engine.SetVariable("tessedit_char_blacklist", "¢§+~»~`!@#$%^&*()_+={}[]|\\:\";<>?,./");

            if (!Directory.Exists(PersistentPath))
            {
                Directory.CreateDirectory(PersistentPath);
            }

            engineBusy = false;

            Title = "Quick Dictionary v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            title = Title;

            WebClient client = new WebClient();
            Stream stream = await client.OpenReadTaskAsync("https://raw.githubusercontent.com/anudeepND/blacklist/master/adservers.txt");
            StreamReader reader = new StreamReader(stream);
            string content = reader.ReadToEnd();
            var matches = Regex.Matches(content, @"0\.0\.0\.0 (.+)");
            foreach (Match match in matches)
            {
                Adhosts.Add(match.Groups[1].Value);
            }

            await Helper.WaitUntil(() => browser.IsBrowserInitialized);

            // Initialize the clipboard now that we have a window source to use
            var windowClipboardManager = new ClipboardManager(this);
            windowClipboardManager.ClipboardChanged += ClipboardChanged;

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
            if (checkPause.IsSelected) return;
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
                    if (!ShowNewWordPanel)
                    {
                        Keyboard.Focus(txtWord);
                        txtWord.Focus();
                        txtWord.SelectAll();
                    }
                    List<Dictionary> dicts = listDictionaries.SelectedItems.Cast<Dictionary>().ToList();
                    List<Task<bool>> validations = new List<Task<bool>>();
                    foreach (Dictionary dict in dicts)
                    {
                        var task = dict.ValidateQuery(dict.Url.Replace("%s", WebUtility.UrlEncode(word)), word);
                        validations.Add(task);
                    }
                    if (validations.Count > 0)
                        await Task.WhenAny(validations.ToArray());
                    bool done = false;
                    for (int i = 0; i < validations.Count; i++)
                    {
                        await Task.WhenAny(validations[i]);
                        var res = DictionaryResults.FirstOrDefault(x => x.Dictionary == dicts[i]);
                        bool validation = validations[i].Result;
                        if (res != null)
                        {
                            res.HasEntry = validation;
                            res.ToolTip = validation ? $"View \"{word}\" in {dicts[i].Name}" : $"No results of \"{word}\" in {dicts[i].Name}";
                            res.Url = dicts[i].Url.Replace("%s", WebUtility.UrlEncode(word));
                        }
                        if (validation && !done)
                        {
                            browser.Load(dicts[i].Url.Replace("%s", WebUtility.UrlEncode(word)));

                            stopSelectionUpdate = true;
                            listSwitchDictionaries.SelectedItem = res;
                            stopSelectionUpdate = false;

                            done = true;
                        }
                    }
                    if (!done)
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
            foreach (Dictionary dict in Dictionaries)
            {
                dict.Precedence = listDictionaries.SelectedItems.IndexOf(dict) + 1;
            }
            var selectedDicts = listDictionaries.SelectedItems.Cast<Dictionary>();
            Config.SelectedDictionaries = selectedDicts.Select(x => x.Name).ToList();
            List<DictionaryResultPair> results = new List<DictionaryResultPair>();
            results.AddRange(DictionaryResults);
            DictionaryResults.Clear();
            selectedDicts.Select(x => results.FirstOrDefault(y => y.Dictionary == x) ?? new DictionaryResultPair() { Dictionary = x, HasEntry = false, ToolTip = null }).ForEach(x => DictionaryResults.Add(x));
            Config.SaveConfig();
        }

        private async void browser_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            Dispatcher.Invoke(() => progressLoading.Visibility = e.IsLoading ? Visibility.Visible : Visibility.Hidden);
            if (e.IsLoading) return;
            string address = null;
            Dispatcher.Invoke(() => address = browser.Address);
            if (string.IsNullOrWhiteSpace(address)) return;
            Dictionary dict;
            if (Uri.TryCreate(address, UriKind.RelativeOrAbsolute, out Uri uri))
            {
                if (uri.IsAbsoluteUri)
                    dict = Dictionaries.FirstOrDefault(x => x.ValidateUrl(address).Result);
                else
                    dict = null;
            }
            else
                dict = null;
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
                if (!string.IsNullOrWhiteSpace(headword))
                    Dispatcher.Invoke(() =>
                    {
                        txtWord.Text = headword;

                        if (!ShowNewWordPanel && !(wordListWindow?.IsActive).GetValueOrDefault())
                        {
                            Keyboard.Focus(txtWord);
                            txtWord.Focus();
                            txtWord.SelectAll();
                        }
                    });
            }
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

            protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
            {
                bool block = Adhosts.Exists(x => request.Url.Contains(x));
                if (block) Console.WriteLine("BLOCKED: " + request.Url);
                if (block)
                    return new AdBlockResourceRequestHandler();
                else
                    return null;
            }
        }

        public class AdBlockResourceRequestHandler : ResourceRequestHandler
        {
            protected override CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
            {
                return CefReturnValue.Cancel;
            }
        }

        bool canExit = false;

        private async void mainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (canExit)
            {
                return;
            }
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

                Dictionary dict;
                if (Uri.TryCreate(browser.Address, UriKind.RelativeOrAbsolute, out Uri uri))
                {
                    if (uri.IsAbsoluteUri)
                        dict = Dictionaries.FirstOrDefault(x => x.ValidateUrl(browser.Address).Result);
                    else
                        dict = null;
                }
                else
                    dict = null;
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
            if (wordListWindow == null) wordListWindow = new WordLists(this);
            if (!Application.Current.Windows.OfType<WordLists>().Contains(wordListWindow))
            {
                wordListWindow = new WordLists(this);
            }
            wordListWindow.Show();
            wordListWindow.Activate();
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
            string path = Path.Combine(WordListManager.WordListPath, $"{listname}.xml");
            PathWordListPair wordlistPair = new PathWordListPair(path, new WordList() { Name = listname, Created = DateTime.Now });
            WordListManager.WordLists.Add(wordlistPair);
            WordListManager.SaveList(wordlistPair);
            if (WordListManager.DeletedPaths.Contains(path))
                WordListManager.DeletedPaths.Remove(path);
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

        private void check_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.PauseClipboard = checkPause.IsSelected;
            Config.AlwaysOnTop = checkTopmost.IsSelected;
            Config.SaveConfig();
        }

        public void NavigateWord(WordEntry word)
        {
            if (!browser.IsBrowserInitialized) return;
            progressLoading.Visibility = Visibility.Visible;
            if (!string.IsNullOrWhiteSpace(word.Url))
            {
                browser.Load(word.Url);
            }
            else
            {
                search(word.Word);
            }
            Unminimize();
            Activate();
            if (!ShowNewWordPanel)
            {
                Keyboard.Focus(txtWord);
                txtWord.Focus();
                txtWord.SelectAll();
            }
        }

        private void listSwitchDictionaries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (stopSelectionUpdate) return;
            if (listSwitchDictionaries.SelectedItem != null)
            {
                browser.Load((listSwitchDictionaries.SelectedItem as DictionaryResultPair).Url);
            }
        }

        private void btnSwitchDictionaryPanel_Click(object sender, RoutedEventArgs e)
        {
            SwitchDictionaryExpanded = !SwitchDictionaryExpanded;
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

        // Function to check if a URL belongs to this dictionary
        public Func<string, Task<bool>> ValidateUrl { get; set; }

        // Function to validate a query given the query url and query text
        public Func<string, string, Task<bool>> ValidateQuery { get; set; }

        public Func<ChromiumWebBrowser, Task<string>> GetWord { get; set; }

        public Func<ChromiumWebBrowser, Task<string>> GetDescription { get; set; }

        // Pack icon in toolbar
        public PackIconKind Icon { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }

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
            ValidateUrl = async (url) =>
            {
                return new Uri(url).Host.Trim().ToLower().Contains("cambridge.org");
            },
            ValidateQuery = async (url, word) =>
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
            ValidateUrl = async (url) =>
            {
                return new Uri(url).Host.Trim().ToLower().Contains("merriam-webster.com");
            },
            ValidateQuery = async (url, word) =>
            {
                return await Helper.GetFinalStatusCodeAsync(url) == HttpStatusCode.OK;
                //var web = new HtmlWeb();
                //var doc = await web.LoadFromWebAsync(url);
                //if (web.StatusCode != System.Net.HttpStatusCode.OK)
                //{
                //    return false;
                //}
                //var failNode1 = doc.DocumentNode.SelectSingleNode(@"//p[contains(@class,""missing-query"")]");
                //if (failNode1 != null) return false;
                //var failNode2 = doc.DocumentNode.SelectSingleNode(@"//div[contains(@class,""no-spelling-suggestions"")]");
                //if (failNode2 != null) return false;
                //return true;
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
            ValidateUrl = async (url) =>
            {
                return new Uri(url).Host.Trim().ToLower().Contains("oxfordlearnersdictionaries.com");
            },
            ValidateQuery = async (url, word) =>
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
            ValidateUrl = async (url) =>
            {
                return new Uri(url).Host.Trim().ToLower().Contains("dictionary.com");
            },
            ValidateQuery = async (url, word) =>
            {
                return await Helper.GetFinalStatusCodeAsync(url) == HttpStatusCode.OK;
            },
            GetWord = async (browser) =>
            {
                var headword = await browser.GetInnerTextByXPath(@"//h1[@class=""css-1jzk4d9 e1rg2mtf8""]");
                if (!string.IsNullOrWhiteSpace(headword))
                    return headword;
                Match match = Regex.Match(browser.Address, @"www\.dictionary\.com\/definition\/[\w-_]+\/([^?]+)");
                if (match.Success)
                {
                    headword = WebUtility.UrlDecode(match.Groups[1].Value);
                    return headword;
                }
                return null;
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
            ValidateUrl = async (url) =>
            {
                return new Uri(url).Host.Trim().ToLower().Contains("www.google.com");
            },
            ValidateQuery = async (url, word) =>
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

        public static Dictionary GoogleTranslate = new Dictionary()
        {
            Url = "https://translate.google.com/#view=home&op=translate&sl=en&tl=zh-TW&text=%s",
            ValidateUrl = async (url) =>
            {
                return new Uri(url).Host.Trim().ToLower().Contains("translate.google.com");
            },
            ValidateQuery = async (url, word) =>
            {
                return true;
            },
            GetWord = async (browser) =>
            {
                return null;
            },
            GetDescription = async (browser) =>
            {
                return await browser.GetInnerTextByXPath(@"//div[contains(@class,""text-wrap tlid-copy-target"")]");
            },
            Icon = PackIconKind.GoogleTranslate,
            Name = "Google Translate",
        };

        public static Dictionary Wikipedia = new Dictionary()
        {
            Url = "https://www.wikipedia.org/wiki/%s",
            ValidateUrl = async (url) =>
            {
                return new Uri(url).Host.Trim().ToLower().Contains("wikipedia.org");
            },
            ValidateQuery = async (url, word) =>
            {   
                return await Helper.GetFinalStatusCodeAsync(url) == HttpStatusCode.OK;
            },
            GetWord = async (browser) =>
            {
                var headword = await browser.GetInnerTextByXPath(@"//div[@class=""page-heading""]");
                if (!string.IsNullOrWhiteSpace(headword))
                    return headword;
                Match match = Regex.Match(browser.Address, @"wikipedia\.org\/w\/index\.php\?title=([^&]+)");
                if (match.Success)
                {
                    headword = WebUtility.UrlDecode(match.Groups[1].Value);
                    return headword;
                }
                match = Regex.Match(browser.Address, @"wikipedia\.org\/wiki\/([^?]+)");
                if (match.Success)
                {
                    headword = WebUtility.UrlDecode(match.Groups[1].Value);
                    return headword;
                }
                return null;
            },
            GetDescription = async (browser) =>
            {
                string res = await browser.GetInnerTextByXPath(@"(//div[@id=""bodyContent""]//p[not(@class)])[1]");
                if (res == null)
                    return null;
                else
                    return Regex.Replace(res, @"\[\d+\]", "");
            },
            Icon = PackIconKind.Wikipedia,
            Name = "Wikipedia",
        };
    }

    public class DictionaryDragAndDropListBox : DragAndDropListBox<Dictionary> { }

    public class DictionaryResultPair : NotifyPropertyChanged
    {
        private Dictionary dictionary;
        public Dictionary Dictionary
        {
            get => dictionary;
            set => SetAndNotify(ref dictionary, value);
        }


        private bool hasEntry = false;
        public bool HasEntry
        {
            get => hasEntry;
            set => SetAndNotify(ref hasEntry, value);
        }


        private string toolTip = null;
        public string ToolTip
        {
            get => toolTip;
            set => SetAndNotify(ref toolTip, value);
        }


        private string url = null;
        public string Url
        {
            get => url;
            set => SetAndNotify(ref url, value);
        }
    }
}
