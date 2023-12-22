using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Bert.Nuget;
using AsyncCommand;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using System;
using Bert.Storage;
using Bert.Wpf.Commands;

namespace Bert.Wpf.ViewModels;

public class AnswerViewModel : ViewModelBase
{
    private ObservableCollection<string> _chatMessages = new ObservableCollection<string>();

    public ObservableCollection<string> ChatMessages
    {
        get
        {
            return _chatMessages;
        }
        set
        {
            OnPropertyChanged(nameof(ChatMessages));    
        }
    }


    private string _question;
    public string Question
    {
        get
        {
            return _question;
        }
        set
        {
            _question = value;
            OnPropertyChanged(nameof(Question));
        }
    }

    public string Text { get; set; }

    public bool IsTextDownloaded { get; set; } = false;

    private string _statusMessage;
    public string StatusMessage
    {
        get
        {
            return _statusMessage;
        }
        set
        {
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public ICommand AnswerCommand { get; }

    public ICommand CancelCommand { get; }


    private BertModelConnection _bertModelConnection;

    private JsonStorage _storage;

    public CancellationTokenSource cts { get; set; }

    public AnswerViewModel(BertModelConnection bertModelConnection, JsonStorage storage)
    {
        AnswerCommand = new AsyncRelayCommand(Execute);
        CancelCommand = new RelayCommand(Cancel);
        _bertModelConnection = bertModelConnection;
        _storage = storage;
    }

    public void Cancel(object parameter)
    {
        StatusMessage = "Canceled";
        cts.Cancel();
    }

    public async Task Execute(object parameter)
    {
        if (Question == "/load")
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                Text = File.ReadAllText(filePath);
            }
            ChatMessages.Add(Text);
            IsTextDownloaded = true;
            return;
        }
        
        var answerFromHistory = await _storage.AnswerAlreadyAskedQuestion(Question, Text);
        if (answerFromHistory != null)
        {
            ChatMessages.Add(Question);
            ChatMessages.Add(answerFromHistory);
            StatusMessage = "Successfully answered.";
            return;
        }
        
        if (!IsTextDownloaded)
        {
            ChatMessages.Add("Please choose text");
        }
        else
        {
            StatusMessage = "Answering...";
            await _bertModelConnection.CreateOrDownloadAsync("https://storage.yandexcloud.net/dotnet4/bert-large-uncased-whole-word-masking-finetuned-squad.onnx");
            cts = new CancellationTokenSource();
            var token = cts.Token;
            try
            {
                ChatMessages.Add(Question);
                var answer = await _bertModelConnection.ExecuteAsync(Question, Text, token);

                ChatMessages.Add(answer);
                await _storage.AddQuestionToHistory(Question, answer);
                StatusMessage = "Successfully answered.";
            }
            catch(Exception ex) 
            {
                MessageBox.Show(ex.Message);
            }
        }

    }
}