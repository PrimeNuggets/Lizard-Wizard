using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

//-- Loops one clip on this animator so we can look at all the animations side by side
[RequireComponent(typeof(Animator))]
public class ClipShowcase : MonoBehaviour
{
    [SerializeField] private AnimationClip clip;
    [SerializeField] private float pauseBetweenLoops = 0.5f;

    private PlayableGraph graph;
    private AnimationClipPlayable playable;

    private void OnEnable()
    {
        if (clip == null) return;

        Animator animator = GetComponent<Animator>();
        animator.runtimeAnimatorController = null;
        animator.applyRootMotion = false;
        playable = AnimationPlayableUtilities.PlayClip(animator, clip, out graph);
    }

    private void Update()
    {
        if (clip == null) return; //-- dont spam errors if no clip is set

        //-- Restart clips that don't loop (jump, land, etc)
        if (!clip.isLooping && playable.IsValid() && playable.GetTime() > clip.length + pauseBetweenLoops)
            playable.SetTime(0);
    }

    private void OnDisable()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
