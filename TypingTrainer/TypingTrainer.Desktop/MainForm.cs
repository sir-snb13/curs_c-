using TypingTrainer.Core;

namespace TypingTrainer.Desktop;

public sealed class MainForm : Form
{
    private const string AppTitle = "Тренажёр набора текста";

    private readonly CommandLineOptions _options;
    private readonly DictionaryService _dictionaryService;
    private readonly StatisticsService _statisticsService;
    private readonly SettingsService _settingsService;
    private readonly Random _random = new();
    private readonly System.Windows.Forms.Timer _trainingTimer = new();
    private readonly Dictionary<char, Button> _keyboardButtons = new();

    private AppSettings _settings;
    private List<TypingDictionary> _dictionaries;
    private TypingSession? _session;
    private string _activeDictionaryName = string.Empty;
    private bool _isRefreshingDictionaries;
    private bool _isLoadingSettingsControls;
    private bool _isFinishingTraining;
    private bool _isRestoringInput;
    private string _lastAcceptedInput = string.Empty;

    private Label _titleLabel = null!;
    private TabControl _tabs = null!;

    private ComboBox _dictionaryCombo = null!;
    private Button _startButton = null!;
    private Button _resetButton = null!;
    private RichTextBox _sourceTextBox = null!;
    private TextBox _inputTextBox = null!;
    private Label _timerValueLabel = null!;
    private Label _speedValueLabel = null!;
    private Label _errorsValueLabel = null!;
    private Label _accuracyValueLabel = null!;
    private Label _progressValueLabel = null!;
    private Label _statusLabel = null!;
    private ProgressBar _progressBar = null!;
    private FlowLayoutPanel _keyboardPanel = null!;

    private ListBox _dictionaryList = null!;
    private ListBox _phrasesList = null!;
    private TextBox _newPhraseTextBox = null!;

    private DataGridView _statisticsGrid = null!;
    private Label _totalTrainingsValueLabel = null!;
    private Label _bestSpeedValueLabel = null!;
    private Label _averageAccuracyValueLabel = null!;
    private Label _totalErrorsValueLabel = null!;

    private NumericUpDown _fontSizeInput = null!;
    private CheckBox _showKeyboardCheck = null!;
    private ComboBox _durationCombo = null!;
    private ComboBox _themeCombo = null!;

    public MainForm(CommandLineOptions options)
    {
        _options = options;

        var dataDirectory = AppContext.BaseDirectory;
        _dictionaryService = new DictionaryService(dataDirectory);
        _statisticsService = new StatisticsService(dataDirectory);
        _settingsService = new SettingsService(dataDirectory);
        _settings = _settingsService.LoadSettings();
        _dictionaries = _dictionaryService.LoadDictionaries().ToList();

        BuildUi();
        LoadSettingsIntoControls();
        RefreshDictionaryControls(_options.DictionaryName);
        RefreshStatistics();
        ResetTraining();
        ApplyTheme();

        _trainingTimer.Interval = 500;
        _trainingTimer.Tick += (_, _) => OnTrainingTimerTick();

        Shown += (_, _) =>
        {
            if (_options.ShowHelp)
            {
                ShowHelp();
            }
        };
    }

    private Color BackgroundColor => _settings.Theme == AppTheme.Dark
        ? Color.FromArgb(27, 31, 39)
        : Color.FromArgb(244, 247, 251);

    private Color SurfaceColor => _settings.Theme == AppTheme.Dark
        ? Color.FromArgb(39, 45, 56)
        : Color.White;

    private Color SurfaceAltColor => _settings.Theme == AppTheme.Dark
        ? Color.FromArgb(49, 56, 69)
        : Color.FromArgb(235, 241, 249);

    private Color TextColor => _settings.Theme == AppTheme.Dark
        ? Color.FromArgb(237, 242, 247)
        : Color.FromArgb(31, 41, 55);

    private Color MutedTextColor => _settings.Theme == AppTheme.Dark
        ? Color.FromArgb(172, 184, 199)
        : Color.FromArgb(92, 107, 128);

    private Color AccentColor => Color.FromArgb(37, 99, 235);

    private Color SuccessColor => Color.FromArgb(22, 163, 74);

    private Color ErrorColor => Color.FromArgb(220, 38, 38);

