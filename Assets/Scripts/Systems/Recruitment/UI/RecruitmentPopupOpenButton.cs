using UI;
using UnityEngine;

namespace Recruitment
{
    public class RecruitmentPopupOpenButton : MonoBehaviour
    {
        [SerializeField] private UIManager _uiManager;

        public void OpenRecruitmentPopup()
        {
            if (!_uiManager)
                return;

            _uiManager.OpenPopup(UIPopupType.Recruitment);
        }
    }
}
