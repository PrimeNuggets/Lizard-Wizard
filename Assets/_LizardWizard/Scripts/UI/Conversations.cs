using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Conversations", menuName = "Scriptable Objects/Conversations")]
public class Conversations : ScriptableObject
{
    [Serializable]
    public class Lines
    {
        [TextArea, Tooltip("This is limited to the character limit defined in the Dialogue Script.")] public string line;
        //[TextArea] public List<string> responses; //If needed
        [SerializeField, Tooltip("If left empty, no quest progresses")] public Quests progressQuest;
    }
    public List<Lines> lines;
    [LabelOverride("Current Line Index"), NonSerialized] public int index = 0;

    public void NextLine()
    {
        index += 1;
        if (lines.Count < index) index = 0; //restart convo if index exceeds existing lines
    }

    public string GetLine(int index)
    {
        return lines[index].line;
    }

    public Quests GetProgression(int index)
    {
        return lines[index].progressQuest;
    }

    private void OnValidate()
    {
        if (lines != null)
        {
            foreach (Lines entry in lines)
            {
                if (entry == null || string.IsNullOrEmpty(entry.line)) continue;

                if (entry.line.Length > Dialogue.CHARLIMIT)
                {
                    entry.line = entry.line.Substring(0, Dialogue.CHARLIMIT);
                }
            }
        }
    }
}
