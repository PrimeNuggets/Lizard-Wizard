using System.Collections.Generic;
using UnityEngine;

public class Scoring : MonoBehaviour
{
    public static Dictionary<string, int> scoreList = new Dictionary<string, int>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void AddValue(string key, int incr)
    {
        key = key.ToLowerInvariant(); //ToLowerInvariant is better for language compatability

        scoreList.TryGetValue(key, out int current);
        scoreList[key] = current + incr;
    }

    public static int FindValue(string key)
    {
        key = key.ToLowerInvariant();
        scoreList.TryGetValue(key, out int val);
        return val;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetScores() //Call when returning to main menu to prevent scoring from persisting between saves
    {
        scoreList.Clear();
    }
}
