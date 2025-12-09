namespace agent.md.parser;

public static class AgentPromptManager
{
    private const string resourceFileName = "Agents.md";
    private const string agentMark = "## ";
    private const string promptMark = "### ";
    public static readonly AgentCache agentCache = new();
    public static bool VerboseLogging { get; set; } = false;

    public static void Initialize()
    {
        //var markdown = ReadFileContents(fileName, agentsMarkdownPath);
        var markdown = ReadEmbeddedResource(resourceFileName);
        if (!string.IsNullOrEmpty(markdown))
        {
            Parse(markdown);
        }
        else
        {
            throw new FileNotFoundException($"Agents configuration file not found at: {resourceFileName}");
        }

        Console.WriteLine("\nListing all agents and prompts found in definition file:");
        ListAgents();
    }

    public static void Parse(string markdown)
    {
        Console.WriteLine("Starting agent markdown parsing");

        if (string.IsNullOrWhiteSpace(markdown))
        {
            Console.WriteLine("Markdown content is empty or null");
            return;
        }

        bool inAgentSection = false;
        bool inPromptSection = false;

        var currentAgent = string.Empty;
        var currentInstructions = string.Empty;
        var currentPrompt = string.Empty;
        var promptsForAgent = new Dictionary<string, string>();
        var agentCount = 0;

        var lines = markdown.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (VerboseLogging) Console.WriteLine($"Parsing {lines.Length} lines of markdown");

        foreach (var line in lines)
        {
            if (inAgentSection)
            {
                if (inPromptSection)
                {
                    if (line.StartsWith(promptMark))
                    {
                        // Save previous prompt
                        if (!string.IsNullOrEmpty(currentPrompt))
                        {
                            if (VerboseLogging) Console.WriteLine($"Completed prompt '{currentPrompt}' for agent '{currentAgent}' ({currentInstructions.Trim().Length} chars)");
                            promptsForAgent[currentPrompt] = currentInstructions.Trim();
                        }

                        // Start new prompt
                        currentPrompt = line[promptMark.Length..].Trim();
                        if (VerboseLogging) Console.WriteLine($"Starting new prompt '{currentPrompt}' for agent '{currentAgent}'");
                        currentInstructions = string.Empty;
                    }
                    else if (line.StartsWith(agentMark))
                    {
                        // Save last prompt of the agent
                        if (!string.IsNullOrEmpty(currentPrompt))
                        {
                            if (VerboseLogging) Console.WriteLine($"Saving final prompt '{currentPrompt}' for agent '{currentAgent}'");
                            promptsForAgent[currentPrompt] = currentInstructions.Trim();
                        }

                        if (promptsForAgent.Any())
                        {
                            if (VerboseLogging) Console.WriteLine($"Finalizing agent '{currentAgent}' with {promptsForAgent.Count} prompts");
                            var agentEntry = MakeAgent(currentAgent, currentInstructions, new Dictionary<string, string>(promptsForAgent));
                            agentCache.AddAgent(agentEntry);
                            agentCount++;
                            if (VerboseLogging) Console.WriteLine($"Successfully cached agent '{agentEntry.AgentName}'");
                        }

                        currentAgent = line[agentMark.Length..].Trim();
                        currentInstructions = string.Empty;
                        currentPrompt = string.Empty;
                        promptsForAgent.Clear();
                        Console.WriteLine($"Found agent definition {agentCount + 1}: '{currentAgent}'");
                    }
                    else
                    {
                        // Accumulate instructions
                        currentInstructions += line + "\n";
                    }
                }
                else
                {
                    if (line.StartsWith(promptMark))
                    {
                        inPromptSection = true;
                        currentPrompt = line[promptMark.Length..].Trim();
                        if (VerboseLogging) Console.WriteLine($"Entering prompt section '{currentPrompt}' for agent '{currentAgent}'");
                        currentInstructions = string.Empty;
                    }
                    else if (line.StartsWith(agentMark))
                    {
                        // Start new agent
                        currentAgent = line[agentMark.Length..].Trim();
                        currentInstructions = string.Empty;
                        promptsForAgent.Clear();
                        if (VerboseLogging) Console.WriteLine($"Found agent definition {agentCount + 1}: '{currentAgent}'");
                    }
                }
            }
            else if (line.StartsWith(agentMark))
            {
                inAgentSection = true;
                inPromptSection = false;
                currentAgent = line[agentMark.Length..].Trim();
                Console.WriteLine($"Found agent definition {agentCount+1}: '{currentAgent}'");
            }
        }

        // Handle any remaining agent at end of file
        if (inAgentSection && !string.IsNullOrEmpty(currentAgent))
        {
            if (!string.IsNullOrEmpty(currentPrompt))
            {
                if (VerboseLogging) Console.WriteLine($"Processing final prompt '{currentPrompt}' for agent '{currentAgent}' at end of file");
                promptsForAgent[currentPrompt] = currentInstructions.Trim();
            }

            if (promptsForAgent.Any())
            {
                if (VerboseLogging) Console.WriteLine($"Finalizing last agent '{currentAgent}' with {promptsForAgent.Count} prompts");
                var agentEntry = MakeAgent(currentAgent, currentInstructions, new Dictionary<string, string>(promptsForAgent));
                agentCache.AddAgent(agentEntry);
                agentCount++;
                if (VerboseLogging) Console.WriteLine($"Successfully cached final agent '{agentEntry.AgentName}'");
            }
        }

        Console.WriteLine($"Agent markdown parsing complete: {agentCount} agents created!");
    }

