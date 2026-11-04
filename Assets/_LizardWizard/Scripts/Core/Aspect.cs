using UnityEngine;

[RequireComponent(typeof(PaletteReciever))]
public class Aspect : MonoBehaviour
{
    public enum aspect
    {
        Player,
        Enemy,
        NPC
    }

    public aspect aspectName;
}