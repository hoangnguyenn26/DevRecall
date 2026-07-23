namespace DevRecall.Application.Identity;

public interface IPasswordHasher
{
    string Hash(string password);
}
