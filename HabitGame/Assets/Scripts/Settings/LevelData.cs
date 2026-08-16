using System;
using System.Collections.Generic;

/// <summary>
/// This is the level data we collect while playing a level
/// </summary>

[Serializable]
public class LevelData
{
    public int Index;
    public float LevelTime;
    public LevelType Level;

    public List<PhaseTimeData> PhaseTimes;

    #region Phase1
    public int SpikeDificulty;
    public float EndSpikeTime;
    public float SpikesDamageTaken;
    #endregion

    #region Phase2
    public float TimeLeftWhenDoorOpens;
    public float TimeLeftWhenInChestRange;
    public bool OpendMinigame;
    public bool FinishedMinigame;
    public float StartTimeMinigame;
    public float EndTimeMinigame;
    public float Phase2EnterTime;
    public float Phase2ExitTime;
    public WalkData.Data[] WalkData;
    #endregion

    #region Phase3
    public int CurrentBoss;
    public bool EnteredBossRoom;
    public bool KilledTheBoss;
    public float BossDamageTaken;
    public float BossDamageDone;
    public float BossHealthLeft;
    public float TimeLeft;
    #endregion
}

[Serializable]
public enum LevelType
{
    Tutorial,
    Training,
    Test
}

[Serializable]
public struct PhaseTimeData
{
    public Phases Phase;
    public float ExitPhaseTime;
}

[Serializable]
public enum ChestSide
{
    Left,
    Right
}
