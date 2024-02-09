
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Azure.AI.OpenAI.Assistants;
using CsvHelper;
using CsvHelper.Configuration;

Console.WriteLine("Hi there!");
Console.WriteLine("This is a client that will ask custom ChatGPT assistant to answer examination questions.");
Console.WriteLine();

var fileOption = new Option<FileInfo?>(name: "--questionsFile", description: "The file to read questions from.");

var rootCommand = new RootCommand("A client that will ask custom ChatGPT assistant to answer examination questions");
rootCommand.AddOption(fileOption);
rootCommand.SetHandler(async (context) =>
{

    var questionsFile = context.ParseResult.GetValueForOption(fileOption);
    if (questionsFile is null)
    {
        Console.WriteLine("Please provide a file with questions using --questionsFile option.");
        return;
    }

    if (!questionsFile.Exists)
    {
        Console.WriteLine($"File {questionsFile.FullName} does not exist.");
        return;
    }

    var questions = new List<Question>();

    var configuration = new CsvConfiguration(CultureInfo.GetCultureInfo("pl-PL"))
    {
        Delimiter = ",",
        HasHeaderRecord = true,
    };
    using (var reader = new StreamReader(questionsFile.FullName))
    using (var csv = new CsvReader(reader, configuration))
        questions.AddRange(csv.GetRecords<Question>());

    var chatApiKey = Environment.GetEnvironmentVariable("UKE_CHAT_KEY");
    var client = new AssistantsClient(chatApiKey);


    var assistant = (await client.GetAssistantAsync("asst_ZOAt9skmml9OsvboIXAz9O5j")).Value;
    Console.WriteLine($"Assistant: {assistant.Name}");
    Console.WriteLine();

    foreach (var question in questions.Where(q => string.IsNullOrEmpty(q.uzasadnienie)).Take(1))
    {
        Console.WriteLine("------------------------------------------------------------------------------------------");
        var query = $"{question.pytanie}\na. {question.odpa}\nb. {question.odpb}\nc. {question.odpc}";
        Console.WriteLine(query);
        Console.WriteLine();

        var threadRun = (await client.CreateThreadAndRunAsync(
            new CreateAndRunThreadOptions(assistant.Id)
            {
                Thread = new AssistantThreadCreationOptions
                {
                    Messages = {
                    new ThreadInitializationMessage(MessageRole.User, query)
                    }
                }
            })).Value;

        Debug.WriteLine($"Created thread: {threadRun.ThreadId} run: {threadRun.Id} status: {threadRun.Status}");

        var run = (await client.GetRunAsync(threadRun.ThreadId, threadRun.Id)).Value;
        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
        {
            Debug.WriteLine($"Run: {run.Id} status: {run.Status}. Waiting...");
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            run = (await client.GetRunAsync(threadRun.ThreadId, threadRun.Id)).Value;
        }

        var messages = (await client.GetMessagesAsync(threadRun.ThreadId)).Value;
        var assistantMessages = messages.Where(m => m.Role == MessageRole.Assistant).ToList();

        var answer = new StringBuilder();
        foreach (var message in assistantMessages)
        {
            answer.AppendLine(message.ContentItems.Where(c => c is MessageTextContent).Cast<MessageTextContent>().First().Text);
        }

        Console.WriteLine(answer.ToString());

        question.uzasadnienie = answer.ToString();

        using (var writer = new StreamWriter("questions.csv", false))
        using (var csv = new CsvWriter(writer, configuration))
        {
            csv.WriteRecords(questions);
        }
        Console.WriteLine();
    }



    //write questions to csv
    // using (var writer = new StreamWriter("questions.csv", false))
    // using (var csv = new CsvWriter(writer, configuration))
    // {
    //     csv.WriteRecords(questions);
    // }
});

return await rootCommand.InvokeAsync(args);

class Question
{
    public int id { get; set; }
    public string pytanie { get; set; }
    public string ilustracja { get; set; }
    public string odpa { get; set; }
    public string odpb { get; set; }
    public string odpc { get; set; }
    public string odp { get; set; }
    public string uzasadnienie { get; set; }
}
