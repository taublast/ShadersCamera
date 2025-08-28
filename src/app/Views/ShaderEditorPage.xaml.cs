namespace ShadersCamera.Views;

public partial class ShaderEditorPage : ContentPage
{
    private readonly Action<string> _callback;
    private string _originalCode;
    private bool _hasUnsavedChanges;

    private bool HasError { get; set; }

    public void ReportCompilationError(string error)
    {
        if (!HasError)
        {
            HasError = true;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!string.IsNullOrEmpty(error))
                {
                    ErrorLabel.Text = error;
                    ErrorPanel.IsVisible = true;
                }
                else
                {
                    HideError();
                }
            });
        }
    }

    private void HideError()
    {
        ErrorPanel.IsVisible = false;
        ErrorLabel.Text = string.Empty;
    }

    public ShaderEditorPage(string code, Action<string> onSave)
	{
		InitializeComponent();

        _originalCode = code ?? string.Empty;
        Editor.Text = _originalCode;
        _callback = onSave;
        
        // Set up editor events
        Editor.TextChanged += OnEditorTextChanged;
        
        // Add keyboard accelerators
        SetupKeyboardShortcuts();
        
        UpdateTitle();
        
        // Initialize with normal editor view
        Editor.TextColor = Color.FromArgb("#d4d4d4");
        HighlightScrollView.IsVisible = false;
    }

    private void SetupKeyboardShortcuts()
    {
        // Simple keyboard shortcuts setup
    }

    private void OnEditorFocused(object sender, FocusEventArgs e)
    {
        // Show normal editor, hide syntax highlighting
        Editor.TextColor = Color.FromArgb("#d4d4d4");
        HighlightScrollView.IsVisible = false;
    }

    private void OnEditorUnfocused(object sender, FocusEventArgs e)
    {
        // Hide editor text, show syntax highlighting
        Editor.TextColor = Colors.Transparent;
        HighlightScrollView.IsVisible = true;
        UpdateSyntaxHighlighting();
    }

    private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
    {
        _hasUnsavedChanges = Editor.Text != _originalCode;
        UpdateTitle();
        
        // Only update syntax highlighting if editor is not focused (overlay is visible)
        if (HighlightScrollView.IsVisible)
        {
            UpdateSyntaxHighlighting();
        }
    }

    private void UpdateSyntaxHighlighting()
    {
        if (string.IsNullOrEmpty(Editor.Text))
        {
            HighlightLabel.FormattedText = new FormattedString();
            return;
        }

        var formattedString = new FormattedString();
        var text = Editor.Text;
        
        // SKSL keywords
        var keywords = new[]
        {
            "void", "float", "vec2", "vec3", "vec4", "mat2", "mat3", "mat4",
            "int", "bool", "sampler2D", "if", "else", "for", "while", "return",
            "uniform", "varying", "attribute", "const", "in", "out", "inout",
            "precision", "highp", "mediump", "lowp", "struct", "true", "false"
        };

        // Built-in functions
        var functions = new[]
        {
            "texture", "texture2D", "sin", "cos", "tan", "sqrt", "pow", "exp",
            "log", "abs", "floor", "ceil", "fract", "mod", "min", "max",
            "clamp", "mix", "step", "smoothstep", "length", "distance",
            "dot", "cross", "normalize", "reflect", "refract"
        };

        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            if (string.IsNullOrEmpty(line))
            {
                formattedString.Spans.Add(new Span { Text = "\n" });
                continue;
            }

            // Simple tokenization and highlighting
            var tokens = TokenizeLine(line);
            foreach (var token in tokens)
            {
                var span = new Span { Text = token.Text };
                
                if (keywords.Contains(token.Text))
                {
                    span.TextColor = Color.FromArgb("#569cd6"); // Blue for keywords
                    span.FontAttributes = FontAttributes.Bold;
                }
                else if (functions.Contains(token.Text))
                {
                    span.TextColor = Color.FromArgb("#dcdcaa"); // Yellow for functions
                }
                else if (token.Text.StartsWith("//"))
                {
                    span.TextColor = Color.FromArgb("#6a9955"); // Green for comments
                }
                else if (IsNumeric(token.Text))
                {
                    span.TextColor = Color.FromArgb("#b5cea8"); // Light green for numbers
                }
                else if (token.Text.Contains("\""))
                {
                    span.TextColor = Color.FromArgb("#ce9178"); // Orange for strings
                }
                else
                {
                    span.TextColor = Color.FromArgb("#d4d4d4"); // Default text color
                }

                formattedString.Spans.Add(span);
            }
            
            if (i < lines.Length - 1)
            {
                formattedString.Spans.Add(new Span { Text = "\n" });
            }
        }

        HighlightLabel.FormattedText = formattedString;
    }

    private List<Token> TokenizeLine(string line)
    {
        var tokens = new List<Token>();
        var currentToken = "";
        var inComment = false;
        var inString = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            // Handle comments
            if (!inString && i < line.Length - 1 && line[i] == '/' && line[i + 1] == '/')
            {
                if (!string.IsNullOrEmpty(currentToken))
                {
                    tokens.Add(new Token(currentToken));
                    currentToken = "";
                }
                tokens.Add(new Token(line.Substring(i))); // Rest of line is comment
                break;
            }

            // Handle strings
            if (c == '"')
            {
                if (inString)
                {
                    currentToken += c;
                    tokens.Add(new Token(currentToken));
                    currentToken = "";
                    inString = false;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentToken))
                    {
                        tokens.Add(new Token(currentToken));
                        currentToken = "";
                    }
                    currentToken += c;
                    inString = true;
                }
                continue;
            }

            if (inString)
            {
                currentToken += c;
                continue;
            }

            // Handle delimiters
            if (char.IsWhiteSpace(c) || "(){}[];,=+-*/<>!&|".Contains(c))
            {
                if (!string.IsNullOrEmpty(currentToken))
                {
                    tokens.Add(new Token(currentToken));
                    currentToken = "";
                }
                
                if (!char.IsWhiteSpace(c))
                {
                    tokens.Add(new Token(c.ToString()));
                }
                else
                {
                    tokens.Add(new Token(" "));
                }
            }
            else
            {
                currentToken += c;
            }
        }

        if (!string.IsNullOrEmpty(currentToken))
        {
            tokens.Add(new Token(currentToken));
        }

        return tokens;
    }

    private bool IsNumeric(string text)
    {
        return float.TryParse(text, out _) || int.TryParse(text, out _);
    }

    private class Token
    {
        public string Text { get; }

        public Token(string text)
        {
            Text = text;
        }
    }

    private void UpdateTitle()
    {
        var baseTitle = "Shader Editor";
        Title = _hasUnsavedChanges ? $"{baseTitle} •" : baseTitle;
    }

    private void Close()
    {
        if (_hasUnsavedChanges)
        {
            // Simple confirmation - in a real app you might want a proper dialog
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var result = await DisplayAlert(
                    "Unsaved Changes", 
                    "You have unsaved changes. Do you want to save before closing?", 
                    "Save", 
                    "Discard");
                    
                if (result)
                {
                    _callback?.Invoke(Editor.Text);
                }
                
                Application.Current?.CloseWindow(Application.Current.Windows.Last());
            });
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current?.CloseWindow(Application.Current.Windows.Last());
            });
        }
    }

    private async void ButtonSave_OnClicked(object sender, EventArgs e)
    {
        Apply();

        await Task.Delay(30);

        try
        {
            if (!HasError)
            {
                _originalCode = Editor.Text;
                _hasUnsavedChanges = false;
                UpdateTitle();

                Close();
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Error", $"Failed to save shader: {ex.Message}", "OK");
            });
        }
    }

    private void ButtonClose_OnClicked(object sender, EventArgs e)
    {
        Close();
    }

    void Apply()
    {
        try
        {
            // Hide any previous errors
            HideError();
            HasError = false;

            _callback?.Invoke(Editor.Text);
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Error", $"Failed to apply shader: {ex.Message}", "OK");
            });
        }
    }
    private void ButtonApply_OnClicked(object sender, EventArgs e)
    {
        Apply();
    }

    private void CloseErrorButton_OnClicked(object sender, EventArgs e)
    {
        HideError();
    }

    private async void CopyErrorButton_OnClicked(object sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(ErrorLabel.Text))
            {
                await Clipboard.SetTextAsync(ErrorLabel.Text);

                // Show brief feedback that copy was successful
                var originalText = CopyErrorButton.Text;
                CopyErrorButton.Text = "✓";
                CopyErrorButton.TextColor = Colors.LightGreen;

                // Reset after 1 second
                await Task.Delay(1000);
                CopyErrorButton.Text = originalText;
                CopyErrorButton.TextColor = Color.FromArgb("#d73a49");
            }
        }
        catch (Exception ex)
        {
            // Fallback if clipboard access fails
            await DisplayAlert("Copy Failed", $"Could not copy to clipboard: {ex.Message}", "OK");
        }
    }

    protected override bool OnBackButtonPressed()
    {
        Close();
        return true; // Prevent default back button behavior
    }

    private void Editor_OnFocused(object sender, FocusEventArgs e)
    {
        if (e.IsFocused)
        {
            OnEditorFocused(sender, e);
        }
        else
        {
            OnEditorUnfocused(sender, e);
        }
    }
}
