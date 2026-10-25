using UnityEngine;

public class TestStart : MonoBehaviour
{
    void Start()
    {
        GetComponent<DialogueTrigger>().TriggerDialogue();
    }
}