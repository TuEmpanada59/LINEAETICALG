namespace LineaEtica.Domain;

/// <summary>Datos que no cumplen las reglas. Su mensaje se muestra al usuario.</summary>
public sealed class ErrorValidacion(string mensaje) : Exception(mensaje);

/// <summary>El recurso no existe o el usuario no tiene acceso: ambos casos responden igual.</summary>
public sealed class ErrorNoEncontrado(string mensaje) : Exception(mensaje);

/// <summary>Otro usuario cambió el registro al mismo tiempo.</summary>
public sealed class ErrorConflicto(string mensaje) : Exception(mensaje);
