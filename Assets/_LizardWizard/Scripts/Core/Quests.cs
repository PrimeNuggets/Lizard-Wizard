using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Quests", menuName = "Scriptable Objects/Quests")]
public class Quests : ScriptableObject
{
    [SerializeField, LabelOverride("Internal Name"), Tooltip("This is what the system will call when refering to this quest\nAvoid spaces & special characters")] private string questId;
    [SerializeField, LabelOverride("Name")] private string questName;
    [SerializeField, TextArea] private string description;

    [Serializable]
    public class Objective
    {
        public enum TaskType
        {
            None,
        }
        [LabelOverride("ID")] public int id;
        public string task;
        public TaskType type;
    }
}
