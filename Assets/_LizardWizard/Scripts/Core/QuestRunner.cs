using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class QuestRunner : MonoBehaviour
{
    [SerializeField] private Quests quest;

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

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        Quests.process += HandleProcess;
    }

    private void OnDisable()
    {
        Quests.process -= HandleProcess;
    }

    private void HandleProcess(Quests quest)
    {
        var currentQuest = process.GetValueOrDefault<Quests, Process>(quest);

        if (currentQuest == null) return;

        /*switch (phase)
        {
            case Quests.ProcessType.Start:*/
                currentQuest.start.Invoke();
            /*    break;
            case Quests.ProcessType.During:*/
                currentQuest.during.Invoke();
                /*break;
            case Quests.ProcessType.End:*/
                currentQuest.end.Invoke();
                /*break;
        }*/
    }
}
