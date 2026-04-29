using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

IChatClient chatClient =
    new ChatClient(
            "gpt-4o-mini",
            new ApiKeyCredential(Environment.GetEnvironmentVariable("GITHUB_TOKEN")!),
            new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") })
        .AsIChatClient();

ChatClientAgent writer = new ChatClientAgent(
    chatClient,
    instructions: "Write stories that are engaging and creative.",
    name: "Writer");

AgentResponse response = await writer.RunAsync("Write a short story about a haunted house.");

Console.WriteLine(response.Text);