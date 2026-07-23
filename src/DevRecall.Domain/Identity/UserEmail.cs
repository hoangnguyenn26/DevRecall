namespace DevRecall.Domain.Identity;

public static class UserEmail
{
    public static string Normalize(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "Email is required.",
                nameof(email));
        }

        return email
            .Trim()
            .ToUpperInvariant();
    }
}
