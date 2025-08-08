namespace TelePilot;

public static class SecretPrompt
{
    public static string? ShowDialog(string caption)
    {
        Form prompt = new Form()
        {
            Width = 400,
            Height = 150,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = caption,
            StartPosition = FormStartPosition.CenterScreen
        };

        Label textLabel = new Label() { Left = 20, Top = 20, Text = $"Enter {caption}: ", AutoSize = true };
        TextBox inputBox = new TextBox() { Left = 20, Top = 50, Width = 340, UseSystemPasswordChar = true };
        Button confirmation = new Button() { Text = "OK", Left = 280, Width = 80, Top = 80, DialogResult = DialogResult.OK };

        confirmation.Click += (_, _) => { prompt.Close(); };

        prompt.Controls.Add(textLabel);
        prompt.Controls.Add(inputBox);
        prompt.Controls.Add(confirmation);
        prompt.AcceptButton = confirmation;

        return prompt.ShowDialog() == DialogResult.OK ? inputBox.Text : null;
    }
}