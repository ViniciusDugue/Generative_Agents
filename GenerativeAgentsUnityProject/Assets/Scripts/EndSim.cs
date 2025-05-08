using UnityEngine;
using UnityEngine.UI;

public class EndSim : MonoBehaviour
{
    [Header("Assign these in the Inspector")]
    public Button finishButton;
    public GameObject runtimeUI;   // your in-game HUD panel
    public GameObject endMetrics;  // your End-of-Sim metrics panel

    private void Start()
    {
        // sanity check
        if (finishButton == null || runtimeUI == null || endMetrics == null)
            Debug.LogError("EndSim: please assign finishButton, runtimeUI and endMetrics in the Inspector!");
        else
            finishButton.onClick.AddListener(OnFinishClicked);

        // make sure panels start in the right state:
        runtimeUI.SetActive(true);
        endMetrics.SetActive(false);
    }

    private void OnFinishClicked()
    {
        // hide the in-game UI
        runtimeUI.SetActive(false);

        // tell your metrics-UI to populate & show
        EndSimMetricsUI.Instance.OpenUI();
    }
}
