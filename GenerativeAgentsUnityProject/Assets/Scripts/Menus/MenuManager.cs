using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenu, settingsMenu, aboutUsMenu, contactUsMenu, environment;
    public Camera overviewCamera; // Assign this in Inspector

    void Start()
    {
        // Find menus in the hierarchy
        mainMenu = transform.Find("MainMenu")?.gameObject;
        settingsMenu = transform.Find("SettingsMenu")?.gameObject;
        aboutUsMenu = transform.Find("AboutUsMenu")?.gameObject;
        contactUsMenu = transform.Find("ContactUsMenu")?.gameObject;
        environment = GameObject.Find("Environment"); // Search globally for Environment
        overviewCamera = GameObject.Find("OverviewCamera")?.GetComponent<Camera>();

        // Debug logs to check references
        Debug.Log($"MainMenu assigned: {mainMenu != null}");
        Debug.Log($"SettingsMenu assigned: {settingsMenu != null}");
        Debug.Log($"AboutUsMenu assigned: {aboutUsMenu != null}");
        Debug.Log($"ContactUsMenu assigned: {contactUsMenu != null}");
        Debug.Log($"Environment assigned: {environment != null}");
        Debug.Log($"OverviewCamera assigned: {overviewCamera != null}");

        // Ensure none of the references are null
        if (mainMenu == null || settingsMenu == null || aboutUsMenu == null || contactUsMenu == null || environment == null || overviewCamera == null)
        {
            Debug.LogError("One or more menus, the environment, or the camera are not assigned or found!");
            return;
        }

        // Set default states
        mainMenu.SetActive(true);
        settingsMenu.SetActive(false);
        aboutUsMenu.SetActive(false);
        contactUsMenu.SetActive(false);
        environment.SetActive(false); // Environment is inactive by default

        Debug.Log("MenuManager initialized successfully.");
    }

    public void StartEnvironment()
    {
        Debug.Log("StartEnvironment() called");

        if (environment != null)
        {
            environment.SetActive(true);
            Debug.Log("Environment activated.");
        }
        else
        {
            Debug.LogError("Environment is not assigned!");
        }

        // Hide UI
        mainMenu?.SetActive(false);
        settingsMenu?.SetActive(false);
        aboutUsMenu?.SetActive(false);
        contactUsMenu?.SetActive(false);

        // Hide UIManager
        GameObject uiManager = GameObject.Find("UIManager");
        if (uiManager != null)
        {
            uiManager.SetActive(false);
            Debug.Log("UIManager deactivated.");
        }
        else
        {
            Debug.LogError("UIManager not found!");
        }

        // Move OverviewCamera to Environment Position
        if (overviewCamera != null)
        {
            overviewCamera.transform.position = new Vector3(0, 10, -10); // Adjust position as needed
            overviewCamera.transform.LookAt(environment.transform);
            Debug.Log("Camera moved to Environment.");
        }
        else
        {
            Debug.LogError("OverviewCamera is not assigned!");
        }

        Debug.Log("Menus deactivated.");
    }

    public void OpenMainMenu()
    {
        mainMenu.SetActive(true);
        settingsMenu.SetActive(false);
        aboutUsMenu.SetActive(false);
        contactUsMenu.SetActive(false);
        environment.SetActive(false);

        Debug.Log("Returned to Main Menu.");
    }

    public void OpenSettingsMenu()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(true);
        aboutUsMenu.SetActive(false);
        contactUsMenu.SetActive(false);
        environment.SetActive(false);

        Debug.Log("Navigated to Settings Menu.");
    }

    public void OpenAboutUsMenu()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(false);
        aboutUsMenu.SetActive(true);
        contactUsMenu.SetActive(false);
        environment.SetActive(false);

        Debug.Log("Navigated to About Us Menu.");
    }

    public void OpenContactUsMenu()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(false);
        aboutUsMenu.SetActive(false);
        contactUsMenu.SetActive(true);
        environment.SetActive(false);

        Debug.Log("Navigated to Contact Us Menu.");
    }

    public void BackToMainMenu()
    {
        OpenMainMenu();
    }
}
