using System.Numerics;

namespace Mod.DynamicEncounters.Features.Scripts.Actions.Data;

public class ScriptActionAreaItem
{
    public string Type { get; set; } = "sphere";
    public float Radius { get; set; } = 200000;
    public float MinRadius { get; set; } = 100000;
    public float Height { get; set; } = 200000;
    public QuaternionItem Rotation { get; set; } = new();

    public class QuaternionItem
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; } = 1;

        public Quaternion ToQuaternion() => new() { X = X, Y = Y, Z = Z, W = W };
    }
}