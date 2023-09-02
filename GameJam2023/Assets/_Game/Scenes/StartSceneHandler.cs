using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneHandler : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _versionText;

    private void Start()
    {
        if (_versionText != null)
        {
            _versionText.text = $"{Application.companyName} - {Application.productName} - version: {Application.version}";
        }
    }

    public void StartGame()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
