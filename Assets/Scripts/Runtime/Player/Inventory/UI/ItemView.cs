using Runtime.Player.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Inventory.UI
{
    public class ItemView : MonoBehaviour
    {
        [SerializeField] protected Image _image;
        [SerializeField] protected TMP_Text _countText;

        [Sirenix.OdinInspector.Button]
        public virtual void SetItem(Item item, int amount )
        {
            _image.sprite = item.Sprite;
            _countText.text = amount.ToString();
        }
    }
}