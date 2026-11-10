using System;
using System.Globalization;

namespace AvaloniaChat;

/// <summary>
/// 啟動參數：
///   --fake            使用假服務（預設連 Ollama）
///   --max-tokens N    Ollama 的 num_predict，範圍 16–2048，預設 512
/// 其他未知參數一律忽略。
/// </summary>
public sealed record AppOptions(bool UseFake, int MaxTokens)
{
    public const int DefaultMaxTokens = 512;
    public const int MinMaxTokens = 16;
    public const int MaxMaxTokens = 2048;

    /// <summary>
    /// 解析成功回傳 true；失敗回傳 false 並在 error 給出可讀的錯誤訊息。
    /// </summary>
    public static bool TryParse(string[] args, out AppOptions options, out string? error)
    {
        var useFake = false;
        var maxTokens = DefaultMaxTokens;
        options = new AppOptions(useFake, maxTokens);
        error = null;

        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--fake", StringComparison.OrdinalIgnoreCase))
            {
                useFake = true;
            }
            else if (string.Equals(args[i], "--max-tokens", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    error = "--max-tokens 後面缺少數值。";
                    return false;
                }

                var raw = args[++i];
                if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxTokens))
                {
                    error = $"--max-tokens 的值「{raw}」不是整數。";
                    return false;
                }

                if (maxTokens < MinMaxTokens || maxTokens > MaxMaxTokens)
                {
                    error = $"--max-tokens 的值 {maxTokens} 超出範圍，合法範圍是 {MinMaxTokens} 到 {MaxMaxTokens}。";
                    return false;
                }
            }
        }

        options = new AppOptions(useFake, maxTokens);
        return true;
    }
}
