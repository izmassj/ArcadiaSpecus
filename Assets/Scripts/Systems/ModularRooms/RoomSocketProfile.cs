using UnityEngine;

/// <summary>
/// Perfil de conexiones de una pieza de habitación.
/// Esto es lo que nos permite imponer reglas tipo:
/// "una puerta no puede tocar una pared".
/// 
/// Puedes añadir este componente a los prefabs para personalizar.
/// Si no existe, ModularRoomSystem asigna un perfil por defecto según RoomKind.
/// </summary>
public class RoomSocketProfile : MonoBehaviour
{
    public enum SocketType
    {
        Wall,   // pared cerrada
        Open,   // apertura/pasillo
        Door    // puerta
    }

    [Header("Sockets (en el plano X/Y)")]
    public SocketType left = SocketType.Wall;
    public SocketType right = SocketType.Wall;
    public SocketType up = SocketType.Wall;
    public SocketType down = SocketType.Wall;

    [Header("Debug")]
    public bool showDebugGizmos = false;

    public void ApplyDefaults(RoomKind _kind)
    {
        // Defaults simples (suficientes para reglas "Bé")
        switch (_kind)
        {
            case RoomKind.LEFT:
                left = SocketType.Wall;
                right = SocketType.Open;
                up = SocketType.Wall;
                down = SocketType.Wall;
                break;

            case RoomKind.RIGHT:
                left = SocketType.Open;
                right = SocketType.Wall;
                up = SocketType.Wall;
                down = SocketType.Wall;
                break;

            case RoomKind.MIDDLE:
                left = SocketType.Open;
                right = SocketType.Open;
                up = SocketType.Wall;
                down = SocketType.Wall;
                break;

            case RoomKind.INTERSECTION:
                left = SocketType.Open;
                right = SocketType.Open;
                up = SocketType.Open;
                down = SocketType.Open;
                break;

            case RoomKind.DOORWALL:
                // Interpretación práctica:
                // es un segmento de pasillo que tiene "puerta" hacia la derecha.
                // Esto nos permite validar el caso: "puerta pegada a una pared a la derecha".
                left = SocketType.Open;
                right = SocketType.Door;
                up = SocketType.Wall;
                down = SocketType.Wall;
                break;

            default:
                left = SocketType.Wall;
                right = SocketType.Wall;
                up = SocketType.Wall;
                down = SocketType.Wall;
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
