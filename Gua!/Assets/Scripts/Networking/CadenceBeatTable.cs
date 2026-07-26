using System.Collections.Generic;
using UnityEngine;

namespace FrogCamp.Networking
{
    [System.Serializable]
    internal sealed class CadenceTimelineData
    {
        public int version;
        public string audioFile;
        public float audioDuration;
        public float firstBeatTime;
        public float beatInterval;
        public int beatCount;
        public int[] pattern;
        public int sourceAnchorCount;
        public string sourceDescription;
        public float fitMaxError;
        public float fitRmsError;
    }

    public struct CadenceBeatPoint
    {
        public float time;
        public int beat;
    }

    public static class CadenceBeatTable
    {
        private const string ResourcePath = "Cadence/RunCadenceTimeline";
        private static List<CadenceBeatPoint> points;

        public static IReadOnlyList<CadenceBeatPoint> Points
        {
            get
            {
                if (points == null) points = LoadPoints();
                return points;
            }
        }

        private static List<CadenceBeatPoint> LoadPoints()
        {
            List<CadenceBeatPoint> result = new List<CadenceBeatPoint>();
            TextAsset json = Resources.Load<TextAsset>(ResourcePath);
            if (json == null)
            {
                Debug.LogError("未找到项目跑操时间轴 Resources/" + ResourcePath + ".json");
                return result;
            }

            CadenceTimelineData data = JsonUtility.FromJson<CadenceTimelineData>(json.text);
            if (data == null || data.version < 1 || data.firstBeatTime < 0f ||
                data.beatInterval <= 0f || data.beatCount <= 0 ||
                data.pattern == null || data.pattern.Length == 0)
            {
                Debug.LogError("项目跑操时间轴数据无效：" + ResourcePath + ".json");
                return result;
            }

            for (int index = 0; index < data.beatCount; index++)
            {
                int beat = data.pattern[index % data.pattern.Length];
                if (beat < 1 || beat > 4)
                    Debug.LogWarning("跑操时间轴包含范围外拍号：" + beat);

                result.Add(new CadenceBeatPoint
                {
                    time = data.firstBeatTime + data.beatInterval * index,
                    beat = Mathf.Clamp(beat, 1, 4)
                });
            }

            return result;
        }
    }
}
