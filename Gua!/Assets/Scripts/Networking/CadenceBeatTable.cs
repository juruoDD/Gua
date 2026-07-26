using System.Collections.Generic;
using UnityEngine;

namespace FrogCamp.Networking
{
    [System.Serializable]
    internal sealed class CadenceTimelineBeatData
    {
        public float time;
        public int beat;
    }

    [System.Serializable]
    internal sealed class CadenceTimelineData
    {
        public int version;
        public string audioFile;
        public float audioDuration;
        public float repeatInterval;
        public int sourceAnchorCount;
        public string sourceDescription;
        public CadenceTimelineBeatData[] baseBeats;
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
            if (data == null || data.version < 1 || data.audioDuration <= 0f ||
                data.repeatInterval <= 0f || data.baseBeats == null ||
                data.baseBeats.Length == 0)
            {
                Debug.LogError("项目跑操时间轴数据无效：" + ResourcePath + ".json");
                return result;
            }

            int repeatCount = Mathf.CeilToInt(data.audioDuration / data.repeatInterval);
            for (int repeat = 0; repeat < repeatCount; repeat++)
            {
                float offset = repeat * data.repeatInterval;
                foreach (CadenceTimelineBeatData source in data.baseBeats)
                {
                    if (source == null || source.time < 0f ||
                        source.beat < 1 || source.beat > 4)
                    {
                        Debug.LogWarning("项目跑操时间轴包含无效基础拍点。");
                        continue;
                    }

                    float time = source.time + offset;
                    if (time > data.audioDuration) continue;
                    result.Add(new CadenceBeatPoint
                    {
                        time = time,
                        beat = source.beat
                    });
                }
            }

            result.Sort((left, right) => left.time.CompareTo(right.time));
            return result;
        }
    }
}
