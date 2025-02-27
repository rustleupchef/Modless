using System.Text;
using Newtonsoft.Json;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Modless;

class Program
{
    private static string guidelines;
    private static string _model;
    private static int spamming;
    private static Dictionary<string, Chatter> chatters = new Dictionary<string, Chatter>();
    
    internal static void Main()
    {
        // read config json
        dynamic json = JsonConvert.DeserializeObject(File.ReadAllText("config.json"));
        string accessToken = json.access_token;
        string channel = json.channel;
        guidelines = json.guidelines;
        _model = json.model;
        spamming = json.spamming;

        // set default guidelines if hone are set
        if (string.IsNullOrEmpty(guidelines))
            guidelines = "No hate speech";

        // interpret guidelines through file paths
        if (File.Exists(guidelines))
            guidelines = File.ReadAllText(guidelines);
        
        Console.WriteLine($"Guidelines:\n{guidelines}");
        
        ConnectionCredentials credentials = new(channel, accessToken);
        TwitchClient client = new();
        
        client.Initialize(credentials, channel);
        client.Connect();

        client.OnMessageReceived += messaged;
        
        // halt execution
        Console.WriteLine("Connected! Press any key to exit");
        Console.ReadKey();
        Console.WriteLine("\nDisconnecting...");
        
        client.Disconnect();
    }

    private static void messaged(object sender, OnMessageReceivedArgs e)
    {
        if (!chatters.ContainsKey(e.ChatMessage.Username))
        {
            chatters.Add(e.ChatMessage.Username, new Chatter(1, DateTime.Now.ToString("HH:mm:ss")));
        }
        else
        {
            Chatter chatter = chatters[e.ChatMessage.Username];
            chatter.messages++;
            if (chatter.difference(DateTime.Now.ToString("HH:mm:ss")) > spamming)
                chatters.Add(e.ChatMessage.Username, new Chatter(1, DateTime.Now.ToString("HH:mm:ss")));
            else if (chatter.messages > 10)
                Console.WriteLine(e.ChatMessage.Username + " is spamming");
        }
        
        async Task run()
        {
            // load ollama 
            using HttpClient client = new();
            client.BaseAddress = new Uri("http://localhost:11434");
            
            // create json data
            var payload = new
            {
                model = _model,
                system =
                    $"Rules:\n" +
                    $"{guidelines}\n" +
                    $"You're a great sentence analyzer that determines if a sentence violates a these set of rules." +
                    $"If it does violate the rules you say negative otherwise say positive." +
                    $"The only words you are permitted to say is negative and positive",
                prompt = $"\"{e.ChatMessage.Message}\", does this message violate the rules you were given?",
                options = new
                {
                    num_predict = 1,
                    temperature = 0
                },
                stream = false
            };
            
            // generate response
            StringContent content = new(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync("/api/generate", content);


            // handle error
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.ReasonPhrase}");
                return;
            }
            
            // interpret text
            string text = await response.Content.ReadAsStringAsync();
            dynamic json = JsonConvert.DeserializeObject(text);
            text = (string) json.response;
            Console.WriteLine(text);
        }

        run();
    }
}