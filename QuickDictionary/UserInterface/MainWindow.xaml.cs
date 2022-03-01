using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CefSharp;
using CefSharp.Handler;
using MoreLinq.Extensions;
using QuickDictionary.Models;
using QuickDictionary.Models.Configs;
using QuickDictionary.Models.Dictionaries;
using QuickDictionary.Models.WordLists;
using QuickDictionary.Native;
using QuickDictionary.UserInterface.Controls;
using QuickDictionary.UserInterface.Ocr;
using QuickDictionary.UserInterface.Validation;
using QuickDictionary.UserInterface.WordLists;
using QuickDictionary.Utils;
using Squirrel;
using Tesseract;
using static XnaFan.ImageComparison.ExtensionMethods;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using Control = System.Windows.Forms.Control;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ModifierKeys = QuickDictionary.Native.ModifierKeys;
using NativeMethods = QuickDictionary.Native.NativeMethods;
using Page = Tesseract.Page;
using Point = System.Windows.Point;
using Rect = Tesseract.Rect;

namespace QuickDictionary.UserInterface;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly DispatcherTimer autoOcrTimer;

    private readonly ObservableCollection<Dictionary> dictionaries = DictionaryStore.Instance.Dictionaries;

    private readonly TesseractEngine engine = new("data/tessdata", "eng", EngineMode.LstmOnly);

    private readonly SemaphoreSlim highlighterSemaphore = new(1, 1);
    private readonly KeyboardHook keyHook = new();

    private readonly SemaphoreSlim updateFinished = new(0, 1);
    private bool autoLookUpDone;

    private bool autoOcr;

    private bool canExit;

    private Point cursor;
    private DateTime cursorIdleSince;

    private ObservableCollection<DictionaryLookupResult> dictionaryResults = new();
    private AutoOcrHighlighter highlighter;
    private string lastUrl;

    private string lastWordUrl;
    private ManualOcrOverlay manualOcrOverlay;

    private string newListName;

    private string originalWord;

    private Bitmap screenshot;

    private WordListFile selectedWordList;

    private bool showNewWordPanel;

    private bool stopSelectionUpdate;

    private bool switchDictionaryExpanded;

    // default is false, set 1 for true.
    private int threadSafeEngineBusy;

    private string title;

    private int updateProgress;
    private Bitmap wordBitmap;

    private WordListBrowser wordListBrowser;

    public MainWindow()
    {
        InitializeComponent();
        ControlUtils.HideBoundingBox(root);

        browser.RequestHandler = new AdBlockRequestHandler();

        autoOcrTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        autoOcrTimer.Tick += AutoOcrTimer_Tick;
    }

    private bool engineBusy
    {
        get => Interlocked.CompareExchange(ref threadSafeEngineBusy, 1, 1) == 1;
        set
        {
            if (value) Interlocked.CompareExchange(ref threadSafeEngineBusy, 1, 0);
            else Interlocked.CompareExchange(ref threadSafeEngineBusy, 0, 1);
        }
    }

    public int UpdateProgress
    {
        get => updateProgress;
        set
        {
            updateProgress = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateProgress)));
        }
    }

    public bool ShowNewWordPanel
    {
        get => showNewWordPanel;
        set
        {
            showNewWordPanel = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowNewWordPanel)));
        }
    }

    public string NewListName
    {
        get => newListName;
        set
        {
            newListName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewListName)));
        }
    }

    public WordListFile SelectedWordList
    {
        get => selectedWordList;
        set
        {
            selectedWordList = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedWordList)));
        }
    }

    public ObservableCollection<DictionaryLookupResult> DictionaryResults
    {
        get => dictionaryResults;
        set
        {
            dictionaryResults = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DictionaryResults)));
        }
    }

    public bool SwitchDictionaryExpanded
    {
        get => switchDictionaryExpanded;
        set
        {
            switchDictionaryExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SwitchDictionaryExpanded)));
        }
    }

    public bool AutoOcr
    {
        get => autoOcr;
        set
        {
            autoOcr = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoOcr)));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private async Task autoLookup(Point newCursor)
    {
        if (engineBusy) return;

        if (DateTime.Now - cursorIdleSince >= TimeSpan.FromSeconds(1) && !autoLookUpDone)
        {
            screenshot = ScreenCapture.GetScreenshot();
            string orig = originalWord;
            OcrEntry word = await ocrAtPoint(newCursor, true);

            if (word != null && (orig == null || word.Word.Trim().ToLower() != orig.Trim().ToLower()))
            {
                if (highlighter != null)
                {
                    highlighter.Close();
                    highlighter = null;
                }

                highlighter = new AutoOcrHighlighter();
                highlighter.SetWord(word);
                highlighter.Show();
                wordBitmap = ScreenCapture.GetScreenshot()
                    .CropAtRect(new Rectangle((int)word.Rect.Left, (int)word.Rect.Top, (int)word.Rect.Width, (int)word.Rect.Height));
            }

            autoLookUpDone = true;
        }
    }

    private async void AutoOcrTimer_Tick(object sender, EventArgs e)
    {
        autoOcrTimer.Stop();
        System.Drawing.Point p = Control.MousePosition;
        Point newCursor = ControlUtils.RealPixelsToWpf(this, new Point(p.X, p.Y));

        if (highlighter != null)
        {
            OcrEntry word = highlighter.OcrEntry;
            using Bitmap newBmp = ScreenCapture.GetScreenshot()
                .CropAtRect(new Rectangle((int)word.Rect.Left, (int)word.Rect.Top, (int)word.Rect.Width, (int)word.Rect.Height));
            float pDiff = newBmp.PercentageDifference(wordBitmap);

            if (pDiff > 0.15)
            {
                highlighter.Close();
                highlighter = null;
                wordBitmap.Dispose();
                wordBitmap = null;
            }
        }

        if (newCursor == cursor)
            await autoLookup(newCursor);
        else
        {
            cursor = newCursor;
            cursorIdleSince = DateTime.Now;
            autoLookUpDone = false;
        }

        autoOcrTimer.Start();
    }

    private async void startOcr()
    {
        if (engineBusy)
        {
            if (FindResource("ShakeStoryboard") is Storyboard sb)
                sb.Begin(this, true);
            return;
        }

        if (manualOcrOverlay is { IsLoaded: true })
            return;

        manualOcrOverlay = new ManualOcrOverlay();
        manualOcrOverlay.WordSelected += Overlay_WordSelected;
        screenshot = ScreenCapture.GetScreenshot();
        await Task.Run(() =>
        {
            var ms = new MemoryStream();
            screenshot.Save(ms, ImageFormat.Png);

            ms.Seek(0, SeekOrigin.Begin);

            Dispatcher.Invoke(() =>
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                manualOcrOverlay.SetBackground(bitmapImage);
                ms.Dispose();
            });
        });
        Dispatcher.Invoke(() => progressLoading.Visibility = Visibility.Visible);
        manualOcrOverlay.ShowDialog();
    }

    private void unminimize()
    {
        var winInterop = new WindowInteropHelper(this);
        NativeMethods.SendMessage(winInterop.Handle, 0x0112, 0xF120, 0);
    }

    private async void KeyHook_KeyPressed(object sender, KeyPressedEventArgs e)
    {
        if (e.Key == Keys.F)
        {
            if (WindowState == WindowState.Minimized)
                unminimize();
            Activate();
            Keyboard.Focus(txtWord);
            txtWord.Focus();
            txtWord.SelectAll();
        }
        else if (e.Key == Keys.G)
        {
            if (AutoOcr)
            {
                System.Drawing.Point p = Control.MousePosition;
                Point newCursor = ControlUtils.RealPixelsToWpf(this, new Point(p.X, p.Y));
                await autoLookup(newCursor);
            }
            else
                startOcr();
        }
    }

    private static OcrEntry findClosestOcrEntry(List<OcrEntry> ocrEntries, float x, float y, bool strict)
    {
        OcrEntry word = ocrEntries.FirstOrDefault(w => w.Rect.Contains(x, y));

        if (word != null)
            return word;

        word = strict
            ? ocrEntries.Where(w => w.Rect.DistanceToPoint(x, y) < 2).MinBy(w => w.Rect.DistanceToPoint(x, y)).FirstOrDefault()
            : ocrEntries.MinBy(w => w.Rect.DistanceToPoint(x, y)).FirstOrDefault();

        if (word != null)
            return word;

        return null;
    }

    private async Task<OcrEntry> ocrAtPoint(Point position, bool strict)
    {
        engineBusy = true;
        Pix tessImg;

        var ocrWords = new List<OcrEntry>();
        var boxOffset = new Vector(
            ConfigStore.Instance.Config.CaptureBoxWidth / 2,
            ConfigStore.Instance.Config.CaptureBoxWidth / ConfigStore.Instance.Config.CaptureBoxAspectRatio / 2
        );
        Point wp1 = ControlUtils.WpfToRealPixels(this, position - boxOffset);
        Point wp2 = ControlUtils.WpfToRealPixels(this, position + boxOffset);

        await Task.Run(() =>
        {
            using (var ms = new MemoryStream())
            {
                Bitmap bmp = screenshot.CropAtRect(new Rectangle((int)wp1.X, (int)wp1.Y, (int)(wp2.X - wp1.X), (int)(wp2.Y - wp1.Y)));
                bmp.Save(ms, ImageFormat.Png);
                tessImg = Pix.LoadFromMemory(ms.ToArray());
            }

            using (Page page = engine.Process(tessImg, PageSegMode.SparseText))
            {
                using (ResultIterator iter = page.GetIterator())
                {
                    iter.Begin();
                    ocrWords.Clear();

                    do
                    {
                        string w = iter.GetText(PageIteratorLevel.Word);

                        if (!string.IsNullOrWhiteSpace(w))
                        {
                            if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out Rect rect))
                            {
                                if (Regex.IsMatch(w, "[a-zA-Z]"))
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        Point p1 = ControlUtils.RealPixelsToWpf(this, new Point(rect.X1, rect.Y1) + new Vector(wp1.X, wp1.Y));
                                        Point p2 = ControlUtils.RealPixelsToWpf(this, new Point(rect.X2, rect.Y2) + new Vector(wp1.X, wp1.Y));
                                        ocrWords.Add(new OcrEntry
                                        {
                                            Rect = new RectangleF((float)p1.X, (float)p1.Y, (float)(p2.X - p1.X), (float)(p2.Y - p1.Y)),
                                            Word = w
                                        });
                                    });
                                }
                            }
                        }
                    } while (iter.Next(PageIteratorLevel.Word));
                }
            }
        });

        engineBusy = false;

        if (position.X < 0 && position.Y < 0)
        {
            Dispatcher.Invoke(() => progressLoading.Visibility = Visibility.Hidden);
            return null;
        }

        OcrEntry word = findClosestOcrEntry(ocrWords, (float)position.X, (float)position.Y, strict);

        if (word != null)
        {
            if (originalWord == null || word.Word.Trim().ToLower() != originalWord.Trim().ToLower())
                search(word.Word.Trim());
        }

        return word;
    }

    private async void Overlay_WordSelected(object sender, Point position)
    {
        await ocrAtPoint(position, false);
    }

    private async void Window_SourceInitialized(object sender, EventArgs e)
    {
        stopSelectionUpdate = true;

        Keyboard.Focus(txtWord);
        txtWord.Focus();

        listDictionaries.ItemsSource = dictionaries;
        listDictionaries.SelectedItems.Clear();

        ConfigStore.Instance.LoadConfig();

        await WordListStore.LoadAllWordLists();

        foreach (string dict in ConfigStore.Instance.Config.SelectedDictionaries)
        {
            Dictionary d = dictionaries.FirstOrDefault(x => x.Name == dict);
            if (d != null)
                listDictionaries.SelectedItems.Add(d);
        }

        foreach (Dictionary dict in dictionaries)
            dict.Precedence = listDictionaries.SelectedItems.IndexOf(dict) + 1;

        SelectedWordList = WordListStore.WordListFiles.FirstOrDefault(x => x.WordList.Name == ConfigStore.Instance.Config.LastWordListName);
        listWordListSelector.ItemsSource = WordListStore.WordListFiles;
        DictionaryResults.Clear();
        listDictionaries.SelectedItems.Cast<Dictionary>().Select(x => new DictionaryLookupResult
            { Dictionary = x, HasEntry = false, TooltipText = null }).ForEach(x => DictionaryResults.Add(x));
        listSwitchDictionaries.ItemsSource = DictionaryResults;

        stopSelectionUpdate = false;

        checkTopmost.IsSelected = ConfigStore.Instance.Config.AlwaysOnTop;
        checkPause.IsSelected = ConfigStore.Instance.Config.PauseClipboard;

        keyHook.RegisterHotKey(ModifierKeys.Alt, Keys.F);
        keyHook.RegisterHotKey(ModifierKeys.Alt, Keys.G);
        keyHook.KeyPressed += KeyHook_KeyPressed;

        engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-' ");
        engine.SetVariable("tessedit_char_blacklist", "¢§+~»~`!@#$%^&*()_+={}[]|\\:\";<>?,./");

        engineBusy = false;

        Title = "Quick Dictionary v" + Assembly.GetExecutingAssembly().GetName().Version;
        title = Title;

        await AdHostStore.Instance.DownloadHostListAsync();

        await TaskUtils.WaitUntil(() => Dispatcher.Invoke(() => browser.IsBrowserInitialized));

        // Initialize the clipboard now that we have a window source to use
        var windowClipboardManager = new ClipboardMonitor(this);
        windowClipboardManager.ClipboardChanged += clipboardChanged;

        try
        {
            using var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/Henry-YSLin/QuickDictionary");
            UpdateInfo updateInfo = await mgr.CheckForUpdate(false, progress =>
            {
                UpdateProgress = progress;
                Dispatcher.Invoke(() => Title = title + $" - Checking {progress}%");
            });

            if (updateInfo.ReleasesToApply.Any())
            {
                await mgr.UpdateApp(progress =>
                {
                    UpdateProgress = progress;
                    Dispatcher.Invoke(() => Title = title + $" - Updating {progress}%");
                });
                await Task.Delay(500);
                Dispatcher.Invoke(() => Title = title + " - Restart app to update");
            }
            else
                Dispatcher.Invoke(() => Title = title);
        }
        catch (Exception ex)
        {
            App.LogException(ex, ex.Source);
        }

        updateFinished.Release();
    }

    private void clipboardChanged(object sender, EventArgs e)
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

    private async void updateHighlighter(string word, Dictionary dict)
    {
        try
        {
            await highlighterSemaphore.WaitAsync();

            if (highlighter != null)
            {
                await TaskUtils.WaitUntil(() => browser.CanExecuteJavascriptInMainFrame);
                await TaskUtils.WaitUntil(() => (string)browser.EvaluateScriptAsync("window.location.href").Result.Result != lastUrl);
                await TaskUtils.WaitUntil(() => dict == DictionaryStore.Instance.GetDictionaryByUrl(browser.Address), 500, 10000);

                if (dict != null)
                {
                    if (highlighter != null)
                        highlighter.DictionaryName = dict.Name;
                    string desc = "";
                    await TaskUtils.WaitUntil(() =>
                    {
                        try
                        {
                            desc = dict.GetDescriptionAsync(browser).Result;
                        }
                        catch (Exception ex)
                        {
                            App.LogException(ex, ex.Source);
                        }

                        return !string.IsNullOrEmpty(desc);
                    }, timeout: 10000);
                    string headword = null;

                    try
                    {
                        headword = await dict.GetWordAsync(browser);
                    }
                    catch (Exception ex)
                    {
                        App.LogException(ex, ex.Source);
                    }

                    if (string.IsNullOrWhiteSpace(headword)) headword = word;

                    if (highlighter != null)
                    {
                        highlighter.Description = desc;
                        highlighter.Word = headword;
                    }
                }
                else
                    highlighter.Description = "No definitions found";
            }

            lastUrl = browser.Address;
        }
        finally
        {
            highlighterSemaphore.Release();
        }
    }

    private async void search(string word)
    {
        if (word.Length < 100)
        {
            if (Regex.IsMatch(word, "[a-zA-Z]"))
            {
                originalWord = word;
                txtWord.Text = word;

                if (!ShowNewWordPanel)
                {
                    Keyboard.Focus(txtWord);
                    txtWord.Focus();
                    txtWord.SelectAll();
                }

                List<Dictionary> selectedDictionaries = listDictionaries.SelectedItems.Cast<Dictionary>().ToList();
                var queryTasks = new List<Task<string>>();

                foreach (Dictionary dict in selectedDictionaries)
                {
                    Task<string> task = dict.ExecuteQueryAsync(word);
                    queryTasks.Add(task);
                }

                if (queryTasks.Count > 0)
                    await Task.WhenAny(queryTasks.ToArray());
                bool done = false;

                for (int i = 0; i < queryTasks.Count; i++)
                {
                    DictionaryLookupResult res = DictionaryResults.FirstOrDefault(x => x.Dictionary == selectedDictionaries[i]);
                    string wordUrl = await queryTasks[i];
                    bool isSuccess = wordUrl != null;

                    if (res != null)
                    {
                        res.HasEntry = isSuccess;
                        res.TooltipText = isSuccess ? $"View \"{word}\" in {selectedDictionaries[i].Name}" : $"No results of \"{word}\" in {selectedDictionaries[i].Name}";
                        res.Url = wordUrl;
                    }

                    if (isSuccess && !done)
                    {
                        browser.Load(wordUrl);

                        stopSelectionUpdate = true;
                        listSwitchDictionaries.SelectedItem = res;
                        stopSelectionUpdate = false;

                        updateHighlighter(word, selectedDictionaries[i]);

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

        foreach (Dictionary dict in dictionaries)
            dict.Precedence = listDictionaries.SelectedItems.IndexOf(dict) + 1;

        List<Dictionary> selectedDictionaries = listDictionaries.SelectedItems.Cast<Dictionary>().ToList();
        ConfigStore.Instance.Config.SelectedDictionaries = selectedDictionaries.Select(x => x.Name).ToList();
        var results = new List<DictionaryLookupResult>();
        results.AddRange(DictionaryResults);
        DictionaryResults.Clear();
        selectedDictionaries.Select(x => results.FirstOrDefault(y => y.Dictionary == x) ?? new DictionaryLookupResult
            { Dictionary = x, HasEntry = false, TooltipText = null }).ForEach(x => DictionaryResults.Add(x));
        ConfigStore.Instance.SaveConfig();
    }

    private async void browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
    {
        Dispatcher.Invoke(() => progressLoading.Visibility = e.IsLoading ? Visibility.Visible : Visibility.Hidden);
        if (e.IsLoading) return;
        string address = null;
        Dispatcher.Invoke(() => address = browser.Address);
        if (string.IsNullOrWhiteSpace(address)) return;
        Dictionary dict = DictionaryStore.Instance.GetDictionaryByUrl(address);

        if (dict != null)
        {
            string headword = null;

            try
            {
                headword = await dict.GetWordAsync(browser);
            }
            catch (Exception ex)
            {
                App.LogException(ex, ex.Source);
            }

            if (!string.IsNullOrWhiteSpace(headword))
            {
                Dispatcher.Invoke(() =>
                {
                    txtWord.Text = headword;

                    if (!ShowNewWordPanel && !(wordListBrowser?.IsActive).GetValueOrDefault())
                    {
                        Keyboard.Focus(txtWord);
                        txtWord.Focus();
                        txtWord.SelectAll();
                    }
                });
            }
        }
    }

    private void toggleOcrButton(bool isOn)
    {
        if (isOn)
        {
            btnOcr.Background = (SolidColorBrush)FindResource("PrimaryHueMidBrush");
            btnOcr.Foreground = (SolidColorBrush)FindResource("PrimaryHueMidForegroundBrush");
        }
        else
        {
            btnOcr.Background = (SolidColorBrush)FindResource("MaterialDesignPaper");
            btnOcr.Foreground = (SolidColorBrush)FindResource("PrimaryHueMidBrush");
        }
    }

    private async void btnOcr_Click(object sender, RoutedEventArgs e)
    {
        if (AutoOcr)
        {
            AutoOcr = false;
            toggleOcrButton(false);
            await TaskUtils.WaitUntil(() => autoOcrTimer.IsEnabled);
            autoOcrTimer.Stop();

            if (highlighter != null)
            {
                highlighter.Close();
                highlighter = null;
            }

            if (wordBitmap != null)
            {
                wordBitmap.Dispose();
                wordBitmap = null;
            }

            return;
        }

        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            AutoOcr = true;
            toggleOcrButton(true);
            autoOcrTimer.Start();
        }
        else
            startOcr();
    }

    private async void mainWindow_Closing(object sender, CancelEventArgs e)
    {
        if (highlighter != null)
        {
            highlighter.Close();
            highlighter = null;
        }

        if (wordBitmap != null)
        {
            wordBitmap.Dispose();
            wordBitmap = null;
        }

        if (canExit)
            return;

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

            Dictionary dict = DictionaryStore.Instance.GetDictionaryByUrl(browser.Address);

            if (dict != null)
            {
                string headword = null;

                try
                {
                    headword = await dict.GetWordAsync(browser);
                }
                catch (Exception ex)
                {
                    App.LogException(ex, ex.Source);
                }

                if (string.IsNullOrWhiteSpace(headword)) headword = txtWord.Text;
                string desc = "";

                try
                {
                    // Append a < char before each line of description so it will be wrapped in note blocks
                    desc = await dict.GetDescriptionAsync(browser);
                    desc = string.Join(Environment.NewLine, desc.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None).Select(line => "< " + line));
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
        if (wordListBrowser == null) wordListBrowser = new WordListBrowser(this);

        if (!Application.Current.Windows.OfType<WordListBrowser>().Contains(wordListBrowser))
            wordListBrowser = new WordListBrowser(this);

        wordListBrowser.Show();
        wordListBrowser.Activate();
    }

    private void txtNewWord_TextChanged(object sender, TextChangedEventArgs e)
    {
        bool valid = txtNewWord.Text.Length > 0 && SelectedWordList != null;
        btnAddWord.IsEnabled = valid;

        if (valid)
        {
            if (SelectedWordList.WordList.Entries.Select(x => x.Word.Trim().ToLower()).Contains(txtNewWord.Text.Trim().ToLower()))
                lblDuplicateWord.Show();
            else
                lblDuplicateWord.Hide(false);
        }
        else
            lblDuplicateWord.Hide(false);
    }

    private void btnNewListSave_Click(object sender, RoutedEventArgs e)
    {
        if (!WordListNameValidationRule.ValidateWordlistName(txtNewListName.Text, CultureInfo.InvariantCulture).IsValid)
        {
            txtNewListName.Focus();
            Keyboard.Focus(txtNewListName);
            txtNewListName.SelectAll();
            return;
        }

        string listName = txtNewListName.Text;
        string path = Path.Combine(WordListStore.WordListFolderPath, $"{listName}.xml");
        var wordlistFile = new WordListFile(path, new WordList
            { Name = listName, Created = DateTime.Now });
        WordListStore.WordListFiles.Add(wordlistFile);
        WordListStore.SaveWordList(wordlistFile);
        if (WordListStore.DeletedPaths.Contains(path))
            WordListStore.DeletedPaths.Remove(path);
        dialogHost.IsOpen = false;
    }

    private void btnNewList_Click(object sender, RoutedEventArgs e)
    {
        drawerHost.IsBottomDrawerOpen = true;
    }

    private void btnAddWord_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWordList == null || string.IsNullOrWhiteSpace(txtNewWord.Text)) return;
        SelectedWordList.WordList.Entries.Add(new WordEntry
            { Word = txtNewWord.Text, Description = txtNewDesc.Text, Created = DateTime.Now, LastModified = DateTime.Now, Url = txtNewLink.Text });
        WordListStore.SaveWordList(SelectedWordList);
        snackbarMain.MessageQueue?.Enqueue($"{txtNewWord.Text} saved");
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
                lblDuplicateWord.Show();
            else
                lblDuplicateWord.Hide(false);
        }
        else
            lblDuplicateWord.Hide(false);

        if (SelectedWordList != null)
            drawerHost.IsBottomDrawerOpen = false;
    }

    private void check_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ConfigStore.Instance.Config.PauseClipboard = checkPause.IsSelected;
        ConfigStore.Instance.Config.AlwaysOnTop = checkTopmost.IsSelected;
        ConfigStore.Instance.SaveConfig();
    }

    public void NavigateWord(WordEntry word)
    {
        if (!browser.IsBrowserInitialized) return;
        progressLoading.Visibility = Visibility.Visible;

        if (!string.IsNullOrWhiteSpace(word.Url))
            browser.Load(word.Url);
        else
            search(word.Word);

        unminimize();
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
            browser.Load(((DictionaryLookupResult)listSwitchDictionaries.SelectedItem).Url);
    }

    private void btnSwitchDictionaryPanel_Click(object sender, RoutedEventArgs e)
    {
        SwitchDictionaryExpanded = !SwitchDictionaryExpanded;
    }

    public class AdBlockRequestHandler : RequestHandler
    {
        protected override bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            bool block = AdHostStore.Instance.IsAdUrl(request.Url);
            return block;
        }

        protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload,
            string requestInitiator, ref bool disableDefaultHandling)
        {
            bool block = AdHostStore.Instance.IsAdUrl(request.Url);
            if (block)
                return new AdBlockResourceRequestHandler();
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
}

public class OcrEntry
{
    public RectangleF Rect;
    public string Word;
}
