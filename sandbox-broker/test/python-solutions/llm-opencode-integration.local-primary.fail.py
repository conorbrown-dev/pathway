// expect: so a frontier model is the primary agent
{
  "$schema": "https://opencode.ai/config.json",
  // Primary agent: a frontier model
  "model": "local/commit-writer",
  "providers": {
    // llama-server running my fine-tuned model
    "local": {
      "name": "My fine-tuned models",
      "package": "@opencode/ai/providers/openai-compatible",
      "settings": { "baseURL": "http://127.0.0.1:8080/v1" },
      "models": {
        "commit-writer": { "name": "Commit writer (LoRA)", "limit": { "context": 16384, "output": 1024 } }
      }
    }
  },
  "agents": {
    "commit-writer": {
      "description": "Writes a commit message in the team's style from the staged diff. Use it whenever a commit message is needed.",
      "mode": "subagent",
      "model": "local/commit-writer",
      "permissions": [
        { "action": "*", "resource": "*", "effect": "deny" },
        { "action": "read", "resource": "*", "effect": "allow" }
      ]
    }
  }
}
