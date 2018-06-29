namespace GmailBackupper
{
    static class Extensions
    {
        public static string Trim(this string value, int count)
        {
            if (value == null) return null;
            if (value.Length <= count) return value;
            return value.Remove(count);
        }
    }
}
