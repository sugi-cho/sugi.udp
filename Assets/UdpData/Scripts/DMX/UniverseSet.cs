using UnityEngine;

public class UniverseSet : MonoBehaviour
{
    public string label;
    public int universe;
    public DMXDevice[] devices;

    [ContextMenu("set universes")]
    private void SetupUniverse()
    {
        foreach (var d in devices)
            d.universe = universe;
    }

    [ContextMenu("set channels")]
    private void SetChannels()
    {
        SetupUniverse();
        var startChannel = 0;
        foreach (var d in devices)
        {
            d.startChannel = startChannel;
            d.gameObject.name = $"{d.modelName}-u{d.universe}-c{d.startChannel}-n{d.NumChannels}";
            startChannel += d.NumChannels;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(d);
#endif
        }
    }

    private void Awake()
    {
        SetupUniverse();
        foreach (var d in devices)
            DmxController.Instance.AddDevice(d);
    }
}
