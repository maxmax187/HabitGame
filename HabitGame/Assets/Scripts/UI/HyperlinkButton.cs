using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HyperlinkButton : MonoBehaviour
{
    [Header("Link Settings")]
    [SerializeField] private string _targetUrl = "https://example.com";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void OpenNewTab(string url);
#endif

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClicked);
    }

    private void Start()
    {
        // Check config to see if this button should be active on the final home screen
        if (ConfigManager.Instance != null && ConfigManager.Instance.Config != null)
        {
            bool shouldShow = ConfigManager.Instance.Config.ShowHyperlinkButton;
            gameObject.SetActive(shouldShow);
        }
    }

    public void OnButtonClicked()
    {
        if (string.IsNullOrEmpty(_targetUrl))
        {
            Debug.LogWarning("[HyperlinkButton] Target URL is empty.");
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        OpenNewTab(_targetUrl);
#else
        Application.OpenURL(_targetUrl);
#endif
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}