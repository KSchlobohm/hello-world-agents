using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
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

ChatClientAgent editor = new ChatClientAgent(
    chatClient,
    instructions: "Make the story more engaging, fix grammar, and enhance the plot.",
    name: "Editor");

// Create a workflow that connects writer to editor
Workflow workflow =
    AgentWorkflowBuilder
        .BuildSequential([writer, editor]);

using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

await using Run run = await InProcessExecution.Default.RunAsync(
    workflow,
    new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "Write a short story about a haunted house."),
    "session-1",
    cts.Token);

// The workflow emits streaming AgentResponseUpdateEvent chunks.
// The final edited story comes from the last executor (Editor).
// Identify the Editor's executor ID and concatenate its text chunks.
var updates = run.OutgoingEvents
    .OfType<AgentResponseUpdateEvent>()
    .ToList();

string editorExecutorId = updates.Last().ExecutorId;

string story = string.Concat(
    updates
        .Where(e => e.ExecutorId == editorExecutorId)
        .Select(e => e.Update.Text));

Console.WriteLine(story);
