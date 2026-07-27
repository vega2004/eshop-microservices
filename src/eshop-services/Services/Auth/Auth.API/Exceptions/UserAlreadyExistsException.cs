namespace Auth.API.Exceptions;

public class UserAlreadyExistsException()
    : Exception("El nombre de usuario o correo electrónico ya está registrado.");