    public static void ListAgents()
    {
        var agents = agentCache.GetAgents();
        for (int i = 0; i < agents.Length; i++)
        {
            Console.WriteLine($"{i+1}. Agent: {agents[i]}");
            var prompts = agentCache.GetPromptsForAgent(agents[i]);
            for (int j = 0; j < prompts.Length; j++)
            {
                Console.WriteLine($"   Prompt {j + 1}: {prompts[j]}");
            }
        }
    }

    public static string[] GetAgentList()
    {
        return agentCache.GetAgents();
    }

    public static string[] GetPromptsForAgent(string agentName)
    {
        return agentCache.GetPromptsForAgent(agentName);
    }

    public static string GetPrompt(string agentName, string promptKey)
    {
        return agentCache.GetPrompt(agentName, promptKey);
    }

    private static string ReadEmbeddedResource(string fileName)
    {
        var assembly = typeof(AgentPromptManager).Assembly;
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith($"{fileName}", StringComparison.OrdinalIgnoreCase));
        if (resourceName != null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream!);
            var content = reader.ReadToEnd();
            return content.Trim();
        }
        else
        {
            Console.WriteLine($"Embedded resource not found: {fileName}");
            throw new FileNotFoundException($"Embedded resource not found at: {fileName}");
        }
    }

    private static AgentCacheEntry MakeAgent(string name, string instructions, IDictionary<string, string> promptsForAgent)
    {
        if (VerboseLogging) Console.WriteLine($"Creating agent from markdown: {name}");
        var agentType = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        name = name[(agentType.Length)..].Trim();
        if (VerboseLogging) Console.WriteLine($"Agent type: {agentType}, Agent name: {name}, Prompt count: {promptsForAgent.Count}");
        foreach (var prompt in promptsForAgent)
        {
            if (VerboseLogging) Console.WriteLine($"Agent {name} prompt registered: {prompt.Key}");
        }

        // simplified use case -- just names and prompts
        return new AgentCacheEntry
        {
            AgentName = name,
            //Agent = agent,
            //ChatClient = chatClientUsed,
            //ImageGenerator = imageGenerator,
            Instructions = instructions,
            Prompts = new Dictionary<string, string>(promptsForAgent)
        };

        // complex use case -- create actual agents (commented out for now)
        //AIAgent? agent;
        //IChatClient? chatClientUsed = null;
        //IImageGenerator? imageGenerator = null;
        //switch (agentType.ToLowerInvariant())
        //{
        //    case "conversational":
        //        Console.WriteLine("Creating conversational agent: {AgentName}", name);
        //        chatClientUsed = client.GetConversationalClient();
        //        agent = chatClientUsed.CreateAIAgent(instructions, name, loggerFactory: factory);
        //        Console.WriteLine($"Created conversational agent: {name} with {promptsForAgent.Count} prompts");
        //        break;
        //    case "vision":
        //        Console.WriteLine("Creating vision agent: {AgentName}", name);
        //        chatClientUsed = client.GetVisionClient();
        //        agent = chatClientUsed.CreateAIAgent(instructions, name, loggerFactory: factory);
        //        Console.WriteLine($"Created vision agent: {name} with {promptsForAgent.Count} prompts");
        //        break;
        //    case "image":
        //        Console.WriteLine("Creating image generation agent: {AgentName}", name);
        //        imageGenerator = client.GetImageClient();
        //        var agentBase = client.GetVisionClient(); // agent doesn't direct support image generator yet
        //        agent = agentBase.CreateAIAgent(name: name, loggerFactory: factory);
        //        Console.WriteLine($"Created image generation agent: {name} with {promptsForAgent.Count} prompts");
        //        break;
        //    default:
        //        logger.LogError($"Unsupported agent type: {agentType} for agent: {name}");
        //        throw new NotSupportedException($"Agent type '{agentType}' is not supported.");
        //}

        // var agentName = agent.AgentName ?? throw new InvalidOperationException("Agent should have a unique name.");

        //foreach (var prompt in promptsForAgent)
        //{
        //    Console.WriteLine($"Agent {agent.AgentName} prompt registered: {prompt.Key}");
        //}

        //return new AgentCacheEntry
        //{
        //    AgentName = agent.AgentName,
        //    //Agent = agent,
        //    //ChatClient = chatClientUsed,
        //    //ImageGenerator = imageGenerator,
        //    Instructions = instructions,
        //    Prompts = new Dictionary<string, string>(promptsForAgent)
        //};
    }

    //public string ReadFileContents(string fileName, string? agentsMarkdownPath = null)
    //{
    //    var filePath = agentsMarkdownPath ?? Path.Combine(AppContext.BaseDirectory, fileName);
    //    if (File.Exists(filePath))
    //    {
    //        var data = File.ReadAllText(filePath);
    //        return data;
    //    }
    //    else
    //    {
    //        Console.WriteLine($"File contents not found at: {filePath}");
    //        throw new FileNotFoundException($"File contents not found at: {filePath}");
    //    }
    //}
}
