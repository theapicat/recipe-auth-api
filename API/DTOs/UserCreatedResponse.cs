namespace API.DTOs;

public class UserCreatedResponse : LoginResponse
{
    public string Message { get; set; } = "Bruker opprettet med hell.";
}