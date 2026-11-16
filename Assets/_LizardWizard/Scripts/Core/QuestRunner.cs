using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;

[DisallowMultipleComponent]
public class QuestRunner : MonoBehaviour
{
    [NonSerialized] public Quests quest;
    [NonSerialized] public Quests.Objective objective;

    [SerializeField] private Quests achievementTrack;
    [SerializeField] private Canvas achievementDone;
    [SerializeField] private float achivementDisplay = 3f;
    [SerializeField] private TMP_Text achievementText;

    private readonly Queue<Quests> achievementQueue = new Queue<Quests>();
    private readonly HashSet<Quests> trackedAchievements = new HashSet<Quests>();
    private bool achievementComplete;

    [Serializable]
    public class Process {
        public UnityEvent start = new UnityEvent();
        public UnityEvent during = new UnityEvent();
        public UnityEvent end = new UnityEvent();
    }
    [SerializeField] private Dictionary<Quests, Process> process;

    private void Awake()
    {
        if (achievementDone != null)
        {
            achievementDone.enabled = false;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (achievementTrack != null && achievementDone != null)
        {
            StartQuest(achievementTrack);
        }
    }

    void Update()
    {
    }

    public void StartQuest(Quests quest)
    {
        achievementComplete = false;

        this.quest = quest;
        CurrentObjective(0);
        HandleProcess(this.quest, Quests.ProcessType.Start);

        if (achievementTrack != null && this.quest == achievementTrack)
        {
            return;
        }

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
        if (achievementDone != null && achievementComplete)  // Stops checking achievements upon completion.
        {
            return;
        }

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
        if (process == null) return;

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
            if (achievementDone != null && !achievementComplete)
            {
                achievementComplete = true;
                StartCoroutine(showAchievement());
            }

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

    public void TrackAchievement(Quests achievement, Action<Quests> onCompleted)
    {
        if (!trackedAchievements.Add(achievement))
        {
            return;
        }

        StartCoroutine(TrackAchievementProgress(achievement, onCompleted));
    }

    private IEnumerator TrackAchievementProgress(Quests achievement, Action<Quests> onCompleted)
    {
        foreach (var achievementObjective in achievement.objectiveChain)
        {
            bool objectiveCompleted = false;

            while (!objectiveCompleted)
            {
                objectiveCompleted = true;

                foreach (var condition in achievementObjective.endCon)
                {
                    if (Scoring.FindValue(condition.Key) < condition.Value)
                    {
                        objectiveCompleted = false;
                        break;
                    }
                }

                if (!objectiveCompleted)
                {
                    yield return null;
                }
            }
        }

        onCompleted?.Invoke(achievement);
        QueueAchievement(achievement);
    }

    private void QueueAchievement(Quests achievement)
    {
        achievementQueue.Enqueue(achievement);

        if (!achievementComplete)
        {
            achievementComplete = true;
            StartCoroutine(showAchievement());
        }
    }

    private IEnumerator showAchievement()
    {
        while (achievementQueue.Count > 0)
        {
            Quests achievement = achievementQueue.Dequeue();

            achievementText.text = $"{achievement.questName} acquired!";

            achievementDone.enabled = true;

            yield return new WaitForSecondsRealtime(achivementDisplay);

            achievementDone.enabled = false;

            yield return new WaitForSecondsRealtime(0.25f);
        }

        achievementComplete = false;
    }
}
