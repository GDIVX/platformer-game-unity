using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Utilities.UI
{
    [ExecuteInEditMode]
    public class ProgressBar : MonoBehaviour
    {
        public int minimum;
        public int maximum;
        public int current;
        public Image mask;

        #if UNITY_EDITOR
        [MenuItem("GameObject/UI/ProgressBar")]
        public static void CreateProgressBar()
        {
            GameObject obj = Instantiate(Resources.Load<GameObject>("UI/ProgressBar"));
            obj.transform.SetParent(Selection.activeGameObject.transform,false);
        }
        #endif


        // Update is called once per frame
        void Update()
        {
            GetCurrentFill();
        }

        void GetCurrentFill()
        {
            float currentOffset = current - minimum;
            float maximumOffset = maximum - minimum;
            var fillAmount = currentOffset / maximumOffset;
            mask.fillAmount = fillAmount;
        }
    }
}