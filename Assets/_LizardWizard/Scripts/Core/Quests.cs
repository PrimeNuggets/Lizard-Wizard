using System;
using System.Collections.Generic;
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
    [SerializeField, LabelOverride("Name")] private string questName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private List<Objective> objectiveChain;
    public static event Action<string, int, ProcessType> process;

    [Serializable]
    public class Objective
    {
        public enum TaskType
        {
            None,
            Interaction,
        }
        public enum RewardType
        {
            None,
            Water,
        }
        public int id {get; private set; }
        public string task;
        [TextArea] public string description;
        public TaskType type;
        [SerializeField] public Dictionary<RewardType, float> reward;
    }

    public void StartProcess(int objIndex, ProcessType phase)
    {
        process?.Invoke(questId, objIndex, phase);
    }
}
