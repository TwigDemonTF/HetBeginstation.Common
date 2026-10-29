namespace HetBeginstation.Common;

public static class StringExtensions
{
    public static bool IsNullOrEmptyOrWhiteSpace(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }
}