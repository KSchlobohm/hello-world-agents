# HelloWorldAgents.Console

A follow-along implementation of the console app from the Microsoft Agent Framework blog post:
[Introducing Microsoft Agent Framework Preview](https://devblogs.microsoft.com/dotnet/introducing-microsoft-agent-framework-preview/)

This README walks through each step in the blog's progression — with corrections for the APIs that don't match the published v1.3.0 NuGet packages.

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

The blog instructs you to create a new console app and add Agent Framework packages. Note that the stable v1.3.0 packages exist on NuGet — you don't need `--prerelease` for most of them.

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

### Blog code

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

AIAgent writer = new ChatClientAgent(
    chatClient,
    new ChatClientAgentOptions         // ❌ does not exist in v1.3.0
    {
        Name = "Writer",
        Instructions = "Write stories that are engaging and creative."
    });

AgentRunResponse response = await writer.RunAsync("Write a short story about a haunted house.");  // ❌ AgentRunResponse does not exist

Console.WriteLine(response.Text);
```

### ✅ Correction

`ChatClientAgentOptions` does not exist. Use the constructor overload with named parameters instead. `AgentRunResponse` does not exist; use `AgentResponse`.

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

---

## Step 3: Orchestrate Multiple Agents

### Blog code

The blog adds an editor agent and then connects the two in a sequential workflow:

```bash
dotnet add package Microsoft.Agents.AI.Workflows --prerelease
```

```csharp
AIAgent editor = new ChatClientAgent(
    chatClient,
    new ChatClientAgentOptions         // ❌ does not exist in v1.3.0
    {
        Name = "Editor",
        Instructions = "Make the story more engaging, fix grammar, and enhance the plot."
    });

Workflow workflow =
    AgentWorkflowBuilder
        .BuildSequential(writer, editor);   // ❌ no params overload — must pass a collection

AIAgent workflowAgent = await workflow.AsAgentAsync();  // ❌ does not exist in v1.3.0

AgentRunResponse workflowResponse =
    await workflowAgent.RunAsync("Write a short story about a haunted house."); // ❌ AgentRunResponse does not exist

Console.WriteLine(workflowResponse.Text);
```

### ✅ Correction

Three things to fix:

1. Use the constructor overload instead of `ChatClientAgentOptions`
2. Pass a collection to `BuildSequential`: `BuildSequential([writer, editor])`
3. `workflow.AsAgentAsync()` does not exist — use `InProcessExecution.Default.RunAsync()` instead

`RunAsync` streams the response as many `AgentResponseUpdateEvent` chunks rather than returning a single final message. To reconstruct the full output, collect all events, find the last executor's ID (the Editor in a sequential workflow), and concatenate its text chunks.

```csharp
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
```

---

## Step 4: Empower Agents with Tools

### Blog code

The blog adds two local functions as tools for the writer agent:

```csharp
[Description("Gets the author of the story.")]
string GetAuthor() => "Jack Torrance";

[Description("Formats the story for display.")]
string FormatStory(string title, string author, string story) =>
    $"Title: {title}\nAuthor: {author}\n\n{story}";

AIAgent writer = new ChatClientAgent(
    chatClient,
    new ChatClientAgentOptions         // ❌ does not exist in v1.3.0
    {
        Name = "Writer",
        Instructions = "Write stories that are engaging and creative.",
        ChatOptions = new ChatOptions  // ❌ ChatClientAgentOptions.ChatOptions does not exist
        {
            Tools = [
                AIFunctionFactory.Create(GetAuthor),
                AIFunctionFactory.Create(FormatStory)
            ],
        }
    });
```

### ✅ Correction

Use the `tools:` named parameter on the `ChatClientAgent` constructor instead:

```csharp
[Description("Gets the author of the story.")]
string GetAuthor() => "Jack Torrance";

[Description("Formats the story for display.")]
string FormatStory(string title, string author, string story) =>
    $"Title: {title}\nAuthor: {author}\n\n{story}";

ChatClientAgent writer = new ChatClientAgent(
    chatClient,
    name: "Writer",
    instructions: "Write stories that are engaging and creative.",
    tools: [
        AIFunctionFactory.Create(GetAuthor),
        AIFunctionFactory.Create(FormatStory)
    ]);
```

---

## Final Program.cs

Putting it all together — this is the complete, compiling Program.cs combining Steps 2, 3, and 4:

```csharp
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
    name: "Writer",
    tools: [
        AIFunctionFactory.Create(GetAuthor),
        AIFunctionFactory.Create(FormatStory)
    ]);

ChatClientAgent editor = new ChatClientAgent(
    chatClient,
    instructions: "Make the story more engaging, fix grammar, and enhance the plot.",
    name: "Editor");

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

[System.ComponentModel.Description("Gets the author of the story.")]
string GetAuthor() => "Jack Torrance";

[System.ComponentModel.Description("Formats the story for display.")]
string FormatStory(string title, string author, string story) =>
    $"Title: {title}\nAuthor: {author}\n\n{story}";
```

---

## API Correction Summary

| Blog (broken in v1.3.0) | Working equivalent |
|---|---|
| `new ChatClientAgentOptions { Name = ..., Instructions = ... }` | `new ChatClientAgent(chatClient, name: ..., instructions: ...)` |
| `ChatClientAgentOptions.ChatOptions.Tools` | `tools:` parameter on `ChatClientAgent` constructor |
| `AgentRunResponse` | `AgentResponse` (single agent) |
| `workflow.AsAgentAsync()` + `RunAsync()` | `InProcessExecution.Default.RunAsync(workflow, message, sessionId, token)` |
| `AgentWorkflowBuilder.BuildSequential(writer, editor)` | `AgentWorkflowBuilder.BuildSequential([writer, editor])` |
| Result from single `AgentResponseEvent` | Collect `AgentResponseUpdateEvent` stream, group by `ExecutorId`, concatenate `.Update.Text` |
