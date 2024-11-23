using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenu, settingsMenu, aboutUsMenu, contactUsMenu, environment; // References for menus and environment

    void Start()
    {
        // Find menus in the hierarchy
        mainMenu = transform.Find("MainMenu")?.gameObject;
        settingsMenu = transform.Find("SettingsMenu")?.gameObject;
        aboutUsMenu = transform.Find("AboutUsMenu")?.gameObject;
        contactUsMenu = transform.Find("ContactUsMenu")?.gameObject;
        environment = GameObject.Find("Environment"); // Search globally for Environment

        // Debug logs to check references
        Debug.Log($"MainMenu assigned: {mainMenu != null}");
        Debug.Log($"SettingsMenu assigned: {settingsMenu != null}");
        Debug.Log($"AboutUsMenu assigned: {aboutUsMenu != null}");
        Debug.Log($"ContactUsMenu assigned: {contactUsMenu != null}");
        Debug.Log($"Environment assigned: {environment != null}");

        // Ensure none of the references are null
        if (mainMenu == null || settingsMenu == null || aboutUsMenu == null || contactUsMenu == null || environment == null)
        {
            Debug.LogError("One or more menus or the environment objects are not assigned or found!");
            return; // Stop execution to prevent further null reference errors
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

        // Hide all menus
        mainMenu?.SetActive(false);
        settingsMenu?.SetActive(false);
        aboutUsMenu?.SetActive(false);
        contactUsMenu?.SetActive(false);
        Debug.Log("Menus deactivated.");
    }

    public void OpenMainMenu()
    {
        // Show Main Menu and deactivate other menus and environment
        mainMenu.SetActive(true);
        settingsMenu.SetActive(false);
        aboutUsMenu.SetActive(false);
        contactUsMenu.SetActive(false);
        environment.SetActive(false);

        Debug.Log("Returned to Main Menu.");
    }

    public void OpenSettingsMenu()
    {
        // Show Settings Menu
        mainMenu.SetActive(false);
        settingsMenu.SetActive(true);
        aboutUsMenu.SetActive(false);
        contactUsMenu.SetActive(false);
        environment.SetActive(false);

        Debug.Log("Navigated to Settings Menu.");
    }

    public void OpenAboutUsMenu()
    {
        // Show About Us Menu
        mainMenu.SetActive(false);
        settingsMenu.SetActive(false);
        aboutUsMenu.SetActive(true);
        contactUsMenu.SetActive(false);
        environment.SetActive(false);

        Debug.Log("Navigated to About Us Menu.");
    }

    public void OpenContactUsMenu()
    {
        // Show Contact Us Menu
        mainMenu.SetActive(false);
        settingsMenu.SetActive(false);
        aboutUsMenu.SetActive(false);
        contactUsMenu.SetActive(true);
        environment.SetActive(false);

        Debug.Log("Navigated to Contact Us Menu.");
    }

    public void BackToMainMenu()
    {
        // Navigate back to Main Menu from any menu or environment
        OpenMainMenu();
    }
}
