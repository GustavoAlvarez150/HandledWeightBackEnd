namespace HandledWeight.Interfaces
{
    public interface IJwktGenerator
    {
        string GenerateToken(DTOs.Auth.User user);
    }
}
