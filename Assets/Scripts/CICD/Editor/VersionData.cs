using UnityEditor;
using UnityEngine;

namespace CICD.Editor
{
    [CreateAssetMenu(fileName = "VersionData", menuName = "CICD/Version Data", order = 0)]
    public class VersionData : ScriptableObject
    {
        [SerializeField] private int major = 0;
        [SerializeField] private int minor = 0;
        [SerializeField] private int patch = 1;

        public string Semantic => $"{major}.{minor}.{patch}";

        public void IncrementPatch()
        {
            patch++;
            Save();
        }

        public void IncrementMinor()
        {
            minor++;
            patch = 0;
            Save();
        }

        public void IncrementMajor()
        {
            major++;
            minor = 0;
            patch = 0;
            Save();
        }

        private void Save()
        {
#if UNITY_EDITOR

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }

        public override string ToString() => Semantic;
    }
}