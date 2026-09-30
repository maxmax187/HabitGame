using TMPro;
using UnityEngine;

public class UpgradeWeaponUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _stateUpgrade;
    [SerializeField] private GameObject _imageGold;
    [SerializeField] private GameObject _imageSilver;

    public void SetState(Vector2 upgradeDamageData, bool isTestLevel)
    {
        _imageGold.SetActive(!isTestLevel);
        _imageSilver.SetActive(isTestLevel);
        _title.text = "Your weapon has been upgraded";
        _stateUpgrade.text = $"Damage: {upgradeDamageData.x} -> {upgradeDamageData.y}";
    }

    public void SetNoUpgradeState(float upgradeDamage)
    {   
        _imageGold.SetActive(false);
        _imageSilver.SetActive(true);
        _title.text = "No upgrade available";
        _stateUpgrade.text = $"Damage: {upgradeDamage}";
    }

    public void SetAlreadyStrongerState(Vector2 upgradeDamageData, float currentDamage)
    {
        // Group 1 has the top weapon (12 dmg) -> show Gold image
        _imageGold.SetActive(true);
        _imageSilver.SetActive(false);
        _title.text = "Your weapon has been upgraded";

        _stateUpgrade.text = $"Damage: {upgradeDamageData.x} -> {upgradeDamageData.y}\n<color=#FFD700>You already have a stronger weapon (Damage: {currentDamage})</color>";
    }
}