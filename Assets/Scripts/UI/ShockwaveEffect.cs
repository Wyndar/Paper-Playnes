//using UnityEngine;
//using UnityEngine.Rendering.PostProcessing;
//using System.Collections;

//public class ShockwaveEffect : MonoBehaviour
//{
//    private PostProcessVolume postVolume;
//    private ChromaticAberration chromatic;

//    private void Start()
//    {
//        postVolume = GetComponent<PostProcessVolume>();
//        postVolume.profile.TryGetSettings(out chromatic);
//    }

//    public void TriggerEffect(float duration) => StartCoroutine(ShockwaveRoutine(duration));

//    private IEnumerator ShockwaveRoutine(float duration)
//    {
//        float t = 0;
//        while (t < duration)
//        {
//            t += Time.deltaTime;
//            chromatic.intensity.value = Mathf.Lerp(1, 0, t / duration);
//            yield return null;
//        }
//    }
//}
