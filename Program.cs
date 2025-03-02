using System.Text;
using Newtonsoft.Json;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using TwitchLib.Client.Extensions;
using TwitchLib.Communication.Events;

namespace Modless;

class Program
{
    private static TwitchClient client;
    private static string guidelines;
    private static string _model;
    private static string channel;
    private static int spamming;
    private static int timeout;
    private static int banned;
    private static Dictionary<string, Chatter> chatters = new Dictionary<string, Chatter>();
    private static Dictionary<string, int> timeouts = new Dictionary<string, int>();
    
    internal static void Main()
    {
        // read config json
        dynamic json = JsonConvert.DeserializeObject(File.ReadAllText("config.json"));
        string accessToken = json.access_token;
        channel = json.channel;
        guidelines = json.guidelines;
        _model = json.model;
        spamming = json.spamming;
        timeout = json.timeout;
        banned = json.banned;

        // set default guidelines if hone are set
        if (string.IsNullOrEmpty(guidelines))
            guidelines = "No hate speech";

        // interpret guidelines through file paths
        if (File.Exists(guidelines))
            guidelines = File.ReadAllText(guidelines);
        
        Console.WriteLine($"Guidelines:\n{guidelines}");
        
        ConnectionCredentials credentials = new(channel, accessToken);
        client = new();
        
        client.Initialize(credentials, channel);
        client.Connect();
        client.OnConnected += connected;

        client.OnMessageReceived += messaged;
        
        // halt execution
        Console.WriteLine("Connected! Press any key to exit");
        Console.ReadKey();
        Console.WriteLine("\nDisconnecting...");
        
        client.SendMessage(channel, "Goodbye. The Modless bot will stop moderating this steam");
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
            {
                chatters.Remove(e.ChatMessage.Username);
                chatters.Add(e.ChatMessage.Username, new Chatter(1, DateTime.Now.ToString("HH:mm:ss")));
            }
            else if (chatter.messages > 10)
            {
                client.SendMessage(channel, $"{e.ChatMessage.Username} is spamming");
                if (chatter.messages > 15)
                {
                    timeoutUser(e.ChatMessage.Username, $"{e.ChatMessage.Username} will be timed out for {timeout} minutes");
                    chatters.Remove(e.ChatMessage.Username);
                }
            }
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
            if (text.ToLower().Contains("neg"))
                timeoutUser(e.ChatMessage.Username, $"{e.ChatMessage.Username} will be timed out for {timeout} minutes");
        }

        run();
    }

    private static void connected(object? sender, OnConnectedArgs e)
    {
        client.SendMessage(channel, "Hello Twitch! The Modless bot will be moderating this steam");
    }

    private static void timeoutUser(string user, string message)
    {
        client.SendMessage(channel, message);
        client.TimeoutUser(channel, user, TimeSpan.FromMinutes(timeout), message);
        if (timeouts.ContainsKey(user))
            timeouts[user]++;
        else
            timeouts.Add(user, 1);
        if (timeouts[user] > banned)
        {
            client.SendMessage(channel, $"{user} will be banned");
            client.BanUser(channel, user);
        }
    }
}