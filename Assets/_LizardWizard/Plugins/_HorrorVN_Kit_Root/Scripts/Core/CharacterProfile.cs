using System.Collections.Generic; 
using UnityEngine;

[System.Serializable]
public struct EmotionPortrait
{
    public Emotion emotion;
    public Sprite sprite;
}

[CreateAssetMenu(fileName = "New Character", menuName = "Horror VN/Character Profile")]
public class CharacterProfile : ScriptableObject
{
    public string characterName;

    [Header("Emotion Portraits")]
    public List<EmotionPortrait> portraits = new List<EmotionPortrait>();

    public Sprite GetPortrait(Emotion requestedEmotion)
    {
        foreach (EmotionPortrait ep in portraits)
        {
            if (ep.emotion == requestedEmotion)
            {
                return ep.sprite;
            }
        }
        return null; 
    }

    // --// Returns true if this profile has a portrait for the requested emotion.
    public bool HasEmotion(Emotion requestedEmotion)
    {
        // --// Neutral is always a safe fallback.
        if (requestedEmotion == Emotion.Neutral) return true;

        foreach (EmotionPortrait ep in portraits)
        {
            if (ep.emotion == requestedEmotion) return true;
        }
        return false;
    }
}

public enum Emotion
{
    Neutral,
    Angry,
    Fear,
    Sad,
    Concern,
    Embarrassed,
    ClosedEyes,
    SlightSmile,
    NoFace,
    Frown, 
    Huh,   
    Smile, 
    Smirk  
}
