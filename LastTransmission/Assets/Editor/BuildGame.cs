using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastTransmission.Editor
{
    public static class BuildGame
    {
        public static void Prepare()
        {
            PlayerSettings.companyName = "Snowline Studio";
            PlayerSettings.productName = "Last Transmission";
            PlayerSettings.bundleVersion = "0.12.0";
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.stripEngineCode = false;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            var terrain=AssetImporter.GetAtPath("Assets/Resources/Art/RescueTerrain-v1.png") as TextureImporter;
            if(terrain!=null&&(terrain.mipmapEnabled||terrain.textureCompression!=TextureImporterCompression.Uncompressed||terrain.npotScale!=TextureImporterNPOTScale.None))
            {
                terrain.mipmapEnabled=false; terrain.textureCompression=TextureImporterCompression.Uncompressed;
                terrain.maxTextureSize=2048; terrain.npotScale=TextureImporterNPOTScale.None;
                terrain.alphaSource=TextureImporterAlphaSource.None; terrain.SaveAndReimport();
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Control room camera").AddComponent<Camera>();
            camera.backgroundColor = new Color(.02f, .04f, .05f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.orthographic = true; camera.gameObject.AddComponent<AudioListener>();
            new GameObject("Last Transmission").AddComponent<Game>();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/ControlRoom.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/ControlRoom.unity", true) };
            AssetDatabase.SaveAssets();
            Debug.Log("LAST_TRANSMISSION_PREPARED");
        }
        public static void Windows()
        {
            Prepare();
            Verification.Run();
            var root = Directory.GetParent(Application.dataPath).Parent.FullName;
            var output = Path.Combine(root, "Builds", "LastTransmission-0.12.0-Windows", "LastTransmission.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/ControlRoom.unity" }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            Debug.Log("LAST_TRANSMISSION_BUILD " + report.summary.result + " " + report.summary.totalSize);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed");
            PackageNotices(Path.GetDirectoryName(output),root);
        }
        public static void PackageNotices(string output,string root)
        {
            var notices=Path.Combine(output,"ThirdPartyNotices"); Directory.CreateDirectory(notices);
            File.Copy("Assets/Resources/Fonts/SUIT-OFL.txt",Path.Combine(notices,"SUIT-OFL.txt"),true);
            File.Copy("Assets/Resources/Audio/Kenney-License.txt",Path.Combine(notices,"Kenney-CC0.txt"),true);
            File.Copy(Path.Combine(root,"Docs","ASSETS.md"),Path.Combine(notices,"ASSETS.md"),true);
            File.Copy(Path.Combine(root,"Docs","CREDITS.md"),Path.Combine(notices,"CREDITS.md"),true);
            File.Copy(Path.Combine(root,"Docs","MAP_ART.md"),Path.Combine(notices,"MAP_ART.md"),true);
            File.Copy(Path.Combine(root,"Docs","UI_REFERENCES.md"),Path.Combine(notices,"UI_REFERENCES.md"),true);
            if(File.Exists(Path.Combine(root,"Docs","ART_PROMPTS.md"))) File.Copy(Path.Combine(root,"Docs","ART_PROMPTS.md"),Path.Combine(notices,"ART_PROMPTS.md"),true);
            File.Copy(Path.Combine(root,"LICENSE"),Path.Combine(output,"LICENSE.txt"),true);
            File.WriteAllText(Path.Combine(output,"시작하기.txt"),
                "마지막 교신 / Last Transmission v0.12.0\r\n\r\nLastTransmission.exe를 실행하세요. 이 폴더의 나머지 파일과 Data 폴더가 필요합니다.\r\n\r\n마우스로 장치 클릭 / R 무전 / M 지도 / L 근무 기록 / B 상황판 / D 출입 제어 / H 교신 기록\r\nN 다음에 확인할 장치로 이동 / Space 즉시 표시 및 다음 문장\r\nEsc 장치 닫기, 관제실에서 일시정지 / 긴 본문은 마우스 휠로 스크롤\r\n장치 화면 아래의 메뉴로 다른 장치에 바로 이동할 수 있습니다.\r\n지도와 기록은 실행 중 마지막 문서와 스크롤 위치를 기억합니다.\r\n\r\n지도와 기록을 대조하고 상황판에 판단 근거 두 가지를 남기세요.\r\n마지막에는 두 색을 섞어 확인등을 직접 세 번 표시하고 보고를 비교하세요.\r\n진행은 자동 저장됩니다. 새 게임 전에 기존 진행 교체를 확인합니다.\r\n한 번 확인한 결말은 새 게임을 시작해도 엔딩 기록에 보관됩니다.\r\n게임 창에서 벗어나면 저장하고 일시정지합니다.\r\n저장: AppData/LocalLow/Snowline Studio/Last Transmission\r\n\r\n전체·배경 음악·효과음·환경음 볼륨, 본문 크기 100~130%, 텍스트 속도, 즉시 표시와 눈보라 움직임은 설정에서 조절하세요.\r\n외부 음향·글꼴의 조건은 ThirdPartyNotices 폴더를 확인하세요.\r\nSteam 기능과 성우 음성은 없습니다.\r\n",System.Text.Encoding.UTF8);
        }
    }
}
