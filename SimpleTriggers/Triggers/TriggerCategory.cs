using System.Collections.Generic;
using System.Linq;

namespace SimpleTriggers.Triggers;

public class TriggerCategory
{
    public string Name;
    public bool enabled = true;
    public bool opened = true;
    public List<TriggerEntry> Triggers;

    public TriggerCategory(string name)
    {
        Name = name;
        Triggers = [];
    }

    public TriggerCategory(string name, List<TriggerEntry> t)
    {
        Name = name;
        Triggers = [];
        foreach(var te in t)
        {
            Triggers.Add(new TriggerEntry(te));
        }
    }

    public TriggerCategory(TriggerCategory tc)
    {
        Name = tc.Name;
        enabled = tc.enabled;
        opened = tc.opened;
        Triggers = [];
        foreach(var te in tc.Triggers)
        {
            Triggers.Add(new TriggerEntry(te));
        }
    }

    public TriggerCategory()
    {
        Name = "";
        Triggers = [];
    }

    public void Add(TriggerEntry te)
    {
        Triggers.Add(te);
    }

    public void Remove(TriggerEntry te)
    {
        Triggers.Remove(te);
    }

    public bool IsEmpty() => Triggers.Count == 0;

    public void SwapTriggers(int idx1, int idx2)
    {
        var temp = new TriggerEntry(Triggers.ElementAt(idx1));
        Triggers[idx1] = Triggers[idx2];
        Triggers[idx2] = temp;
    }
}