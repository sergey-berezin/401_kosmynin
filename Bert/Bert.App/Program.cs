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
        
        var taskList = new List<Task>();
        while (!token.IsCancellationRequested)
        {
            var question = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(question))
            {
                cts.Cancel();
                token.ThrowIfCancellationRequested();
            }
            try
            {
                var answer =  bertModelConnection.ExecuteAsync(question, text, token).ContinueWith(answer =>
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