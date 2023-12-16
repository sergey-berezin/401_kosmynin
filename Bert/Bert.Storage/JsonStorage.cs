using Newtonsoft.Json;

namespace Bert.Storage;

public class JsonStorage
{
    public async Task AddQuestionToHistory(string question, string answer)
    {
        var tmpPath = Path.GetTempPath();
        var path = Path.Combine(tmpPath, "storage.json");
        if (!File.Exists(path))
        {
            File.Create(path);
        }
        
        string jsonString = await File.ReadAllTextAsync(path);
        var newHistoryObject = new HistoryObject
        {
            Question = question,
            Answer = answer
        };
        
        var list = JsonConvert.DeserializeObject<List<HistoryObject>>(jsonString) ?? new List<HistoryObject>();
        list.Add(newHistoryObject);
        var convertedJson = JsonConvert.SerializeObject(list);
        await File.WriteAllTextAsync(path, convertedJson);
    }

    public async Task<string?> AnswerAlreadyAskedQuestion(string question)
    {
        var tmpPath = Path.GetTempPath();
        var path = Path.Combine(tmpPath, "storage.json");
        if (!File.Exists(path))
        {
            File.Create(path);
        }
        string jsonString = await File.ReadAllTextAsync(path);
        var history = JsonConvert.DeserializeObject<List<HistoryObject>>(jsonString);
        if (history != null)
            foreach (var historyObject in history)
            {
                if (historyObject.Question == question)
                {
                    return historyObject.Answer + " " + "This question was already answered earlier";
                }
            }
        return null;
    }
}

