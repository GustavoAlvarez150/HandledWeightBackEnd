namespace HandledWeight.DTOs.Auth
{

  public record LoginDto(string email, string password);
  public record RegisterDto(string Name, string LastName, string Email, string Password, int? IdRole);
  public record User(string name, string email, int? idRole);
  public record JWT(string message,string tokenType, double expiresIn, string token, User user);

}
