using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>Small physical-feeling cues shared by the field tools: device vibration and a camera kick.</summary>
public static class FieldToolFeedback
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void GeoModel_Vibrate(int milliseconds);
#endif

    /// <summary>Vibrates phones and tablets that support it (Android browsers); silently does nothing elsewhere.</summary>
    public static void Vibrate(int milliseconds)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            GeoModel_Vibrate(milliseconds);
        }
        catch (System.Exception)
        {
            // Haptics are optional.
        }
#endif
    }

    public static void KickCamera(Camera camera, float amplitude = 0.012f, float duration = 0.16f)
    {
        if (camera == null || !Application.isPlaying) return;
        CameraKick kick = camera.GetComponent<CameraKick>();
        if (kick == null) kick = camera.gameObject.AddComponent<CameraKick>();
        kick.Play(amplitude, duration);
    }
}

/// <summary>Brief decaying positional shake. Only the local position is touched, so mouse look is unaffected.</summary>
public class CameraKick : MonoBehaviour
{
    private Vector3 restPosition;
    private bool active;
    private float elapsed;
    private float duration;
    private float amplitude;

    public void Play(float kickAmplitude, float kickDuration)
    {
        if (!active) restPosition = transform.localPosition;
        active = true;
        elapsed = 0f;
        amplitude = kickAmplitude;
        duration = Mathf.Max(0.01f, kickDuration);
    }

    void LateUpdate()
    {
        if (!active) return;
        elapsed += Time.unscaledDeltaTime;
        float t = elapsed / duration;
        if (t >= 1f)
        {
            Restore();
            return;
        }

        float decay = (1f - t) * (1f - t);
        transform.localPosition = restPosition +
            new Vector3(Mathf.Sin(elapsed * 70f) * 0.5f, Mathf.Sin(elapsed * 95f), 0f) * (amplitude * decay);
    }

    void OnDisable()
    {
        Restore();
    }

    private void Restore()
    {
        if (!active) return;
        transform.localPosition = restPosition;
        active = false;
    }
}
