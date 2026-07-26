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
        public float loopStart;
        public float loopEnd;
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
        private static float loopStartTime;
        private static float loopEndTime;
        private static int loopStartIndex;

        public static IReadOnlyList<CadenceBeatPoint> Points
        {
            get
            {
                if (points == null) points = LoadPoints();
                return points;
            }
        }

        public static float LoopStartTime
        {
            get
            {
                EnsureLoaded();
                return loopStartTime;
            }
        }

        public static float LoopEndTime
        {
            get
            {
                EnsureLoaded();
                return loopEndTime;
            }
        }

        public static int LoopStartIndex
        {
            get
            {
                EnsureLoaded();
                return loopStartIndex;
            }
        }

        private static void EnsureLoaded()
        {
            if (points == null) points = LoadPoints();
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
            float configuredLoopEnd = data == null ? 0f :
                (data.loopEnd > 0f ? data.loopEnd : data.repeatInterval);
            if (data == null || data.version < 1 || data.audioDuration <= 0f ||
                data.loopStart < 0f || configuredLoopEnd <= data.loopStart ||
                data.baseBeats == null ||
                data.baseBeats.Length == 0)
            {
                Debug.LogError("项目跑操时间轴数据无效：" + ResourcePath + ".json");
                return result;
            }

            loopStartTime = data.loopStart;
            loopEndTime = configuredLoopEnd;
            foreach (CadenceTimelineBeatData source in data.baseBeats)
            {
                if (source == null || source.time < 0f ||
                    source.time >= loopEndTime ||
                    source.beat < 1 || source.beat > 4)
                {
                    Debug.LogWarning("项目跑操时间轴包含无效基础拍点。");
                    continue;
                }

                result.Add(new CadenceBeatPoint
                {
                    time = source.time,
                    beat = source.beat
                });
            }

            result.Sort((left, right) => left.time.CompareTo(right.time));
            loopStartIndex = result.FindIndex(point => point.time >= loopStartTime);
            if (loopStartIndex < 0)
            {
                Debug.LogError("项目跑操循环起点之后没有拍点。");
                result.Clear();
                loopStartIndex = 0;
            }
            return result;
        }
    }
}
