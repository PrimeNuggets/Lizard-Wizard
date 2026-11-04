using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using TMPro;

[DisallowMultipleComponent]
public class QuestRunner : MonoBehaviour
{
    [NonSerialized] public Quests quest;
    [NonSerialized] public Quests.Objective objective;

    [Serializable]
    public class Process {
        public UnityEvent start = new UnityEvent();
        public UnityEvent during = new UnityEvent();
        public UnityEvent end = new UnityEvent();
    }
    [SerializeField] private Dictionary<Quests, Process> process;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void Update()
    {
    }

    public void StartQuest(Quests quest)
    {
        this.quest = quest;
        CurrentObjective(0);
        HandleProcess(this.quest, Quests.ProcessType.Start);
        Scoring.AddValue($"quest.{this.quest.questId.ToLowerInvariant()}.started", 1);
        Scoring.AddValue($"quests_started", 1);
    }

    public void SetQuest(Quests newQuest)
    {
        quest = newQuest;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (quest == null) return;

        HandleProcess(quest, Quests.ProcessType.During);

        bool progress = objective.endCon.Count > 0; //returns true as long as an end con exists
        foreach (var con in objective.endCon) //con is the pairing
        {
            if (Scoring.FindValue(con.Key) < objective.endCon[con.Key])
            {
                progress = false; //Forces false if any end con isn't fulfilled
            }
        }
        if (progress)
        {
            ProgressQuest(quest);
        }
    }

    private void OnEnable()
    {
        Quests.process += HandleProcess;
    }

    private void OnDisable()
    {
        Quests.process -= HandleProcess;
    }

    private void HandleProcess(Quests quest, Quests.ProcessType phase)
    {
        var currentQuest = process.GetValueOrDefault<Quests, Process>(quest);

        if (currentQuest == null) return;

        switch (phase)
        {
            case Quests.ProcessType.Start:
                currentQuest.start.Invoke();
                break;
            case Quests.ProcessType.During:
                currentQuest.during.Invoke();
                break;
            case Quests.ProcessType.End:
                currentQuest.end.Invoke();
                break;
        }
    }
    public void CurrentObjective(int index)
    {
        objective = quest.objectiveChain[index];
    }

    public void ProgressQuest(Quests quest)
    {
        //Add Objective Rewards here
        int nextIndex = quest.objectiveChain.IndexOf(objective) + 1;
        if (nextIndex >= quest.objectiveChain.Count)
        {
            HandleProcess(quest, Quests.ProcessType.End);
        } else
        {
            CurrentObjective(nextIndex);
        }
    }

    public void UpdateDisplayText(TMP_Text textBox)
    {
        textBox.text = $"{quest.questName}:\n{objective.description}";
    }
}
