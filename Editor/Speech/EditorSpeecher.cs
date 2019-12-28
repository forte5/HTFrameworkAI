﻿using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace HT.Framework.AI
{
    public sealed class EditorSpeecher : HTFEditorWindow
    {
        [MenuItem("HTFramework.AI/Speech/Editor Speecher")]
        private static void OpenEditorSpeecher()
        {
            EditorSpeecher window = GetWindow<EditorSpeecher>();
            window.titleContent.text = "Speecher";
            window.minSize = new Vector2(400, 400);
            window.maxSize = new Vector2(400, 400);
            window.Show();
        }

        private string APIKEY = "";
        private string SECRETKEY = "";
        private string TOKEN = "";
        private string _synthesisText = "";
        private Vector2 _synthesisTextScroll = Vector2.zero;
        private string _savePath = "";
        private string _saveFullPath = "";
        private string _saveName = "NewAudio";
        private SynthesisType _format = SynthesisType.MP3;
        private int _timeout = 60000;
        private Speaker _speaker = Speaker.Woman;
        private int _volume = 15;
        private int _speed = 5;
        private int _pitch = 5;
        private bool _isSynthesis = false;

        private void OnEnable()
        {
            APIKEY = EditorPrefs.GetString(EditorPrefsTableAI.Speech_APIKEY, "");
            SECRETKEY = EditorPrefs.GetString(EditorPrefsTableAI.Speech_SECRETKEY, "");
            TOKEN = EditorPrefs.GetString(EditorPrefsTableAI.Speech_TOKEN, "");
        }
        protected override void OnTitleGUI()
        {
            base.OnTitleGUI();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("About", EditorStyles.toolbarButton))
            {
                Application.OpenURL(@"http://ai.baidu.com/");
            }
            if (GUILayout.Button("Console Login", EditorStyles.toolbarButton))
            {
                Application.OpenURL(@"https://login.bce.baidu.com/");
            }
        }
        protected override void OnBodyGUI()
        {
            base.OnBodyGUI();

            SynthesisIdentityGUI();
            SynthesisTextGUI();
            SynthesisArgsGUI();
            SynthesisButtonGUI();
        }
        private void SynthesisIdentityGUI()
        {
            GUILayout.BeginHorizontal("DD HeaderStyle");
            GUILayout.FlexibleSpace();
            GUILayout.Label("Speech Synthesis in Editor");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


            GUILayout.BeginVertical(EditorGlobalTools.Styles.Box);

            GUILayout.BeginHorizontal();
            GUILayout.Label("API Key:", GUILayout.Width(80));
            string apikey = EditorGUILayout.PasswordField(APIKEY);
            if (apikey != APIKEY)
            {
                APIKEY = apikey;
                EditorPrefs.SetString(EditorPrefsTableAI.Speech_APIKEY, APIKEY);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Secret Key:", GUILayout.Width(80));
            string secretkey = EditorGUILayout.PasswordField(SECRETKEY);
            if (secretkey != SECRETKEY)
            {
                SECRETKEY = secretkey;
                EditorPrefs.SetString(EditorPrefsTableAI.Speech_SECRETKEY, SECRETKEY);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Token:", GUILayout.Width(80));
            EditorGUILayout.TextField(TOKEN);
            GUI.enabled = (APIKEY != "" && SECRETKEY != "");
            if (GUILayout.Button("Generate", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                string uri = string.Format("https://openapi.baidu.com/oauth/2.0/token?grant_type=client_credentials&client_id={0}&client_secret={1}", APIKEY, SECRETKEY);
                UnityWebRequest request = UnityWebRequest.Get(uri);
                UnityWebRequestAsyncOperation async = request.SendWebRequest();
                async.completed += GenerateTOKENDone;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        private void SynthesisTextGUI()
        {
            GUILayout.BeginVertical(EditorGlobalTools.Styles.Box);
            GUILayout.Label("Synthesis Text:");
            _synthesisTextScroll = GUILayout.BeginScrollView(_synthesisTextScroll);
            _synthesisText = EditorGUILayout.TextArea(_synthesisText);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        private void SynthesisArgsGUI()
        {
            GUILayout.BeginVertical(EditorGlobalTools.Styles.Box);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Save Path:", GUILayout.Width(80));
            EditorGUILayout.TextField(_savePath);
            if (GUILayout.Button("Browse", EditorStyles.miniButton))
            {
                string path = EditorUtility.OpenFolderPanel("Select Save Path", Application.dataPath, "");
                if (path.Length != 0)
                {
                    _savePath = path.Replace(Application.dataPath, "");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Save Name:", GUILayout.Width(80));
            _saveName = EditorGUILayout.TextField(_saveName, GUILayout.Width(120));
            GUILayout.Label("Format:");
            _format = (SynthesisType)EditorGUILayout.EnumPopup(_format);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Speaker:", GUILayout.Width(80));
            if (GUILayout.Button(_speaker.GetRemark(), EditorGlobalTools.Styles.MiniPopup, GUILayout.Width(120)))
            {
                GenericMenu gm = new GenericMenu();
                foreach (var speaker in typeof(Speaker).GetEnumValues())
                {
                    Speaker s = (Speaker)speaker;
                    gm.AddItem(new GUIContent(s.GetRemark()), _speaker == s, () =>
                    {
                        _speaker = s;
                    });
                }
                gm.ShowAsContext();
            }
            GUILayout.Label("Timeout:");
            _timeout = EditorGUILayout.IntField(_timeout);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Volume:", GUILayout.Width(80));
            _volume = EditorGUILayout.IntSlider(_volume, 0, 15);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed:", GUILayout.Width(80));
            _speed = EditorGUILayout.IntSlider(_speed, 0, 9);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pitch:", GUILayout.Width(80));
            _pitch = EditorGUILayout.IntSlider(_pitch, 0, 9);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        private void SynthesisButtonGUI()
        {
            GUI.enabled = (!_isSynthesis && TOKEN != "" && _synthesisText != "" && _saveName != "");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Synthesis", EditorGlobalTools.Styles.LargeButton))
            {
                _saveFullPath = string.Format("{0}{1}/{2}.{3}", Application.dataPath, _savePath, _saveName, _format);
                SynthesisInEditor(_synthesisText, _saveFullPath, _format, _timeout, _speaker, _volume, _speed, _pitch);
            }
            GUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        /// <summary>
        /// 合成语音，并保存语音文件（编辑器内）
        /// </summary>
        /// <param name="text">合成文本</param>
        /// <param name="savePath">语音文件保存路径</param>
        /// <param name="timeout">超时时长</param>
        /// <param name="audioType">音频文件格式</param>
        /// <param name="speaker">发音人</param>
        /// <param name="volume">音量</param>
        /// <param name="speed">音速</param>
        /// <param name="pitch">音调</param>
        private void SynthesisInEditor(string text, string savePath, SynthesisType audioType = SynthesisType.MP3, int timeout = 60000, Speaker speaker = Speaker.DuYaYa, int volume = 15, int speed = 5, int pitch = 5)
        {
            if (string.IsNullOrEmpty(text) || text == "" || Encoding.Default.GetByteCount(text) >= 1024)
            {
                GlobalTools.LogError("合成语音失败：文本为空或长度超出了1024字节的限制！");
                return;
            }
            if (File.Exists(savePath))
            {
                GlobalTools.LogError("合成语音失败：已存在音频文件 " + savePath);
                return;
            }

            string url = string.Format("http://tsn.baidu.com/text2audio?tex='{0}'&tok={1}&cuid={2}&ctp={3}&lan={4}&spd={5}&pit={6}&vol={7}&per={8}&aue={9}",
                text, TOKEN, SystemInfo.deviceUniqueIdentifier, 1, "zh", speed, pitch, volume, (int)speaker, (int)audioType);

            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, audioType == SynthesisType.MP3 ? AudioType.MPEG : AudioType.WAV);
            UnityWebRequestAsyncOperation async = request.SendWebRequest();
            async.completed += SynthesisDone;
            _isSynthesis = true;
        }

        private void GenerateTOKENDone(AsyncOperation asyncOperation)
        {
            UnityWebRequestAsyncOperation async = asyncOperation as UnityWebRequestAsyncOperation;
            if (async != null)
            {
                if (string.IsNullOrEmpty(async.webRequest.error))
                {
                    JsonData json = GlobalTools.StringToJson(async.webRequest.downloadHandler.text);
                    TOKEN = json["access_token"].ToString();
                    EditorPrefs.SetString(EditorPrefsTableAI.Speech_TOKEN, TOKEN);
                    Repaint();
                }
                else
                {
                    GlobalTools.LogError("获取Token失败：" + async.webRequest.responseCode + " " + async.webRequest.error);
                }
            }
            else
            {
                GlobalTools.LogError("获取Token失败：错误的请求操作！");
            }
        }

        private void SynthesisDone(AsyncOperation asyncOperation)
        {
            UnityWebRequestAsyncOperation async = asyncOperation as UnityWebRequestAsyncOperation;
            if (async != null)
            {
                if (string.IsNullOrEmpty(async.webRequest.error))
                {
                    File.WriteAllBytes(_saveFullPath, async.webRequest.downloadHandler.data);
                    AssetDatabase.Refresh();
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath(GlobalTools.StringConcat("Assets", _savePath, "/", _saveName, ".", _format.ToString()), typeof(AudioClip));
                }
                else
                {
                    GlobalTools.LogError("合成语音失败：" + async.webRequest.responseCode + " " + async.webRequest.error);
                }
            }
            else
            {
                GlobalTools.LogError("合成语音失败：错误的请求操作！");
            }
            _isSynthesis = false;
        }
    }
}