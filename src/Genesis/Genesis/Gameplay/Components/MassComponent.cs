namespace Genesis.Gameplay.Components;

public struct MassComponent(int baseValue)
{
    public float mValue = baseValue / 100f;
}