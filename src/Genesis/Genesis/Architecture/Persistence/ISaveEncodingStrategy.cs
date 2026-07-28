using System;
using System.Text;

namespace Genesis.Architecture.Persistence;

public interface ISaveEncodingStrategy
{
    string Extension { get; }
    string Encode(string content);
    string Decode(string content);
}

public class PlainTextStrategy : ISaveEncodingStrategy
{
    public string Extension => ".json";
    public string Encode(string content) => content;
    public string Decode(string content) => content;
}

public class Base64Strategy : ISaveEncodingStrategy
{
    public string Extension => ".sav";

    public string Encode(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return Convert.ToBase64String(bytes);
    }

    public string Decode(string content)
    {
        try
        {
            var bytes = Convert.FromBase64String(content);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            Console.WriteLine("[SaveManager] Failed to decode Base64 content.");
            return null;
        }
    }
}
