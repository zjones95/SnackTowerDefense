using System.IO;
using System.Text;
using UnityEngine;

// Unity -batchmode -projectPath <project> -executeMethod TDTrainCheck.Verify -quit
// Builds the bedroom room the way the game does and checks the toy train:
// the loop closes, stays continuous, sits outside the play grid, and the
// engine plus two carts exist on the track. Writes
// %TEMP%/opencode/train_check.txt. Motion smoothness needs a human play-test.
public static class TDTrainCheck
{
    public static void Verify()
    {
        StringBuilder report = new StringBuilder();
        GameObject world = new GameObject("TrainCheckWorld");
        try
        {
            TDMap map = TDBoardBuilder.CreateMap(TDGameManager.Layout, TDGameManager.Route, 2f, Vector3.zero);
            TDRoom.Build(world.transform, map, BoardTheme.Bedroom);

            TrainSet train = world.GetComponentInChildren<TrainSet>();
            if (train == null) throw new System.Exception("no TrainSet built in bedroom room");

            float gx = map.Width * map.Cell * 0.5f, gz = map.Height * map.Cell * 0.5f;
            float p = train.LoopLength;
            report.AppendLine("loop=" + p);
            Vector3 first = train.SampleLoop(0f);
            Vector3 prev = first;
            float maxStep = 0f;
            for (int i = 1; i <= 200; i++)
            {
                Vector3 cur = train.SampleLoop(p * i / 200f);
                if (float.IsNaN(cur.x + cur.y + cur.z)) throw new System.Exception("NaN at i=" + i);
                // centreline must stay clear of the tile faces (train is 0.7 wide)
                float dx = Mathf.Max(0f, Mathf.Abs(cur.x) - gx);
                float dz = Mathf.Max(0f, Mathf.Abs(cur.z) - gz);
                if (Mathf.Sqrt(dx * dx + dz * dz) < 0.4f)
                    throw new System.Exception("loop too close to grid at " + cur);
                maxStep = Mathf.Max(maxStep, Vector3.Distance(cur, prev));
                prev = cur;
            }
            if (Vector3.Distance(prev, first) > 0.05f) throw new System.Exception("loop does not close");
            report.AppendLine("maxStep=" + maxStep);
            if (maxStep > 0.6f) throw new System.Exception("loop discontinuity: " + maxStep);

            int sleepers = 0, rails = 0, wheels = 0, cars = 0, engines = 0;
            foreach (Transform tr in world.GetComponentsInChildren<Transform>())
            {
                if (tr.name == "Sleeper") sleepers++;
                else if (tr.name == "Rail") rails++;
                else if (tr.name == "Wheel") wheels++;
                else if (tr.name == "Car") cars++;
                else if (tr.name == "Engine") engines++;
            }
            report.AppendLine("sleepers=" + sleepers + " rails=" + rails +
                " wheels=" + wheels + " engines=" + engines + " cars=" + cars);
            if (sleepers < 100 || rails < 50) throw new System.Exception("track underbuilt");
            if (engines != 1 || cars != 2) throw new System.Exception("train composition wrong");
            if (wheels != 14) throw new System.Exception("wheel count wrong: " + wheels);
        }
        finally
        {
            Object.DestroyImmediate(world);
        }

        string dir = Path.Combine(Path.GetTempPath(), "opencode");
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, "train_check.txt");
        File.WriteAllText(file, report.ToString());
        Debug.Log("TDTrainCheck OK\n" + report + "\nWrote " + file);
    }
}
