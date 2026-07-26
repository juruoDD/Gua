using System.Collections.Generic;
using System.Linq;
using FrogCamp.Gameplay;
using FrogCamp.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    public sealed class GameSceneController : MonoBehaviour
    {
        [SerializeField] private RectTransform actorLayer;
        [SerializeField] private Text roomText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text announcementText;
        [SerializeField] private Button exitButton;
        [SerializeField] private FrogAnimationSet greenAnimations = new FrogAnimationSet();
        [SerializeField] private FrogAnimationSet pinkAnimations = new FrogAnimationSet();
        [SerializeField] private AudioClip cadenceMusic;
        [SerializeField] private AudioClip frogSound;
        [SerializeField] private AudioClip tongueCastSound;
        [SerializeField] private AudioClip tongueCorrectSound;
        [SerializeField] private AudioClip tongueWrongSound;
        [SerializeField] private AudioClip whistleSound;
        [SerializeField] private AudioSource cadenceMusicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private Button settingsButton;
        [SerializeField] private RectTransform settingsPanel;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Text masterVolumeValue;
        [SerializeField] private Text musicVolumeValue;
        [SerializeField] private Text sfxVolumeValue;
        [SerializeField] private MusicWaveformGraphic musicWaveform;
        [SerializeField] private RhythmCommandTrack rhythmCommandTrack;

        private readonly Dictionary<string, FrogActorView> actorViews =
            new Dictionary<string, FrogActorView>();
        private readonly Dictionary<string, int> actorSoundEventIds =
            new Dictionary<string, int>();
        private float nextInputTime;
        private int lastAnnouncementId;
        private bool cadenceMusicStarted;

        private const string MasterVolumeKey = "FrogCamp.MasterVolume";
        private const string MusicVolumeKey = "FrogCamp.MusicVolume";
        private const string SfxVolumeKey = "FrogCamp.SfxVolume";

        private void Awake()
        {
            if (actorLayer == null || roomText == null || statusText == null ||
                announcementText == null || exitButton == null ||
                settingsButton == null || settingsPanel == null ||
                closeSettingsButton == null || masterVolumeSlider == null ||
                musicVolumeSlider == null || sfxVolumeSlider == null ||
                cadenceMusicSource == null || sfxSource == null ||
                musicWaveform == null || rhythmCommandTrack == null)
            {
                Debug.LogError("游戏界面的 UI 引用不完整，请重新烘焙场景。");
                enabled = false;
                return;
            }
            exitButton.onClick.AddListener(ExitGame);
            settingsButton.onClick.AddListener(ToggleSettings);
            closeSettingsButton.onClick.AddListener(CloseSettings);
            masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            LoadVolumeSettings();
            settingsPanel.gameObject.SetActive(false);
            announcementText.gameObject.SetActive(false);
            if (cadenceMusicSource != null)
            {
                cadenceMusicSource.playOnAwake = false;
                cadenceMusicSource.loop = false;
                cadenceMusicSource.spatialBlend = 0f;
                if (cadenceMusicSource.clip == null)
                    cadenceMusicSource.clip = cadenceMusic;
            }
            musicWaveform.Configure(cadenceMusicSource);
        }

        private void Update()
        {
            HandleInput();
            RefreshActors();
        }

        public void BuildLayoutForEditor()
        {
#if UNITY_EDITOR
            greenAnimations.SetTextures(
                LoadFrogTexture("待机"), LoadFrogTexture("小跳"), LoadFrogTexture("大跳"),
                LoadFrogTexture("伸右手"), LoadFrogTexture("伸左手"),
                LoadFrogTexture("伸左腿"), LoadFrogTexture("伸右腿"),
                LoadFrogTexture("张嘴"), LoadFrogTexture("吐舌"), null,
                LoadFrogTexture("敬礼"));
            pinkAnimations.SetTextures(
                LoadFrogTexture("粉色待机"), LoadFrogTexture("粉色小跳"),
                LoadFrogTexture("粉色大跳"), null, null, null, null,
                LoadFrogTexture("粉色张嘴"), LoadFrogTexture("粉色吐舌"),
                LoadFrogTexture("粉色吹哨"), null);
            RefreshAudioAssetsForEditor();
            cadenceMusicSource = GetComponent<AudioSource>();
            if (cadenceMusicSource == null)
                cadenceMusicSource = gameObject.AddComponent<AudioSource>();
            cadenceMusicSource.clip = cadenceMusic;
            cadenceMusicSource.playOnAwake = false;
            cadenceMusicSource.loop = false;
            cadenceMusicSource.spatialBlend = 0f;
            cadenceMusicSource.volume = 0.3f;
            AudioSource[] sources = GetComponents<AudioSource>();
            sfxSource = sources.FirstOrDefault(source => source != cadenceMusicSource);
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
#endif
            CampUiFactory.EnsureEventSystem();
            Canvas canvas = CampUiFactory.CreateCanvas(transform);
            RectTransform map = CampUiFactory.Panel(canvas.transform, "CampMap", Vector2.zero,
                Vector2.one, Vector2.zero, Vector2.zero, CampUiFactory.Page);
            CampUiFactory.Panel(map, "HorizontalRoad", new Vector2(0f, 0.44f),
                new Vector2(1f, 0.56f), Vector2.zero, Vector2.zero, CampUiFactory.Paper);
            CampUiFactory.Panel(map, "VerticalRoad", new Vector2(0.46f, 0f),
                new Vector2(0.54f, 1f), Vector2.zero, Vector2.zero, CampUiFactory.Paper);
            AddBuilding(map, "营帐 A", new Vector2(0.08f, 0.68f), new Vector2(0.28f, 0.86f));
            AddBuilding(map, "营帐 B", new Vector2(0.72f, 0.68f), new Vector2(0.92f, 0.86f));
            AddBuilding(map, "仓库", new Vector2(0.08f, 0.14f), new Vector2(0.28f, 0.32f));
            AddBuilding(map, "食堂", new Vector2(0.72f, 0.14f), new Vector2(0.92f, 0.32f));
            AddFlag(map);

            actorLayer = CampUiFactory.Panel(map, "ActorLayer", Vector2.zero,
                Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            actorLayer.GetComponent<Image>().raycastTarget = false;

            RectTransform networkCard = CampUiFactory.Panel(map, "NetworkCard",
                new Vector2(0.02f, 0.89f), new Vector2(0.30f, 0.98f),
                Vector2.zero, Vector2.zero, new Color(CampUiFactory.Deep.r,
                    CampUiFactory.Deep.g, CampUiFactory.Deep.b, .9f), true);
            roomText = CampUiFactory.Text(networkCard, "Room", "房间 ----", 20,
                CampUiFactory.White, new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.94f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            statusText = CampUiFactory.Text(networkCard, "Status", "联机状态确认中",
                15, CampUiFactory.Mint, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.48f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            exitButton = CampUiFactory.Button(map, "ExitButton", "退出",
                new Vector2(0.90f, 0.92f), new Vector2(0.98f, 0.975f),
                Vector2.zero, Vector2.zero, null, false);
            announcementText = CampUiFactory.Text(map, "Announcement", "",
                27, CampUiFactory.White, new Vector2(0.31f, 0.89f),
                new Vector2(0.69f, 0.97f), Vector2.zero, Vector2.zero,
                TextAnchor.MiddleCenter, true);
            CampUiFactory.Text(map, "Controls",
                "WASD / 方向键 移动   空格 大跳   Q 呱叫   E 吐舌   R 敬礼/吹哨   U/I/J/K 伸展",
                15, CampUiFactory.Leaf, new Vector2(0.18f, 0.01f),
                new Vector2(0.82f, 0.055f), Vector2.zero, Vector2.zero,
                TextAnchor.MiddleCenter, true);
            BuildSettingsLayoutForEditor(map);
            BuildWaveformLayoutForEditor(map);
            BuildRhythmTrackLayoutForEditor(map);
        }

        private void HandleInput()
        {
            if (settingsPanel != null && settingsPanel.gameObject.activeSelf) return;
            LanRoomService service = LanRoomService.Instance;
            if (service.CurrentRoom == null || !service.CurrentRoom.inGame) return;
            if (Time.unscaledTime >= nextInputTime)
            {
                nextInputTime = Time.unscaledTime + 0.05f;
                float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                          (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
                float y = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f) -
                          (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f);
                service.SetGameInput(x, y);
            }
            if (Input.GetKeyDown(KeyCode.Space)) service.TriggerGameAction("jump");
            if (Input.GetKeyDown(KeyCode.U)) service.TriggerGameAction("armLeft");
            if (Input.GetKeyDown(KeyCode.I)) service.TriggerGameAction("armRight");
            if (Input.GetKeyDown(KeyCode.J)) service.TriggerGameAction("legLeft");
            if (Input.GetKeyDown(KeyCode.K)) service.TriggerGameAction("legRight");
            if (Input.GetKeyDown(KeyCode.Q)) service.TriggerGameAction("croak");
            if (Input.GetKeyDown(KeyCode.E)) service.TriggerGameAction("tongue");
            if (Input.GetKeyDown(KeyCode.R))
            {
                RoomPlayerData localPlayer = service.GetLocalPlayer();
                service.TriggerGameAction(localPlayer != null &&
                    localPlayer.role == "officer" ? "whistle" : "salute");
            }
        }

        private void RefreshActors()
        {
            LanRoomService service = LanRoomService.Instance;
            RoomStateData room = service.CurrentRoom;
            roomText.text = room == null ? "离线" : "房间 " + room.code;
            statusText.text = room == null ? "当前未连接房间" : service.Status;
            if (room == null || room.game == null) return;
            SyncCadenceMusic(room.game);
            rhythmCommandTrack.Apply(room.game);

            List<GameActorData> actors = room.game.npcs.Concat(room.game.players).ToList();
            HashSet<string> activeIds = new HashSet<string>();
            foreach (GameActorData actor in actors)
            {
                activeIds.Add(actor.id);
                FrogActorView view;
                if (!actorViews.TryGetValue(actor.id, out view))
                {
                    view = FrogActorView.Create(actorLayer, actor, greenAnimations,
                        pinkAnimations);
                    actorViews.Add(actor.id, view);
                }
                view.Apply(actor);
                SyncActorSound(actor);
            }
            foreach (string id in actorViews.Keys.Where(id => !activeIds.Contains(id)).ToArray())
            {
                Destroy(actorViews[id].gameObject);
                actorViews.Remove(id);
                actorSoundEventIds.Remove(id);
            }
            foreach (FrogActorView view in actorViews.Values.OrderBy(view => view.SortY))
                view.transform.SetAsLastSibling();

            if (room.game.announcementId != lastAnnouncementId)
            {
                lastAnnouncementId = room.game.announcementId;
                StopAllCoroutines();
                StartCoroutine(ShowAnnouncement(room.game.announcement));
            }
        }

        private void SyncActorSound(GameActorData actor)
        {
            int previousId;
            if (!actorSoundEventIds.TryGetValue(actor.id, out previousId))
            {
                actorSoundEventIds[actor.id] = actor.soundEventId;
                if (actor.soundEventId <= 0) return;
                bool eventStillActive = actor.soundEvent == "frog"
                    ? actor.action == "croak"
                    : actor.action == "tongue";
                if (!eventStillActive) return;
            }
            else
            {
                if (actor.soundEventId == previousId) return;
                actorSoundEventIds[actor.id] = actor.soundEventId;
            }

            AudioClip clip = null;
            if (actor.soundEvent == "frog") clip = frogSound;
            else if (actor.soundEvent == "tongueCast") clip = tongueCastSound;
            else if (actor.soundEvent == "tongueCorrect") clip = tongueCorrectSound;
            else if (actor.soundEvent == "tongueWrong") clip = tongueWrongSound;
            else if (actor.soundEvent == "whistle") clip = whistleSound;
            if (clip != null && sfxSource != null)
                sfxSource.PlayOneShot(clip);
        }

        public void RefreshAudioAssetsForEditor()
        {
#if UNITY_EDITOR
            cadenceMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Sound/跑操音乐.mp3");
            frogSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Sound/frog.wav");
            tongueCastSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Sound/tongue_cast.mp3");
            tongueCorrectSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Sound/tongue_correct.mp3");
            tongueWrongSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Sound/tongue_wrong.mp3");
            whistleSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Sound/whistle.mp3");
#endif
        }

        public void BuildSettingsLayoutForEditor(RectTransform map)
        {
#if UNITY_EDITOR
            if (cadenceMusicSource == null)
                cadenceMusicSource = GetComponent<AudioSource>();
            if (cadenceMusicSource != null)
                cadenceMusicSource.volume = 0.3f;
            AudioSource[] sources = GetComponents<AudioSource>();
            sfxSource = sources.FirstOrDefault(source => source != cadenceMusicSource);
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 1f;

            Transform oldButton = map.Find("SettingsButton");
            Transform oldPanel = map.Find("SettingsPanel");
            if (oldButton != null) DestroyImmediate(oldButton.gameObject);
            if (oldPanel != null) DestroyImmediate(oldPanel.gameObject);

            settingsButton = CampUiFactory.Button(map, "SettingsButton", "设置",
                new Vector2(0.82f, 0.92f), new Vector2(0.895f, 0.975f),
                Vector2.zero, Vector2.zero, null, false);
            settingsPanel = CampUiFactory.Panel(map, "SettingsPanel",
                new Vector2(0.69f, 0.55f), new Vector2(0.96f, 0.90f),
                Vector2.zero, Vector2.zero, CampUiFactory.White, true);
            CampUiFactory.Text(settingsPanel, "Title", "声音设置", 28,
                CampUiFactory.Deep, new Vector2(0.07f, 0.82f), new Vector2(0.72f, 0.96f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            closeSettingsButton = CampUiFactory.Button(settingsPanel, "CloseButton", "×",
                new Vector2(0.82f, 0.82f), new Vector2(0.94f, 0.95f),
                Vector2.zero, Vector2.zero, null, false);

            masterVolumeSlider = CreateVolumeSlider(settingsPanel, "MasterVolume",
                "总音量", 0.60f, out masterVolumeValue);
            musicVolumeSlider = CreateVolumeSlider(settingsPanel, "MusicVolume",
                "背景音乐", 0.38f, out musicVolumeValue);
            sfxVolumeSlider = CreateVolumeSlider(settingsPanel, "SfxVolume",
                "音效", 0.16f, out sfxVolumeValue);
            settingsPanel.gameObject.SetActive(false);
#endif
        }

        public void BuildWaveformLayoutForEditor(RectTransform map)
        {
#if UNITY_EDITOR
            Transform existing = map.Find("MusicWaveformPanel");
            RectTransform panel;
            if (existing == null)
            {
                GameObject panelObject = new GameObject("MusicWaveformPanel",
                    typeof(RectTransform));
                panelObject.transform.SetParent(map, false);
                panel = panelObject.GetComponent<RectTransform>();
            }
            else
                panel = (RectTransform)existing;
            CampUiFactory.SetRect(panel, new Vector2(0.04f, 0f),
                new Vector2(0.96f, 0.15f),
                Vector2.zero, Vector2.zero);

            Transform label = panel.Find("LiveLabel");
            if (label != null) DestroyImmediate(label.gameObject);
            foreach (Outline outline in panel.GetComponents<Outline>())
                DestroyImmediate(outline);
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null) DestroyImmediate(panelImage);

            musicWaveform =
                panel.GetComponentInChildren<MusicWaveformGraphic>(true);
            if (musicWaveform == null)
            {
                GameObject waveformObject = new GameObject("RealtimeWaveform",
                    typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(MusicWaveformGraphic));
                waveformObject.transform.SetParent(panel, false);
                musicWaveform =
                    waveformObject.GetComponent<MusicWaveformGraphic>();
            }
            RectTransform waveformRect =
                musicWaveform.GetComponent<RectTransform>();
            CampUiFactory.SetRect(waveformRect, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            musicWaveform.Configure(cadenceMusicSource);
            musicWaveform.ApplyReferenceStyle();

            Transform controls = map.Find("Controls");
            if (controls != null)
            {
                CampUiFactory.SetRect((RectTransform)controls,
                    new Vector2(0.18f, 0.158f), new Vector2(0.82f, 0.195f),
                    Vector2.zero, Vector2.zero);
                panel.SetSiblingIndex(controls.GetSiblingIndex());
            }
#endif
        }

        public void BuildRhythmTrackLayoutForEditor(RectTransform map)
        {
#if UNITY_EDITOR
            Transform existing = map.Find("RhythmCommandTrack");
            if (existing != null)
            {
                rhythmCommandTrack = existing.GetComponent<RhythmCommandTrack>();
                if (rhythmCommandTrack != null)
                    rhythmCommandTrack.ApplyEditorPreview();
                return;
            }

            GameObject trackObject = new GameObject("RhythmCommandTrack",
                typeof(RectTransform), typeof(RhythmCommandTrack));
            trackObject.transform.SetParent(map, false);
            RectTransform trackRect = trackObject.GetComponent<RectTransform>();
            CampUiFactory.SetRect(trackRect, new Vector2(0.31f, 0.835f),
                new Vector2(0.80f, 0.985f), Vector2.zero, Vector2.zero);

            RectTransform lane = CampUiFactory.Panel(trackRect, "LaneLine",
                new Vector2(0.085f, 0.48f), new Vector2(0.98f, 0.52f),
                Vector2.zero, Vector2.zero,
                new Color(CampUiFactory.Line.r, CampUiFactory.Line.g,
                    CampUiFactory.Line.b, 0.42f));
            lane.GetComponent<Image>().raycastTarget = false;
            RectTransform marker = CampUiFactory.Panel(trackRect, "BeatTarget",
                new Vector2(0.035f, 0.18f), new Vector2(0.135f, 0.82f),
                Vector2.zero, Vector2.zero,
                new Color(CampUiFactory.Accent.r, CampUiFactory.Accent.g,
                    CampUiFactory.Accent.b, 0.92f), true);
            Image markerImage = marker.GetComponent<Image>();
            markerImage.raycastTarget = false;
            Text markerLabel = CampUiFactory.Text(marker, "Label", "拍", 25,
                CampUiFactory.White, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
            markerLabel.raycastTarget = false;

            const int slotCount = 8;
            RectTransform[] roots = new RectTransform[slotCount];
            Text[] labels = new Text[slotCount];
            Image[] images = new Image[slotCount];
            for (int index = 0; index < slotCount; index++)
            {
                RectTransform root = CampUiFactory.Panel(trackRect,
                    "CommandSlot" + (index + 1),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-43f, -28f), new Vector2(43f, 28f),
                    CampUiFactory.Leaf, true);
                roots[index] = root;
                images[index] = root.GetComponent<Image>();
                images[index].raycastTarget = false;
                labels[index] = CampUiFactory.Text(root, "Label", "", 22,
                    CampUiFactory.White, Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
                labels[index].raycastTarget = false;
            }

            rhythmCommandTrack = trackObject.GetComponent<RhythmCommandTrack>();
            rhythmCommandTrack.Configure(marker, markerImage,
                roots, labels, images);
            rhythmCommandTrack.ApplyEditorPreview();
            trackRect.SetAsLastSibling();
            if (settingsPanel != null) settingsPanel.SetAsLastSibling();
#endif
        }

#if UNITY_EDITOR
        private static Slider CreateVolumeSlider(RectTransform parent, string name,
            string label, float anchorY, out Text valueText)
        {
            RectTransform row = CampUiFactory.Panel(parent, name,
                new Vector2(0.07f, anchorY), new Vector2(0.93f, anchorY + 0.16f),
                Vector2.zero, Vector2.zero, Color.clear);
            CampUiFactory.Text(row, "Label", label, 20, CampUiFactory.Deep,
                new Vector2(0f, 0f), new Vector2(0.26f, 1f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true);
            RectTransform track = CampUiFactory.Panel(row, "Track",
                new Vector2(0.27f, 0.34f), new Vector2(0.82f, 0.66f),
                Vector2.zero, Vector2.zero, CampUiFactory.Mint);
            Slider slider = track.gameObject.AddComponent<Slider>();
            RectTransform fill = CampUiFactory.Panel(track, "Fill",
                Vector2.zero, Vector2.one, new Vector2(3f, 3f),
                new Vector2(-3f, -3f), CampUiFactory.Accent);
            RectTransform handle = CampUiFactory.Panel(track, "Handle",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(-11f, -11f), new Vector2(11f, 11f),
                CampUiFactory.Deep);
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            valueText = CampUiFactory.Text(row, "Value", "100%", 19,
                CampUiFactory.Leaf, new Vector2(0.84f, 0f), Vector2.one,
                Vector2.zero, Vector2.zero, TextAnchor.MiddleRight, true);
            return slider;
        }
#endif

        private void ToggleSettings()
        {
            settingsPanel.gameObject.SetActive(!settingsPanel.gameObject.activeSelf);
        }

        private void CloseSettings()
        {
            settingsPanel.gameObject.SetActive(false);
        }

        private void LoadVolumeSettings()
        {
            masterVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            musicVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MusicVolumeKey, 0.3f));
            sfxVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
            ApplyVolumeSettings(false);
        }

        private void OnVolumeChanged(float unused)
        {
            ApplyVolumeSettings(true);
        }

        private void ApplyVolumeSettings(bool save)
        {
            AudioListener.volume = masterVolumeSlider.value;
            if (cadenceMusicSource != null)
                cadenceMusicSource.volume = musicVolumeSlider.value;
            if (sfxSource != null)
                sfxSource.volume = sfxVolumeSlider.value;
            if (masterVolumeValue != null)
                masterVolumeValue.text = Mathf.RoundToInt(masterVolumeSlider.value * 100f) + "%";
            if (musicVolumeValue != null)
                musicVolumeValue.text = Mathf.RoundToInt(musicVolumeSlider.value * 100f) + "%";
            if (sfxVolumeValue != null)
                sfxVolumeValue.text = Mathf.RoundToInt(sfxVolumeSlider.value * 100f) + "%";
            if (!save) return;
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolumeSlider.value);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolumeSlider.value);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolumeSlider.value);
            PlayerPrefs.Save();
        }

        private System.Collections.IEnumerator ShowAnnouncement(string message)
        {
            announcementText.text = message;
            announcementText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(2.8f);
            announcementText.gameObject.SetActive(false);
        }

        private void SyncCadenceMusic(GameStateData game)
        {
            if (musicWaveform != null)
                musicWaveform.SetMusicTime(game.musicTime);
            if (cadenceMusicSource == null || cadenceMusic == null) return;
            if (cadenceMusicSource.clip != cadenceMusic)
                cadenceMusicSource.clip = cadenceMusic;

            float desiredTime = Mathf.Clamp(game.musicTime, 0f, cadenceMusic.length);
            if (desiredTime >= cadenceMusic.length - 0.05f)
            {
                if (cadenceMusicSource.isPlaying) cadenceMusicSource.Stop();
                return;
            }

            if (!cadenceMusicStarted || !cadenceMusicSource.isPlaying)
            {
                cadenceMusicSource.time = desiredTime;
                cadenceMusicSource.Play();
                cadenceMusicStarted = true;
                return;
            }

            if (Mathf.Abs(cadenceMusicSource.time - desiredTime) > 0.12f)
                cadenceMusicSource.time = desiredTime;
        }

        private static void AddBuilding(RectTransform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform building = CampUiFactory.Panel(parent, label, anchorMin, anchorMax,
                Vector2.zero, Vector2.zero, CampUiFactory.Leaf, true);
            CampUiFactory.Panel(building, "Roof", new Vector2(0.08f, 0.78f),
                new Vector2(0.92f, 1f), Vector2.zero, Vector2.zero, CampUiFactory.Deep);
            CampUiFactory.Text(building, "Label", label, 24, CampUiFactory.White,
                new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.78f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
        }

#if UNITY_EDITOR
        private static Texture2D LoadFrogTexture(string fileName)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Frog/" + fileName + ".png");
        }
#endif

        private static void AddFlag(RectTransform map)
        {
            RectTransform pole = CampUiFactory.Panel(map, "FlagPole",
                new Vector2(.497f, .53f), new Vector2(.502f, .70f),
                Vector2.zero, Vector2.zero, CampUiFactory.Deep);
            CampUiFactory.Panel(pole, "Flag", new Vector2(1f, .64f),
                new Vector2(7f, 1f), Vector2.zero, Vector2.zero,
                CampUiFactory.Hex("#F18A78"));
        }

        private void ExitGame()
        {
            if (cadenceMusicSource != null) cadenceMusicSource.Stop();
            if (sfxSource != null) sfxSource.Stop();
            LanRoomService.Instance.LeaveRoom();
            SceneManager.LoadScene(CampScenes.Start);
        }
    }
}
