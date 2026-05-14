internal interface IUserPrompt
{
    void Write(string text);
    void WriteLine(string text = "");
    string? ReadLine();
}
