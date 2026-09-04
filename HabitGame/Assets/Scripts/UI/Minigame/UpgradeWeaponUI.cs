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

    public void SetNoUpgradeState(float normalDamage)
    {
        _title.text = "No upgrade available";
        _stateUpgrade.text = $"No weapon upgrade (Damage: {normalDamage})";
    }
}