using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Reflection;

namespace NoSLoofah.CustomEditorWindow
{
    public class ScriptableObjectCreator : EditorWindow
    {

        [MenuItem("Tools/SOCreator")]
        public static void OpenWindow()
        {
            ScriptableObjectCreator wd = GetWindow<ScriptableObjectCreator>();
            wd.titleContent = new GUIContent("SOCreator");
        }
        //语言
        private readonly string[] LANGUAGES = { "中文", "English" };
        private int langSelection = 0;

        //路径和文件名
        private string dirPath = "Assets";
        private const string DEFALUT_PATH = "Assets";
        private string fullDirPath;
        string fullFileName;
        string fileName;

        //程序集
        private const string ASSEMBLY_NAME = "Assembly-CSharp";
        private Assembly assembly;
        //父类选择
        int selectedIndex0 = 0;
        int lastSelectIndex0 = 0;
        bool changedSelection0 = false;
        //子类选择
        int selectedIndex1 = 0;
        int lastSelectIndex1 = 0;
        bool changedSelection1 = false;

        ScriptableObject editingObject;
        SerializedObject editingSo;
        SerializedProperty editingSp;
        //类
        Type fatherType;
        List<Type> classes;
        string[] classNames;
        List<Type> subClasses;
        string[] subClassNames;

        int nameIndex = 0;
        int nameIndexLength = 3;

        bool useClassNamePrefix;    //使用类名作为前缀
        bool keepInput;             //创建后保留输入
        bool useIndexName;          //使用序号命名

        private void OnEnable()
        {
            Initialize();
        }
        private void Initialize()
        {
            selectedIndex0 = 0;
            lastSelectIndex0 = 0;
            changedSelection0 = false;
            assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name.Equals(ASSEMBLY_NAME));
            classes = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ScriptableObject)) && t.IsAbstract).ToList();
            classes.Insert(0, typeof(ScriptableObject));
            fatherType = typeof(ScriptableObject);
            classNames = classes.Select(c => c.Name).ToArray();
            UpdateSubClass();
            NextFile();
        }
        /// <summary>
        /// 更新子类列表
        /// </summary>
        private void UpdateSubClass()
        {
            fatherType = classes[selectedIndex0];
            selectedIndex1 = 0;
            lastSelectIndex1 = 0;
            changedSelection1 = false;
            subClasses = assembly.GetTypes().Where(t => t.IsSubclassOf(fatherType) && !t.IsAbstract).ToList();
            subClassNames = subClasses.Select(c => c.Name).ToArray();
        }
        /// <summary>
        /// 创建下一个文件
        /// </summary>
        private void NextFile()
        {
            if (keepInput && editingObject != null && (editingObject.GetType() == (subClasses[selectedIndex1]))) editingObject = Instantiate(editingObject);
            else editingObject = CreateInstance(subClassNames[selectedIndex1]);
            if (useIndexName) nameIndex++;
            editingSo = new SerializedObject(editingObject);
        }
        /// <summary>
        /// 绘制分界线
        /// </summary>
        private void DrawDivider()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("---------------------------------------------------------------------------------------------------------------");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        private string I18nString(string c, string e)
        {
            if (langSelection == 0) return c;
            else return e;
        }

        private void OnGUI()
        {
            selectedIndex0 = EditorGUILayout.Popup(I18nString("抽象父类", "Abstract Super Class"), selectedIndex0, classNames);
            selectedIndex1 = EditorGUILayout.Popup(I18nString("类", "Class"), selectedIndex1, subClassNames);

            changedSelection0 = selectedIndex0 != lastSelectIndex0;
            lastSelectIndex0 = selectedIndex0;
            changedSelection1 = selectedIndex1 != lastSelectIndex1;
            lastSelectIndex1 = selectedIndex1;
            if (changedSelection0)
            {
                changedSelection1 = true;
                UpdateSubClass();
            }

            if (changedSelection1)
            {
                editingObject = CreateInstance(subClassNames[selectedIndex1]);
                editingSo = new SerializedObject(editingObject);
            }


            EditorGUILayout.Separator();
            DrawDivider();
            editingSp = editingSo.GetIterator();
            editingSp.NextVisible(true);
            editingSo.UpdateIfRequiredOrScript();
            while (editingSp.NextVisible(true))
            {
                EditorGUILayout.PropertyField(editingSp, true);
            }
            if (GUI.changed) editingSo.ApplyModifiedProperties();
            DrawDivider();
            EditorGUILayout.Separator();

            fileName = EditorGUILayout.TextField(I18nString("文件名", "File name"), fileName);
            useClassNamePrefix = EditorGUILayout.Toggle(new GUIContent(I18nString("使用类名作为名称前缀", "Class name as prefix")), useClassNamePrefix);

            useIndexName = EditorGUILayout.Toggle(new GUIContent(I18nString("使用序号名称后缀", "Index as suffix")), useIndexName);
            if (useIndexName)
            {
                nameIndex = EditorGUILayout.IntField(new GUIContent(I18nString("序号", "Index")), nameIndex);
                nameIndexLength = EditorGUILayout.IntSlider(new GUIContent(I18nString("后缀长度", "Suffix length")), nameIndexLength, 1, 6);
            }

            fullFileName = useClassNamePrefix ? subClassNames[selectedIndex1] + "_" + fileName : fileName;
            if (useIndexName) fullFileName += "_" + nameIndex.ToString().PadLeft(nameIndexLength, '0');
            fullFileName += ".asset";

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(I18nString("保存路径", "Save path")), GUILayout.Width(80f));
            dirPath = EditorGUILayout.TextField(dirPath, GUILayout.ExpandWidth(true));

            if (GUILayout.Button(new GUIContent(I18nString("浏览", "Browse")), GUILayout.Width(50f)))
            {
                fullDirPath = EditorUtility.OpenFolderPanel(I18nString("选择保存路径", "Chose save path"), dirPath, "");
                dirPath = fullDirPath.Replace('/', '\\').Replace(Directory.GetCurrentDirectory(), "").TrimStart('\\');
            }
            if (String.IsNullOrEmpty(dirPath)) dirPath = DEFALUT_PATH;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            if (editingObject != null)
            {
                EditorGUILayout.BeginHorizontal();
                keepInput = EditorGUILayout.Toggle(new GUIContent(I18nString("创建后保留输入", "Keep last inputs")), keepInput);
                if (GUILayout.Button(new GUIContent(I18nString("创建", "Create"))))
                {
                    if (String.IsNullOrEmpty(fileName)) return;
                    string p = Path.Combine(dirPath, fullFileName);
                    bool result = true;
                    if (File.Exists(p))//如果是要覆盖
                    {
                        result = EditorUtility.DisplayDialog(I18nString("覆盖操作", "Overwrite Operation"), I18nString(p + "已经存在，确定要覆盖吗", p + " already exists. Are you sure you want to overwrite it?")
                            , I18nString("是", "Yes"), I18nString("否", "No"));
                        if (result) File.Delete(p);
                    }
                    if (result)
                    {
                        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
                        AssetDatabase.CreateAsset(editingObject, p);
                        NextFile();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(I18nString("刷新", "Refresh"), GUILayout.Width(60f)))
            {
                Initialize();
                if (useIndexName) nameIndex--;
            }
            GUILayout.FlexibleSpace();
            langSelection = EditorGUILayout.Popup("Language", langSelection, LANGUAGES);
            EditorGUILayout.EndHorizontal();
        }
    }
}