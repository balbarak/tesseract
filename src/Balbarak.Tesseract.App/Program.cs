namespace Balbarak.Tesseract.App;

internal class Program
{
    /// <summary>
    /// Tesseract cli wrapper
    /// </summary>
    /// <param name="action">action type can be 'read' or 'raw', 'raw' to execute full tesseract command default: 'read'</param>
    /// <param name="input">Input image file path ex: C:/path/to/file.png</param>
    /// <param name="output">To specifiy output file, when empty result will be in terminal, ex: C:/path/to/file.txt,</param>
    /// <param name="cmd">Only works when action is raw</param>
    static async Task Main(string action, string input, string output, string cmd)
    {
        if (string.IsNullOrEmpty(action))
            action = "read";

        var actionType = GetAction(action);

        var client = new TesseractClient();

        var result = "";

        switch (actionType)
        {
            case ActionType.Read:

                if (string.IsNullOrWhiteSpace(output))
                    result = await client.Read(input);
                else
                    result = await client.Read(input, output);

                break;
            case ActionType.Raw:

                if (string.IsNullOrEmpty(cmd))
                {
                    Console.WriteLine("<cmd> args cannot be empty when --action raw is specified.");
                    return;
                }

                result = await client.Raw(cmd);

                break;
            default:
                break;
        }

        Console.WriteLine(result);

    }

    public static ActionType GetAction(string action)
    {
        if (action.Trim().ToLower() == "raw")
            return ActionType.Raw;

        return ActionType.Read;
    }
}