    private void BuildUi()
    {
        SuspendLayout();

        Text = AppTitle;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1000, 650);
        MinimumSize = new Size(900, 600);
        KeyPreview = true;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _titleLabel = new Label
        {
            AutoSize = true,
            Text = AppTitle,
            Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 14)
        };

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill
        };
        _tabs.TabPages.Add(BuildTrainingTab());
        _tabs.TabPages.Add(BuildDictionariesTab());
        _tabs.TabPages.Add(BuildStatisticsTab());
        _tabs.TabPages.Add(BuildSettingsTab());

        root.Controls.Add(_titleLabel, 0, 0);
        root.Controls.Add(_tabs, 0, 1);
        Controls.Add(root);

        ResumeLayout(false);
    }

    private TabPage BuildTrainingTab()
    {
        var tab = new TabPage("Тренировка") { Padding = new Padding(14) };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 12)
        };
        topBar.Controls.Add(CreateLabel("Словарь:", 10F, FontStyle.Bold, new Padding(0, 9, 8, 0)));

        _dictionaryCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 260,
            Margin = new Padding(0, 4, 12, 0)
        };
        _startButton = CreateButton("Начать", primary: true);
        _startButton.Click += (_, _) => StartTraining();
        _resetButton = CreateButton("Сброс");
        _resetButton.Click += (_, _) => ResetTraining();
        topBar.Controls.Add(_dictionaryCombo);
        topBar.Controls.Add(_startButton);
        topBar.Controls.Add(_resetButton);

        var workArea = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        workArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
        workArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));

        var textArea = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0, 0, 12, 0)
        };
        textArea.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        textArea.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
        textArea.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        textArea.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));

        _sourceTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            DetectUrls = false,
            Margin = new Padding(0, 4, 0, 10),
            ScrollBars = RichTextBoxScrollBars.Vertical
        };
        _inputTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            Enabled = false,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 4, 0, 0),
            ScrollBars = ScrollBars.Vertical
        };
        _inputTextBox.TextChanged += (_, _) => OnInputTextChanged();
        _inputTextBox.KeyDown += OnInputKeyDown;

        textArea.Controls.Add(CreateSectionLabel("Текст для набора"), 0, 0);
        textArea.Controls.Add(_sourceTextBox, 0, 1);
        textArea.Controls.Add(CreateSectionLabel("Ваш ввод"), 0, 2);
        textArea.Controls.Add(_inputTextBox, 0, 3);

        var metricsPanel = CreateSurfacePanel();
        var metricsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9
        };
        metricsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
        metricsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
        metricsPanel.Controls.Add(metricsLayout);

        AddMetric(metricsLayout, 0, "Время", out _timerValueLabel);
        AddMetric(metricsLayout, 1, "Скорость", out _speedValueLabel);
        AddMetric(metricsLayout, 2, "Ошибки", out _errorsValueLabel);
        AddMetric(metricsLayout, 3, "Точность", out _accuracyValueLabel);
        AddMetric(metricsLayout, 4, "Прогресс", out _progressValueLabel);

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 20,
            Margin = new Padding(0, 12, 0, 6)
        };
        metricsLayout.Controls.Add(_progressBar, 0, 6);
        metricsLayout.SetColumnSpan(_progressBar, 2);

        _statusLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Margin = new Padding(0, 8, 0, 0)
        };
        metricsLayout.Controls.Add(_statusLabel, 0, 7);
        metricsLayout.SetColumnSpan(_statusLabel, 2);

        workArea.Controls.Add(textArea, 0, 0);
        workArea.Controls.Add(metricsPanel, 1, 0);

        _keyboardPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 14, 0, 0)
        };
        BuildKeyboard();

        layout.Controls.Add(topBar, 0, 0);
        layout.Controls.Add(workArea, 0, 1);
        layout.Controls.Add(_keyboardPanel, 0, 4);
        tab.Controls.Add(layout);

        return tab;
    }

    private TabPage BuildDictionariesTab()
    {
        var tab = new TabPage("Словари") { Padding = new Padding(14) };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

        var dictionariesPanel = CreateSurfacePanel(new Padding(12), new Padding(0, 0, 12, 0));
        var dictionariesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        dictionariesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        dictionariesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        dictionariesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        dictionariesPanel.Controls.Add(dictionariesLayout);

        _dictionaryList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Margin = new Padding(0, 8, 0, 12)
        };
        _dictionaryList.SelectedIndexChanged += (_, _) =>
        {
            if (!_isRefreshingDictionaries)
            {
                UpdatePhraseList();
            }
        };

        var dictionaryButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true
        };
        var createDictionaryButton = CreateButton("Создать словарь", primary: true, width: 150);
        createDictionaryButton.Click += (_, _) => CreateDictionary();
        var deleteDictionaryButton = CreateButton("Удалить словарь", width: 150);
        deleteDictionaryButton.Click += (_, _) => DeleteDictionary();
        dictionaryButtons.Controls.Add(createDictionaryButton);
        dictionaryButtons.Controls.Add(deleteDictionaryButton);

        dictionariesLayout.Controls.Add(CreateSectionLabel("Список словарей"), 0, 0);
        dictionariesLayout.Controls.Add(_dictionaryList, 0, 1);
        dictionariesLayout.Controls.Add(dictionaryButtons, 0, 2);

        var phrasesPanel = CreateSurfacePanel();
        var phrasesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        phrasesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phrasesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        phrasesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phrasesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        phrasesPanel.Controls.Add(phrasesLayout);

        _phrasesList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Margin = new Padding(0, 8, 0, 12)
        };

        var addPhrasePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 8)
        };
        addPhrasePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        addPhrasePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _newPhraseTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 8, 0)
        };
        var addPhraseButton = CreateButton("Добавить строку", primary: true, width: 150);
        addPhraseButton.Click += (_, _) => AddPhrase();
        addPhrasePanel.Controls.Add(_newPhraseTextBox, 0, 0);
        addPhrasePanel.Controls.Add(addPhraseButton, 1, 0);

        var removePhraseButton = CreateButton("Удалить выбранную строку", width: 210);
        removePhraseButton.Click += (_, _) => RemovePhrase();

        phrasesLayout.Controls.Add(CreateSectionLabel("Строки выбранного словаря"), 0, 0);
        phrasesLayout.Controls.Add(_phrasesList, 0, 1);
        phrasesLayout.Controls.Add(addPhrasePanel, 0, 2);
        phrasesLayout.Controls.Add(removePhraseButton, 0, 3);

        layout.Controls.Add(dictionariesPanel, 0, 0);
        layout.Controls.Add(phrasesPanel, 1, 0);
        tab.Controls.Add(layout);

        return tab;
    }

    private TabPage BuildStatisticsTab()
    {
        var tab = new TabPage("Статистика") { Padding = new Padding(14) };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var summaryPanel = CreateSurfacePanel(new Padding(12), new Padding(0, 0, 0, 12));
        var summaryLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 2
        };
        summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        summaryPanel.Controls.Add(summaryLayout);

        AddSummary(summaryLayout, 0, "Всего тренировок", out _totalTrainingsValueLabel);
        AddSummary(summaryLayout, 1, "Лучшая скорость", out _bestSpeedValueLabel);
        AddSummary(summaryLayout, 2, "Средняя точность", out _averageAccuracyValueLabel);
        AddSummary(summaryLayout, 3, "Всего ошибок", out _totalErrorsValueLabel);

        _statisticsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None
        };
        AddStatisticsColumns();

        layout.Controls.Add(summaryPanel, 0, 0);
        layout.Controls.Add(_statisticsGrid, 0, 1);
        tab.Controls.Add(layout);

        return tab;
    }

    private TabPage BuildSettingsTab()
    {
        var tab = new TabPage("Настройки") { Padding = new Padding(14) };
        var panel = CreateSurfacePanel();
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
        panel.Controls.Add(layout);

        _fontSizeInput = new NumericUpDown
        {
            Minimum = 12,
            Maximum = 32,
            Width = 120
        };
        _fontSizeInput.ValueChanged += (_, _) =>
        {
            if (_isLoadingSettingsControls)
            {
                return;
            }

            _settings.FontSize = (int)_fontSizeInput.Value;
            SaveAndApplySettings();
        };

        _showKeyboardCheck = new CheckBox
        {
            AutoSize = true,
            Text = "Показывать виртуальную клавиатуру"
        };
        _showKeyboardCheck.CheckedChanged += (_, _) =>
        {
            if (_isLoadingSettingsControls)
            {
                return;
            }

            _settings.ShowKeyboard = _showKeyboardCheck.Checked;
            SaveAndApplySettings();
        };

        _durationCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 220
        };
        _durationCombo.Items.Add(new OptionItem<TrainingDurationMode>("Без ограничения", TrainingDurationMode.Unlimited));
        _durationCombo.Items.Add(new OptionItem<TrainingDurationMode>("1 минута", TrainingDurationMode.OneMinute));
        _durationCombo.Items.Add(new OptionItem<TrainingDurationMode>("3 минуты", TrainingDurationMode.ThreeMinutes));
        _durationCombo.Items.Add(new OptionItem<TrainingDurationMode>("5 минут", TrainingDurationMode.FiveMinutes));
        _durationCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_isLoadingSettingsControls || _durationCombo.SelectedItem is not OptionItem<TrainingDurationMode> item)
            {
                return;
            }

            _settings.TrainingDurationMode = item.Value;
            SaveAndApplySettings();
        };

        _themeCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 220
        };
        _themeCombo.Items.Add(new OptionItem<AppTheme>("Светлая", AppTheme.Light));
        _themeCombo.Items.Add(new OptionItem<AppTheme>("Тёмная", AppTheme.Dark));
        _themeCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_isLoadingSettingsControls || _themeCombo.SelectedItem is not OptionItem<AppTheme> item)
            {
                return;
            }

            _settings.Theme = item.Value;
            SaveAndApplySettings();
        };

        AddSettingRow(layout, 0, "Размер шрифта текста", _fontSizeInput);
        AddSettingRow(layout, 1, "Виртуальная клавиатура", _showKeyboardCheck);
        AddSettingRow(layout, 2, "Длительность тренировки", _durationCombo);
        AddSettingRow(layout, 3, "Тема", _themeCombo);

        tab.Controls.Add(panel);

        return tab;
    }

    private void BuildKeyboard()
    {
        var rows = new[]
        {
            "Й Ц У К Е Н Г Ш Щ З Х Ъ",
            "Ф Ы В А П Р О Л Д Ж Э",
            "Я Ч С М И Т Ь Б Ю"
        };

        foreach (var row in rows)
        {
            var rowPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 6)
            };

            foreach (var key in row.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var button = CreateKeyboardButton(key);
                var character = key[0];
                _keyboardButtons[character] = button;
                rowPanel.Controls.Add(button);
            }

            _keyboardPanel.Controls.Add(rowPanel);
        }
    }

    private void StartTraining()
    {
        if (_dictionaryCombo.SelectedItem is not TypingDictionary dictionary)
        {
            MessageBox.Show(this, "Сначала выберите словарь.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (dictionary.Phrases.Count == 0)
        {
            MessageBox.Show(this, "В выбранном словаре нет строк для тренировки.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var sourceText = dictionary.Phrases[_random.Next(dictionary.Phrases.Count)];
        _activeDictionaryName = dictionary.Name;
        _session = new TypingSession(sourceText);
        _session.Start();
        _isFinishingTraining = false;
        _lastAcceptedInput = string.Empty;

        _inputTextBox.Enabled = true;
        _inputTextBox.Clear();
        _inputTextBox.Focus();
        _startButton.Enabled = false;
        _trainingTimer.Start();

        UpdateTrainingView();
    }

    private void ResetTraining()
    {
        _trainingTimer.Stop();
        _session = null;
        _activeDictionaryName = string.Empty;
        _isFinishingTraining = false;
        _lastAcceptedInput = string.Empty;

        _inputTextBox.Enabled = false;
        _inputTextBox.Clear();
        _startButton.Enabled = true;
        _progressBar.Value = 0;
        _timerValueLabel.Text = "00:00";
        _speedValueLabel.Text = "0 симв/мин";
        _errorsValueLabel.Text = "0";
        _accuracyValueLabel.Text = "100%";
        _progressValueLabel.Text = "0%";
        _statusLabel.Text = "Выберите словарь и нажмите «Начать».";

        _sourceTextBox.Clear();
        _sourceTextBox.SelectionColor = MutedTextColor;
        _sourceTextBox.AppendText("Здесь появится текст для тренировки.");
        HighlightNextKey();
    }

    private void OnInputTextChanged()
    {
        if (_isRestoringInput)
        {
            return;
        }

        if (_session is null || _isFinishingTraining)
        {
            return;
        }

        var currentInput = _inputTextBox.Text;
        if (currentInput.Length < _lastAcceptedInput.Length ||
            !currentInput.StartsWith(_lastAcceptedInput, StringComparison.Ordinal))
        {
            RestoreLastAcceptedInput();
            return;
        }

        _lastAcceptedInput = currentInput;
        _session.UpdateInput(currentInput);
        UpdateTrainingView();

        if (_session.IsCompleted)
        {
            FinishTraining(timeExpired: false);
        }
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (_session is null || _isFinishingTraining)
        {
            return;
        }

        if (e.KeyCode == Keys.Back ||
            e.KeyCode == Keys.Delete ||
            (e.Control && e.KeyCode == Keys.X))
        {
            e.SuppressKeyPress = true;
            e.Handled = true;
        }
    }

    private void RestoreLastAcceptedInput()
    {
        _isRestoringInput = true;
        _inputTextBox.Text = _lastAcceptedInput;
        _inputTextBox.SelectionStart = _inputTextBox.TextLength;
        _inputTextBox.SelectionLength = 0;
        _isRestoringInput = false;
    }

    private void OnTrainingTimerTick()
    {
        if (_session is null)
        {
            return;
        }

        var durationLimit = _settings.GetDurationLimitSeconds();
        if (durationLimit is not null && _session.GetDuration().TotalSeconds >= durationLimit.Value)
        {
            FinishTraining(timeExpired: true);
            return;
        }

        UpdateTrainingView();
    }

    private void FinishTraining(bool timeExpired)
    {
        if (_session is null || _isFinishingTraining)
        {
            return;
        }

        _isFinishingTraining = true;
        _trainingTimer.Stop();
        _session.Finish();
        _inputTextBox.Enabled = false;
        _startButton.Enabled = true;
        UpdateTrainingView();

        var durationSeconds = Math.Max(1, (int)Math.Round(_session.GetDuration().TotalSeconds));
        var result = new TypingStatistics
        {
            Date = DateTime.Now,
            DictionaryName = _activeDictionaryName,
            SourceText = _session.SourceText,
            DurationSeconds = durationSeconds,
            CharactersPerMinute = _session.CalculateSpeed(),
            ErrorsCount = _session.ErrorsCount,
            AccuracyPercent = _session.CalculateAccuracy()
        };

        _statisticsService.AddResult(result);
        RefreshStatistics();

        var reason = timeExpired ? "Время тренировки истекло." : "Тренировка завершена.";
        var message =
            $"{reason}{Environment.NewLine}{Environment.NewLine}" +
            $"Время: {FormatDuration(durationSeconds)}{Environment.NewLine}" +
            $"Скорость: {result.CharactersPerMinute:F0} симв/мин{Environment.NewLine}" +
            $"Ошибки: {result.ErrorsCount}{Environment.NewLine}" +
            $"Точность: {result.AccuracyPercent:F1}%";

        MessageBox.Show(this, message, "Результат", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateTrainingView()
    {
        if (_session is null)
        {
            return;
        }

        RenderSourceText();

        var duration = _session.GetDuration();
        var limit = _settings.GetDurationLimitSeconds();
        _timerValueLabel.Text = limit is null
            ? FormatDuration((int)duration.TotalSeconds)
            : $"{FormatDuration((int)duration.TotalSeconds)} / {FormatDuration(limit.Value)}";

        _speedValueLabel.Text = $"{_session.CalculateSpeed():F0} симв/мин";
        _errorsValueLabel.Text = _session.ErrorsCount.ToString();
        _accuracyValueLabel.Text = $"{_session.CalculateAccuracy():F1}%";

        var progress = _session.SourceText.Length == 0
            ? 0
            : Math.Clamp((int)Math.Round(_session.UserInput.Length * 100.0 / _session.SourceText.Length), 0, 100);
        _progressValueLabel.Text = $"{progress}%";
        _progressBar.Value = progress;

        var firstError = _session.GetFirstErrorPosition();
        if (_session.UserInput.Length == 0)
        {
            _statusLabel.Text = "Печатайте текст в поле ввода.";
            _statusLabel.ForeColor = MutedTextColor;
        }
        else if (firstError is null)
        {
            _statusLabel.Text = "Правильно";
            _statusLabel.ForeColor = SuccessColor;
        }
        else
        {
            _statusLabel.Text = $"Ошибка в позиции {firstError.Value}";
            _statusLabel.ForeColor = ErrorColor;
        }

        HighlightNextKey();
    }

    private void RenderSourceText()
    {
        if (_session is null)
        {
            return;
        }

        _sourceTextBox.SuspendLayout();
        _sourceTextBox.Clear();

        for (var index = 0; index < _session.SourceText.Length; index++)
        {
            if (index < _session.UserInput.Length)
            {
                _sourceTextBox.SelectionColor = _session.IsCharacterCorrectAt(index)
                    ? SuccessColor
                    : ErrorColor;
            }
            else
            {
                _sourceTextBox.SelectionColor = TextColor;
            }

            _sourceTextBox.AppendText(_session.SourceText[index].ToString());
        }

        if (_session.UserInput.Length > _session.SourceText.Length)
        {
            _sourceTextBox.SelectionColor = ErrorColor;
            _sourceTextBox.AppendText(_session.UserInput[_session.SourceText.Length..]);
        }

        _sourceTextBox.SelectionStart = 0;
        _sourceTextBox.ResumeLayout();
    }

    private void HighlightNextKey()
    {
        foreach (var button in _keyboardButtons.Values)
        {
            ApplyKeyboardButtonStyle(button, highlighted: false);
        }

        if (_session is null || _session.UserInput.Length >= _session.SourceText.Length)
        {
            return;
        }

        var nextCharacter = char.ToUpperInvariant(_session.SourceText[_session.UserInput.Length]);
        if (_keyboardButtons.TryGetValue(nextCharacter, out var keyButton))
        {
            ApplyKeyboardButtonStyle(keyButton, highlighted: true);
        }
    }

    private void CreateDictionary()
    {
        var name = PromptForText("Создать словарь", "Название словаря:");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (!_dictionaryService.AddDictionary(_dictionaries, name))
        {
            MessageBox.Show(this, "Словарь с таким названием уже существует.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        RefreshDictionaryControls(name);
    }

    private void DeleteDictionary()
    {
        if (_dictionaryList.SelectedItem is not TypingDictionary dictionary)
        {
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Удалить словарь «{dictionary.Name}»?",
            AppTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _dictionaryService.RemoveDictionary(_dictionaries, dictionary.Name);
        RefreshDictionaryControls();
    }

    private void AddPhrase()
    {
        if (_dictionaryList.SelectedItem is not TypingDictionary dictionary)
        {
            return;
        }

        var phrase = _newPhraseTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return;
        }

        if (!_dictionaryService.AddPhrase(_dictionaries, dictionary.Name, phrase))
        {
            MessageBox.Show(this, "Такая строка уже есть в словаре.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _newPhraseTextBox.Clear();
        RefreshDictionaryControls(dictionary.Name);
    }

    private void RemovePhrase()
    {
        if (_dictionaryList.SelectedItem is not TypingDictionary dictionary ||
            _phrasesList.SelectedItem is not string phrase)
        {
            return;
        }

        _dictionaryService.RemovePhrase(_dictionaries, dictionary.Name, phrase);
        RefreshDictionaryControls(dictionary.Name);
    }

    private void RefreshDictionaryControls(string? selectedName = null)
    {
        selectedName ??= (_dictionaryCombo.SelectedItem as TypingDictionary)?.Name
            ?? (_dictionaryList.SelectedItem as TypingDictionary)?.Name;

        _isRefreshingDictionaries = true;
        _dictionaryCombo.Items.Clear();
        _dictionaryList.Items.Clear();

        foreach (var dictionary in _dictionaries)
        {
            _dictionaryCombo.Items.Add(dictionary);
            _dictionaryList.Items.Add(dictionary);
        }

        SelectDictionaryInCombo(selectedName);
        SelectDictionaryInList(selectedName);

        if (_dictionaryCombo.SelectedIndex < 0 && _dictionaryCombo.Items.Count > 0)
        {
            _dictionaryCombo.SelectedIndex = 0;
        }

        if (_dictionaryList.SelectedIndex < 0 && _dictionaryList.Items.Count > 0)
        {
            _dictionaryList.SelectedIndex = 0;
        }

        _isRefreshingDictionaries = false;
        UpdatePhraseList();
    }

    private void UpdatePhraseList()
    {
        _phrasesList.Items.Clear();

        if (_dictionaryList.SelectedItem is not TypingDictionary dictionary)
        {
            _newPhraseTextBox.Enabled = false;
            return;
        }

        _newPhraseTextBox.Enabled = true;
        foreach (var phrase in dictionary.Phrases)
        {
            _phrasesList.Items.Add(phrase);
        }
    }

    private void RefreshStatistics()
    {
        var statistics = _statisticsService.LoadStatistics()
            .OrderByDescending(item => item.Date)
            .ToList();

        _statisticsGrid.DataSource = statistics
            .Select(item => new StatisticRow
            {
                Date = item.Date.ToString("dd.MM.yyyy HH:mm"),
                DictionaryName = item.DictionaryName,
                SourceText = item.SourceText,
                Duration = FormatDuration(item.DurationSeconds),
                Speed = $"{item.CharactersPerMinute:F0}",
                Errors = item.ErrorsCount.ToString(),
                Accuracy = $"{item.AccuracyPercent:F1}%"
            })
            .ToList();

        _totalTrainingsValueLabel.Text = statistics.Count.ToString();
        _bestSpeedValueLabel.Text = $"{_statisticsService.GetBestSpeed(statistics):F0} симв/мин";
        _averageAccuracyValueLabel.Text = $"{_statisticsService.GetAverageAccuracy(statistics):F1}%";
        _totalErrorsValueLabel.Text = _statisticsService.GetTotalErrors(statistics).ToString();
    }

    private void LoadSettingsIntoControls()
    {
        _isLoadingSettingsControls = true;
        _fontSizeInput.Value = Math.Clamp(_settings.FontSize, (int)_fontSizeInput.Minimum, (int)_fontSizeInput.Maximum);
        _showKeyboardCheck.Checked = _settings.ShowKeyboard;
        SelectOption(_durationCombo, _settings.TrainingDurationMode);
        SelectOption(_themeCombo, _settings.Theme);
        _isLoadingSettingsControls = false;

        ApplySettings();
    }

    private void SaveAndApplySettings()
    {
        _settingsService.SaveSettings(_settings);
        ApplySettings();
    }

    private void ApplySettings()
    {
        var trainingFont = new Font("Segoe UI", _settings.FontSize, FontStyle.Regular);
        _sourceTextBox.Font = trainingFont;
        _inputTextBox.Font = new Font("Segoe UI", _settings.FontSize, FontStyle.Regular);
        _keyboardPanel.Visible = _settings.ShowKeyboard;

        ApplyTheme();
        UpdateTrainingView();
    }

    private void ApplyTheme()
    {
        BackColor = BackgroundColor;
        ForeColor = TextColor;
        ApplyThemeToControl(this);
        _titleLabel.ForeColor = TextColor;
        HighlightNextKey();
    }

    private void ApplyThemeToControl(Control control)
    {
        switch (control)
        {
            case Button button when string.Equals(button.Tag as string, "primary", StringComparison.Ordinal):
                ApplyButtonStyle(button, primary: true);
                break;
            case Button button when string.Equals(button.Tag as string, "keyboard", StringComparison.Ordinal):
                ApplyKeyboardButtonStyle(button, highlighted: false);
                break;
            case Button button:
                ApplyButtonStyle(button, primary: false);
                break;
            case Label label when string.Equals(label.Tag as string, "muted", StringComparison.Ordinal):
                label.ForeColor = MutedTextColor;
                break;
            case Label label:
                label.ForeColor = TextColor;
                break;
            case TextBoxBase textBox:
                textBox.BackColor = SurfaceColor;
                textBox.ForeColor = TextColor;
                break;
            case ListBox listBox:
                listBox.BackColor = SurfaceColor;
                listBox.ForeColor = TextColor;
                break;
            case ComboBox comboBox:
                comboBox.BackColor = SurfaceColor;
                comboBox.ForeColor = TextColor;
                break;
            case NumericUpDown numeric:
                numeric.BackColor = SurfaceColor;
                numeric.ForeColor = TextColor;
                break;
            case CheckBox checkBox:
                checkBox.BackColor = SurfaceColor;
                checkBox.ForeColor = TextColor;
                break;
            case DataGridView grid:
                ApplyGridTheme(grid);
                break;
            case TabPage tabPage:
                tabPage.BackColor = BackgroundColor;
                tabPage.ForeColor = TextColor;
                break;
            case TableLayoutPanel:
            case FlowLayoutPanel:
            case Panel:
                var isSurface = string.Equals(control.Tag as string, "surface", StringComparison.Ordinal) ||
                    string.Equals(control.Parent?.Tag as string, "surface", StringComparison.Ordinal);
                control.BackColor = isSurface
                    ? SurfaceColor
                    : BackgroundColor;
                break;
            case TabControl:
                control.BackColor = BackgroundColor;
                control.ForeColor = TextColor;
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeToControl(child);
        }
    }

    private void ApplyGridTheme(DataGridView grid)
    {
        grid.BackgroundColor = SurfaceColor;
        grid.GridColor = SurfaceAltColor;
        grid.DefaultCellStyle.BackColor = SurfaceColor;
        grid.DefaultCellStyle.ForeColor = TextColor;
        grid.DefaultCellStyle.SelectionBackColor = AccentColor;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceAltColor;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
        grid.EnableHeadersVisualStyles = false;
    }

    private void ApplyButtonStyle(Button button, bool primary)
    {
        button.UseVisualStyleBackColor = false;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = primary ? AccentColor : SurfaceAltColor;
        button.ForeColor = primary ? Color.White : TextColor;
    }

    private void ApplyKeyboardButtonStyle(Button button, bool highlighted)
    {
        button.UseVisualStyleBackColor = false;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = highlighted ? AccentColor : SurfaceAltColor;
        button.ForeColor = highlighted ? Color.White : TextColor;
        button.Font = new Font("Segoe UI", 11F, highlighted ? FontStyle.Bold : FontStyle.Regular);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var keyCode = keyData & Keys.KeyCode;
        var hasControl = (keyData & Keys.Control) == Keys.Control;

        if (keyCode == Keys.F1)
        {
            ShowHelp();
            return true;
        }

        if (hasControl && keyCode == Keys.Enter)
        {
            if (_startButton.Enabled)
            {
                StartTraining();
            }

            return true;
        }

        if (keyCode == Keys.Escape)
        {
            ResetTraining();
            return true;
        }

        if (hasControl && keyCode == Keys.S)
        {
            _dictionaryService.SaveDictionaries(_dictionaries);
            MessageBox.Show(this, "Словари сохранены.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }

        if (hasControl && (keyCode == Keys.Oemplus || keyCode == Keys.Add))
        {
            ChangeFontSize(1);
            return true;
        }

        if (hasControl && (keyCode == Keys.OemMinus || keyCode == Keys.Subtract))
        {
            ChangeFontSize(-1);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ChangeFontSize(int delta)
    {
        var nextValue = Math.Clamp((int)_fontSizeInput.Value + delta, (int)_fontSizeInput.Minimum, (int)_fontSizeInput.Maximum);
        _fontSizeInput.Value = nextValue;
    }

    private void ShowHelp()
    {
        var helpText =
            "Как начать тренировку:" + Environment.NewLine +
            "1. Выберите словарь на вкладке «Тренировка»." + Environment.NewLine +
            "2. Нажмите «Начать» или Ctrl+Enter." + Environment.NewLine +
            "3. Набирайте показанный текст в поле ввода." + Environment.NewLine + Environment.NewLine +
            "Словари можно создавать и редактировать на вкладке «Словари». Каждая строка словаря может попасть в тренировку." + Environment.NewLine + Environment.NewLine +
            "Показатели статистики:" + Environment.NewLine +
            "Скорость — количество набранных символов в минуту." + Environment.NewLine +
            "Ошибки — количество несовпадающих символов." + Environment.NewLine +
            "Точность — доля правильно набранных символов." + Environment.NewLine + Environment.NewLine +
            "Горячие клавиши:" + Environment.NewLine +
            "F1 — справка" + Environment.NewLine +
            "Ctrl+Enter — начать тренировку" + Environment.NewLine +
            "Esc — сбросить тренировку" + Environment.NewLine +
            "Ctrl+S — сохранить словари" + Environment.NewLine +
            "Ctrl+Plus — увеличить размер шрифта" + Environment.NewLine +
            "Ctrl+Minus — уменьшить размер шрифта";

        MessageBox.Show(this, helpText, "Справка", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private string? PromptForText(string title, string labelText)
    {
        using var dialog = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(380, 145),
            BackColor = BackgroundColor,
            ForeColor = TextColor,
            Font = Font
        };

        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Location = new Point(14, 16),
            ForeColor = TextColor
        };
        var textBox = new TextBox
        {
            Location = new Point(14, 44),
            Width = 350,
            BackColor = SurfaceColor,
            ForeColor = TextColor
        };
        var okButton = CreateButton("OK", primary: true, width: 92);
        okButton.Location = new Point(172, 94);
        okButton.DialogResult = DialogResult.OK;
        var cancelButton = CreateButton("Отмена", width: 92);
        cancelButton.Location = new Point(272, 94);
        cancelButton.DialogResult = DialogResult.Cancel;

        dialog.Controls.Add(label);
        dialog.Controls.Add(textBox);
        dialog.Controls.Add(okButton);
        dialog.Controls.Add(cancelButton);
        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;

        return dialog.ShowDialog(this) == DialogResult.OK ? textBox.Text.Trim() : null;
    }

    private void SelectDictionaryInCombo(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        for (var index = 0; index < _dictionaryCombo.Items.Count; index++)
        {
            if (_dictionaryCombo.Items[index] is TypingDictionary dictionary &&
                string.Equals(dictionary.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                _dictionaryCombo.SelectedIndex = index;
                return;
            }
        }
    }

    private void SelectDictionaryInList(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        for (var index = 0; index < _dictionaryList.Items.Count; index++)
        {
            if (_dictionaryList.Items[index] is TypingDictionary dictionary &&
                string.Equals(dictionary.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                _dictionaryList.SelectedIndex = index;
                return;
            }
        }
    }

    private static void SelectOption<T>(ComboBox comboBox, T value)
    {
        for (var index = 0; index < comboBox.Items.Count; index++)
        {
            if (comboBox.Items[index] is OptionItem<T> item &&
                EqualityComparer<T>.Default.Equals(item.Value, value))
            {
                comboBox.SelectedIndex = index;
                return;
            }
        }

        if (comboBox.Items.Count > 0)
        {
            comboBox.SelectedIndex = 0;
        }
    }

    private Button CreateButton(string text, bool primary = false, int width = 110)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 36,
            Margin = new Padding(0, 0, 8, 0),
            Tag = primary ? "primary" : "secondary"
        };
        ApplyButtonStyle(button, primary);
        return button;
    }

    private Button CreateKeyboardButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Width = 48,
            Height = 40,
            Margin = new Padding(0, 0, 6, 0),
            TabStop = false,
            Tag = "keyboard"
        };
        button.Click += (_, _) => _inputTextBox.Focus();
        ApplyKeyboardButtonStyle(button, highlighted: false);
        return button;
    }

    private Panel CreateSurfacePanel()
    {
        return CreateSurfacePanel(new Padding(12), Padding.Empty);
    }

    private Panel CreateSurfacePanel(Padding padding, Padding margin)
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            Padding = padding,
            Margin = margin,
            BorderStyle = BorderStyle.FixedSingle,
            Tag = "surface"
        };
    }

    private Label CreateSectionLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            Margin = new Padding(0)
        };
    }

    private Label CreateLabel(string text, float size, FontStyle style, Padding margin)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            Font = new Font("Segoe UI", size, style),
            Margin = margin
        };
    }

    private void AddMetric(TableLayoutPanel layout, int row, string caption, out Label valueLabel)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var captionLabel = new Label
        {
            AutoSize = true,
            Text = caption,
            Margin = new Padding(0, 4, 8, 4),
            Tag = "muted"
        };
        valueLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            Margin = new Padding(0, 4, 0, 4)
        };

        layout.Controls.Add(captionLabel, 0, row);
        layout.Controls.Add(valueLabel, 1, row);
    }

    private void AddSummary(TableLayoutPanel layout, int column, string caption, out Label valueLabel)
    {
        var captionLabel = new Label
        {
            AutoSize = true,
            Text = caption,
            Margin = new Padding(0, 0, 12, 4),
            Tag = "muted"
        };
        valueLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            Margin = new Padding(0, 0, 12, 0)
        };

        layout.Controls.Add(captionLabel, column, 0);
        layout.Controls.Add(valueLabel, column, 1);
    }

    private void AddSettingRow(TableLayoutPanel layout, int row, string caption, Control editor)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = new Label
        {
            AutoSize = true,
            Text = caption,
            Margin = new Padding(0, 7, 16, 13),
            Tag = "muted"
        };
        editor.Margin = new Padding(0, 4, 0, 12);

        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(editor, 1, row);
    }

    private void AddStatisticsColumns()
    {
        _statisticsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Дата",
            DataPropertyName = nameof(StatisticRow.Date),
            Width = 120
        });
        _statisticsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Словарь",
            DataPropertyName = nameof(StatisticRow.DictionaryName),
            Width = 130
        });
        _statisticsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Текст",
            DataPropertyName = nameof(StatisticRow.SourceText),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        _statisticsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Время",
            DataPropertyName = nameof(StatisticRow.Duration),
            Width = 80
        });
        _statisticsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Скорость",
            DataPropertyName = nameof(StatisticRow.Speed),
            Width = 85
        });
        _statisticsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ошибки",
            DataPropertyName = nameof(StatisticRow.Errors),
            Width = 70
        });
        _statisticsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Точность",
            DataPropertyName = nameof(StatisticRow.Accuracy),
            Width = 85
        });
    }

    private static string FormatDuration(int seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes:00}:{time.Seconds:00}";
    }

    private sealed class OptionItem<T>
    {
        public OptionItem(string text, T value)
        {
            Text = text;
            Value = value;
        }

        public string Text { get; }

        public T Value { get; }

        public override string ToString()
        {
            return Text;
        }
    }

    private sealed class StatisticRow
    {
        public string Date { get; init; } = string.Empty;

        public string DictionaryName { get; init; } = string.Empty;

        public string SourceText { get; init; } = string.Empty;

        public string Duration { get; init; } = string.Empty;

        public string Speed { get; init; } = string.Empty;

        public string Errors { get; init; } = string.Empty;

        public string Accuracy { get; init; } = string.Empty;
    }
}
