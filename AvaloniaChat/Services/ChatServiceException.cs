using System;

namespace AvaloniaChat.Services;

/// <summary>
/// 聊天服務本身回報的錯誤（例如 Ollama 回 {"error":"model 'x' not found"}）。
/// </summary>
public sealed class ChatServiceException : Exception
{
    public ChatServiceException(string message) : base(message)
    {
    }
}
