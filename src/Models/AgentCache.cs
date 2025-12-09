namespace agent.md.parser.Models;

public class AgentCache
{
    private readonly Dictionary<string, AgentCacheEntry> _cache = [];

    public void AddAgent(AgentCacheEntry entry)
    {
        _cache[entry.AgentName] = entry;
    }

    public string[] GetAgents()
    {
        return _cache.Keys.ToArray();

    }
    public string[] GetPromptsForAgent(string agentName)
    {
        if (_cache.TryGetValue(agentName, out AgentCacheEntry? entry))
        {
            return [.. entry.Prompts.Keys];
        }
        else
        {
            throw new KeyNotFoundException($"Agent '{agentName}' not found in cache.");
        }
    }
    public string[] GetPromptKeysForAgent(string agentName)
    {
        if (_cache.TryGetValue(agentName, out AgentCacheEntry? entry))
        {
            return [.. entry.Prompts.Keys];
        }
        else
        {
            throw new KeyNotFoundException($"Agent '{agentName}' not found in cache.");
        }
    }

    public string GetPrompt(string agentName, string promptKey)
    {
        if (_cache.TryGetValue(agentName, out AgentCacheEntry? entry))
        {
            if (entry.Prompts.TryGetValue(promptKey, out string? prompt))
            {
                return prompt;
            }
            else
            {
                throw new KeyNotFoundException($"Prompt key '{promptKey}' not found for agent '{agentName}'.");
            }
        }
        else
        {
            throw new KeyNotFoundException($"Agent '{agentName}' not found in cache.");
        }
    }
}

public class AgentCacheEntry
{
    public string AgentName { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;

    public string this[string promptKey]
    {
        get => Prompts.TryGetValue(promptKey, out string? value) ? value : string.Empty;
        set => Prompts[promptKey] = value;
    }

    //public required AIAgent Agent { get; set; }
    //public IChatClient? ChatClient { get; set; }
    //public IImageGenerator? ImageGenerator { get; set; }

    public IDictionary<string, string> Prompts { get; set; } = new Dictionary<string, string>();
}

