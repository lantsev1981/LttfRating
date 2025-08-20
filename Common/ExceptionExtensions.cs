namespace Common;

public static class ExceptionExtensions
{
    /// <summary>
    /// Получает список всех сообщений из цепочки исключений
    /// </summary>
    public static List<string> GetAllMessages(this Exception? exception)
    {
        var messages = new List<string>();
        while (exception != null)
        {
            messages.Add(exception.Message);
            exception = exception.InnerException;
        }
        return messages;
    }
    
    public static string EscapeHtml(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}