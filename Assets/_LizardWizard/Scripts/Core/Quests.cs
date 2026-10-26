using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
//using UnityEngine.Events;

[CreateAssetMenu(fileName = "Quests", menuName = "Scriptable Objects/Quests")]
public class Quests : ScriptableObject
{
    public enum ProcessType
    {
        Start,
        During,
        End,
    }
    [SerializeField, LabelOverride("Internal Name"), Tooltip("The name of the fired event when the event activates\nAvoid special characters & spaces")] private string questId;
    [SerializeField, LabelOverride("Name")] public string questName;
    [SerializeField, TextArea] public string description;
    public List<Objective> objectiveChain;
    public static event Action<Quests, ProcessType> process;

    [Serializable]
    public class Objective
    {
        public enum TaskType
        {
            None,
            Interaction,
            Controls,
        }
        public enum RewardType
        {
            None,
            Trinket,
            Achievement,
        }
        public int id {get; private set; }
        public string task;
        [TextArea] public string description;
        public TaskType type;
        [SerializeField] public Dictionary<RewardType, string> reward;
        [SerializeField, LabelOverride("Completion Conditions"), Tooltip("Input the scoring key.\nThen in the values section, input the cutoff value (the objective will end if the score is greater than/equal to the cutoff)")] public Dictionary<string, int> endCon;
    }

    public Objective GetObjective(int index)
    {
        return objectiveChain[index];
    }
}
