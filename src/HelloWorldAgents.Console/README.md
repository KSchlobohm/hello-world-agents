# HelloWorldAgents.Console

A follow-along implementation of the console app from the Microsoft Agent Framework blog post:
[Introducing Microsoft Agent Framework Preview](https://devblogs.microsoft.com/dotnet/introducing-microsoft-agent-framework-preview/)

---

## Step 0: Configure Prerequisites

You need:

- [.NET 9 SDK or greater](https://dotnet.microsoft.com/download)
- A GitHub Personal Access Token (PAT) with `models` scope — create one in your [GitHub settings](https://github.com/settings/tokens)

Set the token as an environment variable:

**Windows**
```cmd
setx GITHUB_TOKEN "YOUR-GITHUB-TOKEN"
```
Restart your shell to pick up the value.

**Linux / Mac**
```bash
export GITHUB_TOKEN="YOUR-GITHUB-TOKEN"
```

---

## Step 1: Set Up Your Project

Create a new console app and add the Agent Framework packages:

```bash
dotnet new console -o HelloWorldAgents.Console
cd HelloWorldAgents.Console
dotnet add package Microsoft.Agents.AI
dotnet add package Microsoft.Agents.AI.Workflows
dotnet add package OpenAI
dotnet add package Microsoft.Extensions.AI.OpenAI
dotnet add package Microsoft.Extensions.AI
```

---

## Step 2: Write Your First Agent

Add this code to `Program.cs` to create a story-writing agent:

```csharp
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
    name: "Writer",
    instructions: "Write stories that are engaging and creative.");

AgentResponse response = await writer.RunAsync("Write a short story about a haunted house.");

Console.WriteLine(response.Text);
```

Run your application:

```bash
dotnet run
```

That's it! In just a few lines of code, you have a fully functional AI agent.

---

## Step 3: Orchestrate Multiple Agents

Single agents are powerful, but real-world scenarios often require multiple specialized agents working together. Let's add an editor agent to review and improve the writer's output.

First, add the Workflows package if you haven't already:

```bash
dotnet add package Microsoft.Agents.AI.Workflows
```

Then add an editor agent and connect both in a sequential workflow:

```csharp
ChatClientAgent editor = new ChatClientAgent(
    chatClient,
    name: "Editor",
    instructions: "Make the story more engaging, fix grammar, and enhance the plot.");

Workflow workflow =
    AgentWorkflowBuilder
        .BuildSequential([writer, editor]);
```

Running a workflow uses `InProcessExecution`, which streams the response as `AgentResponseUpdateEvent` chunks. Collect those events, identify the last agent to run (the Editor), and concatenate its text to get the final output:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

await using Run run = await InProcessExecution.Default.RunAsync(
    workflow,
    new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "Write a short story about a haunted house."),
    "session-1",
    cts.Token);

var updates = run.OutgoingEvents
    .OfType<AgentResponseUpdateEvent>()
    .ToList();

string editorExecutorId = updates.Last().ExecutorId;

string story = string.Concat(
    updates
        .Where(e => e.ExecutorId == editorExecutorId)
        .Select(e => e.Update.Text));

Console.WriteLine(story);
```

Now when you run your application, the writer creates the initial story and the editor automatically reviews and improves it. The entire workflow appears to the outside world as a single, more capable agent.

---

## Step 4: Empower Agents with Tools

Agent Framework makes it easy to give agents access to external functions. Define the tools as local functions with a `[Description]` attribute, then pass them to the agent:

```csharp
ChatClientAgent writer = new ChatClientAgent(
    chatClient,
    name: "Writer",
    instructions: "Write stories that are engaging and creative.",
    tools: [
        AIFunctionFactory.Create(GetAuthor),
        AIFunctionFactory.Create(FormatStory)
    ]);

[Description("Gets the author of the story.")]
string GetAuthor() => "Jack Torrance";

[Description("Formats the story for display.")]
string FormatStory(string title, string author, string story) =>
    $"Title: {title}\nAuthor: {author}\n\n{story}";
```

Running the application produces a formatted story:

```
Title: The Haunting of Blackwood Manor
Author: Jack Torrance

On the outskirts of a quaint village, a grand but crumbling mansion...
```

---

## Final Program.cs

Putting it all together:

```csharp
using System.ComponentModel;
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
    name: "Writer",
    instructions: "Write stories that are engaging and creative.",
    tools: [
        AIFunctionFactory.Create(GetAuthor),
        AIFunctionFactory.Create(FormatStory)
    ]);

ChatClientAgent editor = new ChatClientAgent(
    chatClient,
    name: "Editor",
    instructions: "Make the story more engaging, fix grammar, and enhance the plot.");

Workflow workflow =
    AgentWorkflowBuilder
        .BuildSequential([writer, editor]);

using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

await using Run run = await InProcessExecution.Default.RunAsync(
    workflow,
    new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "Write a short story about a haunted house."),
    "session-1",
    cts.Token);

var updates = run.OutgoingEvents
    .OfType<AgentResponseUpdateEvent>()
    .ToList();

string editorExecutorId = updates.Last().ExecutorId;

string story = string.Concat(
    updates
        .Where(e => e.ExecutorId == editorExecutorId)
        .Select(e => e.Update.Text));

Console.WriteLine(story);

[Description("Gets the author of the story.")]
string GetAuthor() => "Jack Torrance";

[Description("Formats the story for display.")]
string FormatStory(string title, string author, string story) =>
    $"Title: {title}\nAuthor: {author}\n\n{story}";
```
