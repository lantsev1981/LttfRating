namespace Common;

public static class EmojiExtensions
{
    public static string ToEmojiDigits<T>(this INumber<T> number, string format)
        where T : INumber<T>?
    {
        var digits = number.ToString(format, null).ToCharArray();
        var emojiDigits = digits.Select(d => d switch
        {
            '0' => "0️⃣",
            '1' => "1️⃣",
            '2' => "2️⃣",
            '3' => "3️⃣",
            '4' => "4️⃣",
            '5' => "5️⃣",
            '6' => "6️⃣",
            '7' => "7️⃣",
            '8' => "8️⃣",
            '9' => "9️⃣",
            _ => d.ToString()
        });
        return string.Join("", emojiDigits);
    }

    public static string ToEmojiPosition<T>(this INumber<T> number)
        where T : INumber<T>?
    {
        var digits = number.ToString()!;
        return digits switch
        {
            "1" => "🥇",
            "2" => "🥈",
            "3" => "🥉",
            _ => number.ToEmojiDigits("0")
        };
    }
}