using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// Calcula a identidade de objetos fixos para não reaparecerem depois de salvos como coletados ou derrotados.
/// </summary>
public static class WorldProgressKey
{
    // A chave usa cena, hierarquia e posição; mover o objeto pode mudar sua identidade em saves antigos.
    public static string For(Component component)
    {
        if (component == null) return string.Empty;

        Transform current = component.transform;
        StringBuilder path = new StringBuilder();
        while (current != null)
        {
            path.Insert(0, "/" + current.name + "[" + current.GetSiblingIndex() + "]");
            current = current.parent;
        }

        Vector3 position = component.transform.position;
        return component.gameObject.scene.path + ":" + component.GetType().Name + path + ":" +
               position.x.ToString("F3", CultureInfo.InvariantCulture) + "," +
               position.y.ToString("F3", CultureInfo.InvariantCulture);
    }
}
