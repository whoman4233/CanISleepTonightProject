public interface IDialogueView
{
    bool IsOpen { get; }
    void Show();
    void Hide();

    void SetSpeaker(string speakerName);
    void SetContent(string content);
    void SetMaxVisibleCharacters(int count);

}
