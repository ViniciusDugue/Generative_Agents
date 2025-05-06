using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HabitatUI : MonoBehaviour
{
    public TMP_Text foodText;

    void Update()
    {
        if (Habitat.Instance != null)
            foodText.text = $"Food: {Habitat.Instance.storedFood}";
    }
}
