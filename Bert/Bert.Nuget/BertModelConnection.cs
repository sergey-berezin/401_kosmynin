using System.Net;
using BERTTokenizers;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Bert.Nuget;

public class BertModelConnection
{
    private static BertModelConnection _bertModelConnection = new BertModelConnection();

    private BertModelConnection()
    {

    }

    public static BertModelConnection GetBertModelConnection()
    {
        return _bertModelConnection;
    }

    private static InferenceSession session;

    private static Semaphore sessionSemaphore = new Semaphore(1, 1);

    public async Task CreateOrDownloadAsync(string url)
    {
        String path = @"C:\Users\Stas\Desktop\Bert\401_kosmynin\Bert\Bert.App\bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
        if (!File.Exists(path))
        {
            var maxAttempts = 3;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    await DownloadModelAsync(url);
                    return; 
                }
                catch (Exception ex)
                {
                    attempt++;
                    if (attempt == maxAttempts)
                    {
                        Console.WriteLine(ex);
                        throw;
                    }
                }
            }
        }
        session = new InferenceSession(path);
    }

    public async Task DownloadModelAsync(string url)
    {
        String path = @"C:\Users\Stas\Desktop\Bert\401_kosmynin\Bert\Bert.App\bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
        using (var httpClient = new HttpClient())
        {
            var stream = await httpClient.GetStreamAsync(url);

            using (var fileStream = new FileStream(path, FileMode.CreateNew))
            {
                await stream.CopyToAsync(fileStream);
            }
        }
    }

    public Task<string> ExecuteAsync(string prompt, CancellationToken token)
    {
        return Task.Factory.StartNew(() =>
        {
            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();

            // Create Tokenizer and tokenize the sentence.
            var tokenizer = new BertUncasedLargeTokenizer();

            // Get the sentence tokens.
            var tokens = tokenizer.Tokenize(prompt);
            // Console.WriteLine(String.Join(", ", tokens));

            // Encode the sentence and pass in the count of the tokens in the sentence.
            var encoded = tokenizer.Encode(tokens.Count(), prompt);

            // Break out encoding to InputIds, AttentionMask and TypeIds from list of (input_id, attention_mask, type_id).
            var bertInput = new BertInput()
            {
                InputIds = encoded.Select(t => t.InputIds).ToArray(),
                AttentionMask = encoded.Select(t => t.AttentionMask).ToArray(),
                TypeIds = encoded.Select(t => t.TokenTypeIds).ToArray(),
            };

            // Create input tensor.

            var input_ids = ConvertToTensor(bertInput.InputIds, bertInput.InputIds.Length);
            var attention_mask = ConvertToTensor(bertInput.AttentionMask, bertInput.InputIds.Length);
            var token_type_ids = ConvertToTensor(bertInput.TypeIds, bertInput.InputIds.Length);


            // Create input data for session.
            var input = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", input_ids),
                NamedOnnxValue.CreateFromTensor("input_mask", attention_mask),
                NamedOnnxValue.CreateFromTensor("segment_ids", token_type_ids)
            };

            // Create an InferenceSession from the Model Path.
            IDisposableReadOnlyCollection<DisposableNamedOnnxValue>? output;
            sessionSemaphore.WaitOne();
            try
            {
                output = session.Run(input);
            }
            catch (Exception ex)
            {
                sessionSemaphore.Release();
                throw;
            }
            sessionSemaphore.Release();

            // Call ToList on the output.
            // Get the First and Last item in the list.
            // Get the Value of the item and cast as IEnumerable<float> to get a list result.
            List<float> startLogits = (output.ToList().First().Value as IEnumerable<float>).ToList();
            List<float> endLogits = (output.ToList().Last().Value as IEnumerable<float>).ToList();

            // Get the Index of the Max value from the output lists.
            var startIndex = startLogits.ToList().IndexOf(startLogits.Max());
            var endIndex = endLogits.ToList().IndexOf(endLogits.Max());

            // From the list of the original tokens in the sentence
            // Get the tokens between the startIndex and endIndex and convert to the vocabulary from the ID of the token.
            var predictedTokens = tokens
                .Skip(startIndex)
                .Take(endIndex + 1 - startIndex)
                .Select(o => tokenizer.IdToToken((int)o.VocabularyIndex))
                .ToList();

            // Print the result.
            return String.Join(" ", predictedTokens);
        }, token);
    }
   public static Tensor<long> ConvertToTensor(long[] inputArray, int inputDimension)
   {
       // Create a tensor with the shape the model is expecting. Here we are sending in 1 batch with the inputDimension as the amount of tokens.
       Tensor<long> input = new DenseTensor<long>(new[] { 1, inputDimension });

       // Loop through the inputArray (InputIds, AttentionMask and TypeIds)
       for (var i = 0; i < inputArray.Length; i++)
       {
           // Add each to the input Tenor result.
           // Set index and array value of each input Tensor.
           input[0,i] = inputArray[i];
       }
       return input;
   }
}
public class BertInput
{
    public long[] InputIds { get; set; }
    public long[] AttentionMask { get; set; }
    public long[] TypeIds { get; set; }
}
