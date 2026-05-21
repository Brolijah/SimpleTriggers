namespace SimpleTriggers.Triggers;

public class TriggerEntry
{
    public string expression = "";
    public string response = "";
    public bool enabled = true;
    public bool doPostInChat = false;
    public bool doResponseTTS = false;
    public bool doPlaySound = false;
    public bool doPopup = false;
    public PopupStyle popupStyle = PopupStyle.Toast;
    public int soundFx = 1;

    public TriggerEntry()
    { }

    public TriggerEntry(string expression)
    {
        this.expression = expression;
    }

    public TriggerEntry(TriggerEntry te)
    {
        this.expression = te.expression;
        this.response = te.response;
        this.enabled = te.enabled;
        this.doPostInChat = te.doPostInChat;
        this.doResponseTTS = te.doResponseTTS;
        this.doPlaySound = te.doPlaySound;
        this.doPopup = te.doPopup;
        this.popupStyle = te.popupStyle;
        this.soundFx = te.soundFx;
    }
}