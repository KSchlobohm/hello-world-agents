# HelloWorldAgents.API

A follow-along implementation of the Minimal Web API from the Microsoft Agent Framework blog post:
[Introducing Microsoft Agent Framework Preview — Deploy with Confidence: Hosting Made Simple](https://devblogs.microsoft.com/dotnet/introducing-microsoft-agent-framework-preview/#deploy-with-confidence:-hosting-made-simple)

---

## Step 0: Configure Prerequisites

You need:

- [.NET 9 SDK or greater](https://dotnet.microsoft.com/download)
- A GitHub Personal Access Token (PAT) with `models` scope — create one in your [GitHub settings](https://github.com/settings/tokens)

---

## Step 1: Set Up Your Project

Create a new Minimal Web API and add the required packages:

```bash
dotnet new webapi -o HelloWorldAgents.API
cd HelloWorldAgents.API
dotnet add package Aspire.OpenAI --prerelease
dotnet add package Microsoft.Agents.AI
dotnet add package Microsoft.Agents.AI.Workflows
dotnet add package Microsoft.Agents.AI.Hosting --prerelease
```

---

## Step 2: Configure the GitHub Models Connection String

The `Aspire.OpenAI` package reads your model endpoint and key from a named connection string. Store it in user secrets so it never lands in source control:

```bash
dotnet user-secrets init
dotnet user-secrets set ConnectionStrings:chat "Endpoint=https://models.github.ai/inference;Key=YOUR-GITHUB-TOKEN"
```

Set the model name you want to use:

**Windows**
```cmd
setx MODEL_NAME "gpt-4o-mini"
```

**Linux / Mac**
```bash
export MODEL_NAME="gpt-4o-mini"
```

---

## Step 3: Register the Chat Client

In `Program.cs`, read the connection string and register an `IChatClient` in the DI container:

```csharp
builder.AddOpenAIClient("chat")
    .AddChatClient(Environment.GetEnvironmentVariable("MODEL_NAME") ?? "gpt-4o-mini");
```

---

## Step 4: Register Your Agents

Use `AddAIAgent` from `Microsoft.Agents.AI.Hosting` to register agents as keyed services. The writer agent also receives tools:

```csharp
builder.AddAIAgent("Writer", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    return new ChatClientAgent(
        chatClient,
        name: key,
        instructions:
            """
            You are a creative writing assistant who crafts vivid,
            well-structured stories with compelling characters based on user prompts,
            and formats them after writing.
            """,
        tools: [
            AIFunctionFactory.Create(GetAuthor),
            AIFunctionFactory.Create(FormatStory)
        ]
    );
});

builder.AddAIAgent(
    name: "Editor",
    instructions:
        """
        You are an editor who improves a writer's draft by providing 4-8 concise recommendations and
        a fully revised Markdown document, focusing on clarity, coherence, accuracy, and alignment.
        """);
```

---

## Step 5: Map the Endpoint

Resolve the agents by name, wire them into a sequential workflow, and return the final output:

```csharp
app.MapGet("/agent/chat", async (
    [FromKeyedServices("Writer")] AIAgent writer,
    [FromKeyedServices("Editor")] AIAgent editor,
    string prompt,
    CancellationToken cancellationToken) =>
{
    Workflow workflow =
        AgentWorkflowBuilder
            .BuildSequential([writer, editor]);

    await using Run run = await InProcessExecution.Default.RunAsync(
        workflow, prompt, "session-1", cancellationToken);

    var updates = run.OutgoingEvents
        .OfType<AgentResponseUpdateEvent>()
        .ToList();

    string lastExecutorId = updates.Last().ExecutorId;

    string result = string.Concat(
        updates
            .Where(e => e.ExecutorId == lastExecutorId)
            .Select(e => e.Update.Text));

    return Results.Ok(result);
});
```

`InProcessExecution.Default.RunAsync` streams the response as `AgentResponseUpdateEvent` chunks. The last executor in a sequential workflow is always the Editor, so grouping by `ExecutorId` and taking the last one gives the final polished output.

---

## Running the App

```bash
dotnet run
```

Then call the endpoint:

```
GET https://localhost:{port}/agent/chat?prompt=Write a short story about a haunted house.
```

Or use the included `.http` file in the project.

---

## Final Program.cs

```csharp
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.AddOpenAIClient("chat")
    .AddChatClient(Environment.GetEnvironmentVariable("MODEL_NAME") ?? "gpt-4o-mini");

builder.AddAIAgent("Writer", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    return new ChatClientAgent(
        chatClient,
        name: key,
        instructions:
            """
            You are a creative writing assistant who crafts vivid,
            well-structured stories with compelling characters based on user prompts,
            and formats them after writing.
            """,
        tools: [
            AIFunctionFactory.Create(GetAuthor),
            AIFunctionFactory.Create(FormatStory)
        ]
    );
});

builder.AddAIAgent(
    name: "Editor",
    instructions:
        """
        You are an editor who improves a writer's draft by providing 4-8 concise recommendations and
        a fully revised Markdown document, focusing on clarity, coherence, accuracy, and alignment.
        """);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseStaticFiles();
}

app.UseHttpsRedirection();

app.MapGet("/agent/chat", async (
    [FromKeyedServices("Writer")] AIAgent writer,
    [FromKeyedServices("Editor")] AIAgent editor,
    string prompt,
    CancellationToken cancellationToken) =>
{
    Workflow workflow =
        AgentWorkflowBuilder
            .BuildSequential([writer, editor]);

    await using Run run = await InProcessExecution.Default.RunAsync(
        workflow, prompt, "session-1", cancellationToken);

    var updates = run.OutgoingEvents
        .OfType<AgentResponseUpdateEvent>()
        .ToList();

    string lastExecutorId = updates.Last().ExecutorId;

    string result = string.Concat(
        updates
            .Where(e => e.ExecutorId == lastExecutorId)
            .Select(e => e.Update.Text));

    return Results.Ok(result);
});

app.Run();

[Description("Gets the author of the story.")]
string GetAuthor() => "Jack Torrance";

[Description("Formats the story for display.")]
string FormatStory(string title, string author, string story) =>
    $"Title: {title}\nAuthor: {author}\n\n{story}";
```
