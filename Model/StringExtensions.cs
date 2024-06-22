namespace Model;

public static class StringExtensions
{
    public static string TrimToMaxLength(this string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || maxLength < 0)
        {
            return input;
        }

        return input.Length <= maxLength ? input : input.Substring(0, maxLength);
    }
}
