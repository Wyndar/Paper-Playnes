using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections;
using TMPro;

public class SceneLoadingManager : MonoBehaviour
{
    public static SceneLoadingManager Instance;

    public GameEvent toggleListenersEvent;
    public GameObject loadingPanel;
    public Slider progressBar;
    public TMP_Text statusText;
    public Image spinner;
    public TMP_Text tipText;
    public Image fadeOverlay;

    public string[] loadingTips;
    public float spinnerSpeed = 200f;
    public float fadeDuration = 1.5f;

    public AudioSource loadingMusic;
    public float musicFadeOutDuration = 1.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public void InitializeSceneLoader() => NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;

    private void OnDisable()
    {
        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
    }

    private void Update()
    {
        if (loadingPanel.activeSelf && spinner != null)
            spinner.transform.Rotate(0, 0, -spinnerSpeed * Time.deltaTime);
    }

    public void LoadScene(LoadingMode mode, string sceneName = null) => StartCoroutine(FadeInAndStartLoading(mode, sceneName));

    private IEnumerator FadeInAndStartLoading(LoadingMode mode, string sceneName)
    {
        if (loadingMusic)
        {
            loadingMusic.Play();
            StartCoroutine(FadeOutMusic(1f, musicFadeOutDuration));
        }
        yield return StartCoroutine(FadeOverlay(1f, fadeDuration));

        toggleListenersEvent.RaiseEvent(false);
        loadingPanel.SetActive(true);
        DisplayRandomTip();
        progressBar.value = 0;
       
        yield return StartCoroutine(FadeOverlay(0f, fadeDuration));

        if (mode == LoadingMode.Local)
        {
            progressBar.gameObject.SetActive(true);
            StartCoroutine(LoadSceneAsync(sceneName));
        }
        else if (mode == LoadingMode.Network)
        {
            progressBar.gameObject.SetActive(false);
            statusText.text = "Waiting for server...";
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            progressBar.value = operation.progress;
            yield return null;
        }

        progressBar.value = 1f;
        //this is dependent on device and may need some variation later
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(FadeOutAndFinishLoading(operation));
    }

    private IEnumerator FadeOutAndFinishLoading(AsyncOperation operation = null)
    {
        if (loadingMusic)
            StartCoroutine(FadeOutMusic(0, musicFadeOutDuration));
        
        yield return StartCoroutine(FadeOverlay(1f, fadeDuration));
        loadingPanel.SetActive(false);
        if (operation != null)
            operation.allowSceneActivation = true;
        toggleListenersEvent.RaiseEvent(true);
        yield return StartCoroutine(FadeOverlay(0f, fadeDuration));
        if(loadingMusic)
            loadingMusic.Stop();
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
        {
            statusText.text = "Load Complete!";
            StartCoroutine(FadeOutAndFinishLoading());
        }
    }

    private void DisplayRandomTip()
    {
        if (loadingTips.Length > 0)
            tipText.text = loadingTips[Random.Range(0, loadingTips.Length)];
    }

    private IEnumerator FadeOverlay(float targetAlpha, float duration)
    {
        float startAlpha = fadeOverlay.color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            fadeOverlay.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
    }

    private IEnumerator FadeOutMusic(float targetVolume, float duration)
    {
        float startVolume = loadingMusic.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            loadingMusic.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            yield return null;
        }
    }
}

