using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SimpleTriggers.Triggers;

public class TriggerTree
{
    [JsonProperty] private List<TriggerCategory> categories;
    public TriggerTree()
    {
        categories = [];
    }

    public TriggerTree(List<TriggerCategory> c)
    {
        categories = c;
    }

    public TriggerCategory? this[string categoryName]
    {
        get { return categories.FirstOrDefault(tt => tt.Name == categoryName); }
        set { categories.Add(new TriggerCategory(categoryName)); }
    }

    public void Add(TriggerCategory tc)
    {
        categories.Add(tc);
    }

    public void Insert(int index, TriggerCategory tc)
    {
        categories.Insert(index, tc);
    }

    public void RemoveAt(int index)
    {
        categories.RemoveAt(index);
    }

    public void Remove(TriggerCategory tc)
    {
        categories.Remove(tc);
    }

    public void SwapCategories(int idx1, int idx2)
    {
        var temp = new TriggerCategory(categories.ElementAt(idx1));
        categories[idx1] = categories[idx2];
        categories[idx2] = temp;
    }

    public void ClearAll()
    {
        categories.Clear();
    }

    public int GetIndexOfCategory(string name)
    {
        return GetIndexOfCategory(categories.FirstOrDefault(tc => tc.Name == name) ?? new TriggerCategory());
    }

    public int GetIndexOfCategory(TriggerCategory tc)
    {
        return categories.IndexOf(tc);
    }

    public TriggerCategory ElementAt(int idx)
    {
        return categories.ElementAt(idx);
    }

    [JsonIgnore] public int Count => categories.Count;

    public int CountAllTriggers()
    {
        var c = 0;
        foreach (var category in categories)
        {
            c += categories.Count;
        }
        return c;
    }

    public IEnumerator<TriggerCategory> GetEnumerator()
    {
        foreach(var tc in categories)
        {
            yield return tc;
        }
    }
}
