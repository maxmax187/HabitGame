using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PhasesData", menuName = "Scriptable Objects/PhasesData")]
public class PhasesData : ScriptableObject
{
    public PhaseData[] PhasesOne; // Spike phases

    [Header("Phase 2 - Tutorial (No chest / Tutorial layout)")]
    public PhaseData PhaseTwoTutorialLeft;   // phase2_1L
    public PhaseData PhaseTwoTutorialRight;  // phase2_1R

    [Header("Phase 2 - Regular (Training & Testing with chest)")]
    public PhaseData PhaseTwoRegularLeft;    // phase2_2L
    public PhaseData PhaseTwoRegularRight;   // phase2_2R

    [Header("Phase 3 - Boss Fights")]
    public PhaseData[] PhasesThree;

    [Serializable]
    public struct PhaseData
    {
        public Phase Phase;
        public float PhaseTime;
    }
}