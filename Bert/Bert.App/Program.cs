using Bert.Nuget;

namespace Bert.App;

class Program
{
    public static async Task Main(string[] args)
    { 
        var bertModelConnection = BertModelConnection.GetBertModelConnection();
        await bertModelConnection.CreateOrDownloadAsync("https://storage.yandexcloud.net/dotnet4/bert-large-uncased-whole-word-masking-finetuned-squad.onnx");
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        
        string filePath = args[0];
        string text = await File.ReadAllTextAsync(filePath, token);
        
        var answerText = await bertModelConnection.ExecuteAsync(text, token);
        Console.WriteLine(answerText);
        var taskList = new List<Task>();
        while (!token.IsCancellationRequested)
        {
            var prompt = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(prompt))
            {
                cts.Cancel();
                token.ThrowIfCancellationRequested();
            }
            try
            {
                var answer =  bertModelConnection.ExecuteAsync(prompt, token).ContinueWith(answer =>
                {
                    Console.WriteLine(answer.Result);
                    taskList.Add(answer);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }await Task.WhenAll(taskList);
    }
}