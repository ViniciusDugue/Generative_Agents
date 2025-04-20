using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField, Range(0, 23)] private int manualHours;
    [SerializeField, Range(0, 59)] private int manualMinutes;
    [SerializeField] private bool manualTimeControl = false;

    [SerializeField] private Texture2D skyboxNight;
    [SerializeField] private Texture2D skyboxSunrise;
    [SerializeField] private Texture2D skyboxDay;
    [SerializeField] private Texture2D skyboxSunset;

    [SerializeField] private Gradient graddientNightToSunrise;
    [SerializeField] private Gradient graddientSunriseToDay;
    [SerializeField] private Gradient graddientDayToSunset;
    [SerializeField] private Gradient graddientSunsetToNight;

    [SerializeField] private Light globalLight;

    private int minutes;
    public int Minutes
    {
        get { return minutes; }
        set { minutes = value; OnMinutesChange(value); }
    }

    private int hours = 8;
    public int Hours
    {
        get { return hours; }
        set { hours = value; OnHoursChange(value); }
    }

    private int days;
    public int Days
    {
        get { return days; }
        set { days = value; }
    }

    // Exposed boolean for day/night state.
    public bool IsDayTime { get; private set; }

    // To detect changes and avoid spamming logs.
    private bool lastIsDayTime;

    private float tempSecond;

        private void Start()
    {
        // Always start at 8:00 AM.
        Hours = 8;
        Minutes = 0;
        Days = 1;
        Debug.Log("Starting time set to 8:00 AM.");
    }

    private void Update()
    {
        // 1 day/night cycle = 4 minutes
        tempSecond += Time.deltaTime;
        float secondsPerGameMinute = 1f / 6f;
        if (tempSecond >= secondsPerGameMinute)
        {
            int minutesToAdd = Mathf.FloorToInt(tempSecond / secondsPerGameMinute);
            Minutes += minutesToAdd;
            tempSecond %= secondsPerGameMinute;
        }
    }

    private void OnMinutesChange(int value)
    {
        // Rotate the light each minute.
        // The following rotation calculation remains the same, but you might adjust it if needed.
        globalLight.transform.Rotate(Vector3.up, (1f / (1440f / 4f)) * 360f, Space.World);

        if (value >= 60)
        {
            Hours++;
            minutes = 0;
        }
        if (Hours >= 24)
        {
            Hours = 0;
            Days++;
            Debug.Log("New day started. Day count: " + Days);

            // Find all BehaviorManager instances in the scene.
            BehaviorManager[] allAgents = GameObject.FindObjectsOfType<BehaviorManager>();
            foreach (var bm in allAgents)
            {
                bm.ApplyDailyHungerPenalty();

                AgentHeal heal = bm.GetComponent<AgentHeal>();
                if (heal != null)
                {
                    heal.foodPortionsReceived = 0; // Reset meter
                }
            }
            
        }
    }

    private void OnHoursChange(int value)
    {
        // Trigger skybox and light transitions at specific hours.
        if (value == 6)
        {
            StartCoroutine(LerpSkybox(skyboxNight, skyboxSunrise, 10f));
            StartCoroutine(LerpLight(graddientNightToSunrise, 10f));
        }
        else if (value == 8)
        {
            StartCoroutine(LerpSkybox(skyboxSunrise, skyboxDay, 10f));
            StartCoroutine(LerpLight(graddientSunriseToDay, 10f));
        }
        else if (value == 18)
        {
            StartCoroutine(LerpSkybox(skyboxDay, skyboxSunset, 10f));
            StartCoroutine(LerpLight(graddientDayToSunset, 10f));
        }
        else if (value == 22)
        {
            StartCoroutine(LerpSkybox(skyboxSunset, skyboxNight, 10f));
            StartCoroutine(LerpLight(graddientSunsetToNight, 10f));
        }

        // Update the day/night status based on current hour.
        UpdateDayStatus();
    }

    // Updates the IsDayTime property and logs changes.
    private void UpdateDayStatus()
    {
        // Here, daytime is defined as 8 <= hour < 18 (between 8am and 6pm).
        bool currentIsDay = (Hours >= 8 && Hours < 18);
        if (currentIsDay != lastIsDayTime)
        {
            lastIsDayTime = currentIsDay;
            IsDayTime = currentIsDay;
            Debug.Log("Daytime status changed: " + IsDayTime + ". Current Day: " + Days);
        }
    }

    private IEnumerator LerpSkybox(Texture2D a, Texture2D b, float time)
    {
        RenderSettings.skybox.SetTexture("_Texture1", a);
        RenderSettings.skybox.SetTexture("_Texture2", b);
        RenderSettings.skybox.SetFloat("_Blend", 0);
        for (float i = 0; i < time; i += Time.deltaTime)
        {
            RenderSettings.skybox.SetFloat("_Blend", i / time);
            yield return null;
        }
        RenderSettings.skybox.SetTexture("_Texture1", b);
    }

    private IEnumerator LerpLight(Gradient lightGradient, float time)
    {
        for (float i = 0; i < time; i += Time.deltaTime)
        {
            globalLight.color = lightGradient.Evaluate(i / time);
            RenderSettings.fogColor = globalLight.color;
            yield return null;
        }
    }
}
