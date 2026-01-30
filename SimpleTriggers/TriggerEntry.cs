namespace SimpleTriggers;

public class TriggerEntry
{
    public string expression = "";
    public string response = "";
    public bool doPostInChat = false;
    public bool doResponseTTS = false;
    public bool doPlaySound = false;
    public int soundFx = 0;

    public TriggerEntry()
    { }

    public TriggerEntry(TriggerEntry te)
    {
        this.expression = te.expression;
        this.response = te.response;
        this.doPostInChat = te.doPostInChat;
        this.doResponseTTS = te.doResponseTTS;
        this.doPlaySound = te.doPlaySound;
        this.soundFx = te.soundFx;
    }
}