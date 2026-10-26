{
  "$schema": "https://opencode.ai/config.json",
  "model": "openai/gpt-codex-latest",
  "provider": {
    "ollama": {
      "npm": "@ai-sdk/openai-compatible",
      "name": "Ollama (local)",
      "options": { "baseURL": "http://localhost:11434/v1" },
      "models": {
        "ticket-labeler": { "name": "Ticket labeler", "limit": { "context": 32768, "output": 512 } }
      }
    }
  },
  "agent": {
    "ticket-labeler": {
      "description": "Labels a support ticket with its category and severity as JSON. Use it for every new ticket.",
      "mode": "subagent",
      "model": "ollama/ticket-labeler",
      "permission": { "edit": "deny", "bash": "deny", "webfetch": "deny" }
    }
  }
}
