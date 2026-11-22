using System.Collections;
using UnityEngine;

//-- lizard blinks red for a couple seconds after taking any damage
[DisallowMultipleComponent]
public class DamageBlink : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Color blinkColor = new Color(0.9f, 0.05f, 0.05f, 1f);
    [SerializeField] private float blinkTime = 2f;
    [Tooltip("How many flashes per second")]
    [SerializeField] private float blinksPerSecond = 6f;

    //-- the toon shader adds emission on top, so red emission tints the whole lizard
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private MaterialPropertyBlock block;
    private Coroutine blinking;

    //-- call this from anything that does damage, does nothing if the target has no DamageBlink
    public static void Play(GameObject target)
    {
        if (target != null && target.TryGetComponent(out DamageBlink blink)) blink.Play();
    }

    public void Play()
    {
        if (!isActiveAndEnabled) return;
        if (blinking != null) StopCoroutine(blinking);
        blinking = StartCoroutine(Blink());
    }

    private IEnumerator Blink()
    {
        float timer = 0f;
        while (timer < blinkTime)
        {
            //-- red for the first half of each flash, normal for the second half
            SetRed(Mathf.Repeat(timer * blinksPerSecond, 1f) < 0.5f);
            yield return null;
            timer += Time.deltaTime;
        }
        SetRed(false);
        blinking = null;
    }

    private void SetRed(bool red)
    {
        block ??= new MaterialPropertyBlock();
        foreach (var render in renderers)
        {
            if (render == null) continue;

            Color normal = Color.black;
            if (render.sharedMaterial != null && render.sharedMaterial.HasProperty(EmissionColor))
                normal = render.sharedMaterial.GetColor(EmissionColor);

            render.GetPropertyBlock(block);
            block.SetColor(EmissionColor, red ? blinkColor : normal);
            render.SetPropertyBlock(block);
        }
    }

    void OnDisable()
    {
        if (blinking != null)
        {
            StopCoroutine(blinking);
            blinking = null;
        }
        SetRed(false);
    }
}
