using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private const int SampleRate = 44100;
    private const string MuteKey = "MonkchessMuted";

    public static SoundManager Instance { get; private set; }

    private AudioSource source;
    private AudioClip clickClip;
    private AudioClip moveClip;
    private AudioClip captureClip;
    private AudioClip checkClip;
    private AudioClip endClip;

    public bool Muted { get; private set; }

    public static SoundManager Ensure()
    {
        if (Instance != null)
        {
            return Instance;
        }

        SoundManager existing = UnityEngine.Object.FindObjectOfType<SoundManager>();
        if (existing != null)
        {
            return existing;
        }

        GameObject soundObject = new GameObject("SoundManager");
        return soundObject.AddComponent<SoundManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0.65f;

        clickClip = BuildClick();
        moveClip = BuildMove();
        captureClip = BuildCapture();
        checkClip = BuildCheck();
        endClip = BuildEnd();
    }

    public void PlayClick()
    {
        Play(clickClip, 0.6f);
    }

    public void PlayMove()
    {
        Play(moveClip, 0.8f);
    }

    public void PlayCapture()
    {
        Play(captureClip, 0.9f);
    }

    public void PlayCheck()
    {
        Play(checkClip, 0.75f);
    }

    public void PlayEnd()
    {
        Play(endClip, 0.9f);
    }

    public bool ToggleMute()
    {
        Muted = !Muted;
        PlayerPrefs.SetInt(MuteKey, Muted ? 1 : 0);
        PlayerPrefs.Save();

        if (Muted && source != null)
        {
            source.Stop();
        }

        return Muted;
    }

    private void Play(AudioClip clip, float volume)
    {
        if (Muted || source == null || clip == null)
        {
            return;
        }

        source.PlayOneShot(clip, volume);
    }

    private static AudioClip BuildClick()
    {
        return BuildClip("ui_click", 0.07f, delegate(float time)
        {
            float frequency = Mathf.Lerp(820f, 520f, time / 0.07f);
            float envelope = Mathf.Exp(-32f * time);
            return Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * 0.4f;
        });
    }

    private static AudioClip BuildMove()
    {
        return BuildClip("piece_move", 0.12f, delegate(float time)
        {
            float frequency = Mathf.Lerp(210f, 125f, time / 0.12f);
            float envelope = Mathf.Exp(-17f * time);
            float tone = Mathf.Sin(2f * Mathf.PI * frequency * time);
            float thud = Mathf.Sin(2f * Mathf.PI * 72f * time) * 0.35f;
            return (tone + thud) * envelope * 0.42f;
        });
    }

    private static AudioClip BuildCapture()
    {
        System.Random random = new System.Random(41);
        return BuildClip("piece_capture", 0.18f, delegate(float time)
        {
            float envelope = Mathf.Exp(-13f * time);
            float low = Mathf.Sin(2f * Mathf.PI * 105f * time) * 0.45f;
            float noise = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-24f * time) * 0.22f;
            return (low + noise) * envelope;
        });
    }

    private static AudioClip BuildCheck()
    {
        return BuildClip("check", 0.28f, delegate(float time)
        {
            float frequency = time < 0.12f ? 620f : 830f;
            float local = time < 0.12f ? time : time - 0.12f;
            float envelope = Mathf.Exp(-14f * local);
            return Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * 0.34f;
        });
    }

    private static AudioClip BuildEnd()
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        return BuildClip("game_end", 0.72f, delegate(float time)
        {
            float noteLength = 0.16f;
            int index = Mathf.Clamp((int)(time / noteLength), 0, notes.Length - 1);
            float local = time - index * noteLength;
            float envelope = Mathf.Exp(-6f * local) * Mathf.Clamp01(1f - time / 0.72f);
            float main = Mathf.Sin(2f * Mathf.PI * notes[index] * time);
            float harmonic = Mathf.Sin(2f * Mathf.PI * notes[index] * 2f * time) * 0.2f;
            return (main + harmonic) * envelope * 0.28f;
        });
    }

    private static AudioClip BuildClip(string name, float duration, Func<float, float> generator)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)SampleRate;
            samples[i] = Mathf.Clamp(generator(time), -0.95f, 0.95f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
